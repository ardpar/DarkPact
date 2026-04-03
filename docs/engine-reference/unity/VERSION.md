# Unity — Version Reference

| Field | Value |
|-------|-------|
| **Engine Version** | Unity 6.3 LTS (6000.3.x) |
| **Release Date** | Early 2026 |
| **Project Pinned** | 2026-04-03 |
| **Last Docs Verified** | 2026-04-03 |
| **LLM Knowledge Cutoff** | May 2025 |

## Knowledge Gap Warning

The LLM's training data likely covers Unity up to ~2023.x / early Unity 6 (6000.0).
Unity 6.1, 6.2, and 6.3 introduced changes that the model may NOT know about.
Always cross-reference this directory before suggesting Unity API calls for newer features.

## Post-Cutoff Version Timeline

| Version | Release | Risk Level | Key Theme |
|---------|---------|------------|-----------|
| Unity 6.0 LTS | Mid 2024 | LOW | Render Graph, GPU Resident Drawer, APV, STP |
| Unity 6.1 | Late 2024 | MEDIUM | Incremental updates on 6.0 |
| Unity 6.2 | Early 2025 | MEDIUM | URP/HDRP improvements, UI Toolkit updates |
| Unity 6.3 LTS | Early 2026 | MEDIUM | Box2D v3, URP Compatibility Mode removed, Render Graph unified |

## Key Changes for This Project (2D Roguelite)

- **Box2D v3 integration**: Multi-threaded 2D physics via `UnityEngine.LowLevelPhysics2D`
- **URP Compatibility Mode removed**: Must use Render Graph API (no fallback)
- **2D Renderer supports Mesh/Skinned Mesh Renderer**: 3D objects in 2D scenes now possible
- **UI Toolkit improvements**: SVG vector graphics, USS filters, UI Test Framework
- **Bloom**: URP Bloom now supports Kawase and Dual filtering (better mobile perf)
- **SerializeField**: Can only apply to fields, not properties/methods

## Verified Sources

- What's new in 6.3: https://docs.unity3d.com/6000.3/Documentation/Manual/WhatsNewUnity63.html
- Upgrade to 6.3: https://docs.unity3d.com/6000.3/Documentation/Manual/UpgradeGuideUnity63.html
- Upgrade to 6.0: https://docs.unity3d.com/6000.2/Documentation/Manual/UpgradeGuideUnity6.html
- Unity 6 releases: https://unity.com/releases/unity-6/support
