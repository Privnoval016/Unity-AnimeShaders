# Unity-AnimeShaders

Anime (non-photorealistic) shaders for Unity's Universal Render Pipeline that can be dropped into any project: a clean
two-tone look in the manner of modern anime games, outlines that hold up on two-sided cloth, faces that shade cleanly
as the head turns, and a light director so a project can set a mood per scene and relight a single shot. Everything is
tuned through a custom inspector with presets; nothing needs code.

**Status: in construction.** This is the skeleton. The shaders, the inspector and the tools arrive in the order below,
each proved before the next.

- **Requires:** Unity 6, URP 17 (Forward or Forward+).
- **Namespace:** `AnimeShaders` (editor tools: `AnimeShaders.EditorTools`).
- **Written by hand in HLSL**, every material property in one per-material block so the SRP Batcher holds, material
  toggles as local keywords so a feature turned off costs nothing.

## What it will hold

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

## Credits and sources

Built from public write-ups of anime rendering (Arc System Works' toon line talk, community breakdowns of modern anime
games, Unity's own toon shader). No code is copied from any GPL source.
