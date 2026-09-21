#!/usr/bin/env node
// Makes a lighter (or darker) colour variant of a sprite while leaving its dark outline alone, so the
// variant stays exactly on-model (same silhouette, caps, nails). Mid and light tones are pushed toward
// white and slightly desaturated; pixels darker than --keep-dark (0..1 luminance) are untouched.
//
// Usage: node Tools/ArtDirector/variant.mjs <in.png> <out.png> [--lighten 0.35] [--desat 0.2] [--keep-dark 0.3]

import fs from "node:fs";
import { decode, encode } from "./png.mjs";

const args = process.argv.slice(2);
let lighten = 0.35, desat = 0.2, keepDark = 0.3; const files = [];
for (let i = 0; i < args.length; i++) {
  if (args[i] === "--lighten") lighten = +args[++i];
  else if (args[i] === "--desat") desat = +args[++i];
  else if (args[i] === "--keep-dark") keepDark = +args[++i];
  else files.push(args[i]);
}
const [inPath, outPath] = files;
if (!inPath || !outPath) { console.error("usage: variant.mjs <in.png> <out.png> [--lighten f] [--desat f] [--keep-dark f]"); process.exit(1); }

const { w, h, px } = decode(fs.readFileSync(inPath));
for (let i = 0; i < px.length; i += 4) {
  if (px[i + 3] === 0) continue;
  let r = px[i] / 255, g = px[i + 1] / 255, b = px[i + 2] / 255;
  const lum = 0.299 * r + 0.587 * g + 0.114 * b;
  //outline and deep shadow stay; fade the effect in above the threshold so the edge doesn't band
  const t = Math.min(1, Math.max(0, (lum - keepDark) / 0.15));
  if (t === 0) continue;
  const grey = lum;
  r = r + (grey - r) * desat * t; g = g + (grey - g) * desat * t; b = b + (grey - b) * desat * t;
  r += (1 - r) * lighten * t; g += (1 - g) * lighten * t; b += (1 - b) * lighten * t;
  px[i] = Math.round(r * 255); px[i + 1] = Math.round(g * 255); px[i + 2] = Math.round(b * 255);
}
fs.writeFileSync(outPath, encode(w, h, px));
console.log(`${outPath}: ${w}x${h}, lighten ${lighten}, desat ${desat}, keep-dark ${keepDark}`);
