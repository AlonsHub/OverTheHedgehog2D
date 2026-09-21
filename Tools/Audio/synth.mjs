#!/usr/bin/env node
// Procedural SFX for Over The Hedgehog. Renders every sound in SOUNDS to Assets/Audio/SFX/<name>.wav
// (44.1 kHz, mono, 16-bit). Everything is soft and cartoonish on purpose: sine/triangle voices,
// low-passed noise, no hard clipping. Tweak a recipe, re-run `node Tools/Audio/synth.mjs`, Unity reimports.
//
//   node Tools/Audio/synth.mjs            # all
//   node Tools/Audio/synth.mjs hen_pop    # one

import fs from "node:fs";
import path from "node:path";

const SR = 44100;
const OUT = path.resolve("Assets/Audio/SFX");

// ---------------------------------------------------------------- building blocks

const len = s => Math.round(s * SR);
const zeros = s => new Float32Array(len(s));
const clamp = (v, a, b) => Math.min(b, Math.max(a, v));

//value or function of time (seconds)
const at = (v, t) => (typeof v === "function" ? v(t) : v);

//oscillators. freq may be a number or f(t). phase-accurate so sweeps don't click
function osc(shape, freq, dur, { phase = 0 } = {}) {
  const out = zeros(dur);
  let ph = phase;
  for (let i = 0; i < out.length; i++) {
    const t = i / SR;
    ph += (at(freq, t) / SR);
    const p = ph - Math.floor(ph);
    switch (shape) {
      case "sine": out[i] = Math.sin(2 * Math.PI * p); break;
      case "tri": out[i] = 1 - 4 * Math.abs(p - 0.5); break;
      case "saw": out[i] = 2 * p - 1; break;
      case "square": out[i] = p < 0.5 ? 1 : -1; break;
      case "pulse": out[i] = p < 0.25 ? 1 : -1; break;
    }
  }
  return out;
}

//white noise with a deterministic seed so every render is identical
function noise(dur, seed = 1) {
  const out = zeros(dur);
  let s = seed >>> 0 || 1;
  for (let i = 0; i < out.length; i++) {
    s ^= s << 13; s ^= s >>> 17; s ^= s << 5;
    out[i] = ((s >>> 0) / 4294967296) * 2 - 1;
  }
  return out;
}

//breakpoint envelope: [[time, value], ...] linearly interpolated, held after the last point
function env(points, dur) {
  const out = zeros(dur ?? points[points.length - 1][0]);
  let k = 0;
  for (let i = 0; i < out.length; i++) {
    const t = i / SR;
    while (k < points.length - 2 && t >= points[k + 1][0]) k++;
    const [t0, v0] = points[k], [t1, v1] = points[Math.min(k + 1, points.length - 1)];
    out[i] = t1 > t0 ? v0 + (v1 - v0) * clamp((t - t0) / (t1 - t0), 0, 1) : v1;
  }
  return out;
}

//exponential decay envelope
function decay(dur, tau, attack = 0.002) {
  const out = zeros(dur);
  for (let i = 0; i < out.length; i++) {
    const t = i / SR;
    out[i] = Math.min(1, t / attack) * Math.exp(-t / tau);
  }
  return out;
}

const mul = (a, b) => a.map((v, i) => v * (b[i] ?? 0));
const gain = (a, g) => a.map(v => v * g);
function mix(...parts) {
  //each part: array or [array, offsetSeconds]
  const n = Math.max(...parts.map(p => Array.isArray(p) ? p[0].length + len(p[1]) : p.length));
  const out = new Float32Array(n);
  for (const p of parts) {
    const [arr, off] = Array.isArray(p) ? [p[0], len(p[1])] : [p, 0];
    for (let i = 0; i < arr.length; i++) out[i + off] += arr[i];
  }
  return out;
}

//one-pole low-pass, cutoff may be f(t)
function lowpass(a, cutoff) {
  const out = zeros(a.length / SR);
  let y = 0;
  for (let i = 0; i < a.length; i++) {
    const c = at(cutoff, i / SR);
    const k = 1 - Math.exp(-2 * Math.PI * c / SR);
    y += k * (a[i] - y);
    out[i] = y;
  }
  return out;
}
function highpass(a, cutoff) {
  const lp = lowpass(a, cutoff);
  return a.map((v, i) => v - lp[i]);
}

