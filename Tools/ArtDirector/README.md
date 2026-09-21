# Art Director bridge

Lets Claude Code consult ChatGPT as the project's Art Director and request generated 2D assets.

## One-time setup
1. Create an API key at https://platform.openai.com/api-keys and add billing.
2. Set it as a **user** environment variable (never paste it in chat or commit it):
   PowerShell:  [Environment]::SetEnvironmentVariable("OPENAI_API_KEY", "sk-...", "User")
3. Fully restart VS Code so the new variable is picked up.
4. Fill in Tools/ArtDirector/STYLE_GUIDE.md.

## Commands
node Tools/ArtDirector/artdirector.mjs direct "Give me a palette and shape language for the shop UI"
node Tools/ArtDirector/artdirector.mjs image "<prompt>" --out Assets/Art/Generated/coin.png --size 1024x1024 --transparent

## Keeping new sprites on-model (use --ref)
Plain text-to-image drifts into plush/clay/3D looks. Passing an existing sheet as a reference keeps the
outlines, shading and proportions consistent:

    node Tools/ArtDirector/artdirector.mjs image "<prompt: 'Redraw the attached sheet ... EXCEPT ...'>" \
      --out Assets/Art/Hogs/X/X_Idle.png --size 1024x1024 --transparent --ref Assets/Art/SpriteSheets/Idle.png

Several refs are allowed (`--ref a.png,b.png`): pass the pose sheet first and the recolored character second,
and say "FIRST"/"SECOND" in the prompt.

## After generating
- `node Tools/ArtDirector/cleanalpha.mjs <png...>` strips the soft glow halo the model leaves on
  transparent backgrounds (keeps anti-aliased edges).
- In Unity, `SpriteSheetImporter.ImportBands(path, ppu)` slices a sheet by detecting the frame rows/columns
  (grids come out slightly uneven); `ImportGrid` for scattered frames (particle bursts); `ImportSingle` for
  single sprites. `ContentBuilder` (menu *Over The Hedgehog > Build > Everything*) rebuilds clips, controllers,
  prefabs, VFX, block sets, levels and scenes from the imported art.
- Rate limit is ~5 images/minute: generate sequentially.
- The key lives in the User environment; in a fresh PowerShell:
  `$env:OPENAI_API_KEY = [Environment]::GetEnvironmentVariable("OPENAI_API_KEY","User")`
