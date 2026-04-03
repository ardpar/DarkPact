# Systems Index: Dark Pact

> **Status**: Approved
> **Created**: 2026-04-03
> **Last Updated**: 2026-04-04
> **Source Concept**: design/gdd/GDD_DarkPact.md

---

## Overview

Dark Pact is a top-down hack-and-slash roguelite driven by the Dark Pact mechanic — players form pacts with dark entities, gaining powerful boons at meaningful costs. The game requires 30 systems spanning combat, procedural generation, pact management, loot economy, progression, and presentation. The core loop (pact selection → dungeon → combat → loot → milestone → boss) demands tight integration between pact, combat, and dungeon systems. The darboğaz systems are Health & Damage, Combat System, and Player Controller — these feed into nearly everything else.

---

## Systems Enumeration

| # | System Name | Category | Priority | Status | Design Doc | Depends On |
|---|-------------|----------|----------|--------|------------|------------|
| 1 | Game Manager | Core | MVP | Designed | design/gdd/game-manager.md | — |
| 2 | Input System | Core | MVP | Designed | design/gdd/input-system.md | — |
| 3 | Room/Tilemap System | Core | MVP | Designed | design/gdd/room-tilemap-system.md | — |
| 4 | VFX System | Core | MVP | Designed | design/gdd/vfx-system.md | — |
| 5 | Camera System | Core | MVP | Designed | design/gdd/camera-system.md | Game Manager, Room/Tilemap |
| 6 | Player Controller | Core | MVP | Designed | design/gdd/player-controller.md | Input System, Game Manager |
| 7 | Health & Damage | Gameplay | MVP | Designed | design/gdd/health-damage.md | Game Manager |
| 8 | Item Database | Economy | MVP | Designed | design/gdd/item-database.md | Game Manager |
| 9 | Run Manager | Core | MVP | Designed | design/gdd/run-manager.md | Game Manager |
| 10 | Combat System | Gameplay | MVP | Designed | design/gdd/combat-system.md | Player Controller, Health & Damage, VFX, Audio Manager |
| 11 | Status Effect System | Gameplay | MVP | Designed | design/gdd/status-effect-system.md | Health & Damage, Combat System |
| 12 | Enemy System | Gameplay | MVP | Designed | design/gdd/enemy-system.md | Health & Damage, Room/Tilemap, Combat System |
| 13 | Enemy AI | Gameplay | MVP | Designed | design/gdd/enemy-ai.md | Enemy System, Player Controller |
| 14 | Pact System | Gameplay | MVP | Designed | design/gdd/pact-system.md | Run Manager, Health & Damage, Status Effect System |
| 15 | Equipment System | Economy | MVP | Designed | design/gdd/equipment-system.md | Item Database, Combat System |
| 16 | Loot System | Economy | MVP | Designed | design/gdd/loot-system.md | Item Database, Enemy System, Equipment System |
| 17 | Procedural Dungeon Generator | Gameplay | MVP | Designed | design/gdd/procedural-dungeon-generator.md | Room/Tilemap, Enemy System |
| 18 | Boss System | Gameplay | MVP | Designed | design/gdd/boss-system.md | Enemy System, Enemy AI, Combat System, Procedural Dungeon Generator |
| 19 | HUD | UI | MVP | Designed | design/gdd/hud.md | Health & Damage, Combat System, Pact System, Skill Tree |
| 20 | Pact Selection UI | UI | MVP | Designed | design/gdd/pact-selection-ui.md | Pact System, Run Manager |
| 21 | Synergy Calculator | Gameplay | Vertical Slice | Not Started | — | Pact System |
| 22 | Level-Up System | Progression | Vertical Slice | Not Started | — | Combat System |
| 23 | Skill Tree | Progression | Vertical Slice | Not Started | — | Level-Up System, Pact System |
| 24 | Audio Manager | Audio | Vertical Slice | Not Started | — | — |
| 25 | Inventory/Equipment UI | UI | Vertical Slice | Not Started | — | Equipment System, Item Database |
| 26 | Skill Tree UI | UI | Vertical Slice | Not Started | — | Skill Tree, Level-Up System |
| 27 | Class System | Gameplay | Alpha | Not Started | — | Player Controller, Pact System, Combat System |
| 28 | Save/Load System | Persistence | Alpha | Not Started | — | Game Manager |
| 29 | Meta Progression | Progression | Alpha | Not Started | — | Run Manager, Save/Load System, Class System |
| 30 | Tutorial/Onboarding | Meta | Full Vision | Not Started | — | (all major systems) |

