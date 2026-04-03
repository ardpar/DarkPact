# Prototype Scene Setup Guide

This document describes how to wire up the prototype in Unity Editor.
All scripts are ready — this covers scene hierarchy, prefabs, and Inspector configuration.

---

## 1. Physics Layer Setup

**Edit → Project Settings → Tags and Layers → Layers:**

| Layer # | Name |
|---------|------|
| 6 | Player |
| 7 | Enemy |
| 8 | Wall |
| 9 | PlayerHitbox |
| 10 | EnemyHurtbox |
| 11 | EnemyAttack |
| 12 | PlayerHurtbox |
| 13 | Projectile |
| 14 | Pickup |

**Edit → Project Settings → Physics 2D → Layer Collision Matrix:**

| | Player | Enemy | Wall | PlayerHitbox | EnemyHurtbox | EnemyAttack | PlayerHurtbox | Projectile | Pickup |
|---|---|---|---|---|---|---|---|---|---|
| **Player** | - | - | ✓ | - | - | - | - | - | ✓ |
| **Enemy** | - | ✓ | ✓ | - | - | - | - | - | - |
| **Wall** | ✓ | ✓ | - | - | - | - | - | ✓ | - |
| **PlayerHitbox** | - | - | - | - | ✓ | - | - | - | - |
| **EnemyHurtbox** | - | - | - | ✓ | - | - | - | ✓ | - |
| **EnemyAttack** | - | - | - | - | - | - | ✓ | - | - |
| **PlayerHurtbox** | - | - | - | - | - | ✓ | - | - | - |
| **Projectile** | - | - | ✓ | - | ✓ | - | ✓ | - | - |
| **Pickup** | ✓ | - | - | - | - | - | - | - | - |

---

## 2. Input System Setup

1. Select `Assets/Settings/DarkPactInput.inputactions`
2. In Inspector, click **Generate C# Class** → enable it
   - **Class Name**: `DarkPactInput`
   - **Namespace**: `DarkPact.Core`
   - **File Path**: `Assets/Scripts/Core/DarkPactInput.cs`
3. Click **Apply**

---

## 3. Scene Hierarchy

Create a new scene `Assets/Scenes/PrototypeArena.unity`:

```
PrototypeArena
├── --- MANAGERS ---
│   ├── GameManager           [GameManager.cs]
│   ├── RunManager            [RunManager.cs]
│   ├── PactManager           [PactManager.cs]
│   ├── VFXManager            [VFXManager.cs]  → _hitSparkPrefab = HitSpark prefab
│   └── GameBootstrap         [GameBootstrap.cs]
│
├── --- CAMERA ---
│   └── Main Camera           [Camera, CameraController.cs, URP 2D Renderer]
│       Settings: Orthographic, Size=8, _pixelsPerUnit=16
│
├── --- ENVIRONMENT ---
│   ├── Grid
│   │   ├── Ground Tilemap    [Tilemap, TilemapRenderer] Layer: Default
│   │   └── Walls Tilemap     [Tilemap, TilemapRenderer, TilemapCollider2D, CompositeCollider2D, Rigidbody2D(Static)]
│   │       Layer: Wall
│   │       TilemapCollider2D → Used by Composite: ✓
│   │       Rigidbody2D → Body Type: Static
│   └── SpawnPoints
│       ├── SpawnPoint_1      [Transform only] (position enemies will spawn)
│       ├── SpawnPoint_2
│       ├── SpawnPoint_3
│       └── SpawnPoint_4
│
├── --- PLAYER ---
│   └── Player                Layer: Player
│       Components:
│       - SpriteRenderer      (ELV knight/warrior sprite)
│       - Rigidbody2D         (Dynamic, Gravity=0, Freeze Rotation Z, Interpolate)
│       - CapsuleCollider2D   (non-trigger, fits sprite)
│       - PlayerController    (_enemyLayer = EnemyHurtbox)
│       - Health              (_maxHP = 100)
│       - DamageNumberSpawner (_damageNumberPrefab = DamageNumber prefab)
│       - PlayerInput         (Actions = DarkPactInput, Default Map = Gameplay, Behavior = Send Messages)
│
├── --- ROOM ---
│   └── RoomManager           [RoomManager.cs]
│       _enemyPrefab = SkeletonWarrior prefab
│       _spawnPoints = [SpawnPoint_1..4]
│       _enemyCount = 4
│
├── --- UI ---
│   └── Canvas                [Canvas (Screen Space - Overlay), CanvasScaler (1920x1080 ref)]
│       ├── HPBar             [Slider] → SimpleHUD._hpBar
│       │   └── Fill          [Image] → SimpleHUD._hpFill
│       ├── PactIcon          [Image, inactive] → SimpleHUD._pactIcon
│       ├── RoomCounter       [TextMeshProUGUI] → SimpleHUD._roomCounterText
│       ├── KillCount         [TextMeshProUGUI] → SimpleHUD._killCountText
│       ├── GameOverText      [TextMeshProUGUI, inactive] → SimpleHUD._gameOverText
│       ├── RestartText       [TextMeshProUGUI, inactive] → SimpleHUD._restartText
│       └── PactSelectionUI   [PactSelectionUI.cs]
│           ├── Overlay       [Image (black, alpha 0.8)] → _overlay, _overlayCanvasGroup
│           ├── FlavorText    [TextMeshProUGUI] → _flavorText
│           └── CardContainer [Panel] → _cardContainer
│               ├── PactName  [TextMeshProUGUI] → _pactNameText
│               ├── BoonText  [TextMeshProUGUI] → _boonText
│               ├── BaneText  [TextMeshProUGUI] → _baneText
│               └── SelectBtn [Button + TextMeshProUGUI "SEÇIM YAP"] → _selectButton
│       └── SimpleHUD        [SimpleHUD.cs] (wire all refs above)
```

