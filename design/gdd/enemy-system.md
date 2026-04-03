# Enemy System

> **Status**: Designed
> **Author**: User + Claude
> **Last Updated**: 2026-04-04
> **Implements Pillar**: Core Gameplay

## Overview

Enemy System, Dark Pact'teki tüm düşman varlıklarını yöneten sistemdir. Düşman spawn'ı, tipleri, stat'ları, ölüm/loot tetikleme ve oda temizlenme takibini kapsar. Her düşman bir `EnemyDefinition` ScriptableObject'ten türer. Düşmanların davranışı Enemy AI'a, hasarları Health & Damage'e, loot'ları Loot System'a aittir — Enemy System bunları orkestre eden katmandır.

## Player Fantasy

Düşmanlar tehdit hissettirir ama adil hisseder. Her düşman tipinin belirgin bir görünüşü ve davranış kalıbı vardır — oyuncu bakarak "bu yakın dövüşçü, bu okçu, bundan uzak durmalıyım" diyebilir. Bir odadaki düşman kombinasyonu "puzzle" hissi verir: "Önce okçuyu mu öldüreyim, yoksa kalkan taşıyanı mı?" Düşmanları biçmek tatmin edici, ama dikkatsizlik ölüme yol açar.

## Detailed Design

### Core Rules

1. **EnemyDefinition SO**: Her düşman tipi bir ScriptableObject — stat'lar, sprite, AI tipi, loot tablosu
2. **Spawn**: Room/Tilemap System spawn point'lerden düşman yerleştirir. Oda girişinde spawn tetiklenir.
3. **Difficulty scaling**: Run Manager'dan gelen `roomDifficulty` değeri düşman stat'larını çarpar
4. **Oda temizlenme**: Odadaki tüm düşmanlar öldüğünde `OnRoomCleared` event'i fırlatılır
5. **Pakt etkileşimi**: Katliam Paktı → düşmanlar bir kez dirilir (respawn flag)
6. **Ölüm**: Düşman HP=0 → ölüm animasyonu → loot drop → pool'a dön (object pooling)
7. **Object pooling**: Düşmanlar pool'dan çekilir/döner, runtime Instantiate yok

### EnemyDefinition ScriptableObject

```
EnemyDefinition (ScriptableObject)
├── enemyId: string
├── enemyName: string
├── category: EnemyCategory enum (Melee, Ranged, Tank, Elite, MiniBoss)
├── sprite: AnimatorController (idle, walk, attack, hit, death)
├── baseStats: EnemyStats
│   ├── maxHP: int
│   ├── damage: int
│   ├── defense: int
│   ├── moveSpeed: float
│   ├── attackSpeed: float
│   ├── attackRange: float
│   └── detectionRange: float
├── damageType: DamageType enum
├── resistances: Dictionary<DamageType, float>
├── immunities: List<string> (status effect immunity tag'leri)
├── aiType: AIBehaviorType enum (bkz. Enemy AI GDD)
├── lootTableRef: LootTable SO reference
├── xpReward: int
├── goldReward: int (base, rng range)
├── canRespawn: bool (Katliam Paktı ile dirilme)
├── respawnHP: float (dirilme HP oranı, ör: 0.5 = %50 HP)
└── spawnWeight: int (zorluk bütçesinde ağırlık)
```

### Düşman Kategorileri (MVP)

| Kategori | Örnek | Davranış | HP | Hasar | Hız |
|----------|-------|----------|-----|-------|-----|
| **Melee** | İskelet Savaşçı | Oyuncuya koş, yakından vur | Düşük | Orta | Hızlı |
| **Ranged** | İskelet Okçu | Mesafe koru, ok at | Çok düşük | Orta | Orta |
| **Tank** | Zırhlı İskelet | Yavaş yaklaş, güçlü vur | Yüksek | Yüksek | Yavaş |
| **Elite** | Lanetli Şövalye | Melee + özel yetenek | Çok yüksek | Çok yüksek | Orta |
| **Swarm** | Yarasa | Grup halinde saldır, tek başına zayıf | Çok düşük | Düşük | Çok hızlı |

### Spawn Sistemi

```
Oda spawn bütçesi = roomDifficulty × BudgetPerDifficulty
Her düşmanın spawnWeight'i bütçeden düşülür
Bütçe dolana kadar düşman eklenir
```

**Örnek:** roomDifficulty=1.5, BudgetPerDifficulty=10 → bütçe=15
- İskelet Savaşçı (weight=3) × 3 = 9
- İskelet Okçu (weight=2) × 2 = 4
- Toplam: 13 (bütçe içinde), kalan 2 → ek swarm veya boş