//RBJ band-pass biquad, resonant. centre may be f(t) (coefficients refreshed every 32 samples)
function bandpass(a, centre, q = 4) {
  const out = zeros(a.length / SR);
  let x1 = 0, x2 = 0, y1 = 0, y2 = 0, b0 = 0, b1 = 0, b2 = 0, a1 = 0, a2 = 0;
  for (let i = 0; i < a.length; i++) {
    if (i % 32 === 0) {
      const w = 2 * Math.PI * clamp(at(centre, i / SR), 20, SR * 0.45) / SR;
      const alpha = Math.sin(w) / (2 * q), a0 = 1 + alpha;
      b0 = alpha / a0; b1 = 0; b2 = -alpha / a0; a1 = -2 * Math.cos(w) / a0; a2 = (1 - alpha) / a0;
    }
    const y = b0 * a[i] + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
    x2 = x1; x1 = a[i]; y2 = y1; y1 = y;
    out[i] = y;
  }
  return out;
}

const soft = a => a.map(v => Math.tanh(v * 1.2) / Math.tanh(1.2));
function normalize(a, peak = 0.9) {
  let m = 0; for (const v of a) m = Math.max(m, Math.abs(v));
  return m > 0 ? gain(a, peak / m) : a;
}
//short fade at both ends so nothing clicks
function tidy(a) {
  const f = Math.min(len(0.004), a.length >> 2);
  for (let i = 0; i < f; i++) { a[i] *= i / f; a[a.length - 1 - i] *= i / f; }
  return a;
}

//a squawk: a nasal pulse voice with a pitch contour, formant band-passes and vibrato.
//pitch: breakpoints [[t, hz]...]; dur seconds
function squawk(pitch, dur, { vib = 30, vibDepth = 0.06, formants = [900, 2300], q = 3, attack = 0.01, release = 0.06 } = {}) {
  const contour = env(pitch, dur);
  const f = t => contour[Math.min(contour.length - 1, Math.round(t * SR))] * (1 + vibDepth * Math.sin(2 * Math.PI * vib * t));
  const voice = mix(gain(osc("pulse", f, dur), 0.6), gain(osc("saw", f, dur), 0.4));
  const shaped = mix(...formants.map((fc, i) => gain(bandpass(voice, fc, q), i === 0 ? 1 : 0.6)), gain(lowpass(voice, 1200), 0.35));
  const a = env([[0, 0], [attack, 1], [dur - release, 0.9], [dur, 0]], dur);
  return mul(shaped, a);
}

//a note for jingles: triangle + sine with a soft attack and a ringing release
function note(hz, dur, { release = 0.25 } = {}) {
  const total = dur + release;
  const v = mix(gain(osc("tri", hz, total), 0.5), gain(osc("sine", hz, total), 0.5), gain(osc("sine", hz * 2, total), 0.12));
  const a = env([[0, 0], [0.012, 1], [dur, 0.7], [total, 0]], total);
  return mul(lowpass(v, 3500), a);
}

// ---------------------------------------------------------------- the sounds

