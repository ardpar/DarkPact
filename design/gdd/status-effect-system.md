# Status Effect System

> **Status**: Designed
> **Author**: User + Claude
> **Last Updated**: 2026-04-04
> **Implements Pillar**: Core Gameplay

## Overview

Status Effect System, Dark Pact'teki tüm süreye bağlı efektleri (buff/debuff) yöneten sistemdir. Zehir, yanma, yavaşlama, hızlanma, hasar artışı gibi efektlerin uygulanması, tick hesaplaması, süresi dolduğunda kaldırılması ve görsel feedback'i bu sisteme aittir. Pakt sistemiyle derin entegrasyonu vardır — birçok pakt bane'i status effect olarak uygulanır (Lanetli Dokunuş → self-poison, Gölge Adımı → durma hasarı).

## Player Fantasy

Status effect'ler "stratejik katman" hissi yaratır. Düşmanı zehirleyip kaçmak, yanma hasarının son tick'iyle öldürmek, hızlanma buff'ıyla boss mekaniğinden kurtulmak — bunlar "zeki oynadım" anlarıdır. Pakt bane'lerinin status effect olarak hissedilmesi "bedelini ödüyorum" gerilimini sürekli canlı tutar.

## Detailed Design

### Core Rules

1. **Effect Stack Model**: Aynı tipten efektler stack etmez — süre yenilenir (duration refresh). Farklı tipten efektler bağımsız çalışır.
2. **Tick sistemi**: DoT (Damage over Time) efektler `TickInterval` aralığında hasar uygular. Tick'ler `Time.deltaTime` tabanlı, pause'da durur.
3. **Duration**: Her efektin toplam süresi var. Süre dolunca otomatik kaldırılır.
4. **Effect tanımları**: ScriptableObject tabanlı — yeni effect eklemek kod gerektirmez.
5. **Immunity**: Bazı varlıklar belirli efektlere immune olabilir (boss'lar stun'a immune gibi).
6. **Cleanse**: Bazı efektler temizlenebilir (antidot → zehir kaldır). MVP'de cleanse item yok, VS'de.

### Effect Tipleri

| Effect | Tip | Davranış | Kaynak |
|--------|-----|----------|--------|
| **Poison** | DoT | Tick başına sabit hasar | Lanetli Dokunuş paktı, zehir silahları, düşman saldırıları |
| **Burn** | DoT | Tick başına hasar, ilk tick daha güçlü | Ateş büyüleri, meşale tuzakları |
| **Slow** | Debuff | Hareket hızı azalır | Buz büyüleri, düşman yeteneği |
| **Speed** | Buff | Hareket hızı artar | Buff iksiri, pakt efekti |
| **DamageUp** | Buff | Hasar çarpanı artar | Katliam Paktı, buff skill |
| **Bleed** | DoT | Hareket ettikçe hasar (durma = hasar yok) | Bazı düşman saldırıları |
| **SelfDamage** | DoT | Oyuncuya sürekli hasar (Gölge Adımı bane: durma hasarı) | Pakt bane'leri |
| **Regen** | HoT | Tick başına iyileşme | Nadir buff, bazı pakt etkileşimleri |

### StatusEffect ScriptableObject

```
StatusEffectDefinition (ScriptableObject)
├── effectId: string
├── effectName: string
├── description: string
├── effectType: EffectType enum (DoT, Buff, Debuff, HoT)
├── duration: float (saniye)
├── tickInterval: float (saniye, DoT/HoT için)
├── tickDamage: float (DoT için)
├── tickHeal: float (HoT için)
├── statModifiers: Dictionary<StatType, float> (Buff/Debuff için)
│   ├── MoveSpeed: float (çarpan, ör: 0.5 = %50 yavaşlama)
│   ├── DamageMultiplier: float
│   └── Defense: float
├── damageType: DamageType enum (DoT hasar tipi)
├── stackBehavior: StackBehavior enum (RefreshDuration, Stack, None)
├── maxStacks: int (Stack davranışında)
├── vfxType: VFXType enum (görsel efekt)
├── icon: Sprite (HUD göstergesi)
└── immunityTags: List<string> (bu tag'e sahip varlıklara uygulanmaz)
```

### States and Transitions

Her aktif efekt instance'ı:

