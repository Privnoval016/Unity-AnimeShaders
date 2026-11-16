# Unity-AnimeShaders

Anime (non-photorealistic) shaders for Unity's Universal Render Pipeline that can be dropped into any project: a clean
two-tone look in the manner of modern anime games, outlines that hold up on two-sided cloth, faces that shade cleanly
as the head turns, and a light director so a project can set a mood per scene and relight a single shot. When it is
done, everything will be tuned through a custom inspector with presets, with nothing needing code.

**Status: in construction (0.3.0).** The lit core, the character shader, the depth-edge rim, the world, ground and sky
shaders, sky presets with clouds, ridges and haze, and detail sets are written; the face, hair and foliage shaders, the full inspector, the director's
runtime and look presets arrive in the order of the table below, each
proved before the next.

- **Requires:** Unity 6, URP 17, the Forward or Forward+ renderer with depth priming off. Deferred is not supported
  (it would draw the line but not the body), and depth priming would test the line against the body's primed depth and
  drop it.
- **Platforms:** D3D11/12, Vulkan, Metal, OpenGL Core and GLES3; the tests compile every pass for each of them from any
  Editor.
- **Namespace:** `AnimeShaders` (editor tools: `AnimeShaders.EditorTools`).
- **Written by hand in HLSL**, every material property in one per-material block so the SRP Batcher holds, material
  toggles as local keywords so a feature turned off costs nothing.

## What it will hold (the core, `Anime/Character`, `Anime/World`, `Anime/Ground` and `Anime/Sky` exist; the rest is planned)

| Part | What it is |
|---|---|
| `Shaders/Includes/` | The one lit core every shader shares: the properties block, the lighting (two tones, shadow tint, rim, highlight, emission, the inside tone of two-sided cloth), the light director's names, the outline pass, the dither fade, cast shadows folded into the tones |
| `Shaders/` | Thin entry shaders over the core: character (body, cloth, accessories), face, hair, an eye overlay for brows through hair, world (terrain, buildings, props), foliage. A project can write its own thin entry shader over the core to add passes of its own |
| `Runtime/` | The light director (a layered stack of light sets: a base, then layers pushed and removed by a token), light sets, head facing for faces, presets, the property names in one place |
| `Editor/` | One inspector for every pack shader (sections, presets, a live ramp preview, fields shown only when their feature is on, a tooltip on each), preset apply and extract, the outline-normal baker (on import, opt-in by label), a face shadow map baker |
| `Tests/` | EditMode tests and a kit that renders a lit mesh to a small target |

None of its assemblies will be auto-referenced.

## Outlines

A second pass of the same shader (one mesh, skinned once) pushes a copy of the mesh outward along a second set of
normals, smoothed so hard edges don't crack the line. Those normals are baked on import into a spare UV channel,
leaving the tangents free for normal maps. Width follows the camera's distance and field of view, can be painted per
vertex, and has a global multiplier.

## What exists now (0.3.0)

