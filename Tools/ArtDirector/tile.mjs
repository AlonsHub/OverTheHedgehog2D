#!/usr/bin/env node
// Downscales a sprite (area-average, premultiplied alpha) and centres it on a square transparent canvas,
// for textures that get repeated along a line or a strip: the empty margin is the gap between repeats.
//
// Usage: node Tools/ArtDirector/tile.mjs <in.png> <out.png> [--size 256] [--fill 0.5]
//   --fill is the sprite's longest side as a fraction of the canvas

import fs from "node:fs";
import { decode, encode } from "./png.mjs";

const args = process.argv.slice(2);
let size = 256, fill = 0.5; const files = [];
for (let i = 0; i < args.length; i++) {
  if (args[i] === "--size") size = +args[++i];
  else if (args[i] === "--fill") fill = +args[++i];
  else files.push(args[i]);
}
const [inPath, outPath] = files;
if (!inPath || !outPath) { console.error("usage: tile.mjs <in.png> <out.png> [--size n] [--fill f]"); process.exit(1); }

const { w, h, px } = decode(fs.readFileSync(inPath));
const scale = (size * fill) / Math.max(w, h);
const dw = Math.max(1, Math.round(w * scale)), dh = Math.max(1, Math.round(h * scale));
const out = Buffer.alloc(size * size * 4);
const ox = Math.floor((size - dw) / 2), oy = Math.floor((size - dh) / 2);
for (let y = 0; y < dh; y++) {
  const sy0 = Math.floor(y / scale), sy1 = Math.min(h, Math.max(sy0 + 1, Math.floor((y + 1) / scale)));
  for (let x = 0; x < dw; x++) {
    const sx0 = Math.floor(x / scale), sx1 = Math.min(w, Math.max(sx0 + 1, Math.floor((x + 1) / scale)));
    let r = 0, g = 0, b = 0, a = 0, n = 0;
    for (let sy = sy0; sy < sy1; sy++) for (let sx = sx0; sx < sx1; sx++) {
      const i = (sy * w + sx) * 4, pa = px[i + 3] / 255;
      r += px[i] * pa; g += px[i + 1] * pa; b += px[i + 2] * pa; a += pa; n++;
    }
    const d = ((y + oy) * size + x + ox) * 4;
    if (a > 0) { out[d] = Math.round(r / a); out[d + 1] = Math.round(g / a); out[d + 2] = Math.round(b / a); out[d + 3] = Math.round(255 * a / n); }
  }
}
fs.writeFileSync(outPath, encode(size, size, out));
console.log(`${outPath}: ${size}x${size}, sprite ${dw}x${dh} at (${ox},${oy})`);