const SOUNDS = {
  //UI -------------------------------------------------------------------------
  ui_click: () => normalize(mix(
    mul(osc("sine", t => 880 - 300 * t / 0.06, 0.07), decay(0.07, 0.02)),
    gain(mul(lowpass(noise(0.02, 7), 2500), decay(0.02, 0.005)), 0.15)), 0.55),
  ui_back: () => normalize(mul(osc("sine", t => 620 - 220 * t / 0.07, 0.08), decay(0.08, 0.025)), 0.5),
  node_pop: () => normalize(mul(osc("sine", t => 420 + 320 * Math.min(1, t / 0.05), 0.09), decay(0.09, 0.03)), 0.5),
  score_tick: () => normalize(mul(osc("sine", 1500, 0.03), decay(0.03, 0.01)), 0.3),
  star: () => normalize(mix(
    mul(osc("sine", t => 1760 + 500 * t, 0.3), decay(0.3, 0.09)),
    gain(mul(osc("sine", t => 2640 + 700 * t, 0.3), decay(0.3, 0.06)), 0.4)), 0.5),

  //slingshot ------------------------------------------------------------------
  grab: () => normalize(mix(
    mul(lowpass(noise(0.05, 3), 1800), decay(0.05, 0.015)),
    gain(mul(osc("sine", t => 220 - 80 * t / 0.05, 0.06), decay(0.06, 0.02)), 0.7)), 0.45),
  //stretching the band: a slow creak, played while dragging
  stretch: () => normalize(mul(bandpass(noise(0.35, 11), t => 700 + 900 * t / 0.35, 6), env([[0, 0], [0.05, 1], [0.3, 0.8], [0.35, 0]])), 0.35),
  launch: () => {
    const pluck = mul(lowpass(osc("saw", t => 180 - 40 * t, 0.3), t => 3000 * Math.exp(-t * 12) + 250), decay(0.3, 0.07));
    const whoosh = mul(bandpass(noise(0.45, 5), t => 500 + 2200 * t / 0.45, 1.5), env([[0, 0], [0.12, 1], [0.45, 0]]));
    return normalize(mix(pluck, gain(whoosh, 0.8)), 0.8);
  },
  hog_walk: () => normalize(mul(lowpass(noise(0.04, 9), 600), decay(0.04, 0.012)), 0.25),

  //hogs -----------------------------------------------------------------------
  hog_impact: () => normalize(mix(
    mul(osc("sine", t => 150 * Math.exp(-t * 18) + 55, 0.18), decay(0.18, 0.05)),
    gain(mul(lowpass(noise(0.08, 13), 1500), decay(0.08, 0.02)), 0.5)), 0.85),
  mini_impact: () => normalize(mix(
    mul(osc("sine", t => 260 * Math.exp(-t * 20) + 90, 0.12), decay(0.12, 0.035)),
    gain(mul(lowpass(noise(0.05, 17), 2200), decay(0.05, 0.015)), 0.4)), 0.6),
  //the little "wee!" as a hog pops off the screen
  hog_pop: () => normalize(mul(osc("tri", t => 520 + 620 * Math.min(1, t / 0.12) + 20 * Math.sin(2 * Math.PI * 28 * t), 0.22), env([[0, 0], [0.02, 1], [0.15, 0.8], [0.22, 0]])), 0.55),
  mini_squeak: () => normalize(mul(osc("tri", t => 1300 + 700 * Math.min(1, t / 0.06), 0.1), env([[0, 0], [0.01, 1], [0.07, 0.7], [0.1, 0]])), 0.45),
  cluster_split: () => {
    const pop = mix(
      mul(osc("sine", t => 320 * Math.exp(-t * 40) + 90, 0.08), decay(0.08, 0.025)),
      gain(mul(lowpass(noise(0.05, 19), 3000), decay(0.05, 0.012)), 0.6));
    const squeaks = [];
    for (let i = 0; i < 5; i++) {
      const base = 1100 + i * 140;
      squeaks.push([gain(mul(osc("tri", t => base + 500 * Math.min(1, t / 0.05), 0.09), env([[0, 0], [0.008, 1], [0.06, 0.6], [0.09, 0]])), 0.5), 0.05 + i * 0.035]);
    }
    return normalize(mix(pop, ...squeaks), 0.7);
  },
  fuse: () => {
    //sizzle: high noise with random crackle
    const n = highpass(noise(0.95, 23), 2500);
    const crackle = noise(0.95, 29).map(v => (v > 0.985 ? 1 : 0.35));
    return normalize(mul(mul(n, crackle), env([[0, 0], [0.05, 0.8], [0.85, 1], [0.95, 0]])), 0.3);
  },
  explosion: () => {
    const sub = mul(osc("sine", t => 70 * Math.exp(-t * 4) + 28, 0.9), env([[0, 0], [0.02, 1], [0.5, 0.5], [0.9, 0]]));
    const body = mul(lowpass(noise(0.8, 31), t => 900 * Math.exp(-t * 5) + 120), env([[0, 0], [0.03, 1], [0.35, 0.45], [0.8, 0]]));
    const puff = mul(bandpass(noise(0.25, 37), 700, 1.2), decay(0.25, 0.07));
    return normalize(soft(mix(gain(sub, 0.9), gain(body, 0.8), gain(puff, 0.35))), 0.8);
  },
  dust_poof: () => normalize(mul(lowpass(noise(0.16, 41), t => 1300 * Math.exp(-t * 14) + 250), env([[0, 0], [0.012, 1], [0.16, 0]])), 0.45),
  wood_knock: () => normalize(mix(
    mul(bandpass(noise(0.12, 43), 420, 9), decay(0.12, 0.03)),
    gain(mul(bandpass(noise(0.08, 47), 1250, 7), decay(0.08, 0.015)), 0.5),
    gain(mul(lowpass(noise(0.02, 53), 4000), decay(0.02, 0.006)), 0.4)), 0.6),

  //hens -----------------------------------------------------------------------
  hen_cluck: () => normalize(mix(
    squawk([[0, 520], [0.03, 640], [0.07, 480]], 0.07, { vibDepth: 0.02, formants: [800, 2000] }),
    [squawk([[0, 500], [0.02, 600], [0.06, 440]], 0.06, { vibDepth: 0.02, formants: [800, 2000] }), 0.11]), 0.5),
  //"bwak!" when a hen takes a hit but isn't down
  hen_hit: () => normalize(mix(
    squawk([[0, 480], [0.04, 900], [0.11, 760], [0.18, 560]], 0.18, { vib: 38, vibDepth: 0.05 }),
    gain(mul(lowpass(noise(0.05, 59), 2000), decay(0.05, 0.012)), 0.4)), 0.85),
  //the big one: "bu-KAWK!" plus a pop and a feather flutter
  hen_pop: () => {
    const s1 = squawk([[0, 520], [0.03, 620], [0.09, 500]], 0.09, { vibDepth: 0.03 });
    const s2 = squawk([[0, 700], [0.05, 1150], [0.14, 1000], [0.24, 780], [0.34, 560]], 0.34, { vib: 34, vibDepth: 0.07, formants: [950, 2500, 3400] });
    const pop = mix(
      mul(osc("sine", t => 380 * Math.exp(-t * 35) + 110, 0.09), decay(0.09, 0.03)),
      gain(mul(lowpass(noise(0.06, 61), 3500), decay(0.06, 0.015)), 0.7));
    const flutter = mul(mul(lowpass(noise(0.55, 67), 3000), osc("sine", 13, 0.55).map(v => 0.5 + 0.5 * v)), env([[0, 0], [0.05, 0.8], [0.4, 0.5], [0.55, 0]]));
    return normalize(mix(gain(s1, 0.9), [s2, 0.13], [gain(pop, 0.8), 0.12], [gain(flutter, 0.35), 0.2]), 0.95);
  },
  boss_hit: () => normalize(mix(
    squawk([[0, 300], [0.05, 560], [0.14, 470], [0.24, 340]], 0.24, { vib: 26, vibDepth: 0.05, formants: [650, 1700] }),
    gain(mul(osc("sine", t => 120 * Math.exp(-t * 15) + 50, 0.2), decay(0.2, 0.06)), 0.7)), 0.9),
  boss_pop: () => {
    const s = squawk([[0, 380], [0.08, 720], [0.35, 640], [0.7, 420], [1.0, 260]], 1.0, { vib: 22, vibDepth: 0.09, formants: [650, 1800, 2600], release: 0.15 });
    const flutter = mul(mul(lowpass(noise(1.1, 71), 2600), osc("sine", 11, 1.1).map(v => 0.5 + 0.5 * v)), env([[0, 0], [0.1, 0.9], [0.8, 0.5], [1.1, 0]]));
    const thump = mul(osc("sine", t => 90 * Math.exp(-t * 10) + 40, 0.5), decay(0.5, 0.12));
    return normalize(soft(mix(s, [gain(flutter, 0.4), 0.25], [gain(thump, 0.8), 0.05])), 0.95);
  },

  //results --------------------------------------------------------------------
  win: () => normalize(mix(
    [note(523.25, 0.13), 0], [note(659.25, 0.13), 0.14], [note(783.99, 0.13), 0.28],
    [note(1046.5, 0.5, { release: 0.6 }), 0.42], [gain(note(659.25, 0.5, { release: 0.6 }), 0.5), 0.42], [gain(note(783.99, 0.5, { release: 0.6 }), 0.5), 0.42]), 0.7),
  lose: () => normalize(mix(
    [mul(lowpass(note(392, 0.32, { release: 0.2 }), t => 2500 - 1800 * Math.min(1, t / 0.4)), env([[0, 1], [0.5, 1]], 0.52)), 0],
    [mul(lowpass(note(329.63, 0.55, { release: 0.5 }), t => 1800 - 1300 * Math.min(1, t / 0.8)), env([[0, 1], [1.0, 1]], 1.05)), 0.36]), 0.6),
  boss_win: () => normalize(mix(
    [note(523.25, 0.12), 0], [note(659.25, 0.12), 0.13], [note(783.99, 0.12), 0.26], [note(1046.5, 0.12), 0.39],
    [note(1318.5, 0.6, { release: 0.8 }), 0.52], [gain(note(1046.5, 0.6, { release: 0.8 }), 0.5), 0.52], [gain(note(783.99, 0.6, { release: 0.8 }), 0.5), 0.52]), 0.75),
};

