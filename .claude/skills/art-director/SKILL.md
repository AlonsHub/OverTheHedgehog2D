---
name: art-director
description: Consult ChatGPT as the project's Art Director for art direction, or generate 2D sprite/UI assets into Assets/Art/Generated via OpenAI. Use when the user asks for art direction, a new sprite/icon/background, or to "ask the art director".
---

# Art Director (ChatGPT via OpenAI API)

Script: `Tools/ArtDirector/artdirector.mjs` (Node). Needs env var `OPENAI_API_KEY`; if missing, tell the user to follow `Tools/ArtDirector/README.md`.

## Workflow for a new asset
1. Ask for direction first:
   `node Tools/ArtDirector/artdirector.mjs direct "Write an image prompt for: <asset description>. Match the style guide."`
2. Generate using the returned prompt:
   `node Tools/ArtDirector/artdirector.mjs image "<prompt>" --out Assets/Art/Generated/<name>.png --transparent`
   Sizes: 1024x1024, 1536x1024, 1024x1536. Use `--transparent` for sprites/icons, omit for backgrounds.
3. Run `assets-refresh` so Unity imports it, then set the texture importer (Sprite 2D, PPU, filter mode) to match existing sprites in Assets/Art.
4. Show the user the result (Read the PNG) before wiring it into a prefab/scene.

## Pure direction (no image)
`node Tools/ArtDirector/artdirector.mjs direct "<question>"` — palette, composition, feedback on a screenshot description, etc.

Keep the style guide at `Tools/ArtDirector/STYLE_GUIDE.md` updated when the user makes style decisions.
