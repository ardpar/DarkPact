# Health & Damage

> **Status**: Designed
> **Author**: User + Claude
> **Last Updated**: 2026-04-03
> **Implements Pillar**: Core Gameplay

## Overview

Health & Damage, Dark Pact'teki tüm canlı varlıkların (oyuncu, düşmanlar, bosslar) can puanlarını ve hasar hesaplamalarını yöneten sistemdir. HP takibi, hasar alma, iyileşme, ölüm tespiti ve geçici can (shield) mekanikleri bu sisteme aittir. Darboğaz sistemdir — Combat, Pact, Enemy, Status Effect ve Boss sistemlerinin tümü bu sisteme bağımlıdır. Tüm hasar ve iyileşme olayları event-driven olarak duyurulur.

## Player Fantasy

Her hasar anlamlı hisseder — oyuncu "bu canıma mal oldu" der, rastgele değil. HP azaldıkça gerilim artar. Geçici can (Kan Kalkanı paktı) "kazandım, bir adım öndeyim" güvenini verir. Ölüm hiçbir zaman "unfair" hissetmez — oyuncu neden öldüğünü anlayabilir.

## Detailed Design

### Core Rules

1. **HP modeli**: Her varlık `CurrentHP`, `MaxHP` ve `TemporaryHP` (geçici can/shield) taşır
2. **Hasar alma sırası**: Gelen hasar önce `TemporaryHP`'den düşer, kalan `CurrentHP`'den düşer
3. **Hasar hesabı**: `finalDamage = baseDamage × damageMultiplier - defense` (minimum 1 hasar garanti)
4. **İyileşme**: `CurrentHP` asla `MaxHP`'yi aşamaz. `TemporaryHP` ayrı limiti var ve zamanla decay eder
5. **Ölüm**: `CurrentHP <= 0` → `OnDeath` event. Oyuncu için Run Manager'a bildirilir, düşman için Enemy System'a
6. **Hasar tipleri**: Physical, Fire, Ice, Poison, Lightning, Dark — her tipin ayrı resistance'ı olabilir
7. **Event'ler**: Tüm hasar/iyileşme/ölüm event-driven → HUD, VFX, Audio hepsine bildirilir

### Damage Pipeline

```
Raw Damage (kaynak: silah + skill + pakt bonus)
    ↓
Damage Type çarpanı (elemental weakness/resistance)
    ↓
Defense çıkarımı (zırh + pakt modifier)
    ↓
Final Damage (min 1)
    ↓
TemporaryHP'den düş → kalan CurrentHP'den düş
    ↓
Events: OnDamaged(amount, type, source), OnHealthChanged(current, max)
    ↓
CurrentHP <= 0 → OnDeath(source)
```

### States and Transitions

| State | Açıklama | Geçiş |
|-------|----------|-------|
| **Alive** | HP > 0, normal gameplay | → Dead (HP <= 0) |
| **Dead** | HP = 0, varlık devre dışı | Terminal (oyuncu: → Run Manager, düşman: → loot drop) |
| **Invulnerable** | Hasar alınamaz (dash, oda geçişi) | → Alive (süre bitince) |

### Interactions with Other Systems

| Sistem | Yön | Arayüz |
|--------|-----|--------|
| **Game Manager** | ← dinler | Paused'da hasar hesabı durur (timer-based damage tick'ler pause'lanır) |
| **Combat System** | ← hasar alır | `ApplyDamage(target, baseDamage, damageType, source)` |
| **Pact System** | ← modifier alır | Katliam Paktı → +%60 hasar, Kan Kalkanı → öldürmede +5 TemporaryHP |
| **Status Effect System** | ← tick hasar alır | Zehir/yanma DoT → `ApplyDamage` her tick |
| **Equipment System** | ← stat alır | Silah → baseDamage, Zırh → defense |
| **Player Controller** | → bildirir | `OnDamaged` → Hit state tetikler, `OnDeath` → Death state |
| **Enemy System** | → bildirir | `OnDeath` → düşman ölüm, loot drop |
| **HUD** | → bildirir | `OnHealthChanged` → HP bar güncelleme |
| **VFX System** | → bildirir | `OnDamaged` → hit flash, damage number, screen shake trigger |

## Formulas

### Hasar Hesabı

