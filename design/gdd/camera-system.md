# Camera System

> **Status**: Designed
> **Author**: User + Claude
> **Last Updated**: 2026-04-03
> **Implements Pillar**: Core Infrastructure

## Overview

Camera System, Dark Pact'in top-down kamerasını yönetir. Oyuncuyu merkeze alarak takip eder, oda sınırları içinde kalır ve oda geçişlerinde yumuşak pan yapar. Screen shake desteği ile combat feedback'e katkıda bulunur. Pixel-perfect rendering sağlayarak 16x16 tile'ların net ve kırpılmadan görünmesini garanti eder.

## Player Fantasy

Oyuncu kamerayı fark etmez — sadece aksiyon netçe görünür. Oda sınırları dışını görmez (sürpriz korunur), oda geçişleri cinematik hisseder ama hızlıdır. Combat sırasında screen shake "impact" hissi verir. Kamera asla oyuncunun aleyhine çalışmaz — düşmanlar her zaman görüş alanında veya hemen dışında yaklaşır.

## Detailed Design

### Core Rules

1. **Pixel Perfect Camera** — Unity URP Pixel Perfect Camera component'i kullanılır. Reference resolution: 320x180 (16:9, 20x11.25 tile görüş alanı). Asset PPU: 16.
2. **Player tracking** — Kamera oyuncunun pozisyonunu yumuşak takip eder (SmoothDamp). Oyuncu tam merkezde değil, hareket yönüne doğru hafif offset (look-ahead).
3. **Room clamping** — Kamera aktif odanın Bounds'u dışına çıkmaz. Oda kameradan küçükse kamera odayı tam ortalar.
4. **Room transition** — Oyuncu yeni odaya geçtiğinde kamera hedef odanın merkezine doğru hızlı pan yapar (0.3s). Pan sırasında input aktif kalır.
5. **Screen shake** — VFX System'dan talep edilir. Kamera pozisyonuna rastgele offset eklenir, decay ile söner. Pixel-perfect snap korunur.
6. **Zoom** — Sabit zoom, runtime'da değişmez. Boss odasında farklı zoom seviyesi kullanılabilir (oda boyutuna bağlı).

### States and Transitions

