# Boss System

> **Status**: Designed
> **Author**: User + Claude
> **Last Updated**: 2026-04-04
> **Implements Pillar**: Core Gameplay

## Overview

Boss System, Dark Pact'in akt sonu boss savaşlarını yöneten sistemdir. Enemy System üzerine inşa edilir — boss'lar özel AI, çoklu faz, benzersiz saldırı pattern'leri ve arena mekaniğine sahip güçlü düşmanlardır. MVP'de 1 boss (Minotaur, Akt 1 sonu). Boss savaşları run'ın doruk noktasıdır ve pakt build'inin test edildiği yerdir.

## Player Fantasy

"Final boss — her şey bunun için." Boss odasına girdiğinde müzik değişir, atmosfer ağırlaşır. Boss büyük, tehditkar ve pattern'leri okunabilir ama affetmez. Boss'u yenmek "bu run'ı hak ettim" hissi verir. Pakt build'inin boss'a karşı nasıl çalıştığını görmek stratejik tatmin sağlar.

## Detailed Design

### Core Rules

1. **Enemy System extension**: Boss = özel EnemyDefinition + özel AI BehaviorTree + faz sistemi
2. **Fazlar**: Her boss 2-3 faz. HP eşiklerinde faz geçişi → yeni saldırı pattern'leri.
3. **Arena mekaniği**: Boss odası özel hazırlıklara sahip olabilir (tuzaklar, platformlar)
4. **Telegraphing**: Boss saldırıları uzun telegraph (0.5-1.0s) — adil ama güçlü
5. **Stagger immunity**: Boss'lar normal stagger'a immune, sadece özel anlar (stun window) var
6. **Enrage**: Savaş çok uzarsa (EnrageTimer) boss güçlenir — sonsuz kiting önlemi
7. **No adds MVP**: MVP'de boss tek başına. VS'de minion spawn faz mekaniği eklenebilir.
8. **Pakt istisnaları**: Boss'lar Katliam Paktı'nın `EnemyRespawn` kuralından muaftır — boss ölünce dirilmez. Bu istisna `BossDefinition.canRespawn = false` ile garanti edilir ve `EnemyRespawn` PactRule boss'ları atlar.

### MVP Boss: Minotaur (Akt 1)

**Lore:** Crypt'in derinliklerinde zincirlenmiş, serbest kalmış Minotaur.

**Stat'lar:**

| Stat | Değer |
|------|-------|
| MaxHP | 500 |
| Damage (melee) | 25 |
| Damage (charge) | 40 |
| Defense | 10 |
| MoveSpeed | 3.0 (normal), 12.0 (charge) |
| Stagger Immunity | Yes |

**Fazlar:**

| Faz | HP Eşiği | Davranış |
|-----|----------|----------|
| **Faz 1** (100-60%) | HP > 300 | Yavaş yürüyüş + melee combo (2 hit) + charge attack |
| **Faz 2** (60-30%) | 300 > HP > 150 | Hızlanır (×1.3), charge sıklığı artar, ground slam eklenir |
| **Faz 3** (30-0%) | HP < 150 | Enraged: her saldırı daha hızlı, charge duvara çarpınca stun window (3s) |

**Saldırı Pattern'leri:**

| Saldırı | Telegraph | Hitbox | Davranış |
|---------|-----------|--------|----------|
| Melee Combo | 0.5s kol kaldırma | Yay, 2 tile | 2 ardışık vuruş, knockback |
| Charge | 0.8s kızıl glow + yön kilidi | Dikdörtgen, 8 tile uzunluk | Düz çizgide hızlı koşu, duvara çarparsa 1.5s stun |
| Ground Slam (Faz 2+) | 0.7s havaya zıplama | Daire, 3 tile radius | AoE hasar, küçük screen shake |

### BossDefinition (EnemyDefinition extension)

