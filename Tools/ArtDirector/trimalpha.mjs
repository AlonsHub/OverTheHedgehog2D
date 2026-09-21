#!/usr/bin/env node
// Crops a PNG to the bounding box of its non-transparent pixels so the sprite rect is exactly the painted
// object (gpt-image never fills the canvas edge to edge, and a transparent margin inside a sliced/fitted
// sprite is what makes blocks look thinner than their colliders). Rewrites the file in place.
//
// Usage: node Tools/ArtDirector/trimalpha.mjs <file.png> [...] [--threshold 8] [--pad 0]

import fs from "node:fs";
import { decode, encode } from "./png.mjs";

const args = process.argv.slice(2);
let threshold = 8, pad = 0; const files = [];
for (let i = 0; i < args.length; i++) {
  if (args[i] === "--threshold") threshold = +args[++i];
  else if (args[i] === "--pad") pad = +args[++i];
  else files.push(args[i]);
}
for (const f of files) {
  const { w, h, px } = decode(fs.readFileSync(f));
  let x0 = w, y0 = h, x1 = -1, y1 = -1;
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
    if (px[(y * w + x) * 4 + 3] > threshold) { if (x < x0) x0 = x; if (x > x1) x1 = x; if (y < y0) y0 = y; if (y > y1) y1 = y; }
  }
  if (x1 < 0) { console.log(`${f}: fully transparent, skipped`); continue; }
  x0 = Math.max(0, x0 - pad); y0 = Math.max(0, y0 - pad); x1 = Math.min(w - 1, x1 + pad); y1 = Math.min(h - 1, y1 + pad);
  const nw = x1 - x0 + 1, nh = y1 - y0 + 1, out = Buffer.alloc(nw * nh * 4);
  for (let y = 0; y < nh; y++) px.copy(out, y * nw * 4, ((y + y0) * w + x0) * 4, ((y + y0) * w + x0 + nw) * 4);
  fs.writeFileSync(f, encode(nw, nh, out));
  console.log(`${f}: ${w}x${h} -> ${nw}x${nh} (offset ${x0},${y0})`);
}
