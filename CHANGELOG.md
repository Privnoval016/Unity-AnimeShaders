# Changelog

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
- Tests: passes, messages, the SRP Batcher, the one block, names, every pass compiled for D3D, Vulkan, OpenGL Core,
  Metal and GLES3, and renders of the tones, threshold, softness, emission, rim, toggles, director light colour, cast
  shadows, inside tone, line and fade through a small lit stage; the baker on a hard cube.

## 0.0.1 (2026-11-15)

- The skeleton: README (what the pack will hold, its outline approach, its requirements). No shaders yet.