| State | Açıklama | Geçiş |
|-------|----------|-------|
| **Following** | Oyuncuyu takip ediyor (normal gameplay) | → Transitioning (oda geçişinde) |
| **Transitioning** | Yeni odaya pan yapıyor | → Following (pan tamamlandığında) |
| **Locked** | Sabit pozisyon (cutscene, boss intro) | → Following (lock kaldırıldığında) |
| **Disabled** | Kamera devre dışı (Loading, MainMenu) | → Following (Playing state'e geçince) |

### Interactions with Other Systems

| Sistem | Yön | Arayüz |
|--------|-----|--------|
| **Game Manager** | ← dinler | `OnGameStateChanged` → Disabled/Following geçişi |
| **Room/Tilemap System** | ← veri alır | `GetActiveRoomBounds()` → kamera clamp sınırları |
| **Player Controller** | ← takip eder | `PlayerTransform.position` → kamera hedef pozisyonu |
| **VFX System** | ← talep alır | `ShakeCamera(intensity, duration)` — VFX System çağırır (çağrı zinciri: Combat → VFX → Camera). Shake parametreleri (intensity, duration) VFX System'ın Tuning Knob'larında tanımlıdır, Camera System sadece uygular. |
| **Input System** | ← veri alır | Mouse pozisyonu → look-ahead yönü hesaplaması (aim yönüne doğru offset) |

## Formulas

- **Look-ahead offset** = `aimDirection.normalized × LookAheadDistance` (aimDirection = mouse world pos - player pos)
- **SmoothDamp** = `Vector3.SmoothDamp(currentPos, targetPos + lookAhead, ref velocity, SmoothTime)`
- **Room transition speed** = Lerp over `TransitionDuration` (0.3s), EaseInOut curve
- **Screen shake offset** = `Random.insideUnitCircle × currentIntensity`, `currentIntensity = startIntensity × (1 - elapsed/duration)`
- **Pixel snap** = Kamera pozisyonu her frame `Round(pos × PPU) / PPU` ile snap edilir

| Değişken | Tanım | Varsayılan |
|----------|-------|-----------|
| `LookAheadDistance` | Aim yönüne doğru kamera offset'i (Unity unit) | 1.0 |
| `SmoothTime` | Kamera takip yumuşaklığı (saniye) | 0.15 |
| `TransitionDuration` | Oda geçiş pan süresi (saniye) | 0.3 |
| `PPU` | Pixels Per Unit | 16 |
| `ReferenceResolution` | Pixel Perfect reference | 320x180 |

## Edge Cases

| Durum | Ne olur |
|-------|---------|
| **Oda kamera görüş alanından küçükse** | Kamera odayı tam ortalar, clamp yerine center lock. Oda sınırları dışı siyah veya duvar tile ile doldurulur. |
| **Screen shake sırasında oda geçişi olursa** | Shake iptal edilir, transition öncelikli. |
| **Oyuncu oda köşesindeyse** | Clamp devreye girer, oyuncu merkezden kayar ama her zaman görünür kalır. |
| **Boss odası standart odadan büyükse** | Kamera bounds büyük odanın bounds'unu kullanır. Zoom değişmez — daha fazla alan görünür. |
| **Look-ahead mouse ekran dışındaysa** | Input System'ın son bilinen pozisyonunu kullanır (bkz. Input System edge case). |
| **Transition sırasında oyuncu hareket ederse** | Input aktif kalır, kamera transition hedefine gider. Transition bitince Following'e döner ve oyuncunun yeni pozisyonunu yakalar. |

## Dependencies

**Upstream:**

| Sistem | Bağımlılık Tipi | Arayüz |
|--------|----------------|--------|
| Game Manager | Hard | `OnGameStateChanged` → state'e göre kamera davranışı |
| Room/Tilemap System | Hard | Oda sınırları (Bounds) → clamp |

**Downstream:** Yok — leaf node. Hiçbir sistem Camera System'a bağımlı değil.

## Tuning Knobs

| Knob | Tip | Varsayılan | Güvenli Aralık | Etki |
|------|-----|-----------|----------------|------|
| `SmoothTime` | float (saniye) | 0.15 | 0.05–0.5 | Kamera takip yumuşaklığı. 0.05 → sert, anında takip. 0.5 → yavaş, kaygan. |
| `LookAheadDistance` | float (unit) | 1.0 | 0.0–2.0 | Aim yönüne offset. 0 → tam merkez. 2.0 → agresif look-ahead, oda kenarlarında clamp sorunları. |
| `TransitionDuration` | float (saniye) | 0.3 | 0.1–0.8 | Oda geçiş süresi. 0.1 → sert snap. 0.8 → yavaş, cinematik. |
| `ReferenceResolution` | Vector2Int | (320, 180) | (256,144)–(480,270) | Görüş alanı. Düşük → daha yakın zoom, az tile görünür. Yüksek → daha uzak, daha fazla tile. |

## Visual/Audio Requirements

- Pixel Perfect Camera snap — tile kenarlarında shimmer/jitter olmamalı
- Oda geçişinde kısa fade veya wipe efekti (opsiyonel, VFX System ile koordineli)
- Screen shake pixel snap'i korumalı — shake offseti de PPU'ya snap edilir

## UI Requirements

- Camera System'ın doğrudan UI gereksinimleri yok
- HUD elementleri screen-space'te render edilir, kamera hareketinden etkilenmez

## Acceptance Criteria

| # | Kriter | Doğrulama Yöntemi |
|---|--------|-------------------|
| 1 | Kamera oyuncuyu yumuşak takip eder, SmoothDamp ile | Visual test: oyuncu hareket et, kamera gecikmeyle takip eder |
| 2 | Kamera oda sınırları dışına çıkmaz | Integration test: oyuncuyu oda köşesine gönder, kamera bounds içinde kalır |
| 3 | Oda geçişinde kamera 0.3s içinde yeni odaya pan yapar | Visual test: kapıdan geç, pan gözlemle |
| 4 | Pixel perfect — tile'larda shimmer/jitter yok | Visual test: yavaş hareket, tile kenarlarını gözlemle |
| 5 | Screen shake çalışır ve pixel snap korunur | Visual test: shake tetikle, piksel bütünlüğü kontrol |
| 6 | Look-ahead aim yönüne doğru offset sağlar | Visual test: mouse'u farklı yönlere hareket et, kamera hafifçe kayar |
| 7 | Disabled state'te kamera hareketsiz | Unit test: Loading state'inde kamera pozisyonu değişmez |
| 8 | Küçük odada kamera odayı ortalar | Visual test: kameradan küçük oda, tam ortalanmış |

## Open Questions

1. Oda geçişinde fade mı pan mı? (Şu anki karar: Pan — aksiyon kesilmez. Fade opsiyonel polish.)
2. Boss intro'da zoom-out efekti olacak mı? (Önerilen: MVP sonrası, Locked state altyapısı hazır)
3. Minimap kamerası ayrı mı olacak? (HUD/minimap VS priority — ayrı render texture kamera gerekecek)
