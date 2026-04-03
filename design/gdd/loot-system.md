# Loot System

> **Status**: Designed
> **Author**: User + Claude
> **Last Updated**: 2026-04-04
> **Implements Pillar**: Core Gameplay

## Overview

Loot System, düşman ölümlerinde ve hazine odalarında item ve altın düşürmesini yöneten sistemdir. Loot tabloları (ScriptableObject), rarity roll mekanığı, difficulty-based loot kalitesi ve pakt etkileşimleri (Açgözlülük altın çarpanı) bu sisteme aittir. Item Database'den item seçer, rarity ve bonus stat'ları roll eder, dünyaya yerleştirir.

## Player Fantasy

Her düşman öldürmek "ne düşecek?" merakı yaratır. Mor veya altın rarity item düştüğünde "jackpot!" heyecanı. Loot her run'da farklı — aynı odada farklı item'lar bulursun. Açgözlülük Paktı ile altın yağmuru görmek tatmin edici.

## Detailed Design

### Core Rules

1. **Loot tabloları**: Her düşman tipinin SO tabanlı loot tablosu var
2. **Drop chance**: Her düşman ölümünde loot tablosundan roll edilir
3. **Rarity roll**: Item seçildikten sonra rarity belirlenir (Item Database ağırlıklarına göre)
4. **Bonus stat roll**: Rarity'ye göre random bonus stat eklenir
5. **Altın drop**: Her düşman sabit + random altın düşürür
6. **Difficulty scaling**: Yüksek difficulty → daha yüksek rarity şansı
7. **Ground loot**: Item dünyaya fiziksel obje olarak düşer, oyuncu Interact ile alır
8. **Boss loot**: Boss ölümünde guaranteed Epic+ item + gold pile (100-200 altın). Boss loot tablosu ayrı tanımlıdır.
9. **Pity timer**: Son 15 item drop'unda Rare+ düşmediyse bir sonraki drop garantili Rare. Son 30 drop'ta Epic+ düşmediyse garantili Epic. Bu sayaç run boyunca tutulur.

### LootTable ScriptableObject

```
LootTable (ScriptableObject)
├── tableId: string
├── entries: List<LootEntry>
│   ├── itemDef: ItemDefinition
│   ├── dropWeight: int (ağırlık)
│   ├── minRarity: Rarity
│   └── categoryFilter: ItemCategory (opsiyonel)
├── dropChance: float (0-1, bu tablodan item düşme şansı)
├── guaranteedDrops: List<ItemDefinition> (her zaman düşer)
├── goldRange: Vector2Int (min, max altın)
└── bonusGoldChance: float (ekstra altın şansı)
```

### Drop Pipeline

```
1. Düşman ölür → Enemy System OnEnemyDied event
2. dropChance roll → başarısız ise sadece altın
3. LootTable'dan weighted random item seç
4. Rarity roll (difficulty-adjusted weights)
5. Bonus stat roll (rarity'ye göre sayı, Item Database pool'undan)
6. ItemInstance oluştur
7. World'e loot obje spawn et (fiziksel, pickup trigger)
8. Altın drop (base + random, pakt çarpanı)
```

### States and Transitions

Loot System stateless — her drop bağımsız hesaplanır.

Ground loot objeleri:
| State | Açıklama |
|-------|----------|
| **Spawned** | Yere düştü, fizik simülasyonu (küçük bounce) |
| **Idle** | Yerde duruyor, pickup bekleniyor |
| **PickedUp** | Oyuncu aldı, equip/bırak kararı |
| **Despawned** | Oda değiştiğinde veya timeout sonrası |

### Interactions with Other Systems

| Sistem | Yön | Arayüz |
|--------|-----|--------|
| **Enemy System** | ← tetikler | `OnEnemyDied(enemyDef, position)` |
| **Item Database** | ← sorgular | Item tanımları, bonus stat pool |
| **Equipment System** | → iletir | Pickup → equip/bırak kararı |
| **Run Manager** | ← difficulty alır | roomDifficulty → rarity bonus |
| **Pact System** | ← modifier alır | Açgözlülük GoldMultiplier |
| **Room/Tilemap System** | ← bilgi alır | Hazine odası → garantili iyi loot |

## Formulas

### Rarity Roll (Difficulty-Adjusted)

```
adjustedWeight[rarity] = baseWeight[rarity] × (1 + (roomDifficulty - 1) × RarityBonusPerDifficulty)
// Yüksek rarity'lere bonus verilir
adjustedWeight[Rare] *= difficultyBonus
adjustedWeight[Epic] *= difficultyBonus²
adjustedWeight[Legendary] *= difficultyBonus³
```