| File | What it is |
|---|---|
| `Shaders/AnimeCharacter.shader` | `Anime/Character`: passes ForwardLit (`UniversalForward`), Outline (`SRPDefaultUnlit`, Cull Front), ShadowCaster, DepthOnly, DepthNormals. No Meta pass (characters are not baked) |
| `Shaders/Includes/Core/AnimeProperties.hlsl` | The one `UnityPerMaterial` block every pass includes (SRP Batcher): a core plus feature groups a shader opts into in its `HLSLINCLUDE` (`ANIME_GROUP_BAND`, `_BODY`, `_EMISSION`, `_INSIDE`, `_RIM`, `_LINE`; the code reading a group compiles only with it, and the shader's Properties list holds exactly its groups); its own extra properties through `ANIME_ENTRY_PROPERTIES`; the dynamic-branch keyword macros |
| `Shaders/Textures/AnimeNoise.png` | The pack's noise, baked by `Editor/AnimeNoiseBaker.cs` (Tools > Anime Shaders > Bake Noise Texture; our own code, deterministic, versioned): 1024 square, R value noise, G fbm, B distance to the nearest cell edge, A a value per cell, every channel tiling; held linear, uncompressed and mipmapped by `Editor/AnimeNoiseImport.cs` (label `AnimeNoise`) |
| `Shaders/Includes/Core/AnimeDirector.hlsl` | The light director's global properties (below) |
| `Shaders/Includes/Core/AnimeWorld.hlsl`, `AnimeClouds.hlsl`, `AnimeFog.hlsl` | The world's globals (published by a sky preset), the cloud field the sky and the cloud shadows share, and the haze that replaces URP's fog in the pack's shaders (below) |
| `Shaders/Includes/Core/AnimeSurface.hlsl` | The surface struct, base and emission sampling, alpha clip, the inside tone |
| `Shaders/Includes/Core/AnimeLighting.hlsl` | The ramp, cast shadows folded into it, the second band, additional lights, the rim, emission, ambient: `AnimeShade` |
| `Shaders/Includes/Passes/AnimeForwardPass.hlsl`, `AnimeShadowCasterPass.hlsl`, `AnimeDepthOnlyPass.hlsl`, `AnimeDepthNormalsPass.hlsl` | One file per pass: structs, vertex, fragment |
| `Shaders/Includes/Passes/AnimeOutline.hlsl` | The hull pass: baked-normal reconstruction, screen-consistent width, Z offset |
| `Shaders/Includes/Core/AnimeFade.hlsl` | The dither fade |
| `Runtime/AnimeShaderIds.cs` | Every global and material property name and id, the toggles, the keywords, the line's light mode, and the fade constants |
| `Runtime/AnimeMaterials.cs` | Setting a feature (its keyword and its toggle together), receiving shadows, the line pass on or off, the depth rim on or off |
| `Runtime/Rendering/AnimeRimFeature.cs`, `AnimeRimPass.cs` | The depth-edge rim's renderer feature and its render-graph pass (below) |
| `Runtime/Rendering/AnimeSunShafts.cs`, `AnimeSunShaftsFeature.cs`, `AnimeSunShaftsPass.cs`; `Shaders/AnimeSunShafts.shader` | The sun shafts: the volume override, the renderer feature, its render-graph passes and their shader (below) |
| `Shaders/Includes/Passes/AnimeRimPass.hlsl` | The rim pass: the scene depth read past the line along the normal, the threshold, the light-facing mask |
| `Editor/OutlineNormalBaker.cs` | The import-time outline normal bake |
| `Editor/ShaderDefaults.cs` | Sets the pack shaders' default textures (the noise, the neutral detail map) in every pack shader declaring the property, keeping their other defaults |
| `Editor/DataTextures.cs` | Textures that hold data: written as labelled PNGs, held by their import rule to linear, alpha-as-data, repeating settings, mipmapped and trilinear or (for ids and codes) point-sampled without mips (the noise's, the detail maps' and the stone maps' rules all use it) |
| `Editor/Detail/` | Detail sets (below): `AnimeDetailSet` (the asset), `AnimeDetailSetEditor` (Pack, out of date, video memory), `DetailMath` (the packing as pure image maths), `AnimeDetailPacker` (the sources read from disk, the map written and recorded, the neutral map), `AnimeDetailImport` (the labels `AnimeDetail` and `AnimeStones`), `DetailImage`, `DetailSources`, `DetailSettings`, `NormalConvention`; `Stones/`: `StoneMath` (finding the stones as pure image maths), `StoneSettings`, `StoneMap` |
| `Shaders/Textures/AnimeDetailNeutral.png` | The neutral detail map (shade 0.5, mask 1, flat normal): the ground's default, so a layer with no set renders as its palette's middle (Tools > Anime Shaders > Make Neutral Detail Map) |
| `Shaders/AnimeWorld.shader`, `AnimeGround.shader`, `AnimeSky.shader`; `Shaders/Includes/World/` | The world's shaders and the ground's and sky's own code (below) |
| `Runtime/Sky/` | `SkyPreset`, `SkyState` (everything a preset sets, held once), `SkyRidge`, `SkyApply` (the one writer), `SkyPresetApplier`, and the shipped presets in `Presets/` |
| `Editor/AnimeShaderGUI.cs` | The material inspector (thin for now): the shader's sections, a live preview of the two-tone ramp (the same maths the shader runs, tested against a render), and each feature's settings shown only while the feature is on |
| `Tests/Editor/` | `Support/` (in `Stage/`, `World/` and `Sources/`: `LitStage`: a preview scene, camera and light rendering into a linear float target; `MadePipeline`: a URP asset made for a test with given features, swapped in and always put back; meshes, materials with colours as the shader reads them, frame reads, source readers, the director's globals and the world's globals held neutral (`WorldGlobals`; every stage holds them), a sky preset made field by field (`MadeSky`), a volume only the stage camera sees (`MadeVolume`), the finite scan, the batcher check), `Shader/` (passes, batcher, names and constants, every platform's compile, and `Probe/`: an entry shader adding its own property, and one through the surface hook), `Inspector/` (the feature map, the preview against a render), `Look/` (tones, band, rim, director, cast shadows, cloud shadows, contact shading, inside tone, fade, the surface hook; `Screen/`: the depth rim, the sun shafts), `World/` (the noise, world UVs; `Ground/`: the detail packer and its import, the ground; `Sky/`: the sky, its ridges and clouds, the haze, presets), `Outline/` (the line, the baker) |

A thin entry shader (face, hair, world, or a project's own with extra passes) includes the same files, declares its own
pragmas, and calls `AnimeShade`; it adds nothing else to the lighting. Its own material properties go in through
`ANIME_ENTRY_PROPERTIES` (defined in its SubShader's `HLSLINCLUDE`), its own textures are declared after the core is
included, and its `Properties` block repeats the character shader's whole list before its own: the SRP Batcher refuses a
shader whose per-material variables are not all declared there (ShaderLab cannot include a `Properties` block). The
tests' probe shader (`Tests/Editor/Shader/Probe/`) is the pattern.

### The look

Two tones from an analytic ramp: the lit tone is the base colour times the light, the shadow tone the base colour times
`_ShadowTint`. The ramp runs on a half-lambert value; `_ShadowThreshold` 0.5 puts the terminator at N.L = 0 and
`_ShadowSoftness` is the width of the smoothstep. The main light's cast shadow is a second weight with its edge where
the shadow map crosses one half, as hard or soft as the ramp, joined to the tone by `min`; `_CastShadowStrength` blends
it in linearly (0.5 lands halfway between the tones), and URP's own fade toward the shadow distance lowers that strength
smoothly. *Shadow The Whole Body* (`_ANIME_BODY_SHADOW`, a dynamic branch, off by default as every toggle) reads that
shadow once per renderer instead of per pixel: at its bounds' centre moved `_BodyShadowReach` metres toward the light,
outside the body, so a hat, hair or an arm never shades it and nothing crawls as it animates, while scenery between it
and the light still does; the body then fades into shade with the shadow map's own filtering. Under screen-space
shadows it falls back to the surface. Additional lights cast their shadows when the pipeline gives them. Additional lights go through the same ramp
at `_ExtraLightStrength`, capped by `_ExtraLightCap` (a toggle, off by default). The rim is a fresnel band. Emission is
added after the tones and is never darkened. Ambient is the probe's average, or the director's tint, added gently to the
shadow tone.

### Keywords (forward pass)

Static material keywords (variants, `shader_feature`): `_NORMALMAP`, `_RECEIVE_SHADOWS_OFF`, `_ALPHATEST_ON`,
`_ANIME_EXTRA_LIGHTS`. A build keeps only the variants some material in it uses, so a feature switched on at runtime
needs a material in the build with it on; set a feature with `AnimeMaterials.SetFeature` (its keyword and its toggle
together: setting a toggle's float alone does not change the keyword).
Dynamic branches (no variants): `_ANIME_BAND2`, `_ANIME_RIM`, `_ANIME_EMISSION`, `_ANIME_EMISSION_MAP`,
`_ANIME_INSIDE_TONE`, `_ANIME_INSIDE_FADE`. Outline pass: `_ANIME_OUTLINE_BAKED`, `_ANIME_OUTLINE_VCOLOR`,
`_ANIME_OUTLINE_BASETINT`, `_ALPHATEST_ON`. Scene keywords as URP's Lit has them, fragment-only where the vertex stage
doesn't read them (main light shadows, soft shadow quality, additional lights and their shadows, Forward+, light layers,
rendering layers, fog): about 2560 fragment combinations before the render pipeline strips what its assets never use,
times at most 16 material combinations. Not supported: GPU instancing, lightmaps, reflection probes, decals, vertex
lights, light cookies, LOD cross-fade, and a motion vectors pass (object motion blur and TAA would see characters as
still).

### Two-sided cloth and the inside tone

Set `_Cull` to Off. Back faces (`FRONT_FACE_TYPE`, correct on mirrored rigs) are lit from their own side with the normal
flipped. With the inside tone on, the back face's base colour is tinted by `_InsideTint`, its saturation scaled by
`_InsideSaturation` and darkened by `_InsideDarken`; the fade option ramps that from 0 to full along the U or V axis
between `_InsideFadeStart` and `_InsideFadeEnd`. The shadow caster, depth and depth-normals passes follow `_Cull`.

### The depth-edge rim

Add **`AnimeRimFeature`** to each URP renderer that should show it (Forward or Forward+; the camera depth texture on,
which the feature asks for). It draws, after the skybox, every opaque material whose `AnimeRim` pass is on, additively
over the camera colour, reading URP's own depth copy (scenery and every body), so no extra target and no prepass; the
pass is enqueued every frame and draws nothing where no such body is in view. Each pixel reads the depth past the body's own line, `_RimDepthWidth` pixels (at
1080p, held on screen to the line's far distance, then thinning, never under one pixel) along its normal; where it is
farther than `_RimDepthThreshold` metres (scaled up past 10 m) the pixel takes `_RimColor` times `_RimStrength`, faded
by how much the edge faces the light (`_RimLightMask`). Turn it on with **Use Depth Edge Rim** or
`AnimeMaterials.SetRimDepth` (the switch and the pass together, so a material without it costs nothing). A fading body
takes no rim; a camera whose renderer lacks the feature draws none. XR single-pass is not handled. The fresnel rim
(**Use Fresnel Rim**) is the forward pass's own and needs no feature.

### The world: `Anime/World`, `Anime/Ground`, `Anime/Sky` and sky presets

- **`Anime/World`** (buildings and props): the core with the band and emission groups; cast shadows per surface; no
  line, rim or fade (`ANIME_NO_FADE`: no clip, so tile-based GPUs keep early depth rejection); **Base Map In World XZ**
  tiles the base map by metres (its tiling and offset per metre), so a large plane needs no UVs.
- **`Anime/Ground`** (the ground group, through `ANIME_CUSTOM_SURFACE`): two layers with the same settings, the
  **Field** and, inside an optional disc (centre x, centre z, radius, edge wobble; radius 0 for none), the **Circle**.
  Each is painted from a packed detail map in world XZ (its tile size in metres): the map's shade picks a colour between
  the layer's **Dark** and **Light**, or its **Second** pair where the map's mask is off (blended in a perceptual space),
  optionally in **Shade Steps**; its normal tilts the surface by **Normal Strength**; **Break Up Tiling** blends a second
  read at another scale and angle by the noise, keeping the contrast, so a large field shows no repeat. The baked noise
  adds soft patches (**Patch Strength**) and bare earth on the field; the field's **Patch Tint** (its alpha the strength;
  its own scale, cover and softness) gives broad patches the tint's hue at the colour's own brightness. Reads: the noise
  once (twice with a patch tint), then only the layer that shows (once, or twice with break-up): 2 a pixel inside the
  circle, 3 outside, up to 5 on the edge band, and one point read of a layer's stone map while it varies stones
  (**Stones**, below); the reads sit
  in branches with their gradients taken before them, each map with its own sampler; the steps, painting and tilt run
  after. Measured on the playground fight at 4K (2026-11-16): 10.1 ms median with the branches, 10.6 without. Far off
  (**Detail Fade**) the normal flattens and the steps smooth; the shade is left to the mips, which settle on each map's
  mean of 0.5. Shade Steps are whole numbers; an Edge Softness or Tile Size of 0 is held to a small minimum. The
  depth-normals pass keeps the geometric normal (screen-space effects don't see the detail); the alpha clip runs after
  the surface hook. The material inspector warns under a detail slot holding a texture that is not a packed map.
  Breaking change from 0.1.0's procedural ground: its grass, stone and grout properties (and their ids) are gone.
- **Detail sets** (Create > Anime Shaders > Detail Set): a photo-scanned surface's colour and normal maps, and
  optionally its occlusion and a mask (read from their files' bytes, so their import settings never matter; OpenGL or
  DirectX normals), packed by **Pack** into one map beside the set: R the shade (the colour's brightness, softened by
  **Soften**, a percentage of the tile so it looks the same at any resolution, darkened by occlusion by **Occlusion
  Weight**, stretched over 0..1 and curved to a mean of 0.5), G the mask, B and A the normal's X and Y. Every step
  wraps, so a tiling set packs to a tiling map. The Inspector says when the packed map is out of date (a hash of the
  source files' bytes and the settings), what it costs in video memory, and warns above 2048 square (a 2K map packs in
  seconds; the import rule's version, if raised, reimports every texture in the project). The packed map is held linear, compressed at high quality, mipmapped and repeating by its
  label. Its four channels compress worse than one: measured on the playground's sets (BC7 and ASTC 4x4 alike), a
  smooth stone keeps about 3° of normal and 4 of 255 shade levels, a noisy grass about 9° and 11; a set that needs better
  can be packed without its mask, or its normal split out (not built).
- **Stones** (a detail set's Stones section, for any tiled set whose joints are darker in its occlusion: paving, tiles,
  bricks, flagstones): Pack also writes a **stone map** beside the set (`<set>_Stones.png`, 512 square, 1 MB,
  uncompressed, point-sampled, no mips, label `AnimeStones`). The stones are found at the occlusion's own resolution
  (thin joints vanish when averaged down): **Stone Blur** (percent of the tile) so pitting doesn't split a stone,
  **Stone Threshold** (above it stone, below it joint), the tile seen as a torus so a stone cut by the tile's edge is one
  stone, pieces under **Stone Min Size** joined to a neighbour, every joint texel given its nearest stone, a stone that
  wraps onto itself or spans the whole tile dropped. Each texel holds R its stone's value (1 to 255) and G a code for
  the tile holding the stone's centre relative to the texel's own copy (each axis -1, 0 or 1; 0 none): exact, so both
  pieces of a stone across the tile's edge agree. The Inspector shows the stone count and warns with no occlusion, no
  stones, or over 400 (pitting splitting them). Turning Stones off removes the map. On the ground: the layer's **Stone
  Map**, **Stone Shade** (each stone's shade moves by up to this) and **Stone Mix** (that share of stones painted with
  the layer's Second pair, which a mask also uses). The shader hashes the stone's home tile with its value, so every
  stone on the floor differs and the tile stops repeating; the variation fades in the joints (by the shade), with
  distance (Detail Fade) and where Break Up Tiling blends a second read. Cost: one point read and an integer hash a
  pixel, only on a layer with Stone Shade or Mix above 0; nothing otherwise. To use it on another texture: give its
  detail set the occlusion map, tick Stones, Pack, read the count against the stones you see (raise the blur if it is
  far above, raise the threshold if neighbours merge), then set the layer's Stone Map, Shade and Mix.
- **`Anime/Sky`** (the skybox): the gradient; the **cloud field** where the view ray meets its plane (a world plane at
  the clouds' altitude, so they parallax with the camera; two reads of the noise that never line up, so the field never
  repeats; drawn shapes with an anti-aliased edge and one shade band
  away from the sun; the same field the ground's cloud shadows read); stars at night; the sun's halo and disc where the
  main light points (or the director's override); the haze at the sky's distance; and up to three **ridges** of hills on
  the horizon, drawn analytically (no texture), farthest first, each hazed at its own distance, lit softly along its
  line where it faces the sun, hiding the sun behind it, its foot dissolving into the haze over the sky (the **mist**:
  how high it reaches and how thick, in the preset), so the far ground meets the hills softly. Everything but the disc stays under **Brightest Sky**. Every
  noise read is in uniform flow.
- **Cloud shadows** (`Core/AnimeClouds.hlsl`, in the lighting): the cloud field read where the ray toward the sun meets
  its plane, so a shadow lies under its cloud; it dims the lit tone toward the shadow tone softly (by the cloud's own
  edge, not the ramp; not faded by the shadow distance); a body reads it once at its body-shadow point; a material's
  **Cloud Shadow** sets how much it takes; none within the preset's clear radius round the camera, so a fight never
  changes light; the sky's ridges take it where they stand. Behind the global dynamic branch `_ANIME_CLOUD_SHADOWS`
  (on while a preset has cover and strength): off costs nothing, on one noise read a pixel.
- **Contact shading** (the lighting): URP's screen-space ambient occlusion, when a renderer has its feature, through the
  tone step: the occlusion below a material's **Contact Threshold** turns the lit tone to the shadow tone (a crisp shade
  where things meet, never a smudge), and it dims the ambient; both by the material's **Contact Shading** (low on
  characters by default, so they read bright; full on the world). The forward passes compile `_SCREEN_SPACE_OCCLUSION`;
  the renderer's SSAO settings (intensity, radius) decide how deep the occlusion runs, the threshold where it shows.
- **Sun shafts** (`AnimeSunShafts`, a volume override: Add Override > Anime Shaders > Sun Shafts; drawn by
  **`AnimeSunShaftsFeature`** on the renderers that have it): at half resolution, the sky brighter than **Threshold**
  (near the sky's bloom ceiling: the sun and its hottest glow, so clouds and bodies crossing it break the light into
  rays), where all four depths under a texel are the far plane and weighted by **Reach** round the sun on screen, blurred
  toward the sun twice (**Length**, **Decay**, **Samples**), added before the post in the sun's colour times **Tint** and
  **Intensity**, faded by the sun's angle from the view. Nothing is recorded at intensity 0, with the sun behind, or on a
  camera with post-processing off (menu figures, portraits). Game and Scene view cameras; MSAA unsupported.
- **The haze** (`Core/AnimeFog.hlsl`): distance and height fog in the horizon colour, glowing toward the sun, applied
  per pixel by the pack's forward and outline passes in place of URP's fog while a preset turns it on (the global
  dynamic branch `_ANIME_ATMOSPHERE`); exact on orthographic cameras and transparents; off, URP's fog as before. The sky
  reads it at the sky's distance and each ridge at its own, so far ground, ridges and sky meet.
- **Sky presets** (`SkyPreset` assets: Day, Golden Hour, Dusk, Night in `Runtime/Sky/Presets/`): one `SkyState` each:
  the sky's colours, the sun, the cloud field (cover, size, simplicity, softness, altitude, drift, shade, shadow
  strength), the ridges, the stars, the ambient (Trilight) and the haze (start, density, base height and falloff, most
  opacity, the glow toward the sun, the sky's distance). `SkyApply.Apply` is the one writer: the sky material, the sun
  light, the scene's render settings (reflections off; URP's fog set to the haze's colour and reach for shaders outside
  the pack) and the **world's globals** (`AnimeShaderIds.World`: the cloud field, the haze, the clouds' noise and two
  keywords). **`SkyPresetApplier`** applies one on enable in Edit and Play Mode; no preset (Authored) leaves the scene
  alone; in Edit Mode it writes the sky material itself, in Play Mode a copy (the material stays as authored) and
  switched off it puts back everything it changed, the sun and reflection included; switched off in either mode it
  clears the world's globals. Never applied mid-fight. The light director (when built) becomes its caller.

### The light director's globals

Never set, all are zero and mean no change. Set them with `Shader.SetGlobalVector` (names and ids in
`AnimeShaderIds.Director`).

| Global | Encoding |
|---|---|
| `_AnimeDirectorLightDirection` | xyz: world direction toward the light; w: weight 0..1 (0 = the scene's main light). Steers tones, rim and additional lights; cast shadows still come from the real light |
| `_AnimeDirectorLightColor` | rgb: light colour multiplier minus 1 |
| `_AnimeDirectorShadowColor` | rgb: shadow tone multiplier minus 1 |
| `_AnimeDirectorAmbient` | rgb: ambient colour; w: weight 0..1 of replacing the probe's ambient |
| `_AnimeDirectorScalars` | x: cast shadow strength multiplier minus 1; y: rim strength multiplier minus 1; z: line width multiplier minus 1; w: reserved |

### The dither fade

Set per renderer with `Renderer.SetShaderUserValue(uint)`; it costs no property block and keeps the SRP Batcher. The low
8 bits are the fade level: 0 = solid (what an unset renderer holds), 255 = fully faded; higher bits are reserved.
Coverage is `1 - level / 255` through a fixed screen-space 4x4 Bayer matrix, so a rising level only removes pixels and
nothing flickers. The forward, outline, shadow, depth and depth-normals passes all read it: a fading body's shadow fades
with it, and a fully faded renderer draws no line. Constants: `AnimeShaderIds.Fade`.

### The outline

Add the asset label `AnimeOutlineNormals` to a model and reimport it (labels do not trigger a reimport). The baker
averages normals of vertices at the same quantised position across the whole mesh (weighted by corner angle, across
submeshes so a material seam doesn't crack the line) and writes
them to UV3 as a vector in each vertex's own tangent space (x along the tangent, y along the bitangent, z along the
normal), so the line follows skinning; a mesh without tangents gets built tangents; lighting normals are untouched. Turn on
*Line From Baked Normals* (`_ANIME_OUTLINE_BAKED`) on a material whose meshes were baked; off (the default), the line
follows the vertex normals. The channel is only read with the toggle on: a mesh without UV3 does not read as zero on
every graphics API (Metal reads garbage), so the shader never guesses. Width is in thousandths of the screen height at the view
depth clamped to `_OutlineDistanceMin`..`_OutlineDistanceMax`, times the director's multiplier. The Z offset pushes the
hull away from the camera along the view ray. Width and Z offset can read vertex colour channels (`_ANIME_OUTLINE_VCOLOR`,
off by default, because meshes' vertex colours often mean something else). Disable the line on a material with
`material.SetShaderPassEnabled("SRPDefaultUnlit", false)`.

## Credits and sources

Built from public write-ups of anime rendering (Arc System Works' toon line talk, community breakdowns of modern anime
games, Unity's own toon shader). No code is copied from any GPL source.
