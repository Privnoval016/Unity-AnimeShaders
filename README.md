# Unity-AnimeShaders

Anime (non-photorealistic) shaders for Unity's Universal Render Pipeline that can be dropped into any project: a clean
two-tone look in the manner of modern anime games, outlines that hold up on two-sided cloth, faces that shade cleanly
as the head turns, and a light director so a project can set a mood per scene and relight a single shot. When it is
done, everything will be tuned through a custom inspector with presets, with nothing needing code.

**Status: in construction (0.1.0).** The lit core and the character shader are written; the face, hair, world and
foliage shaders, the inspector, the director's runtime and presets arrive in the order of the table below, each
proved before the next.

- **Requires:** Unity 6, URP 17, the Forward or Forward+ renderer with depth priming off. Deferred is not supported
  (it would draw the line but not the body), and depth priming would test the line against the body's primed depth and
  drop it.
- **Platforms:** D3D11/12, Vulkan, Metal, OpenGL Core and GLES3; the tests compile every pass for each of them from any
  Editor.
- **Namespace:** `AnimeShaders` (editor tools: `AnimeShaders.EditorTools`).
- **Written by hand in HLSL**, every material property in one per-material block so the SRP Batcher holds, material
  toggles as local keywords so a feature turned off costs nothing.

## What it will hold (the core and `Anime/Character` exist; the rest is planned)

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

## What exists now (0.1.0)

| File | What it is |
|---|---|
| `Shaders/AnimeCharacter.shader` | `Anime/Character`: passes ForwardLit (`UniversalForward`), Outline (`SRPDefaultUnlit`, Cull Front), ShadowCaster, DepthOnly, DepthNormals. No Meta pass (characters are not baked) |
| `Shaders/Includes/AnimeProperties.hlsl` | The one `UnityPerMaterial` block every pass includes (SRP Batcher), and the dynamic-branch keyword macros. An entry shader adds its own properties by defining `ANIME_ENTRY_PROPERTIES` in its `HLSLINCLUDE` |
| `Shaders/Includes/AnimeDirector.hlsl` | The light director's global properties (below) |
| `Shaders/Includes/AnimeSurface.hlsl` | The surface struct, base and emission sampling, alpha clip, the inside tone |
| `Shaders/Includes/AnimeLighting.hlsl` | The ramp, cast shadows folded into it, the second band, additional lights, the rim, emission, ambient: `AnimeShade` |
| `Shaders/Includes/AnimeForwardPass.hlsl`, `AnimeShadowCasterPass.hlsl`, `AnimeDepthOnlyPass.hlsl`, `AnimeDepthNormalsPass.hlsl` | One file per pass: structs, vertex, fragment |
| `Shaders/Includes/AnimeOutline.hlsl` | The hull pass: baked-normal reconstruction, screen-consistent width, Z offset |
| `Shaders/Includes/AnimeFade.hlsl` | The dither fade |
| `Runtime/AnimeShaderIds.cs` | Every global and material property name and id, the toggles, the keywords, the line's light mode, and the fade constants |
| `Runtime/AnimeMaterials.cs` | Setting a feature (its keyword and its toggle together), receiving shadows, the line pass on or off |
| `Editor/OutlineNormalBaker.cs` | The import-time outline normal bake |
| `Editor/AnimeShaderGUI.cs` | The material inspector (thin for now): the shader's sections, a live preview of the two-tone ramp (the same maths the shader runs, tested against a render), and each feature's settings shown only while the feature is on |
| `Tests/Editor/` | `Support/` (`LitStage`: a preview scene, camera and light rendering into a linear float target; meshes, materials with colours as the shader reads them, frame reads, source readers, the director's globals held neutral, the batcher check), `Shader/` (passes, batcher, names and constants, every platform's compile, and `Probe/`: an entry shader adding its own property), `Inspector/` (the feature map, the preview against a render), `Look/` (tones, band, rim, director, cast shadows, inside tone, fade), `Outline/` (the line, the baker) |

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