| State | Açıklama | Geçiş |
|-------|----------|-------|
| **Applied** | Efekt yeni uygulandı, ilk tick | → Active |
| **Active** | Efekt devam ediyor, tick'ler çalışıyor | → Expired (süre doldu), → Cleansed (temizlendi) |
| **Expired** | Süre doldu, efekt kaldırılıyor | → (removed) |
| **Cleansed** | Dışarıdan temizlendi | → (removed) |

### Interactions with Other Systems

| Sistem | Yön | Arayüz |
|--------|-----|--------|
| **Health & Damage** | → hasar/heal gönderir | DoT tick → `ApplyDamage()`, HoT tick → `Heal()` |
| **Combat System** | ← tetikler | Saldırı hit → `ApplyEffect(target, effectDef)` |
| **Pact System** | ← tetikler | Pakt seçildiğinde sürekli efektler uygulanır (SelfDamage, DamageUp vb.) |
| **Player Controller** | → modifier sağlar | Stat modifier'lar (MoveSpeed, DamageMultiplier) Player Controller'a iletilir |
| **Enemy System** | ↔ çift yönlü | Düşmanlar da efekt alabilir/uygulayabilir |
| **VFX System** | → tetikler | Efekt başlangıcında looping VFX, bitişinde stop |
| **HUD** | → bildirir | Aktif efekt ikonları + kalan süre |
| **Game Manager** | ← dinler | Paused'da tick timer'lar durur |

## Formulas

### DoT Hasar

```
tickDamage = baseTickDamage × sourceMultiplier
totalDamage = tickDamage × (duration / tickInterval)
```

| Değişken | Tanım | Varsayılan |
|----------|-------|-----------|
| `baseTickDamage` (Poison) | Zehir tick hasarı | 3 |
| `baseTickDamage` (Burn) | Yanma tick hasarı | 5 |
| `tickInterval` | Tick aralığı (s) | 0.5 |
| `duration` (Poison) | Zehir süresi (s) | 4.0 |
| `duration` (Burn) | Yanma süresi (s) | 3.0 |

**Örnek:** Poison → 3 hasar × (4.0 / 0.5) = 3 × 8 tick = 24 toplam hasar

### Stat Modifier Uygulama

```
finalStat = baseStat × product(all active modifiers for this stat)
```

**Örnek:** MoveSpeed=5.0, Slow modifier=0.6, Speed modifier=1.3 → 5.0 × 0.6 × 1.3 = 3.9

### Gölge Adımı Bane (Durma Hasarı)

```
if (playerVelocity.magnitude < StationaryThreshold):
    stationaryTimer += deltaTime
    if (stationaryTimer >= StationaryDamageInterval):
        ApplyDamage(player, StationaryDamagePerTick)
        stationaryTimer = 0
else:
    stationaryTimer = 0
```

| Değişken | Tanım | Varsayılan |
|----------|-------|-----------|
| `StationaryThreshold` | Durma eşiği (unit/s) | 0.1 |
| `StationaryDamageInterval` | Durma hasar aralığı (s) | 1.0 |
| `StationaryDamagePerTick` | Tick başına hasar | 5 |

## Edge Cases

| Durum | Ne olur |
|-------|---------|
| **Aynı DoT tekrar uygulanırsa** | Duration refresh — süre yeniden başlar, stack etmez (default). Stack davranışlı efektler maxStacks'e kadar birikir. |
| **Buff ve debuff aynı stat'ı etkilerse** | Çarpımsal: MoveSpeed × slowMod × speedMod. İkisi de aktifse net etki hesaplanır. |
| **Hedef efekt sırasında ölürse** | Efekt otomatik kaldırılır. Ölüm anında aktif DoT'lar son hasarı uygulamaz. |
| **Pause sırasında aktif efektler** | Tick timer'lar durur, süre uzamaz. Resume'da kaldığı yerden devam. VFX pause edilir. |
| **Immune hedef'e efekt uygulanmaya çalışılırsa** | `ApplyEffect` false döner, efekt uygulanmaz. "Immune" text gösterilebilir. |
| **Gölge Adımı + Yavaşlama aynı anda** | Oyuncu yavaşlamış halde hareket ederse durma eşiğinin üstünde kalabilir → durma hasarı yok. Tam yavaşlama = hareket edemez → durma hasarı aktif. |
| **Regen + Can iksiri yok (Kan Kalkanı bane)** | Regen bir iyileşme efektidir. Pact System `CanHeal()` kontrolü Regen tick'lerinde de uygulanır → Kan Kalkanı aktifken Regen çalışmaz. |

## Dependencies

