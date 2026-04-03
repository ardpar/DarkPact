# Enemy AI

> **Status**: Designed
> **Author**: User + Claude
> **Last Updated**: 2026-04-04
> **Implements Pillar**: Core Gameplay

## Overview

Enemy AI, Dark Pact'teki düşmanların davranış kararlarını yöneten sistemdir. Her düşman tipine atanan AI davranış ağacı (Behavior Tree) ile hareket, saldırı, kaçma ve özel yetenek kararlarını verir. Basit ama okunabilir AI hedeflenir — düşmanlar "zeki" değil "adil" hissettirmelidir. Oyuncu düşmanın ne yapacağını tahmin edebilmeli ama yine de dikkatli olmalıdır.

## Player Fantasy

Düşmanlar "aptal" hissettirmez ama "hile yapıyor" da hissettirmez. Melee düşman direkt koşar — oyuncu bunu bekler ve pozisyonlanır. Okçu mesafe korur — oyuncu "onu önce öldürmeliyim" stratejisi yapar. Tank yavaş gelir ama vurduğunda acıtır. Her düşmanın kalıbı öngörülebilir ama kombinasyonları zorluk yaratır.

## Detailed Design

### Core Rules

1. **Behavior Tree**: Her AI tipi bir davranış ağacı (BT) ile tanımlanır. Basit BT — Selector/Sequence/Condition/Action node'ları.
2. **Perception**: Düşmanlar `detectionRange` içindeki oyuncuyu algılar. Line-of-sight (LOS) kontrolü duvarlar için (ranged düşmanlar).
3. **Pathfinding**: A* pathfinding (Unity NavMesh2D veya custom grid-based). Duvarlardan kaçınma. Odadaki diğer düşmanlardan ayrılma (separation).
4. **Tick rate**: AI tick 0.1s aralıkla (10Hz). Her frame değil — performans için.
5. **Telegraphing**: Her saldırı öncesi minimum 0.3s anticipation animasyonu — oyuncuya tepki süresi.
6. **Leash**: Düşman spawn odasından çıkamaz. Oyuncu odadan çıkarsa düşman spawn pozisyonuna döner.

### AI Tipleri (Behavior Trees)

#### Melee AI (İskelet Savaşçı, Swarm)

```
Root (Selector)
├── [Condition: HP < 30%] → FleeToSafeDistance → Heal (yoksa → aggressive continue)
├── [Condition: PlayerInAttackRange] → Attack
├── [Condition: PlayerDetected] → ChasePlayer
└── [Default] → Patrol (idle wander)
```

#### Ranged AI (İskelet Okçu)

```
Root (Selector)
├── [Condition: PlayerTooClose (< 2 tile)] → FleeFromPlayer
├── [Condition: PlayerInRange AND HasLOS] → Attack (projectile)
├── [Condition: PlayerDetected AND !HasLOS] → MoveToLOSPosition
└── [Default] → Patrol
```

#### Tank AI (Zırhlı İskelet)

```
Root (Selector)
├── [Condition: PlayerInAttackRange] → HeavyAttack (yavaş, güçlü)
├── [Condition: PlayerDetected] → SlowChase (hız modifier 0.6)
└── [Default] → GuardPosition (spawn noktasında bekle)
```

#### Elite AI (Lanetli Şövalye)

```
Root (Selector)
├── [Condition: SpecialAbilityCooldown Ready] → UseSpecialAbility
├── [Condition: PlayerInAttackRange] → ComboAttack (2-hit)
├── [Condition: PlayerDetected AND distance > 3] → DashToPlayer
├── [Condition: PlayerDetected] → ChasePlayer
└── [Default] → Patrol
```

#### Swarm AI (Yarasa)

```
Root (Selector)
├── [Condition: PlayerDetected] → SwarmChase (diğer swarm üyeleriyle birlikte)
├── [Condition: AllyNearby] → FormGroup
└── [Default] → RandomFly
```

### States and Transitions (per enemy)

