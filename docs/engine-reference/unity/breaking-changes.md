# Unity 6.x — Breaking Changes

Last verified: 2026-04-03

## Unity 6.3 LTS (from 6.2)

### Graphics / URP
- **URP Compatibility Mode removed** — Code stripped by default. Must use Render Graph API.
  Temporary workaround: `UPM_COMPATIBILITY_MODE` scripting define (not supported in 6.4+).
- **Legacy ETC Compressor removed** — Projects auto-convert to default ETC compressor.
  Textures may differ visually.
- **Experimental Lightmapping `AdditionalBakedProbes` removed** — Use `LightTransport.IProbeIntegrator`.
- **`CustomBake` API obsolete** — Use `LightTransport.IProbeIntegrator`.

### Scripting
- **`SerializeField` restricted to fields only** — Applying to properties/methods causes
  compile-time error. Use `[field: SerializeField]` for auto-properties.

### UI Toolkit
- **USS Parser upgraded** — Stricter validation; previously ignored invalid USS now causes errors.

### Accessibility
- **`AccessibilityRole` no longer a Flags enum** — Remove bitwise operations.
- **`AccessibilityRole` and `AccessibilityState` changed from `int` to `byte`** — Recompile precompiled assemblies.
- **`AccessibilityNode.selected` deprecated** — Use `AccessibilityNode.invoked`.

### Multiplayer
- **Netcode for GameObjects 1.X deprecated** — Use 2.X.
- **`NetworkTransform.Update` cannot be overridden** — Override `NetworkTransform.OnUpdate` instead.
- **Multiplay Hosting removed from Editor/runtime** — Shutdown March 31, 2026.

### Platforms
- **Android**: Minimum API level now 25. Round/legacy icons deprecated, use adaptive icons.
  New App Category setting replaces `androidIsGame`.
- **Web**: Facebook Instant Games deprecated, use Web platform.

### Packages
- **`UPM_NPM_CACHE_PATH` removed** — Use `UPM_CACHE_ROOT`.
- **Search Index Manager UI removed** — Use Preferences > Search > Indexing.
- **Adaptive Performance**: Moved from package to core module.

## Unity 6.0 (from 2023.x — major version jump)

### Render Pipeline
- **Render Graph is now the default** for URP/HDRP custom passes.
- **`ScriptableRenderPass.Execute` deprecated** — Use `RecordRenderGraph` with Render Graph API.
- **`SetupRenderPasses` deprecated** — Use `AddRenderPasses`.

### UI Toolkit
- **`ExecuteDefaultAction` / `ExecuteDefaultActionAtTarget` deprecated**.
- **`VisualElement.transform` deprecated**.
- **`AtTarget` dispatching phase deprecated**.

### Other
- **Social API deprecated**.
- **`UPM_CACHE_PATH` environment variable removed**.
