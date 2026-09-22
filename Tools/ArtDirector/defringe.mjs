#!/usr/bin/env node
// Strips the pale rim gpt-image bakes around UI props as fully OPAQUE pixels (a cream "glow" outside the
// brown outline). Flood-fills from the canvas border through transparent pixels and any light, low
// saturation pixel connected to the outside, clears them, then feathers the new edge by one pixel.
// Pale details inside the outline (a daisy on a button) are enclosed, so they survive untouched.
//
// Usage: node Tools/ArtDirector/defringe.mjs <file.png> [...] [--lum 0.72] [--sat 0.3] [--feather 1]

import fs from "node:fs";
import { decode, encode } from "./png.mjs";

const args = process.argv.slice(2);
let lumMin = 0.72, satMax = 0.3, feather = 1; const files = [];
for (let i = 0; i < args.length; i++) {
  if (args[i] === "--lum") lumMin = +args[++i];
  else if (args[i] === "--sat") satMax = +args[++i];
  else if (args[i] === "--feather") feather = +args[++i];
  else files.push(args[i]);
}

for (const f of files) {
  const { w, h, px } = decode(fs.readFileSync(f));
  const passable = new Uint8Array(w * h);
  for (let i = 0; i < w * h; i++) {
    const a = px[i * 4 + 3];
    if (a < 8) { passable[i] = 1; continue; }
    const r = px[i * 4] / 255, g = px[i * 4 + 1] / 255, b = px[i * 4 + 2] / 255;
    const max = Math.max(r, g, b), min = Math.min(r, g, b);
    const lum = 0.299 * r + 0.587 * g + 0.114 * b, sat = max === 0 ? 0 : (max - min) / max;
    if (lum >= lumMin && sat <= satMax) passable[i] = 1;
  }
  //flood from every border pixel
  const outside = new Uint8Array(w * h); const stack = [];
  for (let x = 0; x < w; x++) { stack.push(x, (h - 1) * w + x); }
  for (let y = 0; y < h; y++) { stack.push(y * w, y * w + w - 1); }
  while (stack.length) {
    const i = stack.pop();
    if (outside[i] || !passable[i]) continue;
    outside[i] = 1;
    const x = i % w, y = (i - x) / w;
    if (x > 0) stack.push(i - 1); if (x < w - 1) stack.push(i + 1);
    if (y > 0) stack.push(i - w); if (y < h - 1) stack.push(i + w);
  }
  let removed = 0;
  for (let i = 0; i < w * h; i++) if (outside[i] && px[i * 4 + 3] > 0) { px[i * 4 + 3] = 0; removed++; }
  //feather: pixels touching the cleared area get half alpha so the edge isn't jagged
  if (feather > 0) {
    const edge = [];
    for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
      const i = y * w + x; if (outside[i] || px[i * 4 + 3] === 0) continue;
      if ((x > 0 && outside[i - 1]) || (x < w - 1 && outside[i + 1]) || (y > 0 && outside[i - w]) || (y < h - 1 && outside[i + w])) edge.push(i);
    }
    for (const i of edge) px[i * 4 + 3] = Math.round(px[i * 4 + 3] * 0.55);
  }
  fs.writeFileSync(f, encode(w, h, px));
  console.log(`${f}: ${w}x${h}, cleared ${removed} rim pixels`);
}