| State | Açıklama | Geçiş |
|-------|----------|-------|
| **Idle** | Patrol/wander, oyuncu algılanmamış | → Alert (oyuncu detectionRange içinde) |
| **Alert** | Oyuncu algılandı, davranış ağacı aktif | → Attacking, → Chasing, → Fleeing |
| **Chasing** | Oyuncuya doğru hareket | → Attacking (menzil içinde), → Idle (leash) |
| **Attacking** | Saldırı animasyonu (telegraph → hit → recovery) | → Chasing (menzil dışı), → Alert |
| **Fleeing** | Oyuncudan kaçıyor | → Alert (güvenli mesafe), → Idle |
| **Stunned** | Status effect ile hareketsiz | → Alert (süre dolunca) |
| **Dead** | Enemy System'a devredilir | — |

### Interactions with Other Systems

| Sistem | Yön | Arayüz |
|--------|-----|--------|
| **Enemy System** | ← veri alır | EnemyDefinition → AI tipi, stat'lar, detection range |
| **Player Controller** | ← takip eder | Oyuncu pozisyonu, state bilgisi |
| **Combat System** | → saldırı tetikler | Saldırı kararı → hitbox oluşturma |
| **Room/Tilemap System** | ← pathfinding verisi | Duvar collider'lar, walkable area |
| **Health & Damage** | ← durum dinler | Kendi HP'si → flee kararı |
| **Status Effect System** | ← durum dinler | Stun, slow → AI davranışını etkiler |

## Formulas

### Detection

```
canDetect = distance(enemy, player) <= detectionRange
hasLOS = !Physics2D.Linecast(enemy.position, player.position, wallLayer)
```

| Değişken | Tanım | Varsayılan |
|----------|-------|-----------|
| `detectionRange` | Algılama mesafesi (tile) | Melee=6, Ranged=8, Tank=5, Elite=8, Swarm=10 |

### Pathfinding

```
path = AStar.FindPath(currentPos, targetPos, walkableGrid)
nextWaypoint = path[0]
moveDirection = (nextWaypoint - currentPos).normalized
```

**Separation** (düşmanlar birbirine çok yaklaşmaması için):
```
separationForce = sum(foreach nearby ally: (myPos - allyPos).normalized / distance²)
finalDirection = (moveDirection + separationForce × SeparationWeight).normalized
```

| Değişken | Tanım | Varsayılan |
|----------|-------|-----------|
| `SeparationWeight` | Ayrılma kuvveti çarpanı | 0.5 |
| `SeparationRadius` | Ayrılma kontrol mesafesi (tile) | 1.5 |

### Attack Decision

```
canAttack = distance(enemy, player) <= attackRange AND attackCooldown <= 0
attackCooldown = 1.0 / attackSpeed (Enemy System'dan)
```

### Flee Decision (Ranged AI)

```
shouldFlee = distance(enemy, player) < FleeThreshold
fleeDirection = (enemyPos - playerPos).normalized
fleeTarget = enemyPos + fleeDirection × FleeDistance
```

| Değişken | Tanım | Varsayılan |
|----------|-------|-----------|
| `FleeThreshold` | Kaçış mesafesi (tile) | 2.0 |
| `FleeDistance` | Kaçış hedef mesafesi (tile) | 4.0 |

## Edge Cases

| Durum | Ne olur |
|-------|---------|
| **Pathfinding yol bulamazsa** | Düşman direkt hedefe doğru yürür (wall slide ile). 3s sonra hâlâ sıkışıksa spawn pozisyonuna teleport. |
| **Birden fazla düşman aynı pozisyona koşarsa** | Separation force birbirlerini iter. Tam overlap → rastgele yöne küçük offset. |
| **Oyuncu dash ile düşmanın arkasına geçerse** | AI bir sonraki tick'te (0.1s) yeni pozisyonu algılar ve yön değiştirir. |
| **Ranged düşman köşeye sıkışırsa (flee edemez)** | Flee hedefi geçersizse melee davranışına geçer (zorunlu yakın saldırı). |
| **Tüm düşmanlar aynı anda saldırırsa** | Stagger mekanizması: Aynı anda max 2-3 düşman saldırabilir (AttackToken sistemi). Diğerleri bekleme pozisyonunda kalır. |
| **Oyuncu invulnerable iken (dash)** | AI farkında değil — saldırmaya devam eder. Bu adil: düşman oyuncunun dash timing'ini bilmemeli. |
| **Elite özel yetenek cooldown'u sıfırken** | Normal melee saldırı yapar. Özel yetenek hazır olunca öncelikli kullanır. |