| Rarity | Base Weight | Diff=1.0 | Diff=2.0 (bonus=1.1) |
|--------|------------|----------|----------------------|
| Common | 50 | 50 | 50 (değişmez) |
| Uncommon | 25 | 25 | 27.5 (×1.1) |
| Rare | 15 | 15 | 16.5 (×1.1) |
| Epic | 8 | 8 | 9.68 (×1.1²=1.21) |
| Legendary | 2 | 2 | 2.66 (×1.1³=1.331) |

| Değişken | Tanım | Varsayılan |
|----------|-------|-----------|
| `RarityBonusPerDifficulty` | Difficulty başına rarity bonus | 0.1 |

### Altın Drop

```
goldDrop = random(goldRange.min, goldRange.max) × pactGoldMultiplier
pactGoldMultiplier = Açgözlülük aktif ? AcgozlulukGoldMultiplier : 1.0
```

### Treasure Room Loot

```
treasureRoomGuaranteedRarity = max(Rare, difficultyBasedMinRarity)
treasureRoomItemCount = 1-2 item + altın pile
```

## Edge Cases

| Durum | Ne olur |
|-------|---------|
| **Loot tablosu boşsa** | Sadece altın düşer, item yok |
| **Oyuncu item almadan oda değiştirirse** | Item 60s timeout sonrası despawn. Geri dönülürse hâlâ orada. |
| **Loot tablodaki item artık geçersizse** | Skip edilir, bir sonraki entry denenir |
| **Legendary drop ama pakt uyumu yok** | Item düşer ama pakt bonusu inaktif (Epic gibi davranır) |
| **Hazine odasında düşman yoksa** | Hazine doğrudan odada spawn, düşman ölümü gerekmez |
| **Aynı anda çok fazla loot düşerse** | Object pool — max 20 ground loot obje. Fazlası en eski olanı replace eder. |

## Dependencies

**Upstream:**

| Sistem | Bağımlılık Tipi | Arayüz |
|--------|----------------|--------|
| Item Database | Hard | Item tanımları ve bonus stat pool |
| Enemy System | Hard | Ölüm event'i |
| Equipment System | Hard | Pickup → equip akışı |

**Downstream:** Yok — leaf node.

## Tuning Knobs

| Knob | Tip | Varsayılan | Güvenli Aralık | Etki |
|------|-----|-----------|----------------|------|
| `BaseDropChance` | float | 0.3 | 0.1–0.6 | Düşman başına item düşme şansı. Düşük → nadir loot. Yüksek → loot yağmuru. |
| `RarityBonusPerDifficulty` | float | 0.1 | 0.05–0.3 | Difficulty başına rarity şansı artışı. |
| `GoldRangeBase` | Vector2Int | (5, 15) | (1,5)–(20,50) | Temel altın drop aralığı. |
| `LootDespawnTime` | float (s) | 60.0 | 30–120 | Yerdeki loot timeout süresi. |
| `TreasureRoomMinRarity` | Rarity | Rare | Uncommon–Epic | Hazine odasında minimum rarity. |

## Visual/Audio Requirements

- Loot drop: Küçük bounce fizik animasyonu
- Rarity glow: Item yerdeyken rarity renginde parlama
- Pickup SFX: Rarity'ye göre farklı (Common → tık, Legendary → dramatik chime)
- Altın: Sikke sprite + altın parıltı particle
- Loot magnet: (VS) Oyuncuya yakın loot otomatik çekilir

## UI Requirements

- **Loot Popup**: Item alınca ekranda kısa bildirim (ikon + isim + rarity rengi)
- **Quick Compare**: Item yerdeyken yaklaşınca tooltip + mevcut ekipmanla karşılaştırma
- **Altın göstergesi**: HUD'da toplam altın

## Acceptance Criteria

| # | Kriter | Doğrulama Yöntemi |
|---|--------|-------------------|
| 1 | Düşman ölümünde dropChance'a göre item düşer | Unit test: 1000 roll, %30 → ~300 item |
| 2 | Rarity dağılımı ağırlıklara uygun | Unit test: 10000 roll → Common ~50%, Legendary ~2% |
| 3 | Difficulty rarity şansını artırır | Unit test: diff=1 vs diff=2 → diff=2'de daha fazla Rare+ |
| 4 | Açgözlülük Paktı altını 3× artırır | Unit test: base gold × 3 |
| 5 | Hazine odası garantili Rare+ item verir | Integration test: hazine odası → minimum Rare |
| 6 | Ground loot timeout sonrası despawn olur | Integration test: 60s sonra loot kaybolur |

## Open Questions

1. Loot magnet (auto-pickup yakın loot) MVP'de mi? (Önerilen: MVP'de yok, VS'de — QoL feature.)
2. Item parçalama/satma mekanığı? (Önerilen: MVP'de yok — item ya equip edilir ya bırakılır.)
3. Smart loot (oyuncunun class'ına göre ağırlıklı drop) olacak mı? (Önerilen: VS'de — MVP'de tamamen random.)
