#!/usr/bin/env node
// Bridge to OpenAI so Claude Code can consult ChatGPT as the project's Art Director
// and request generated 2D assets. Lives outside Assets/ so Unity never imports it.
//
// Usage:
//   node Tools/ArtDirector/artdirector.mjs direct "<question or brief>"
//   node Tools/ArtDirector/artdirector.mjs image "<prompt>" --out Assets/Art/Generated/name.png [--size 1024x1024] [--transparent] [--quality medium]
//
// Requires env var OPENAI_API_KEY. Optional: ART_DIRECTOR_MODEL (default gpt-4o), ART_IMAGE_MODEL (default gpt-image-1).

import fs from "node:fs";
import path from "node:path";

const API = "https://api.openai.com/v1";
const key = process.env.OPENAI_API_KEY;
if (!key) {
  console.error("OPENAI_API_KEY is not set. See Tools/ArtDirector/README.md");
  process.exit(2);
}

const STYLE_GUIDE_PATH = path.resolve("Tools/ArtDirector/STYLE_GUIDE.md");
const styleGuide = fs.existsSync(STYLE_GUIDE_PATH) ? fs.readFileSync(STYLE_GUIDE_PATH, "utf8") : "";

const SYSTEM = `You are the Art Director for "Over The Hedgehog 2D", a 2D Unity game.
You give concise, decisive art direction: palette, shape language, line weight, lighting, mood, and
sprite specs (pixel dimensions, pivot, padding, sheet layout). When asked for an image prompt, return
ONLY a single production-ready prompt for an image model, no commentary.
${styleGuide ? "\nProject style guide:\n" + styleGuide : ""}`;

function parseArgs(argv) {
  const out = { _: [] };
  for (let i = 0; i < argv.length; i++) {
    const a = argv[i];
    if (a.startsWith("--")) {
      const k = a.slice(2);
      const next = argv[i + 1];
      if (next === undefined || next.startsWith("--")) out[k] = true;
      else { out[k] = next; i++; }
    } else out._.push(a);
  }
  return out;
}

async function post(endpoint, body) {
  const res = await fetch(`${API}${endpoint}`, {
    method: "POST",
    headers: { "Authorization": `Bearer ${key}`, "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`${endpoint} -> HTTP ${res.status}: ${text}`);
  }
  return res.json();
}

async function direct(prompt) {
  const model = process.env.ART_DIRECTOR_MODEL || "gpt-4o";
  const data = await post("/chat/completions", {
    model,
    messages: [
      { role: "system", content: SYSTEM },
      { role: "user", content: prompt },
    ],
  });
  process.stdout.write(data.choices[0].message.content.trim() + "\n");
}

async function image(prompt, opts) {
  const model = process.env.ART_IMAGE_MODEL || "gpt-image-1";
  const out = opts.out || `Assets/Art/Generated/${Date.now()}.png`;
  const body = {
    model,
    prompt,
    n: 1,
    size: opts.size || "1024x1024",
    quality: opts.quality || "medium",
  };
  if (opts.transparent) body.background = "transparent";
  const data = await post("/images/generations", body);
  const b64 = data.data[0].b64_json;
  fs.mkdirSync(path.dirname(out), { recursive: true });
  fs.writeFileSync(out, Buffer.from(b64, "base64"));
  console.log(`Saved ${out}`);
}

const args = parseArgs(process.argv.slice(2));
const [cmd, ...rest] = args._;
const text = rest.join(" ");

try {
  if (cmd === "direct" && text) await direct(text);
  else if (cmd === "image" && text) await image(text, args);
  else {
    console.error('Usage:\n  direct "<brief>"\n  image "<prompt>" --out <path.png> [--size WxH] [--transparent] [--quality low|medium|high]');
    process.exit(1);
  }
} catch (e) {
  console.error(e.message);
  process.exit(1);
}