```
elementalMultiplier = 1.0 + targetWeakness[damageType] - targetResistance[damageType]
damageMultiplier = product(all active pact/buff damageMultipliers)  // Çarpımsal stacking
rawReduced = baseDamage × damageMultiplier × elementalMultiplier
finalDamage = max(1, floor(rawReduced - targetDefense))
```

| Değişken | Tanım | Varsayılan | Aralık |
|----------|-------|-----------|--------|
| `baseDamage` | Silah/skill temel hasarı | silaha bağlı | 5–200 |
| `damageMultiplier` | Pakt + buff çarpanı | 1.0 | 0.5–3.0 |
| `elementalMultiplier` | Element zayıflık/direnç | 1.0 | 0.5–2.0 |
| `targetDefense` | Zırh + buff savunma | 0 | 0–50 |
| `weakness/resistance` | Element başına oran | 0.0 | -0.5–1.0 |

**Örnek:** Kılıç (baseDamage=20) + Katliam Paktı (×1.6) vs düşman (defense=5, no weakness)
→ 20 × 1.6 × 1.0 = 32 - 5 = 27 hasar

### Geçici Can (TemporaryHP)

```
tempHP = min(currentTempHP + gainAmount, MaxTemporaryHP)
tempHP decay: tempHP -= DecayRate × deltaTime (sadece combat dışında)
```

| Değişken | Tanım | Varsayılan |
|----------|-------|-----------|
| `MaxTemporaryHP` | Geçici can limiti | MaxHP × 0.5 |
| `DecayRate` | Combat dışı saniye başı azalma | 1.0/s |
| `GainAmount` (Kan Kalkanı) | Öldürme başına kazanım | 5 |

### İyileşme

```
newHP = min(CurrentHP + healAmount, MaxHP)
```

HP iksiri iyileşmesi: `healAmount = MaxHP × HealPercent`

| Değişken | Tanım | Varsayılan |
|----------|-------|-----------|
| `HealPercent` | HP iksiri iyileşme oranı | 0.30 (MaxHP'nin %30'u) |

## Edge Cases

