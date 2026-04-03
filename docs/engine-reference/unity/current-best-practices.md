# Unity 6.3 — Current Best Practices

Last verified: 2026-04-03

## Render Graph (Required)

URP Compatibility Mode is removed in 6.3. All custom render passes must use the
Render Graph API (`AddRenderPasses` / `RecordRenderGraph`). The Render Graph Viewer
now connects to device builds for real-time profiling on mobile/XR.

Both URP and HDRP share the same Render Graph compiler backend.

## 2D Development

- **Box2D v3** is integrated with multi-threaded performance. Access low-level APIs
  via `UnityEngine.LowLevelPhysics2D` namespace.
- **2D Renderer** now supports Mesh Renderer and Skinned Mesh Renderer alongside
  sprites — useful for mixing 3D elements in a 2D game.
- **Bloom filtering**: Use Kawase or Dual filtering in URP for better mobile performance.

## Per-Renderer Shader Variations

Use `unity_RendererUserValue` to apply shader variations on a shared material
per-renderer, avoiding material duplication.

## UI Toolkit

- SVG assets import directly as Vector Images (no separate package needed).
- USS filters for post-processing effects (opacity, tint, grayscale, blur) on UI sub-trees.
- Use the new UI Test Framework package for automated UI testing.
- `[field: SerializeField]` syntax required for auto-properties.

## Audio

New scriptable audio processors allow Burst-compiled C# audio processing at
specific integration points. Consider for custom SFX processing.

## Profiling

- Use the new Captures List in the Profiler for stored sessions.
- Highlights module provides GPU/CPU resource analysis and category breakdowns.
- Adaptive Performance is now a core module (no extra package needed).

## Build Profiles

Use the improved Build Profiles window to configure only the settings you need
per-platform via the Add Settings button.
