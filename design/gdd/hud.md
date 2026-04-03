# HUD

> **Status**: Designed
> **Author**: User + Claude
> **Last Updated**: 2026-04-04
> **Implements Pillar**: Presentation

## Overview

HUD (Heads-Up Display), oyun sırasında ekranda sürekli görünen bilgi katmanıdır. Oyuncunun HP'si, aktif paktlar, skill cooldown'ları, aktif status effect'ler, ekipman, altın ve oda bilgisi burada gösterilir. Screen-space'te render edilir, kamera hareketinden etkilenmez. Minimal ve okunabilir olmalı — 16x16 pixel-art stiline uygun, ekranı kaplamayan.

## Player Fantasy

Oyuncu bir bakışta durumunu anlar: "HP'im düşük, dash cooldown'um var, zehirliyim." Bilgi açık ve hızlı okunur — combat sırasında HUD'a bakmak 0.5s'den fazla sürmemeli. HUD olmazsa oyuncu "can mı kaldı, skill hazır mı?" bilemez — kör dövüşür.

## Detailed Design

### Core Rules

1. **Screen-space overlay**: UI Toolkit veya Canvas (Screen Space - Overlay) ile render
2. **Pixel-perfect**: ELV Neatpixels font, ikon boyutları 16x16 veya 32x32
3. **Minimal footprint**: Ekranın max %15'i HUD ile kaplı
4. **Context-sensitive**: Bazı elementler sadece ilgili olduğunda görünür (düşman HP bar → hasar alınca)
5. **Pause'da gizlenmez**: HUD pause menüsünün altında kalır (dimmed)

### HUD Layout

```
┌──────────────────────────────────────────┐
│ [HP Bar]──────────── [Pact Icons] [Gold] │  ← Sol üst: HP, Sağ üst: Paktlar, Altın
│ [TempHP overlay]                         │
│ [Status Effects]                         │  ← Sol üst alt: Buff/debuff ikonları
│                                          │
│                                          │
│                                          │
│            (Gameplay area)               │
│                                          │
│                                          │
│                                          │
│ [Skill 1][Skill 2][Skill 3][Skill 4]    │  ← Sol alt: Skill bar
│ [Weapon Icon]          [Room X/Y]        │  ← Sağ alt: Silah + oda sayacı
└──────────────────────────────────────────┘
```

### HUD Elementleri

| Element | Pozisyon | Veri Kaynağı | Güncelleme |
|---------|----------|-------------|------------|
| **HP Bar** | Sol üst | Health & Damage `OnHealthChanged` | Her HP değişiminde |
| **TempHP Bar** | HP bar üstünde (sarı katman) | Health & Damage `OnTempHPChanged` | Her TempHP değişiminde |
| **Delayed Health Bar** | HP bar arkasında (kırmızı→koyu kırmızı) | HP düştüğünde 0.5s gecikmeyle takip | Animasyonlu |
| **Status Effect Icons** | Sol üst, HP altında | Status Effect System `OnEffectChanged` | Efekt eklenince/kaldırılınca |
| **Pact Icons** | Sağ üst | Pact System `activePacts` | Pakt seçildiğinde |
| **Gold Counter** | Sağ üst, paktların altı | Loot System `OnGoldChanged` | Altın alınca |
| **Skill Bar** | Sol alt | Combat System skill cooldowns | Her frame (cooldown timer) |
| **Weapon Icon** | Sol alt, skill bar yanı | Equipment System `equippedWeapon` | Equip değişince |
| **Room Counter** | Sağ alt | Run Manager `currentRoom/totalRooms` | Oda değişince |
| **Dash Cooldown** | Skill bar yanında (opsiyonel) | Player Controller `dashCooldown` | Her frame |

### Enemy HP Bars (World-space)

| Element | Pozisyon | Davranış |
|---------|----------|----------|
| **Normal düşman HP** | Düşman üstünde, küçük | Hasar alınca 3s görünür, sonra fade |
| **Elite düşman HP** | Düşman üstünde, orta | Her zaman görünür, isim ile |
| **Boss HP** | Ekran altı, büyük | Her zaman görünür, faz marker'ları |

### Damage Numbers (World-space)

| Tip | Renk | Boyut | Animasyon |
|-----|------|-------|-----------|
| Physical | Beyaz | Normal | Yukarı float + fade (0.8s) |
| Fire | Turuncu | Normal | Yukarı float + fade |
| Ice | Mavi | Normal | Yukarı float + fade |
| Poison | Yeşil | Normal | Yukarı float + fade |
| Lightning | Sarı | Normal | Yukarı float + fade |
| Dark | Mor | Normal | Yukarı float + fade |
| Critical | Beyaz | Büyük (×1.5) | Bounce + shake + fade (1.0s) |
| Heal | Yeşil | Normal | Yukarı float |

### States and Transitions

| State | Açıklama |
|-------|----------|
| **Visible** | Playing state'inde tam görünür |
| **Dimmed** | Paused state'inde yarı-saydam |
| **Hidden** | MainMenu, Loading, GameOver'da gizli |

### Interactions with Other Systems