### States and Transitions (her düşman instance)

| State | Açıklama | Geçiş |
|-------|----------|-------|
| **Pooled** | Havuzda bekliyor | → Spawning |
| **Spawning** | Oluşturuluyor (kısa fade-in/pop animasyonu) | → Active |
| **Active** | Canlı, AI kontrollü | → Dead (HP=0), → Stunned (stun efekti) |
| **Stunned** | Geçici olarak hareketsiz | → Active (süre dolunca) |
| **Dead** | Ölüm animasyonu, loot drop | → Respawning (Katliam Paktı), → Pooled |
| **Respawning** | Katliam Paktı: dirilme animasyonu | → Active (azaltılmış HP ile) |

### Interactions with Other Systems

| Sistem | Yön | Arayüz |
|--------|-----|--------|
| **Room/Tilemap System** | ← spawn bilgisi | Spawn point pozisyonları, `OnRoomCleared` event fırlatılır |
| **Health & Damage** | ↔ çift yönlü | Düşman HP yönetimi, hasar alma/verme |
| **Combat System** | ↔ çift yönlü | Düşman saldırıları combat pipeline kullanır |
| **Enemy AI** | → davranış sağlar | AI tipi → hareket ve saldırı kararları |
| **Loot System** | → tetikler | `OnEnemyDied(enemyDef, position)` → loot drop |
| **Run Manager** | → bildirir | `OnEnemyKilled` → kill count, XP reward |
| **Pact System** | ← modifier alır | Katliam Paktı → respawn flag, düşman stat modifier |
| **Status Effect System** | ↔ çift yönlü | Düşmanlar efekt alabilir/uygulayabilir |
| **VFX System** | → tetikler | Spawn efekti, ölüm efekti, hit efekti |

## Formulas

### Difficulty Scaling

```
scaledHP = baseHP × (1 + (roomDifficulty - 1) × HPScaleRate)
scaledDamage = baseDamage × (1 + (roomDifficulty - 1) × DamageScaleRate)
scaledDefense = baseDefense × (1 + (roomDifficulty - 1) × DefenseScaleRate)
```

| Değişken | Tanım | Varsayılan |
|----------|-------|-----------|
| `HPScaleRate` | Zorluk başına HP artış oranı | 0.3 |
| `DamageScaleRate` | Zorluk başına hasar artış oranı | 0.2 |
| `DefenseScaleRate` | Zorluk başına savunma artış oranı | 0.15 |

**Örnek:** İskelet Savaşçı (baseHP=30), roomDifficulty=2.0 → scaledHP = 30 × (1 + 1.0 × 0.3) = 39

### Spawn Budget

```
spawnBudget = roomDifficulty × BudgetPerDifficulty
```

| Değişken | Tanım | Varsayılan |
|----------|-------|-----------|
| `BudgetPerDifficulty` | Zorluk birimi başına bütçe | 10 |

### XP Reward

```
xpReward = baseXP × (1 + (roomDifficulty - 1) × 0.5)
```

### Katliam Paktı Respawn

```
respawnHP = maxHP × RespawnHPRatio
respawnDelay = 2.0s (sabit animasyon süresi)
```

| Değişken | Tanım | Varsayılan |
|----------|-------|-----------|
| `RespawnHPRatio` | Dirilme HP oranı | 0.5 |

## Edge Cases