---

## Categories

| Category | Description |
|----------|-------------|
| **Core** | Foundation systems everything depends on — game flow, input, camera, scene management |
| **Gameplay** | Systems that make the game fun — combat, pacts, enemies, dungeon generation |
| **Economy** | Resource creation and consumption — loot, items, equipment |
| **Progression** | How the player grows — XP, skill tree, meta unlocks |
| **Persistence** | Save state and continuity — save/load, settings |
| **UI** | Player-facing information — HUD, pact selection, inventory, skill tree |
| **Audio** | Sound and music — music manager, SFX routing |
| **Meta** | Systems outside the core loop — tutorial, onboarding |

---

## Priority Tiers

| Tier | Definition | Target Milestone | Systems |
|------|------------|------------------|---------|
| **MVP** | Core loop fonksiyonel: 1 akt, 1 class, 5 pakt, 1 boss, ~20 dk run | İlk oynanabilir prototip | 20 sistem |
| **Vertical Slice** | Tam cilalanmış deneyim: sinerji, skill tree, ses, tam UI | Demo / VS | 6 sistem |
| **Alpha** | Tüm mekanikler çalışır: 2. class, save/load, meta progression | Alpha milestone | 3 sistem |
| **Full Vision** | Polish: tutorial, onboarding, accessibility | Beta / Release | 1 sistem |

---

## Dependency Map

### Foundation Layer (no dependencies)

1. **Game Manager** — Oyun akışı, state orchestration, sahne yönetimi
2. **Input System** — Tüm oyuncu etkileşiminin temeli (WASD, mouse, 1-2-3-4)
3. **Room/Tilemap System** — 16x16 tile rendering, oda yapıları
4. **VFX System** — Particle System tabanlı combat feedback
5. **Audio Manager** — Müzik ve SFX altyapısı (VS priority)

### Core Layer (depends on Foundation)

1. **Camera System** — depends on: Game Manager, Room/Tilemap
2. **Player Controller** — depends on: Input System, Game Manager
3. **Health & Damage** — depends on: Game Manager
4. **Item Database** — depends on: Game Manager
5. **Run Manager** — depends on: Game Manager
6. **Save/Load System** — depends on: Game Manager (Alpha priority, genel serializasyon servisi)

### Feature Layer (depends on Core)

1. **Combat System** — depends on: Player Controller, Health & Damage, VFX, Audio Manager
2. **Status Effect System** — depends on: Health & Damage, Combat System
3. **Enemy System** — depends on: Health & Damage, Room/Tilemap, Combat System
4. **Enemy AI** — depends on: Enemy System, Player Controller
5. **Pact System** — depends on: Run Manager, Health & Damage, Status Effect System
6. **Synergy Calculator** — depends on: Pact System
7. **Equipment System** — depends on: Item Database, Combat System
8. **Loot System** — depends on: Item Database, Enemy System, Equipment System
9. **Procedural Dungeon Generator** — depends on: Room/Tilemap, Enemy System
10. **Level-Up System** — depends on: Combat System
11. **Skill Tree** — depends on: Level-Up System, Pact System
12. **Class System** — depends on: Player Controller, Pact System, Combat System
13. **Boss System** — depends on: Enemy System, Enemy AI, Combat System, Procedural Dungeon Generator

### Presentation Layer (depends on Features)

1. **HUD** — depends on: Health & Damage, Combat System, Pact System, Skill Tree
2. **Pact Selection UI** — depends on: Pact System, Run Manager
3. **Inventory/Equipment UI** — depends on: Equipment System, Item Database
4. **Skill Tree UI** — depends on: Skill Tree, Level-Up System