## Dependencies

**Upstream:**

| Sistem | Bağımlılık Tipi | Arayüz |
|--------|----------------|--------|
| Enemy System | Hard | Düşman stat'ları, AI tipi |
| Player Controller | Hard | Oyuncu pozisyonu ve state |

**Downstream:** Yok — Enemy AI kararları Enemy System ve Combat System üzerinden uygulanır.

## Tuning Knobs

| Knob | Tip | Varsayılan | Güvenli Aralık | Etki |
|------|-----|-----------|----------------|------|
| `AITickRate` | float (s) | 0.1 | 0.05–0.3 | AI karar aralığı. Düşük → daha reaktif ama CPU yoğun. Yüksek → daha yavaş tepki. |
| `MaxConcurrentAttackers` | int | 3 | 1–5 | Aynı anda saldırabilen düşman sayısı. 1 → çok kolay. 5 → acımasız. |
| `TelegraphDuration` | float (s) | 0.3 | 0.2–0.8 | Saldırı öncesi uyarı süresi. Kısa → zor. Uzun → kolay. |
| `SeparationWeight` | float | 0.5 | 0.0–2.0 | Düşmanlar arası itme gücü. 0 → üst üste binerler. 2.0 → çok dağınık. |
| `FleeThreshold` | float (tile) | 2.0 | 1.0–4.0 | Ranged kaçış mesafesi. |
| `PatrolRadius` | float (tile) | 3.0 | 1.0–5.0 | Idle'da dolaşma alanı. |

## Visual/Audio Requirements

- **Alert**: Düşman oyuncuyu fark edince kısa "!" simgesi (0.3s pop-up)
- **Telegraph**: Saldırı öncesi kırmızı flash/glow + anticipation animasyonu
- **Flee**: Ranged düşman geri geri koşma animasyonu
- **Stunned**: Yıldız/dönme animasyonu başın üstünde
- Patrol hareketi doğal görünmeli — düz çizgi değil, hafif eğrisel

## UI Requirements

- Enemy AI'ın doğrudan UI gereksinimleri yok
- Debug mode'da: AI state göstergesi (düşman üzerinde metin — sadece editor)

## Acceptance Criteria

| # | Kriter | Doğrulama Yöntemi |
|---|--------|-------------------|
| 1 | Melee düşman oyuncuyu algılayıp kovalıyor | Integration test: oyuncu detectionRange içinde → chase state |
| 2 | Ranged düşman mesafe koruyor, LOS yoksa pozisyon değiştiriyor | Integration test: duvar arkasında → MoveToLOS, yaklaşınca → flee |
| 3 | Saldırı öncesi telegraph en az 0.3s | Visual test: saldırı öncesi uyarı animasyonu süresi |
| 4 | MaxConcurrentAttackers limiti çalışıyor | Integration test: 5 düşman, limit=3 → aynı anda max 3 saldırır |
| 5 | Pathfinding duvarlardan kaçınıyor | Integration test: L şeklinde koridor → düşman köşeyi dönüyor |
| 6 | Separation düşmanların üst üste binmesini engelliyor | Visual test: 5 düşman koşarken dağılıyor |
| 7 | Leash sistemi çalışıyor — düşman odadan çıkmıyor | Integration test: oyuncu odadan çık → düşman spawn'a döner |
| 8 | AI tick rate performansı etkilemiyor (50 düşman) | Performance test: 50 aktif AI → frame time < 2ms AI bütçesi |

## Open Questions

1. NavMesh2D mi yoksa custom grid-based A* mı? (Önerilen: Grid-based A* — tile-based dungeon'la doğal uyumlu, NavMesh bake gerektirmez.)
2. Boss AI bu sistemi mi kullanacak yoksa ayrı mı? (Önerilen: Boss AI bu sistemin üzerine inşa — özel BT ama aynı altyapı.)
3. Düşman gruplaşma/formation davranışı gerekli mi? (Önerilen: MVP'de sadece separation. Formation VS'de — özellikle elite gruplar için.)
