# Over The Hedgehog 2D — Style Guide

The Art Director reads this on every request. Keep it current; when the player makes a style decision, write it here.

## Genre / mood
Angry Birds-style physics launcher, but with hedgehogs. Cozy, reactive, smooth, slow-breathing scene. Warm and natural (never industrial or synthetic) but cartoonish. Wholesome, neat, clean: a sunny cottage garden on a lazy afternoon. Nothing is scary, grimy, or sharp-edged; damage reads as "tumbling over", not destruction.

## Overall look
Hand-painted 2D cartoon with a slight storybook / children's-picture-book feel (think Stardew Valley key art meets Cut the Rope prop rendering). Semi-painterly: soft brushy shading and light surface texture (wood grain, moss speckles, chipped terracotta) inside clean cartoon outlines. Not pixel art, not vector-flat, not photoreal.

## Depth layering (keep this strict)
1. **Background** (sky, mountains, distant trees): soft-focus / depth-of-field blur, low contrast, cool blue-greens, no outlines.
2. **Midground** (greenhouse, fences, sign, pinwheel, vines): crisp, full outlines, full saturation.
3. **Playfield** (hedgehog, blocks, targets, ground): crispest, strongest outline, highest contrast. Silhouettes must read instantly at small size.
4. **Ambient life**: bees, butterflies, drifting petals/leaves, dust motes. Tiny, slow, never distracting.

## Palette
- Leaf green (dominant foliage) `#6FB92B`, sunlit highlight `#B7E24A`, shadow green `#3E7F1E`
- Soil brown `#7A4A23`, warm wood `#B8834A` / light wood `#D9B57A`, dark outline brown `#3B2314`
- Terracotta `#C8673A` (pots), aged with pale moss `#9DC46A` patches
- Sky blue `#8EC5E8`, distant mountain blue `#5F8DB8`, cloud white `#F6F8FA`
- Cream / whitewash `#F3E9D2` (white wood, hedgehog belly), daisy white `#FFFFFF` with yolk-yellow center `#F5B921`
- Accents (small doses only): sunflower yellow `#F2C230`, lavender `#7E6FD1`, blossom pink `#E88CB4`, poppy red `#D64830`, pinwheel orange `#F28C28`
- Hedgehog: quills chocolate `#6B3A1E` with lighter tips `#9C5E37`, face/belly cream `#F6E3C6`, blush/ears pink `#E8A0A0`, nose/eyes black
- Rule: greens dominate the world, so **player-interactive objects should be warm** (wood, terracotta, cream, stone) so they pop against the foliage. Avoid green props on the playfield.

## Line & shape language
- Outlines on everything in mid/playfield: dark warm brown (`#3B2314`), never pure black. Thicker on the outer silhouette, thinner for interior detail. Weight roughly 2–3% of the sprite's height.
- Shapes are chunky, rounded, slightly chubby. Corners are softened; wood beams have gentle bevels and worn ends. No perfectly straight machine edges.
- Everything is a little "lived-in": a moss patch, a chip, a curling vine, a daisy tucked in a corner. One or two such details per prop — not more.
- Shading: soft cel with a painterly edge, 2–3 tones per material plus one highlight. Light comes from the **upper-left, high sun**. Bake only a subtle ambient occlusion on the object itself; **no cast shadows on the ground** baked into sprites (the scene handles that).
- Recurring motifs: white daisies with yellow centers, ivy/vine tendrils, moss on aged surfaces, twisted jute rope, weathered white-painted wood, terracotta.

## Characters
Hedgehog is round, compact, big-headed, with a cream face and small stubby paws. Expressions are the main storytelling: determined, cheeky, dizzy, happy. Keep any "angry" brow playful (determined, not mean) to match the wholesome tone. Consistent 3/4 side view facing right in sheets.

### Cast (keep these consistent)
- **Neutral hog**: chocolate quills, cream face, determined-cheeky. The reference for every other character.
- **Exploding hog**: same body, ember red-orange quills (`#C8673A` / `#F28C28` tips), jute-rope fuse headband with a lit spark, mischievous grin. Lands, puffs its cheeks, bursts into a cream-orange puff cloud.
- **Cluster hog**: plumper, quills are 5-6 round grape-like bumps in sandy-cocoa (`#8A5A32` / `#C08A55` tips), tiny extra eye pairs peeking from the bumps, wide-eyed happy. Splits into **mini hogs**: huge head, tiny body, same sandy-cocoa, dizzy spiral eyes when landed.
- **Hen (enemy)**: one big fluffy cream feather ball (`#F3E9D2`, shadow `#D9B57A`), tiny red comb/wattle, orange beak and feet, half-lidded grumpy stare with one raised brow, faces left. Pops into a burst of feathers, wholesome.
- **Boss rooster**: huge, puffy, dark-green + terracotta tail, tall red comb, golden beak, bushy silly-angry brows, golden bell on a jute rope.

### Generation workflow that works
Always generate character sheets with `--ref` and an existing sheet (see Tools/ArtDirector/README.md): "Redraw the attached sheet keeping everything identical EXCEPT ...". Ask for "no background, no ground shadow, no glow" and run cleanalpha afterwards. Sheets come back as 4x4 or 4x3 grids; that's fine, the importer detects frames.

## Sprite specs
- Delivered as PNG with transparent background, no drop shadow, no ground plane, no text.
- Environment sprites: ~1000–2000 px on the long side, imported at **100 PPU**, bilinear filtering.
- Hedgehog / character sheets: ~300 px per frame, imported at **200 PPU**; sheets on a clean uniform grid with even padding.
- Tileable textures (wood, clay, stone) are square, 1024–1254 px, seamless on all edges.
- Props that get stacked/physicsed (beams, boxes, pots) need simple convex silhouettes and clear top/bottom faces.
- **Physics blocks** (posts/beams) are separate horizontal and vertical sprites, never one rotated: light stays upper-left and grain runs along the length. Generate the beam at 1536x1024 and the post at 1024x1536 spanning the full long axis, trim to alpha, and keep every detail (worn ends, a nail, the daisy, moss) inside the outer 25% end caps; the middle 50% is plain grain because it is 9-sliced and stretched up to 11:1. Nothing may poke outside the rectangle (what you see is what collides). **Chosen 2026-09-21: natural honey oak** (`Assets/Art/Blocks/Oak_*`) with a sun-bleached `OakLight_*` variant (made with variant.mjs) mixed in per block so structures don't read as one slab. Rejected options WhiteWood and Birch stay in Assets/Art/Generated/Blocks for reference.
- Sizes for generation: use 1024x1024 for single props/textures, 1536x1024 for wide props (fences, signs, ground strips), 1024x1536 for tall ones (posts, towers).

## UI (when it comes up)
Same world materials: rounded wooden plank panels with jute-rope hangers, cream parchment for text areas, daisy/leaf corner accents, chunky rounded buttons in terracotta or leaf green with the same brown outline. Font feel: soft rounded sans or a friendly hand-lettered display face. No glossy, glassy, neon, or sci-fi UI.

## Do / Don't
- Do: warm sunlight, soft blur in the background, one cute detail per prop, consistent brown outlines, transparent PNGs.
- Don't: pure black lines, harsh specular highlights, gradients-only flat vector look, metal/industrial materials, gore/damage textures, baked ground shadows, text in sprites, mixing pixel-art with painted assets.

## Reference games / artists
Stardew Valley (key art, not pixel sprites), Cut the Rope (prop rendering), Angry Birds (silhouette readability, structure gameplay), Studio Ghibli countryside backgrounds (mood only), Animal Crossing (wholesomeness).
