# Changelog

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