---

## 4. Prefabs to Create

### `Assets/Prefabs/SkeletonWarrior.prefab`
- Layer: **Enemy**
- Components:
  - SpriteRenderer (ELV skeleton sprite)
  - Rigidbody2D (Dynamic, Gravity=0, Freeze Rotation Z, Interpolate)
  - CapsuleCollider2D (non-trigger, Layer: Enemy)
  - EnemyAI (_moveSpeed=3, _detectionRange=6, _attackRange=1.2, _attackDamage=10)
  - Health (_maxHP=30)
  - DamageNumberSpawner (_damageNumberPrefab = DamageNumber prefab)

### `Assets/Prefabs/DamageNumber.prefab`
- Components:
  - TextMeshPro (3D text, not UI) — font size 5, alignment center
  - DamageNumber.cs (defaults are fine)

### `Assets/Prefabs/HitSpark.prefab`
- Components:
  - ParticleSystem
    - Duration: 0.3s, Looping: off
    - Start Lifetime: 0.2s
    - Start Speed: 3
    - Start Size: 0.1
    - Start Color: white/yellow
    - Emission: Burst 8 particles
    - Shape: Circle, Radius 0.1
    - Color over Lifetime: white → transparent
    - Renderer: Default-Particle material

---

## 5. Player Rigidbody2D Settings

| Property | Value |
|----------|-------|
| Body Type | Dynamic |
| Gravity Scale | 0 |
| Linear Drag | 5 |
| Angular Drag | 0 |
| Freeze Rotation | Z ✓ |
| Collision Detection | Continuous |
| Interpolate | Interpolate |

Same for enemy Rigidbody2D, except:
- Linear Drag: 8 (stops faster after knockback)

---

## 6. Camera Settings

| Property | Value |
|----------|-------|
| Projection | Orthographic |
| Size | 8 |
| Background | Dark gray (#1a1a2e) |
| Clear Flags | Solid Color |

Attach `CameraController.cs` with defaults (_smoothTime=0.15, _pixelsPerUnit=16).

---

## 7. Tilemap Quick Setup

1. Window → 2D → Tile Palette → Create New Palette "Crypt"
2. Drag ELV Crypt tileset sprites into palette
3. Paint ground tiles on Ground tilemap
4. Paint wall tiles on Walls tilemap (perimeter + obstacles)
5. Walls tilemap: Add TilemapCollider2D → check "Used by Composite"
6. Walls tilemap: Add CompositeCollider2D
7. Walls tilemap: Rigidbody2D → Static

Minimum room size: 20x15 tiles (320x240 pixels).

---

## 8. Build Settings

1. File → Build Settings → Add `PrototypeArena` scene
2. Edit → Project Settings → Player:
   - Resolution: 1280x720 (or 1920x1080)
   - Default Fullscreen: Windowed
3. Edit → Project Settings → Quality:
   - VSync: Every V Blank
   - Target Frame Rate: 60

---

## 9. Quick Test Checklist

After setup, verify in Play mode:

- [ ] WASD moves player, diagonal is normalized
- [ ] Left-click attacks in mouse direction, arc hitbox works
- [ ] Space dashes with i-frames
- [ ] Enemies detect and chase player
- [ ] Enemy telegraph (red flash) before attack
- [ ] Damage numbers appear on hit
- [ ] HP bar updates on damage
- [ ] ESC pauses/unpauses
- [ ] Player death → Game Over text
- [ ] R on Game Over → restart
- [ ] N starts a new run (opens pact selection)
- [ ] Pact selection modal appears, Enter selects
- [ ] Kill count and room counter update