| Durum | Ne olur |
|-------|---------|
| **Oda spawn bütçesi tek düşman bile almıyorsa** | Minimum 1 düşman spawn edilir (en düşük weight'li) |
| **Katliam Paktı + dirilen düşman tekrar ölürse** | İkinci ölüm finaldir — düşmanlar sadece 1 kez dirilir |
| **Oyuncu odadan çıkıp geri gelirse** | Active düşmanlar yerinde kalır. Dead düşmanlar geri gelmez (cleared değilse) |
| **Tüm düşmanlar spawn anında AoE ile ölürse** | OnRoomCleared anında tetiklenir. Loot hepsi aynı anda düşer. |
| **Elite düşman kapı yanında spawn ederse** | Spawn point'ler kapılardan minimum 3 tile uzak olmalı (şablon kuralı) |
| **Düşman duvara sıkışırsa** | NavMesh/pathfinding ile engellenir. Sıkışma tespit edilirse en yakın geçerli pozisyona teleport. |
| **Respawning düşmanlar oda temizlenme sayılır mı** | Hayır — tüm düşmanlar (respawn dahil) ölene kadar oda temizlenmez |

## Dependencies

**Upstream:**

| Sistem | Bağımlılık Tipi | Arayüz |
|--------|----------------|--------|
| Health & Damage | Hard | Düşman HP yönetimi |
| Room/Tilemap System | Hard | Spawn point'ler, oda yapısı |
| Combat System | Hard | Saldırı pipeline |

**Downstream:**

| Sistem | Bağımlılık Tipi | Arayüz |
|--------|----------------|--------|
| Enemy AI | Hard | Düşman davranış kararları |
| Loot System | Hard | Ölüm → loot drop tetikleme |
| Run Manager | Soft | Kill count, XP |
| Boss System | Hard | Boss düşman olarak bu sistemin üstüne inşa |

## Tuning Knobs

| Knob | Tip | Varsayılan | Güvenli Aralık | Etki |
|------|-----|-----------|----------------|------|
| `BudgetPerDifficulty` | int | 10 | 5–20 | Oda başına düşman yoğunluğu. Düşük → seyrek. Yüksek → kalabalık. |
| `HPScaleRate` | float | 0.3 | 0.1–0.5 | Zorluk başına HP artışı. Yüksek → bullet sponge riski. |
| `DamageScaleRate` | float | 0.2 | 0.1–0.4 | Zorluk başına hasar artışı. Yüksek → one-shot riski. |
| `RespawnHPRatio` | float | 0.5 | 0.3–1.0 | Katliam Paktı dirilme HP'si. 0.3 → kolay ikinci öldürme. 1.0 → tam güçte dirilme. |
| `SpawnFadeInDuration` | float (s) | 0.5 | 0.2–1.0 | Düşman spawn animasyon süresi. Spawn sırasında düşman invulnerable. |
| `MinSpawnDistanceFromDoor` | int (tile) | 3 | 2–5 | Kapıdan minimum spawn mesafesi. |

## Visual/Audio Requirements

- ELV Rogue Adventure düşman sprite'ları kullanılır
- Spawn: Yerden yükselme veya karanlık portaldan çıkış efekti
- Hit: Düşman beyaz flash (0.1s)
- Ölüm: Yere çökme + fade + kan particle
- Dirilme (Katliam Paktı): Karanlık aura ile ayağa kalkma animasyonu
- Her düşman tipine özel saldırı SFX
- Elite düşmanlar: Etrafta aura efekti (tehdit göstergesi)

## UI Requirements

- **Düşman HP bar**: Düşman üzerinde küçük bar, hasar alınca 3s görünür
- **Elite/MiniBoss göstergesi**: İsim + HP bar daha büyük
- **Oda düşman sayacı**: (opsiyonel) Kalan düşman sayısı HUD'da

## Acceptance Criteria

| # | Kriter | Doğrulama Yöntemi |
|---|--------|-------------------|
| 1 | Spawn budget'a göre doğru sayıda düşman oluşur | Unit test: difficulty=1.5 → bütçe=15, düşman weight toplamı ≤ 15 |
| 2 | Difficulty scaling HP ve hasarı doğru artırır | Unit test: baseHP=30, diff=2.0 → scaledHP=39 |
| 3 | Tüm düşmanlar öldüğünde OnRoomCleared tetiklenir | Integration test: 3 düşman öldür → event |
| 4 | Katliam Paktı respawn çalışır, sadece 1 kez | Integration test: düşman öl → diril → tekrar öl → final death |
| 5 | Object pool çalışır, runtime Instantiate yok | Profiler: GC allocation olmadan 20 düşman spawn/despawn |
| 6 | Düşman spawn noktaları kapıdan uzak | Visual test: spawn point'ler minimum 3 tile mesafede |
| 7 | XP ve gold reward doğru hesaplanır | Unit test: baseXP=10, diff=1.5 → xp=12.5 |
| 8 | Elite düşmanlar normal düşmanlardan görsel olarak ayrılır | Visual test: elite aura efekti görünür |

## Open Questions

1. Düşman çeşitliliği MVP'de kaç tip? (Önerilen: 5 tip — Melee, Ranged, Tank, Elite, Swarm. Akt 1 Crypt temalı.)
2. Düşman spawn wave sistemi mi yoksa hepsi baştan mı? (Önerilen: MVP'de hepsi baştan. Wave sistem VS'de — gerilim artırır.)
3. Düşman saldırı telegraphing (uyarı animasyonu) gerekli mi? (Önerilen: Evet — en az 0.3s anticipation. Oyuncuya adil tepki süresi.)
