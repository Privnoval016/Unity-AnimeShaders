# Changelog

## 0.3.0 (2026-11-16)

- **Breaking:** `SkyState` is one serializable struct held by `SkyPreset` (the presets migrated); `SkyApply` also
  publishes the world's globals (`AnimeShaderIds.World`); the sky's cloud cover, scale, speed and wind moved from its
  material to the preset's cloud field.
- The cloud field (`Core/AnimeClouds.hlsl`): one world-plane layer the sky draws and the cloud shadows will read.
  `Anime/Sky` redrawn: the field's drawn clouds, up to three analytic ridges hazed by distance, the haze, every noise
  read in uniform flow, a trig-free hash.
- The haze (`Core/AnimeFog.hlsl`): distance and height fog with a glow toward the sun, in the pack's forward and outline
  passes in place of URP's fog behind `_ANIME_ATMOSPHERE`; URP's fog takes the same colour and reach for other shaders.
- Sky presets' **shade** (`SkyState.Shade`, published as `_AnimeWorldShade`, zero for white): the time of day's tint on
  every pack material's shade tone, the director's on top; presets older than it load as white. Golden Hour, Dusk and
  Night darker and cooler (at dusk the dim sun had pulled the lit tone level with the shade, so cast shadows read as
  lilac stripes); Dusk's sun at 10° (4° stretched shadows 14 times a body's height and lifted them off the feet). The
  presets' air: thinner haze capped higher, more mist at the hills' feet; Day's sky deeper. Tests: the tint scales only
  the shade, the director's on top; the preset publishes and clears it; break proof.
- **Stones** (variation per stone, for any tiled set with an occlusion map): a detail set's Stones section writes a
  stone map beside it (`StoneMath`: the stones found on the tile as a torus at the occlusion's resolution, an exact
  home-tile code per texel; `AnimeStones` label, point-sampled, uncompressed); `Anime/Ground` gains per layer **Stone
  Map**, **Stone Shade** and **Stone Mix** (one point read and an integer hash, only while a layer varies stones).
  `DataTextures.HoldTo` takes a point-data option; `DetailMath.Blur` is shared. Tests: the stones of a made grid (one
  centred on the tile's corner) one home and value across the edges, pitting, joints, the fill across the edge, bands
  and flat maps, determinism, the codes; the stone map's import, Stones off removing it, the stale hash per setting; on
  the ground, no map or no variation the plain frame, a stone one shade and unlike its repeats, no seam across the tile's
  edge, the mix by ratio, the fade, the joints, Break Up; break proofs.
- `Anime/Ground`: the field's **Patch Tint** (`_PatchTint`, alpha the strength; `_PatchTintScale`, `_PatchTintCover`,
  `_PatchTintSoftness`): broad patches take the tint's hue at the colour's own brightness, from a second noise read
  (fbm) only when the strength is above 0. `AnimeLuma` in `Core/AnimeSurface.hlsl` holds the brightness weights. Test:
  no strength or cover is the plain frame, patches take the hue at the same brightness, the circle takes none; break
  proofs.
- The look pass (2c step F, part): the cloud field reads its noise twice at a scale and angle that never line up,
  blended to keep its contrast, so it never repeats (a test one noise tile on); stars as small dots from the value
  channel with a `starScale` in the state (the cell channel drew shards); Golden Hour, Dusk and Night retuned with Day's
  build (large drawn clouds, three ridges with mist, haze with a glow, their own colours).