**Upstream:**

| Sistem | Bağımlılık Tipi | Arayüz |
|--------|----------------|--------|
| Health & Damage | Hard | DoT/HoT tick'leri hasar/heal API'si kullanır |
| Combat System | Hard | Saldırılar efekt tetikler |

**Downstream:**

| Sistem | Bağımlılık Tipi | Arayüz |
|--------|----------------|--------|
| Pact System | Hard | Pakt bane'leri status effect olarak uygulanır |
| Player Controller | Soft | Stat modifier'lar (hız, hasar) |
| Enemy System | Soft | Düşman stat modifier'lar |
| HUD | Soft | Aktif efekt göstergesi |

## Tuning Knobs

| Knob | Tip | Varsayılan | Güvenli Aralık | Etki |
|------|-----|-----------|----------------|------|
| `PoisonTickDamage` | float | 3.0 | 1.0–10.0 | Zehir tick hasarı. Düşük → hafif rahatsızlık. Yüksek → tehlikeli. |
| `BurnTickDamage` | float | 5.0 | 2.0–15.0 | Yanma tick hasarı. |
| `SlowModifier` | float | 0.6 | 0.3–0.9 | Yavaşlama çarpanı. 0.3 → neredeyse durma. 0.9 → hafif yavaşlama. |
| `DefaultTickInterval` | float (s) | 0.5 | 0.25–1.0 | Tick aralığı. Kısa → sık hasar. Uzun → nadir ama güçlü tick. |
| `StationaryDamagePerTick` | float | 5.0 | 2.0–10.0 | Gölge Adımı durma hasarı. Oyuncuyu hareket etmeye zorlamalı ama insta-kill olmamalı. |

## Visual/Audio Requirements

- **Poison**: Yeşil particle hedef üzerinde, yeşil tint
- **Burn**: Turuncu alev particle, turuncu tint
- **Slow**: Mavi kristal/buz particle, mavi tint
- **Speed**: Sarı hız çizgileri particle
- **Bleed**: Kırmızı damla particle
- **SelfDamage**: Koyu mor aura (pakt kaynaklı olduğunu gösterir)
- Her DoT tick'inde küçük damage number pop-up
- Efekt sona erdiğinde kısa "dissipate" animasyonu

## UI Requirements

- **Buff/Debuff bar** (HUD): Oyuncunun aktif efektleri ikon olarak sıralanır, kalan süre radial overlay
- **Debuff büyük gösterim**: Pakt bane'leri (SelfDamage gibi) diğer debuff'lardan farklı çerçeveyle gösterilir (mor çerçeve)
- Düşman üzerinde: Aktif efekt ikonu düşman HP bar'ının altında

## Acceptance Criteria

| # | Kriter | Doğrulama Yöntemi |
|---|--------|-------------------|
| 1 | Poison tick'leri doğru aralıkta hasar verir | Unit test: Poison(3dmg, 0.5s tick, 4s) → 8 tick × 3 = 24 toplam |
| 2 | Aynı efekt tekrar uygulandığında süre yenilenir, stack etmez | Unit test: Poison uygula, 2s sonra tekrar uygula → süre 4s'e resetlenir |
| 3 | Slow modifier hareket hızını düşürür | Integration test: Slow(0.6) → hız %40 azalır |
| 4 | Buff + Debuff aynı stat'ta çarpımsal çalışır | Unit test: Speed(1.3) + Slow(0.6) → net modifier = 0.78 |
| 5 | Immune hedefte efekt uygulanmaz | Unit test: boss + stun immunity → ApplyEffect false |
| 6 | Pause'da tick'ler durur | Unit test: Paused → 5s bekle → tick count değişmez |
| 7 | Hedef ölünce efektler temizlenir | Unit test: target OnDeath → aktif efekt sayısı = 0 |
| 8 | Gölge Adımı durma hasarı çalışır | Integration test: velocity < 0.1 → 1s sonra 5 hasar |

## Open Questions

1. Efekt stack davranışı bazı efektler için farklı olmalı mı? (Önerilen: MVP'de hep RefreshDuration. Stack özel efektler VS'de.)
2. Elemental reaction sistemi (ateş + buz = buhar) olacak mı? (Önerilen: Full Vision — çok güzel ama scope riski çok yüksek)
3. Cleanse mekanik MVP'de gerekli mi? (Önerilen: Hayır — MVP'de efektler süre ile biter. Antidot VS'de.)
