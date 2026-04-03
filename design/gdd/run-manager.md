# Run Manager

> **Status**: Designed
> **Author**: User + Claude
> **Last Updated**: 2026-04-04
> **Implements Pillar**: Core Gameplay

## Overview

Run Manager, Dark Pact'in roguelite core loop'unu yöneten sistemdir. Run başlangıcı, pakt teklif zamanlaması (milestone'lar), akt ilerlemesi, run sonu (ölüm/zafer) ve run istatistiklerini takip eder. Game Manager'ın state machine'i üzerine inşa edilir — Game Manager "oyun durumunu" yönetirken, Run Manager "run durumunu" yönetir. Her run'ın seed'ini, seçilen paktları, geçilen odaları ve kazanılan loot'u kaydeder.

## Player Fantasy

Oyuncu her run'ın benzersiz olduğunu hisseder — farklı paktlar, farklı dungeon, farklı loot. Milestone'larda yeni pakt teklif edilmesi "güçleniyorum ama bedeli artıyor" gerilimini sürekli canlı tutar. Ölümde "bir daha denemeliyim, bu sefer şu paktı alırım" motivasyonu doğar. Run istatistikleri "ne kadar iyi oynadım" hissini verir.

## Detailed Design

### Core Rules

1. **Run lifecycle**: NewRun → PactSelection → DungeonPhase → (Milestone → PactSelection)* → BossPhase → RunEnd
2. **Run seed**: Her run'a benzersiz seed atanır. Aynı seed = aynı dungeon layout + aynı loot tablosu (replay/debug için)
3. **Milestone'lar**: Her X oda temizlendikten sonra yeni pakt teklif edilir. MVP'de milestone = akt ortası (8. oda)
4. **Akt ilerlemesi**: Akt 1 → Boss → Akt 2 → Boss → Akt 3 → Final Boss. MVP'de sadece Akt 1.
5. **Run istatistikleri**: Süre, öldürme sayısı, alınan hasar, toplanan altın, seçilen paktlar — run sonunda gösterilir
6. **Restart**: Run sonu ekranından "Yeni Run" seçildiğinde tüm run state sıfırlanır

### Run State

```
RunData
├── seed: int
├── currentAct: int (1-3)
├── currentRoom: int (akt içindeki oda numarası)
├── totalRoomsCleared: int
├── selectedPacts: List<PactDefinition>
├── activeClass: ClassDefinition
├── runTimer: float (geçen süre)
├── stats: RunStatistics
│   ├── killCount: int
│   ├── damageTaken: int
│   ├── damageDealt: int
│   ├── goldCollected: int
│   ├── itemsFound: int
│   └── roomsExplored: int
└── isCompleted: bool
```

### States and Transitions

| State | Açıklama | Geçiş |
|-------|----------|-------|
| **Inactive** | Run başlamamış (MainMenu) | → Starting (oyuncu Play'e basar) |
| **Starting** | Run initialize ediliyor (seed, class, initial state) | → PactSelection (otomatik) |
| **PactSelection** | Oyuncu 3 pakt arasından seçim yapıyor | → DungeonPhase (pakt seçildiğinde) |
| **DungeonPhase** | Aktif gameplay — odalar, combat, loot | → PactSelection (milestone), → BossPhase (akt sonu), → RunEnd (ölüm) |
| **BossPhase** | Boss savaşı aktif | → RunEnd (boss yenilgisi/ölüm), → Starting (sonraki akt — MVP sonrası) |
| **RunEnd** | Run bitti — istatistik ekranı | → Inactive (ana menüye dön), → Starting (yeni run) |

**Milestone tetikleyici:** `totalRoomsCleared % MilestoneInterval == 0`

### Interactions with Other Systems

| Sistem | Yön | Arayüz |
|--------|-----|--------|
| **Game Manager** | → talep eder | `RequestStateChange(Loading)` yeni run için, `RequestStateChange(GameOver)` run bittiğinde |
| **Pact System** | ↔ çift yönlü | `GeneratePactOptions(count)` → 3 pakt teklifi, `SelectPact(pact)` → seçim onayı |
| **Procedural Dungeon Generator** | → tetikler | `GenerateDungeon(seed, act, difficulty)` → dungeon oluştur |
| **Player Controller** | ← dinler | `OnPlayerDeath` → RunEnd tetikler |
| **Health & Damage** | ← istatistik alır | `OnDamaged` / `OnDeath` event'leri → run istatistiklerine eklenir |
| **Loot System** | ← istatistik alır | `OnItemPickedUp` → itemsFound++ |
| **Enemy System** | ← istatistik alır | `OnEnemyKilled` → killCount++ |
| **Room/Tilemap System** | ← dinler | `OnRoomCleared` → roomsCleared++, milestone kontrolü |
| **Meta Progression** | → bildirir | Run sonunda run verileri meta progression'a gönderilir |
| **Save/Load System** | → bildirir | Run sonunda meta save tetiklenir |

## Formulas

### Milestone Aralığı

```
milestoneInterval = BaseMilestoneInterval (sabit, oda sayısı)
nextMilestone = totalRoomsCleared + milestoneInterval
```

| Değişken | Tanım | Varsayılan |
|----------|-------|-----------|
| `BaseMilestoneInterval` | Kaç oda arasında pakt teklifi | 8 |

### Difficulty Scaling (akt içi)

```
roomDifficulty = baseActDifficulty + (currentRoom × DifficultyPerRoom)
```

| Değişken | Tanım | Varsayılan | Aralık |
|----------|-------|-----------|--------|
| `baseActDifficulty` | Akt başlangıç zorluğu | Akt1=1.0, Akt2=2.0, Akt3=3.0 | 1.0–5.0 |
| `DifficultyPerRoom` | Oda başına zorluk artışı | 0.1 | 0.05–0.3 |

**Örnek:** Akt 1, 5. oda → difficulty = 1.0 + (5 × 0.1) = 1.5. Bu değer Enemy System'a ve Loot System'a gönderilir.

### Run Score (istatistik)

```
runScore = (killCount × 10) + (goldCollected × 1) + (roomsExplored × 50) - (damageTaken × 2) + (timeBonus)
timeBonus = max(0, MaxTimeBonus - (runTimer × TimePenaltyPerSecond))
```

| Değişken | Tanım | Varsayılan |
|----------|-------|-----------|
| `MaxTimeBonus` | Maksimum zaman bonusu | 5000 |
| `TimePenaltyPerSecond` | Saniye başına bonus azalma | 5 |

## Edge Cases

| Durum | Ne olur |
|-------|---------|
| **Tüm paktlar zaten seçilmişse milestone'da** | Pakt havuzunda seçilmemiş pakt kalmadıysa milestone atlanır, DungeonPhase devam eder |
| **Oyuncu PactSelection'da oyunu kapatırsa** | Run state kaybolur — roguelite'ta mid-run save yok (MVP). Yeni run başlar. |
| **Boss yenildiğinde oyuncu da ölürse (trade kill)** | Boss ölümü öncelikli → zafer sayılır. Oyuncu HP 0'da ama run başarılı. |
| **Seed overflow** | int.MaxValue'ya ulaşırsa wrap-around (C# int overflow davranışı). Pratikte imkansız. |
| **Aynı run'da aynı pakt tekrar teklif edilirse** | Olmaz — GeneratePactOptions zaten seçilmiş paktları filtreler |
| **Run süresi çok uzarsa (AFK)** | Timer pause'da durur. GameOver'da toplam aktif süre gösterilir. |

## Dependencies

**Upstream:**

| Sistem | Bağımlılık Tipi | Arayüz |
|--------|----------------|--------|
| Game Manager | Hard | State change API, game state events |

**Downstream:**

| Sistem | Bağımlılık Tipi | Arayüz |
|--------|----------------|--------|
| Pact System | Hard | Milestone'da pakt teklifi tetikleme |
| Procedural Dungeon Generator | Hard | Dungeon generation tetikleme |
| Loot System | Soft | Difficulty değeri loot kalitesini etkiler |
| Enemy System | Soft | Difficulty değeri düşman gücünü etkiler |
| Meta Progression | Soft | Run sonucu verileri |

## Tuning Knobs

| Knob | Tip | Varsayılan | Güvenli Aralık | Etki |
|------|-----|-----------|----------------|------|
| `BaseMilestoneInterval` | int | 8 | 4–15 | Kaç oda sonra yeni pakt. Düşük → sık pakt, hızlı güçlenme. Yüksek → nadir pakt, her seçim daha değerli. |
| `RoomsPerAct` | int | 15 | 10–25 | Akt başına oda sayısı. Run uzunluğunu doğrudan belirler. MVP hedef: ~20dk → 15 oda × ~1dk/oda + boss. |
| `DifficultyPerRoom` | float | 0.1 | 0.05–0.3 | Oda başına zorluk artışı. Düşük → düz deneyim. Yüksek → son odalar çok zor. |
| `PactOptionsCount` | int | 3 | 2–5 | Milestone'da kaç pakt teklif edilir. 2 → az seçenek. 5 → karar felci. |

## Visual/Audio Requirements

- Run başlangıcı: Kısa "dungeon girişi" animasyonu veya fade-in
- Milestone: Pakt teklif ekranı karanlık/dramatik atmosfer (bkz. Pact Selection UI)
- Run sonu: İstatistik ekranı, run score ile birlikte (sayılar sırayla animasyonla açılır)
- Boss girişi: Kısa boss tanıtım animasyonu (isim + sprite)

## UI Requirements

- **Run HUD overlay**: Aktif paktlar ikonu, oda sayacı, akt göstergesi, run timer (opsiyonel)
- **Milestone bildirimi**: "Yeni Pakt!" uyarısı → Pact Selection UI'a geçiş
- **Run End Screen**: Run istatistikleri tablosu, run score, "Yeni Run" / "Ana Menü" butonları
- **Boss Intro**: Boss ismi + splash art (ELV pack sprite büyütülmüş)

## Acceptance Criteria

| # | Kriter | Doğrulama Yöntemi |
|---|--------|-------------------|
| 1 | Yeni run başlatılabilir, seed atanır | Unit test: NewRun → seed != 0, state = Starting |
| 2 | PactSelection milestone'larda tetiklenir | Unit test: 8. oda temizle → PactSelection state |
| 3 | Run istatistikleri doğru toplanır | Integration test: 5 düşman öldür → killCount = 5 |
| 4 | Oyuncu ölünce RunEnd tetiklenir | Integration test: HP=0 → RunEnd state, istatistik ekranı |
| 5 | Boss yenilince run başarılı biter | Integration test: boss HP=0 → RunEnd(victory=true) |
| 6 | Difficulty oda ilerledikçe artar | Unit test: room 1 → diff 1.1, room 5 → diff 1.5 |
| 7 | Aynı seed aynı dungeon üretir | Unit test: seed X ile 2 run → aynı layout |
| 8 | Yeni Run tüm state'i sıfırlar | Unit test: RunEnd → NewRun → killCount=0, pacts empty, room=0 |

## Open Questions

1. Mid-run save gerekli mi? (Önerilen: MVP'de yok — roguelite geleneği. Mobile port'ta gerekecek.)
2. Daily challenge (sabit seed, skor tablosu) olacak mı? (Önerilen: Full Vision — altyapı seed-based olduğu için hazır)
3. New Game+ mekanik değişiklikleri neler? (Önerilen: Faz 2'de tasarlanacak — difficulty multiplier + ek pakt slotu)
