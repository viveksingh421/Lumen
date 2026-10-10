# LUMEN – Computer Graphics Concepts

Every concept below is implemented **by hand** (no Unity helper doing the maths for us) and is visible/toggleable in the game.
All code is under `Assets/_Lumen/Scripts/` (C#) and `Assets/_Lumen/Shaders/` (URP HLSL). Namespace: `Lumen`.

Open the **CG Lab Panel** with `Tab` while playing – the game slows to 0.25x so you can watch an effect.

---

## 1. Geometric transformations (2D/3D)
**Theory.** Points are lifted to homogeneous coordinates `(x,y,z,1)`; every affine transform is a 4×4 matrix and a chain of
transforms is a matrix product (rightmost applied first).

```
Translate  | 1 0 0 tx |   Scale | sx 0 0 0 |   RotZ(a) | c -s 0 0 |   Reflect(n): p' = p - 2(p·n)n
           | 0 1 0 ty |         | 0 sy 0 0 |           | s  c 0 0 |   Shear: off-diagonal terms
           | 0 0 1 tz |         | 0 0 sz 0 |           | 0  0 1 0 |   TRS = T · Ry · Rx · Rz · S
           | 0 0 0 1  |         | 0 0  0 1 |           | 0  0 0 1 |
```
**Code.** `Transformations/MatrixMath.cs` → `Translate, Scale, RotateX/Y/Z, Shear, Reflect, TRS, MultiplyPoint, Lerp, Format`
(all filled element-by-element). Used by `World/Shard.cs` (`M = T·Ry(spin)·Rx(tilt)·S(pulse)` → `Graphics.DrawMesh`),
`World/Portal.cs`, `Core/MenuController.cs`.
**See it.** Shards spin/bob/pulse. Lab Panel → *Transformation matrix of nearest shard* shows its live 4×4 matrix.

## 2. Viewing & projection
**Theory.** Perspective maps the view frustum to the clip cube with `w = -z` (divide gives foreshortening); orthographic is a
parallel projection (`w = 1`). Both are 4×4 matrices; matrices can be interpolated component-wise.

```
Perspective: m00 = 1/(aspect·tan(fov/2)), m11 = 1/tan(fov/2), m22 = -(f+n)/(f-n), m23 = -2fn/(f-n), m32 = -1
Ortho:       m00 = 2/(r-l), m11 = 2/(t-b), m22 = -2/(f-n), m03 = -(r+l)/(r-l), m13 = -(t+b)/(t-b), m23 = -(f+n)/(f-n)
```
**Code.** `Projection/ProjectionController.cs` → `Perspective(), Orthographic(), LateUpdate()` (assigned to `camera.projectionMatrix`).
The ortho box is sized to the player's focus distance (`halfHeight = d·tan(fov/2)`) so the morph looks like a dolly-zoom.
**See it.** `P` (1 s animated matrix lerp). Lab Panel → *Projection*: ortho toggle + FOV slider.

## 3. Scan-conversion / rasterisation
**Theory.** Convert geometric primitives to pixels using incremental integer-friendly decisions.
* **DDA:** `steps = max(|dx|,|dy|)`, add `dx/steps, dy/steps`, round each step.
* **Bresenham:** integer error term `err = dx + dy`; `e2 = 2·err; if e2 ≥ dy: x += sx; if e2 ≤ dx: y += sy`.
* **Midpoint circle:** decision `p = 1 − r`, 8-way symmetry. **Midpoint ellipse:** two regions (slope < 1 / ≥ 1), 4-way symmetry.
* **Scan-line fill:** intersect each scan line with the polygon edges, sort x, fill between pairs (even-odd).

**Code.** `Raster/LineDDA.cs`, `LineBresenham.cs`, `MidpointCircle.cs`, `MidpointEllipse.cs`, `ScanlineFill.cs`, framebuffer
`PixelCanvas.cs`, and `RadarRenderer.cs` (the 256×256 `Texture2D` minimap redrawn every 3 frames).
**See it.** The radar (top-right). Lab Panel → *Radar rasterisation*: DDA ↔ Bresenham toggle, pixel counts and a timing benchmark of both.

## 4. Clipping
**Theory.** *Cohen-Sutherland:* 4-bit outcodes (L,R,B,T); both 0 → accept, AND ≠ 0 → reject, else move an outside endpoint to the
window edge (`x = x0 + (x1−x0)(yEdge−y0)/(y1−y0)`) and repeat. *Sutherland-Hodgman:* clip a polygon against one window edge at a
time, with the four in/out cases for each edge `S→E`.
**Code.** `Clipping/CohenSutherland.cs → Clip()`, `Clipping/SutherlandHodgman.cs → ClipRect()`; called from `RadarRenderer.Line / Cone / Marker`.
**See it.** Lab Panel → *Show UNCLIPPED geometry*: the sentinel view cones and lines then spill out of the radar frame. Off-radar shards/sentinels
are pinned to the window edge (orange) using the clipped segment end-point.

## 5. Curves
**Theory.** Cubic Bézier, two equal evaluations:
`B(t) = (1−t)³P0 + 3(1−t)²t P1 + 3(1−t)t² P2 + t³P3` (Bernstein) and repeated lerp of the control polygon (**de Casteljau**).
Closed chains with Catmull-Rom tangents `d_i = (A_{i+1} − A_{i−1})/2`, handles `A_i ± d_i/3` are C1-smooth. **Catmull-Rom spline** passes through its points:
`C(t) = ½[2P1 + (−P0+P2)t + (2P0−5P1+4P2−P3)t² + (−P0+3P1−3P2+P3)t³]`.
**Code.** `Curves/Bezier.cs → DeCasteljau, Bernstein, Tangent, BuildClosedChain, EvaluateChain`; `Curves/CatmullRom.cs`;
`Curves/CurveVisualizer.cs` (LineRenderers); patrol in `Enemies/Sentinel.cs`; intro camera in `Core/GameManager.cs → UpdateIntro`.
**See it.** `B`: Sentinel Bézier loops (red), control polygon, anchors (yellow) and handles (magenta), plus the green Catmull-Rom intro path.

## 6. Procedural mesh, texturing & noise
**Theory.** Perlin gradient noise: random unit gradients on an integer lattice, dot products with offset vectors, blended with the
quintic fade `6t⁵−15t⁴+10t³`. fBm sums octaves: `Σ persistence^i · noise(p · lacunarity^i)`. Smooth vertex normals: sum the triangle
face normals `(b−a)×(c−a)` into each vertex and normalise.
**Code.** `Procedural/PerlinNoise.cs`, `TerrainGenerator.cs` (200×200 vertices, manual normals/UVs/height-gradient vertex colours),
`ProceduralTextures.cs` (neon grid texture, soft particle sprite), `MeshFactory.cs` (crystal, disc). UV tiling/scrolling in `LumenLit.shader` / `GridScroll.shader`.
**See it.** Seed on the HUD; `R` (Lab Panel open) or the *Regenerate world* button builds a new seed.

## 7. Illumination & shading models
**Theory.** `I = ka·Ia·albedo + Σ_lights [kd·albedo·max(N·L,0) + ks·spec]·radiance·att`.
* **Flat** – one normal per triangle (`cross(ddx(P), ddy(P))`). **Gouraud** – lighting in the vertex shader, colour interpolated.
* **Phong** – per-pixel, `spec = (R·V)^n`, `R = reflect(−L, N)`. **Blinn-Phong** – `spec = (N·H)^n`, `H = normalize(L+V)`.
* **Toon** – `N·L` quantised into bands, binary highlight, rim light, inverted-hull outline pass.
* Point light (the orb) attenuation: `att = 1/(a + b·d + c·d²)`.

**Code.** `Shaders/LumenLit.shader` (`Shade`, `LightTerm`, outline pass), `Rendering/ShadingModeController.cs` (`Shader.SetGlobalFloat("_LumenMode")`, global ka/kd/ks/shininess).
**See it.** Keys `1`–`5`. Lab Panel sliders: ka, kd, ks, shininess, orb light intensity.

## 8. Hidden-surface removal / Z-buffer
**Theory.** The Z-buffer stores per pixel the depth of the nearest fragment; a fragment is kept only if closer. Hardware depth is non-linear
(`~1/z`, reversed on modern GPUs), so to display it we linearise: perspective `eye = 1/(zb.z·d + zb.w)`, orthographic `eye = near + (1−d)(far−near)`; grey = `1 − eye/far`.
**Code.** `LumenLit.shader → ZBufferView()` (uses each fragment's own hardware depth `SV_Position.z`), toggled by `Rendering/DebugViews.cs`. Back-face culling toggle: `ShadingModeController.ApplyCull()`.
**Deviation note.** The spec asks to sample the *camera depth texture* in a full-screen pass. In this URP 17 / RenderGraph setup the copied depth texture was not visible to a
custom overlay pass, so the closest equivalent is used: the very value the depth test writes (`SV_Position.z`) is linearised and printed by the lit shader itself.
**See it.** `Z`. Lab Panel → *Back-face culling*.

## 9. Wireframe & normals
**Theory.** A geometry shader emits each triangle with barycentric coordinates `(1,0,0) (0,1,0) (0,0,1)`; after interpolation the smallest component is ≈0 on an edge, `fwidth()` gives constant pixel-width lines.
**Code.** `Shaders/Wireframe.shader` (overlay via `Graphics.DrawMesh` in `DebugViews.DrawWireframe`); vertex normals as a line mesh `DebugViews.RebuildNormals` + `Shaders/LineUnlit.shader`.
**See it.** `F` wireframe, `N` normals (within 16 units of the orb).

## 10. Animation
**Theory.** Keyframes interpolate scale (lerp) and rotation (**slerp**: `sin((1−t)Ω)/sinΩ·a + sin(tΩ)/sinΩ·b`, shortest arc via sign flip) with easing curves
(linear, smoothstep `3t²−2t³`, cubic Hermite `h00p0 + h10m0 + h01p1 + h11m1`). Squash & stretch preserves volume: `sx = sz = 1/√sy`.
**Code.** `Animation/KeyframeAnimator.cs`, `Easing.cs`, `QuaternionMath.cs` (used for Sentinel turning in `Enemies/Sentinel.cs`); portal in `World/Portal.cs`;
orb squash in `Player/OrbController.AnimateScale`; shader waves in `Shaders/PortalWave.shader` (vertex displacement `A·sin(kr − ωt)`) and UV scrolling in the grid shaders.
**See it.** Collect all 12 shards: the portal opens with a keyframed scale/rotation. Jump and land for squash & stretch.

## 11. Particle systems
**Theory.** Emitter + per-particle lifetime, velocity and colour/size-over-lifetime curves; additive blending `out = src·α + dst`.
**Code.** `Rendering/ParticleFactory.cs` – `Burst` (shard pickup, pulse impact, hit), `Trail` (orb), `PortalSwirl`, `Dust`; shader `Shaders/ParticleAdditive.shader`. No assets.

## 12. Ray casting & collision
**Theory.** Ray–sphere: solve `t² + 2(d·m)t + (m·m − r²) = 0`, `m = o − c`. Ray–AABB slab test: `tNear = max(t1)`, `tFar = min(t2)`, hit if `tNear ≤ tFar`. Sphere–sphere: `|c1−c2|² < (r1+r2)²`.
View cone: `dot(forward, dirToTarget) ≥ cos(halfAngle)` plus a line-of-sight march against the terrain height-field.
**Code.** `Collision/RayIntersect.cs` (`RaySphere, RayAABB, SphereSphere, InViewCone`); `Player/PulseWeapon.cs` (AABB broad phase → sphere narrow phase);
`World/Shard.cs` (pickup); `Enemies/Sentinel.CanSee / TerrainLineOfSight`. `Physics` is used only for ground contact.
**See it.** Left mouse fires the pulse; the Lab Panel shows the last ray origin/direction and hit result.

## 13. Post-processing & colour
**Theory.** Bloom = bright-pass + blur + add; vignette = radial darkening; HSV conversion `V = max, S = (max−min)/max, H` from the dominant channel, then rotate H and convert back.
**Code.** `Rendering/PostFX.cs` (URP Volume built in code: Bloom, Vignette, ColorAdjustments, ACES tonemapping); `Shaders/HueShift.shader` (hand-written RGB→HSV→RGB full-screen effect), driven by `DebugViews`.
**See it.** `H` toggles the hue shift; it also fires automatically for 1 s whenever a Sentinel hits you. Lab Panel → Bloom / Vignette / Colour grading toggles.

---

## Concept → file map

| # | Concept | Main files |
|---|---|---|
| 1 | Transformations | `Transformations/MatrixMath.cs`, `World/Shard.cs` |
| 2 | Projection | `Projection/ProjectionController.cs` |
| 3 | Rasterisation | `Raster/*.cs` |
| 4 | Clipping | `Clipping/CohenSutherland.cs`, `Clipping/SutherlandHodgman.cs` |
| 5 | Curves | `Curves/Bezier.cs`, `CatmullRom.cs`, `CurveVisualizer.cs` |
| 6 | Procedural | `Procedural/PerlinNoise.cs`, `TerrainGenerator.cs`, `ProceduralTextures.cs` |
| 7 | Shading | `Shaders/LumenLit.shader`, `Rendering/ShadingModeController.cs` |
| 8 | Z-buffer / culling | `Shaders/LumenLit.shader (ZBufferView)`, `Rendering/DebugViews.cs` |
| 9 | Wireframe / normals | `Shaders/Wireframe.shader`, `Rendering/DebugViews.cs` |
| 10 | Animation | `Animation/*.cs`, `World/Portal.cs`, `Shaders/PortalWave.shader` |
| 11 | Particles | `Rendering/ParticleFactory.cs`, `Shaders/ParticleAdditive.shader` |
| 12 | Ray casting | `Collision/RayIntersect.cs`, `Player/PulseWeapon.cs`, `Enemies/Sentinel.cs` |
| 13 | Post / colour | `Rendering/PostFX.cs`, `Shaders/HueShift.shader` |

Verified by `Lumen → Run Algorithm Tests` (`Assets/_Lumen/Editor/AlgorithmTests.cs`): custom `TRS` equals `Matrix4x4.TRS`, de Casteljau equals Bernstein, Bresenham endpoints / pixel count,
circle/ellipse axis points, scan-line fill, both clippers, slerp midpoint, ray tests, Perlin determinism.