| Sistem | Yön | Arayüz |
|--------|-----|--------|
| **Health & Damage** | ← event dinler | HP, TempHP, damage number verileri |
| **Combat System** | ← event dinler | Skill cooldown'ları |
| **Pact System** | ← event dinler | Aktif pakt ikonları |
| **Status Effect System** | ← event dinler | Aktif buff/debuff ikonları + süreleri |
| **Equipment System** | ← event dinler | Silah ikonu |
| **Loot System** | ← event dinler | Altın sayacı |
| **Run Manager** | ← event dinler | Oda sayacı |
| **Player Controller** | ← event dinler | Dash cooldown |
| **Game Manager** | ← event dinler | State'e göre visible/dimmed/hidden |

## Formulas

### Delayed Health Bar

```
delayedHP lerps toward currentHP at DelayedBarSpeed per second
starts moving after DelayedBarDelay seconds
```

| Değişken | Tanım | Varsayılan |
|----------|-------|-----------|
| `DelayedBarDelay` | Gecikmeli bar bekleme süresi (s) | 0.5 |
| `DelayedBarSpeed` | Gecikmeli bar hızı (HP/s) | 50.0 |

### Damage Number Animation

```
position.y += FloatSpeed × deltaTime
alpha -= FadSpeed × deltaTime (after FloatDuration × 0.5)
```

## Edge Cases

| Durum | Ne olur |
|-------|---------|
| **Çok fazla status effect aktifse** | Max 8 ikon gösterilir, fazlası "+N" ile belirtilir |
| **Çok fazla damage number aynı anda** | Object pool, max 20 aktif. En eski recycle. |
| **HP bar ile TempHP bar toplamı MaxHP'yi aşarsa** | TempHP bar HP bar'ın sağına taşar (ek uzunluk) |
| **Boss HP bar ile normal HUD çakışırsa** | Boss bar ekran altında, HUD üstte/yanlarda — çakışmaz |
| **Resolution değişirse** | UI anchoring ile responsive. Minimum 1280x720 desteklenir. |

## Dependencies

**Upstream:**

| Sistem | Bağımlılık Tipi | Arayüz |
|--------|----------------|--------|
| Health & Damage | Hard | HP verileri |
| Combat System | Hard | Skill cooldown verileri |
| Pact System | Hard | Aktif pakt verileri |
| Skill Tree | Soft | Pakt Güçlendirme dalı bilgisi (VS) |

**Downstream:** Yok — pure presentation.

## Tuning Knobs

| Knob | Tip | Varsayılan | Güvenli Aralık | Etki |
|------|-----|-----------|----------------|------|
| `EnemyHPBarVisibleDuration` | float (s) | 3.0 | 1.0–5.0 | Düşman HP bar görünme süresi. |
| `DamageNumberFloatSpeed` | float (unit/s) | 1.5 | 0.5–3.0 | Damage number yükselme hızı. |
| `LowHPThreshold` | float | 0.25 | 0.15–0.4 | Bu HP altında kırmızı vignette + kalp atışı. |
| `MaxStatusEffectIcons` | int | 8 | 4–12 | Görünen status effect ikon limiti. |

## Visual/Audio Requirements

- ELV Neatpixels font: Boss (7×7) başlıklar, Standard metinler
- ELV GUI Kit elemanları (bar frame, buton stilleri)
- HP bar: Kırmızı dolgu, gri arka plan, sarı TempHP katmanı
- Düşük HP: Ekran kenarlarında kırmızı vignette + kalp atışı SFX
- Pakt ikonları: Pakt renginde ışıldama animasyonu
- Skill cooldown: Radial wipe overlay (saat yönünde)

## UI Requirements

- **Responsive layout**: 16:9 ve 16:10 oranlarında çalışır
- **UI Scale**: Ayarlar menüsünde %80-%120 scale seçeneği (accessibility)
- **Colorblind mode**: (VS) Rarity ve damage type renkleri + şekil/sembol ile desteklenir

## Acceptance Criteria

| # | Kriter | Doğrulama Yöntemi |
|---|--------|-------------------|
| 1 | HP bar hasar alınca anında güncellenir | Visual test: hasar al → bar düşer |
| 2 | Delayed health bar 0.5s sonra takip eder | Visual test: hasar al → kırmızı arkasından koyu kırmızı kayar |
| 3 | Skill cooldown radial wipe doğru çalışır | Visual test: skill kullan → cooldown dolana kadar wipe |
| 4 | Damage numbers doğru renk ve boyutta | Visual test: fire hasar → turuncu, crit → büyük |
| 5 | Düşman HP bar hasar alınca görünür, 3s sonra kaybolur | Visual test: düşmana vur → bar görünür → 3s → fade |
| 6 | Pause'da HUD dimmed | Visual test: ESC → HUD yarı-saydam |
| 7 | Düşük HP uyarısı çalışır | Visual test: HP < %25 → vignette + SFX |
| 8 | Pakt ikonu pakt seçilince eklenir | Integration test: pakt seç → ikon HUD'da görünür |

## Open Questions

1. Minimap HUD'da mı yoksa ayrı menüde mi? (Önerilen: VS'de HUD köşesinde küçük minimap.)
2. DPS meter gösterilecek mi? (Önerilen: Hayır — roguelite'ta gereksiz, istatistikler run sonunda.)
3. Combo counter olacak mı? (Önerilen: VS'de — combo sistemi ile birlikte.)