- Sun shafts (2c step D): `AnimeSunShafts` (volume override), `AnimeSunShaftsFeature` and its pass (mask with a
  brightness threshold and a 2x2 far-depth test, two radial blurs, an additive composite before the post, at half
  resolution; skipped at 0, with the sun behind, or without post-processing), `Hidden/AnimeShaders/SunShafts`. Tests
  through a made pipeline and `MadeVolume` (in the stage's own scene, since URP blends only the volumes a camera renders):
  identity at 0, the sun behind, no post, rays over the sky against the wall beside the sun, the sun off the edge;
  break proofs.
- Contact shading (2c step C): URP's SSAO in the pack's forward passes through the tone step (`_ContactShading`,
  `_ContactThreshold` on the core block; characters 0.3, the world 1). Tests through URP's real SSAO feature in a made
  pipeline (a cube's foot shades the floor, the open floor doesn't; a response of 0 is the frame without it); break proofs.
- Cloud shadows (2c step B): the cloud field projected toward the sun dims the lit tone softly in every lit pack shader
  (`_CloudShadow` on the core block, the material's response; `_ANIME_CLOUD_SHADOWS`); once per body at its shadow
  point; a clear radius round the camera (`cloudShadowClearRadius`, `cloudShadowClearFade`; `_AnimeCloudShadowClear`);
  the ridges shaded where they stand. Tests: off and no response, full and part cover, the clear radius, past the shadow
  distance, one shade per body, the cloud overhead over its shadow, the ridges; break proofs.
- Mist at the ridges' feet (`ridgeMistHeight`, `ridgeMist` in the state; `_RidgeMist` on the sky): each ridge dissolves
  into the haze toward its base, so far ground and hills meet with no line.
- `SkyPresetApplier`: a Play Mode copy of the sky material, the sun and reflection restored, the globals cleared when
  switched off.
- Tests: the sky's clouds, ridges (colour, haze by distance, the sun behind, a pixel-wide edge, a varied, steady
  skyline), the haze (off, distance, height, orthographic, the glow, never twice, a wall at the sky's distance meeting
  the sky), presets (every value and global, no noise, clearing, the applier, every preset migrated), the world table;
  `WorldGlobals` in every stage, `MadeSky`, the finite scan. Break proofs.

## 0.2.0 (2026-11-16)

- **Breaking:** `Anime/Ground` paints two layers (Field, Circle) from packed detail maps instead of procedural grass and
  flagstones; the old grass, stone, grout and grain properties are gone. Reads only the layer that shows, its gradients
  taken outside the branches; shade steps; detail normals in a world-XZ frame; break-up tiling that keeps contrast.
- **Breaking:** the forward pass's hook is `ANIME_CUSTOM_SURFACE` (`AnimeCustomSurface(positionWS, uv, inout
  AnimeSurface)`), called once the base colour, faced normal and emission are set; `ANIME_CUSTOM_ALBEDO` is gone.
- **Breaking:** `AnimeShaderIds.MaterialProperties` loses the procedural ground's ids (grass, stone, grout, grain,
  patch scale) and gains the two layers', the patches' and the break-up's.
- Detail sets (`AnimeDetailSet`, `DetailMath`, `AnimeDetailPacker`): a scanned set packed into one map (shade, mask,
  normal XY), wrapping, mean-centred, occlusion optional, soften as a share of the tile, out-of-date check, video memory
  shown; `AnimeDetailImport`; the neutral detail map as the ground's default; `DataTextures` (the labelled data
  textures' writer and import rule, shared with the noise); `ShaderDefaults` sets defaults in every pack shader
  declaring the property, keeping the others; the material inspector warns on an unpacked texture in a detail slot.
- After four reviewers: the detail maps take their own samplers; whole-number steps that can't divide by zero; the
  break-up keeps the normal's spread too; the detail normal's frame turns smoothly on slopes; Edge Softness and tile
  sizes held off zero; the alpha clip after the surface hook; the second break-up read skipped where its weight is 0.
- Tests: the packer (luminance weights, occlusion, the stretch and mean, resampling with its wrap, non-square sets,
  soften and its wrap, every setting in the hash) and its import (the rule, a set's pixels on disk, out of date on a
  source or setting change, the shaders' defaults), the ground's layers each (tile, steps, normal in X and Z, break-up),
  gradient, mask, earth, DirectX through the packer, edge band, distance, degenerate settings, slopes, far from the
  origin, the reads' structure; the surface hook through a probe; the rim's band against its asked width, and baselines
  for its none-cases. Break proofs throughout.

## 0.1.0 (2026-11-15)

- The lit core (properties block, light director globals, surface, lighting, outline, fade, one include per pass) and
  the `Anime/Character` shader: two-tone analytic ramp with cast shadows folded in, second shadow band, additional
  lights through the ramp, fresnel rim, emission, procedural inside tone for two-sided cloth, normal map, alpha clip,
  hull outline from baked tangent-space normals, dither fade from the renderer's user value.
- `AnimeShaderIds` (every global and material property name and id, the toggles included) and the
  `AnimeOutlineNormals` import baker (smoothing across the whole mesh, so a material seam doesn't crack the line).
- The line reads baked normals only with *Line From Baked Normals* on (a mesh without UV3 is not zero on Metal).
- `ANIME_ENTRY_PROPERTIES`: an entry shader's own material properties join the one block.
- After four reviewers: the cast shadow joined by its own weight (edge on the shadow map's half, strength linear, the
  distance fade smooth); additional-light shadows; scene keywords fragment-only; a fully faded body culled in every
  pass; the line writes rendering layers; `AnimeMaterials`; `AnimeShaderIds.OutlineLightMode`; the baker leaves a
  model's own UV3 alone and builds tangents from UVs; clearer property labels with headers.
- *Shadow The Whole Body*: the main light's cast shadow read once per renderer, past the body toward the light (no
  self-shadowing, no crawl on an animated character), with its reach in metres.
- `AnimeShaderGUI`: sections, a live ramp preview, settings shown only while their feature is on.
- The depth-edge rim: `AnimeRimFeature` (an overlay after the skybox reading URP's depth copy) and the `AnimeRim` pass;
  `AnimeMaterials.SetRimDepth`; 7 tests through a made pipeline (`MadePipeline`), with break proofs.
- The core's material block in feature groups (a shader declares only the properties it uses); the includes in
  `Core/` and `Passes/`; the baked noise texture, its baker and import rule.
- `Anime/World`, `Anime/Ground` (procedural, a placeholder for texture sets), `Anime/Sky`; sky presets (Day, Golden Hour,
  Dusk, Night), `SkyApply`, `SkyPresetApplier`; the no-fade, custom-surface and world-UV seams.
- Tests: passes, messages, the SRP Batcher, the one block, names, every pass compiled for D3D, Vulkan, OpenGL Core,
  Metal and GLES3, and renders of the tones, threshold, softness, emission, rim, toggles, director light colour, cast
  shadows, inside tone, line and fade through a small lit stage; the baker on a hard cube.

## 0.0.1 (2026-11-15)

- The skeleton: README (what the pack will hold, its outline approach, its requirements). No shaders yet.