// ---------------------------------------------------------------- wav

function writeWav(file, samples) {
  const n = samples.length;
  const buf = Buffer.alloc(44 + n * 2);
  buf.write("RIFF", 0); buf.writeUInt32LE(36 + n * 2, 4); buf.write("WAVE", 8);
  buf.write("fmt ", 12); buf.writeUInt32LE(16, 16); buf.writeUInt16LE(1, 20); buf.writeUInt16LE(1, 22);
  buf.writeUInt32LE(SR, 24); buf.writeUInt32LE(SR * 2, 28); buf.writeUInt16LE(2, 32); buf.writeUInt16LE(16, 34);
  buf.write("data", 36); buf.writeUInt32LE(n * 2, 40);
  for (let i = 0; i < n; i++) buf.writeInt16LE(Math.round(clamp(samples[i], -1, 1) * 32767), 44 + i * 2);
  fs.writeFileSync(file, buf);
}

const only = process.argv.slice(2);
fs.mkdirSync(OUT, { recursive: true });
for (const [name, make] of Object.entries(SOUNDS)) {
  if (only.length && !only.includes(name)) continue;
  const samples = tidy(make());
  writeWav(path.join(OUT, name + ".wav"), samples);
  console.log(`${name}.wav  ${(samples.length / SR).toFixed(2)}s`);
}