### Polish Layer

1. **Meta Progression** — depends on: Run Manager, Save/Load System, Class System
2. **Tutorial/Onboarding** — depends on: (all major systems)

---

## Recommended Design Order

| Sıra | System | Priority | Layer | Effort |
|------|--------|----------|-------|--------|
| 1 | Game Manager | MVP | Foundation | S |
| 2 | Input System | MVP | Foundation | S |
| 3 | Room/Tilemap System | MVP | Foundation | M |
| 4 | VFX System | MVP | Foundation | S |
| 5 | Camera System | MVP | Core | S |
| 6 | Player Controller | MVP | Core | M |
| 7 | Health & Damage | MVP | Core | M |
| 8 | Item Database | MVP | Core | S |
| 9 | Run Manager | MVP | Core | M |
| 10 | Combat System | MVP | Feature | L |
| 11 | Status Effect System | MVP | Feature | M |
| 12 | Enemy System | MVP | Feature | M |
| 13 | Enemy AI | MVP | Feature | M |
| 14 | Pact System | MVP | Feature | L |
| 15 | Equipment System | MVP | Feature | S |
| 16 | Loot System | MVP | Feature | M |
| 17 | Procedural Dungeon Generator | MVP | Feature | L |
| 18 | Boss System | MVP | Feature | M |
| 19 | HUD | MVP | Presentation | M |
| 20 | Pact Selection UI | MVP | Presentation | S |
| 21 | Synergy Calculator | Vertical Slice | Feature | M |
| 22 | Level-Up System | Vertical Slice | Feature | S |
| 23 | Skill Tree | Vertical Slice | Feature | M |
| 24 | Audio Manager | Vertical Slice | Foundation | S |
| 25 | Inventory/Equipment UI | Vertical Slice | Presentation | S |
| 26 | Skill Tree UI | Vertical Slice | Presentation | S |
| 27 | Class System | Alpha | Feature | M |
| 28 | Save/Load System | Alpha | Core | M |
| 29 | Meta Progression | Alpha | Progression | M |
| 30 | Tutorial/Onboarding | Full Vision | Meta | M |

---

## Circular Dependencies

- **None** — Save/Load System genel serializasyon servisi olarak Core katmanına yerleştirildi, Meta Progression tek yönlü bağımlı. Döngü kırıldı.

---

## High-Risk Systems

| System | Risk Type | Risk Description | Mitigation |
|--------|-----------|-----------------|------------|
| Procedural Dungeon Generator | Technical | Tile-based oda sistemi + oda bağlantıları + oynanabilirlik garantisi zor | Erken prototip, basit algoritmadan başla (BSP veya random walk) |
| Pact System | Design | Boon/Bane dengesi ve sinerji etkileşimleri kombinatorik patlama yaratabilir | 5 pakt ile başla, sinerji matrisini manuel tanımla, playtestle doğrula |
| Combat System | Design + Technical | Oyunun kalbi — "feel" doğru olmazsa her şey çöker | Erken prototip, frame-by-frame polish, juice efektleri |
| Boss System | Design | Her boss benzersiz mekanik gerektiriyor — scope riski | MVP'de 1 boss, basit pattern-based AI |

---

## Progress Tracker

| Metric | Count |
|--------|-------|
| Total systems identified | 30 |
| Design docs started | 20 |
| Design docs reviewed | 0 |
| Design docs approved | 0 |
| MVP systems designed | 20/20 |
| Vertical Slice systems designed | 0/6 |

---

## Next Steps

- [ ] Design MVP-tier systems first (use `/design-system [system-name]`)
- [ ] Run `/design-review` on each completed GDD
- [ ] Prototype high-risk systems early: Procedural Dungeon Generator, Combat System, Pact System
- [ ] Run `/gate-check pre-production` when MVP systems are designed
- [ ] Plan first implementation sprint with `/sprint-plan new`