```
BossDefinition : EnemyDefinition
├── phases: List<BossPhase>
│   ├── hpThreshold: float (0-1, faz geçiş eşiği)
│   ├── behaviorTree: BehaviorTree (faza özel AI)
│   ├── statMultipliers: StatBlock (faz stat çarpanları)
│   └── transitionAnimation: string
├── enrageTimer: float (saniye)
├── enrageMultipliers: StatBlock
├── stunWindows: List<StunWindow>
│   ├── trigger: string (ör: "charge_wall_hit")
│   ├── duration: float
│   └── damageMultiplier: float (stun sırasında alınan hasar çarpanı)
├── introAnimation: string
├── bossMusic: AudioClip
└── arenaSize: Vector2Int
```

### States and Transitions

| State | Açıklama | Geçiş |
|-------|----------|-------|
| **Inactive** | Boss henüz aktif değil | → Intro (oyuncu boss odasına girdiğinde) |
| **Intro** | Boss tanıtım animasyonu, kapılar kitlenir | → Phase1 (animasyon bitince) |
| **Phase1** | İlk faz AI aktif | → PhaseTransition (HP eşiği) |
| **PhaseTransition** | Faz değişim animasyonu (kısa invulnerable) | → Phase2/Phase3 |
| **Phase2** | İkinci faz AI | → PhaseTransition veya → Dead |
| **Phase3** | Üçüncü faz AI (enraged) | → Dead |
| **Stunned** | Stun window — hasar çarpanı aktif | → mevcut Phase |
| **Dead** | Ölüm animasyonu, kapılar açılır | → Run Manager victory |

### Interactions with Other Systems

| Sistem | Yön | Arayüz |
|--------|-----|--------|
| **Enemy System** | ← temel alır | EnemyDefinition + extension |
| **Enemy AI** | ← özel BT | Faza göre behavior tree değişimi |
| **Combat System** | ↔ çift yönlü | Boss saldırıları combat pipeline, oyuncu saldırıları boss'a |
| **Health & Damage** | ↔ çift yönlü | Boss HP, faz eşiği kontrolü |
| **Run Manager** | → bildirir | Boss ölümü → RunEnd(victory) |
| **Procedural Dungeon Generator** | ← oda bilgisi | Boss odası pozisyonu ve boyutu |
| **VFX System** | → tetikler | Boss saldırı efektleri, faz geçiş efekti |
| **Audio Manager** | → tetikler | Boss müziği, saldırı SFX, faz geçiş SFX |
| **Room/Tilemap System** | → kontrol eder | Boss odasında kapılar kitlenir, boss ölünce açılır |

## Formulas

### Faz Geçiş Eşiği

```
phaseTransition = currentHP / maxHP <= phase.hpThreshold
```

### Enrage Timer

```
if (fightDuration >= enrageTimer):
    boss.damageMultiplier *= enrageDamageMultiplier
    boss.attackSpeedMultiplier *= enrageSpeedMultiplier
```

| Değişken | Tanım | Varsayılan |
|----------|-------|-----------|
| `enrageTimer` | Enrage başlangıç süresi (s) | 180 (3 dakika) |
| `enrageDamageMultiplier` | Enrage hasar çarpanı | 1.5 |
| `enrageSpeedMultiplier` | Enrage hız çarpanı | 1.3 |

### Stun Window Hasar Bonus

```
if (boss.isStunned):
    damageToBoss *= stunWindow.damageMultiplier (default 1.5)
```

## Edge Cases

| Durum | Ne olur |
|-------|---------|
| **Boss faz geçişi sırasında oyuncu saldırırsa** | Transition sırasında boss invulnerable (1-2s) |
| **Boss charge ile duvara çarpıp stun ama oyuncu uzakta** | Stun window tam süre devam eder — oyuncu koşup hasar vermeyi seçebilir veya heal/pozisyonlanma |
| **Boss ölürken faz geçiş eşiği geçilirse** | Overkill kontrolü: HP <= 0 → Dead, faz geçişi olmaz |
| **Oyuncu boss odasında ölürse** | Run Manager → RunEnd(defeat). Boss full HP'ye resetlenmez — run bitti. |
| **Boss stun sırasında status effect uygulanırsa** | Uygulanır — boss stun'a immune ama DoT gibi efektlere değil |
| **Enrage + Faz 3 aynı anda** | Çarpımsal: Faz 3 çarpanı × enrage çarpanı |