| Durum | Ne olur |
|-------|---------|
| **Hasar ve iyileşme aynı frame'de olursa** | Hasar önce uygulanır, sonra iyileşme. Ölüm kontrolü hasar sonrası yapılır — iyileşme ölümü engellemez. |
| **TemporaryHP hasardan fazlaysa** | Hasar tamamen TemporaryHP'den düşer, CurrentHP etkilenmez |
| **Hasar 0 veya negatif hesaplanırsa** | Minimum 1 hasar garanti — defense hasarı sıfırlayamaz |
| **MaxHP değişirse (buff/debuff)** | CurrentHP oranı korunur. MaxHP 100→80 iken CurrentHP 50 → 40. MaxHP 80→100 iken CurrentHP 40 → 50. |
| **Oyuncu ölürken invulnerable aktifse** | Invulnerable hasar alımını engeller → ölüm olmaz. Süre bitince normal hasar devam eder. |
| **Kan Kalkanı paktı + can iksiri yok kısıtı** | Heal metodu pakt kısıtını kontrol eder. `CanHeal()` false dönerse iyileşme uygulanmaz. Pact System bu flag'i yönetir. |
| **Aynı kaynaktan çoklu hasar (shotgun pattern)** | Her projectile ayrı `ApplyDamage` çağrısı — her biri bağımsız hesaplanır |
| **Overkill (HP'den fazla hasar)** | CurrentHP 0'a düşer, fazla hasar yok sayılır. Overkill miktarı `OnDeath` event'inde raporlanır (ileride istatistik için). |

## Dependencies

**Upstream:**

| Sistem | Bağımlılık Tipi | Arayüz |
|--------|----------------|--------|
| Game Manager | Hard | `OnGameStateChanged` → timer-based tick'ler pause |
| Pact System | Soft | `CanHeal()` kontrolü: iyileşme uygulanmadan önce Pact System'ın `IsHealingRestricted()` sorgulanır. Kan Kalkanı aktifse heal ve HoT (Regen) engellenir. |

**Downstream:**

| Sistem | Bağımlılık Tipi | Arayüz |
|--------|----------------|--------|
| Combat System | Hard | `ApplyDamage()`, `Heal()` API |
| Status Effect System | Hard | DoT hasar `ApplyDamage()` ile uygulanır |
| Enemy System | Hard | Düşman HP yönetimi, `OnDeath` event |
| Pact System | Hard | Hasar çarpanları, TemporaryHP kazanımı, iyileşme kısıtları |
| Player Controller | Hard | `OnDamaged` → stagger, `OnDeath` → death state |
| HUD | Soft | `OnHealthChanged` → HP bar |
| VFX System | Soft | `OnDamaged` → hit efektleri |

## Tuning Knobs

| Knob | Tip | Varsayılan | Güvenli Aralık | Etki |
|------|-----|-----------|----------------|------|
| `PlayerBaseMaxHP` | int | 100 | 50–200 | Oyuncu başlangıç HP. Düşük → kırılgan, gerilimli. Yüksek → tanky, rahat. |
| `MinimumDamage` | int | 1 | 1–5 | Defense'i aşamayan hasarın minimum değeri. 1 → her vuruş bir şey yapar. |
| `TempHPMaxRatio` | float | 0.5 | 0.25–1.0 | TemporaryHP limiti MaxHP'nin yüzdesi. |
| `TempHPDecayRate` | float (HP/s) | 1.0 | 0.0–5.0 | Combat dışı TemporaryHP azalma hızı. 0 → decay yok. 5 → hızlı erime. |
| `HealPotionPercent` | float | 0.30 | 0.15–0.50 | HP iksiri iyileşme oranı. |
| `InvulnerabilityFlashRate` | float (Hz) | 10.0 | 5.0–20.0 | Invulnerable sırasında görsel flash hızı (VFX'e gönderilir). |

## Visual/Audio Requirements

- Hasar alınca karakter beyaz flash (0.1s) — VFX System'dan
- Damage numbers: Hasar miktarı hedefin üstünde pop-up (renk: beyaz=physical, kırmızı=fire, mavi=ice, yeşil=poison, sarı=lightning, mor=dark)
- HP bar düşerken kırmızı → koyu kırmızıya animasyon (delayed health bar efekti)
- TemporaryHP bar: HP bar'ın üstünde farklı renkte (sarı/altın) katman
- İyileşme: Yeşil particle burst + HP bar yeşil flash
- Ölüm: Karakter sprite fade + kan efekti
- Hasar alma SFX: Hasar tipine göre farklı (kılıç çarpma, ateş yanma, zehir sızlama)
- Düşük HP uyarısı: HP < %25 → kalp atışı SFX + ekran kenarlarında kırmızı vignette

## UI Requirements

- **HP Bar** (HUD): CurrentHP / MaxHP, TemporaryHP ek katman, delayed health bar animasyonu
- **Damage Numbers**: Pop-up floating text, hasar tipine göre renk, crit için büyük font
- **Düşman HP Bar**: Düşmanın üstünde küçük bar, hasar alınca görünür, 3s sonra fade
- **Boss HP Bar**: Ekranın altında büyük, isim + HP yüzdesi

## Acceptance Criteria

| # | Kriter | Doğrulama Yöntemi |
|---|--------|-------------------|
| 1 | Hasar pipeline doğru sırada çalışır (elemental → defense → min 1) | Unit test: bilinen değerlerle hasar hesabı → beklenen sonuç |
| 2 | TemporaryHP önce düşer, kalan CurrentHP'den düşer | Unit test: TempHP=10, Damage=15 → TempHP=0, CurrentHP -5 |
| 3 | CurrentHP <= 0 → OnDeath event fırlatılır | Unit test: ölümcül hasar → event tetiklenir |
| 4 | Minimum hasar 1 garanti | Unit test: defense > baseDamage → finalDamage = 1 |
| 5 | İyileşme MaxHP'yi aşamaz | Unit test: HP=90, MaxHP=100, heal=20 → HP=100 |
| 6 | Invulnerable sırasında hasar 0 | Unit test: invulnerable flag aktif → ApplyDamage etki etmez |
| 7 | MaxHP değiştiğinde oran korunur | Unit test: MaxHP 100→80, HP 50 → HP 40 |
| 8 | Paused'da tick hasar durur | Unit test: Paused state'inde DoT timer ilerlemez |

## Open Questions

1. Critical hit sistemi MVP'de mi? (Önerilen: MVP'de basit — %10 şans, ×2 hasar. Formül ve tuning knob Loot/Equipment ile genişler)
2. Elemental weakness/resistance tablosu nasıl tanımlanacak? (Önerilen: ScriptableObject, düşman başına tanımlı)
3. Damage numbers stilistik mi yoksa kesin sayı mı? (Önerilen: Kesin sayı — roguelite oyuncuları optimizasyon sever, sayıları görmek ister)
