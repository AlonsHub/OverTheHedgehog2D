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