## Dependencies

**Upstream:**

| Sistem | Bağımlılık Tipi | Arayüz |
|--------|----------------|--------|
| Enemy System | Hard | Temel düşman altyapısı |
| Enemy AI | Hard | Behavior tree altyapısı |
| Combat System | Hard | Saldırı pipeline |
| Procedural Dungeon Generator | Hard | Boss odası |

**Downstream:** Yok — leaf node.

## Tuning Knobs

| Knob | Tip | Varsayılan | Güvenli Aralık | Etki |
|------|-----|-----------|----------------|------|
| `MinotaurMaxHP` | int | 500 | 300–800 | Boss HP. Düşük → kısa savaş. Yüksek → uzun, yorucu. |
| `MinotaurChargeTelegraph` | float (s) | 0.8 | 0.5–1.2 | Charge uyarı süresi. Kısa → zor dodge. Uzun → kolay. |
| `MinotaurStunDuration` | float (s) | 3.0 | 1.5–5.0 | Duvara çarpma stun süresi. Kısa → az hasar fırsatı. Uzun → cömert. |
| `EnrageTimer` | float (s) | 180 | 120–300 | Enrage başlangıcı. Kısa → DPS check. Uzun → sabırlı oyunculara tolerans. |
| `Phase2SpeedMultiplier` | float | 1.3 | 1.1–1.5 | Faz 2 hız artışı. |

## Visual/Audio Requirements

- **Boss intro**: İsim + title ekranda belirme ("MINOTAUR — Crypt Guardian"), 2-3s
- **Boss müziği**: Dark Fantasy Music (ELV pack) — normal dungeon'dan farklı, yoğun
- **Faz geçişi**: Kısa roar animasyonu + screen shake + renk değişimi (turuncu aura)
- **Charge**: Kızıl glow trail, duvar çarpması particle + screen shake
- **Ground Slam**: Zemin çatlak efekti, toz bulutu
- **Stun window**: Yıldız efekti + "STUN!" text pop-up — oyuncuya fırsat sinyali
- **Ölüm**: Dramatik yere çöküş, fade, loot explosion

## UI Requirements

- **Boss HP Bar**: Ekran altında büyük bar, boss ismi, faz göstergeleri (küçük marker'lar)
- **Enrage Timer**: (opsiyonel) Kalan süre göstergesi
- **Faz geçiş bildirimi**: "Phase 2" text pop-up

## Acceptance Criteria

| # | Kriter | Doğrulama Yöntemi |
|---|--------|-------------------|
| 1 | Boss intro oynar, kapılar kitlenir | Integration test: boss odasına gir → intro anim → kapılar kapalı |
| 2 | Faz geçişleri HP eşiklerinde tetiklenir | Unit test: HP 301→299 → Phase1→Transition→Phase2 |
| 3 | Charge duvara çarpınca stun window açılır | Integration test: charge → duvar → boss stunned 3s |
| 4 | Stun window'da hasar çarpanı aktif | Unit test: stun sırasında hasar × 1.5 |
| 5 | Boss ölümü Run Manager'a zafer bildirir | Integration test: boss HP=0 → RunEnd(victory=true) |
| 6 | Enrage timer'da boss güçlenir | Unit test: 180s → damage ×1.5, speed ×1.3 |
| 7 | Boss stagger'a immune | Unit test: normal knockback → 0 etki |
| 8 | Transition sırasında boss invulnerable | Unit test: transition'da hasar → 0 |

## Open Questions

1. Boss loot garantili mi? (Önerilen: Evet — guaranteed Epic+ item + gold pile. Boss'u yenmek ödüllendirici olmalı.)
2. Boss'un pakt etkileşimi var mı? (Önerilen: Katliam Paktı boss'u diriltmez — boss özel kural. Diğer paktlar normal çalışır.)
3. Faz 2-3 geçerken boss'un saldırı seti nasıl değişir? (Karar: Yeni saldırı eklenir, eskileri kalır. Faz 3'te charge sıklığı 2x.)
