#!/usr/bin/env node
// Splits the slingshot pouch painting into a back half and a front half along the rim, so the hog can
// be drawn between them and look like it's sitting inside. Both outputs keep the source canvas size so
// they share one pivot.
//
//   node Tools/ArtDirector/splitcup.mjs Assets/Art/Thrower/Cup.png
//
// The split curve is the front lip of the rim: an ellipse arc centred at (cx, cy) with radii (rx, ry).
// Pixels below the arc (and everything outside the ellipse's x-range, i.e. the rope loops) are "front".

import fs from "node:fs";
import path from "node:path";
import zlib from "node:zlib";

function decode(buf) {
  let p = 8, w, h, ct; const idat = [];
  while (p < buf.length) {
    const len = buf.readUInt32BE(p); const type = buf.toString("ascii", p + 4, p + 8);
    const data = buf.subarray(p + 8, p + 8 + len);
    if (type === "IHDR") { w = data.readUInt32BE(0); h = data.readUInt32BE(4); ct = data[9]; }
    else if (type === "IDAT") idat.push(data);
    else if (type === "IEND") break;
    p += 12 + len;
  }
  const bpp = ct === 6 ? 4 : 3;
  const raw = zlib.inflateSync(Buffer.concat(idat));
  const stride = w * bpp, out = Buffer.alloc(w * h * 4);
  let prev = Buffer.alloc(stride), q = 0;
  for (let y = 0; y < h; y++) {
    const f = raw[q++]; const line = Buffer.from(raw.subarray(q, q + stride)); q += stride;
    for (let i = 0; i < stride; i++) {
      const a = i >= bpp ? line[i - bpp] : 0, b = prev[i], c = i >= bpp ? prev[i - bpp] : 0;
      let v = line[i];
      if (f === 1) v += a; else if (f === 2) v += b; else if (f === 3) v += (a + b) >> 1;
      else if (f === 4) { const pp = a + b - c, pa = Math.abs(pp - a), pb = Math.abs(pp - b), pc = Math.abs(pp - c); v += (pa <= pb && pa <= pc) ? a : (pb <= pc ? b : c); }
      line[i] = v & 255;
    }
    for (let x = 0; x < w; x++) { const o = (y * w + x) * 4, s = x * bpp; out[o] = line[s]; out[o + 1] = line[s + 1]; out[o + 2] = line[s + 2]; out[o + 3] = bpp === 4 ? line[s + 3] : 255; }
    prev = line;
  }
  return { w, h, data: out };
}
const CRC = new Int32Array(256).map((_, n) => { let c = n; for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1; return c; });
const crc32 = buf => { let c = -1; for (const b of buf) c = CRC[(c ^ b) & 255] ^ (c >>> 8); return (c ^ -1) >>> 0; };
function chunk(type, data) {
  const len = Buffer.alloc(4); len.writeUInt32BE(data.length);
  const td = Buffer.concat([Buffer.from(type, "ascii"), data]);
  const crc = Buffer.alloc(4); crc.writeUInt32BE(crc32(td));
  return Buffer.concat([len, td, crc]);
}
function encode(w, h, rgba) {
  const raw = Buffer.alloc((w * 4 + 1) * h);
  for (let y = 0; y < h; y++) { raw[y * (w * 4 + 1)] = 0; rgba.copy(raw, y * (w * 4 + 1) + 1, y * w * 4, (y + 1) * w * 4); }
  const ihdr = Buffer.alloc(13); ihdr.writeUInt32BE(w, 0); ihdr.writeUInt32BE(h, 4); ihdr[8] = 8; ihdr[9] = 6;
  return Buffer.concat([Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]), chunk("IHDR", ihdr), chunk("IDAT", zlib.deflateSync(raw, { level: 9 })), chunk("IEND", Buffer.alloc(0))]);
}

const file = process.argv[2];
const { w, h, data } = decode(fs.readFileSync(file));
//front lip of the rim in the 1024x1024 painting, scaled to whatever size we're given
const cx = 0.5 * w, cy = 0.29 * h, rx = 0.345 * w, ry = 0.05 * h, feather = 6;

const front = Buffer.from(data), back = Buffer.from(data);
for (let y = 0; y < h; y++) {
  for (let x = 0; x < w; x++) {
    const o = (y * w + x) * 4;
    const dx = (x - cx) / rx;
    let frontness; //0 = back, 1 = front
    if (Math.abs(dx) >= 1) frontness = 1;
    else {
      const split = cy + ry * Math.sqrt(1 - dx * dx);
      frontness = Math.min(1, Math.max(0, (y - split) / feather + 0.5));
    }
    front[o + 3] = Math.round(data[o + 3] * frontness);
    back[o + 3] = Math.round(data[o + 3] * (1 - frontness));
  }
}
const dir = path.dirname(file), base = path.basename(file, ".png");
fs.writeFileSync(path.join(dir, base + "_Front.png"), encode(w, h, front));
fs.writeFileSync(path.join(dir, base + "_Back.png"), encode(w, h, back));
console.log(`wrote ${base}_Front.png and ${base}_Back.png`);
