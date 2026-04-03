# VFX System

> **Status**: Designed
> **Author**: User + Claude
> **Last Updated**: 2026-04-03
> **Implements Pillar**: Core Infrastructure

## Overview

VFX System, Dark Pact'teki tüm görsel efektleri yöneten altyapıdır. Unity Particle System ve sprite animasyonları kullanarak hit spark'ları, spell efektleri, dash trail'leri, pakt auraları ve çevresel partikülleri üretir. Object pooling ile efektleri yeniden kullanır ve performansı korur. GDD'de belirtildiği gibi combat VFX oyunun en kritik eksik parçasıdır — bu sistem ELV asset pack'te bulunmayan efektleri Particle System ile üretir.

## Player Fantasy

Her saldırı, her spell, her dash "ağırlıklı" ve "etkili" hisseder. Hit spark'ları isabeti doğrular, spell efektleri gücü görselleştirir, pakt auraları oyuncunun yaptığı seçimin sonucunu sürekli hatırlatır. VFX olmadan combat "boş" hisseder — düşmana vuruyorsun ama bir şey olduğunu görmüyorsun. Doğru VFX, 16x16 piksel sprite'ların "juice" ile dolup taşmasını sağlar.

## Detailed Design

### Core Rules

1. **Object Pool** — Her VFX tipi için ayrı pool. Efekt bittiğinde pool'a döner, yeni istendiğinde pool'dan çekilir. Runtime'da `Instantiate`/`Destroy` yapılmaz.
2. **VFX Request API** — Diğer sistemler `VFXManager.Play(VFXType, position, rotation, scale)` ile efekt talep eder. VFX tipi enum ile tanımlı.
3. **Layer sistemi** — Efektler doğru sorting layer'da render edilir:
   - `BehindCharacter` — Zemin efektleri (shadow, ground crack)
   - `AtCharacter` — Hit spark, aura
   - `AboveCharacter` — Spell efektleri, overhead indicator
   - `Overlay` — Screen-space efektler (screen shake companion, flash)
4. **Lifetime yönetimi** — Her efekt tanımlı süre sonra otomatik pool'a döner. Looping efektler (aura gibi) manuel `Stop()` ile kapatılır.
5. **Pixel-perfect** — Efektler 16x16 grid'e uyumlu. Particle sprite'lar pixel-art stilinde, anti-aliasing yok.

### VFX Kategorileri

| Kategori | Örnekler | Üretim Yöntemi |
|----------|---------|----------------|
| **Hit Effects** | Kılıç spark, balta impact, mızrak thrust | Particle System (sprite sheet burst) |
| **Spell Effects** | Ateş topu, buz patlaması, zehir bulutu, şimşek | Particle System + sprite animation |
| **Movement Effects** | Dash trail, hareket tozu | Particle System (trail module) |
| **Pact Auras** | Aktif pakt görsel göstergesi (looping) | Particle System (looping, oyuncuya bağlı) |
| **Status Effects** | Zehir tick, yanma, yavaşlama | Particle System (karakter üzerinde) |
| **Environmental** | Meşale alevi, toz parçacıkları, kan sıçraması | Particle System (sahne objelerine bağlı) |
| **UI Feedback** | Level up flash, loot rarity glow | Sprite animation + screen flash |

### States and Transitions

| State | Açıklama |
|-------|----------|
| **Pooled** | Efekt havuzda bekliyor, devre dışı |
| **Playing** | Efekt aktif, particle emit ediyor |
| **Finishing** | Emit durdu, mevcut particle'lar sönüyor |
| **Returning** | Pool'a geri dönüyor, reset ediliyor |

Geçiş: Pooled → Playing (`Play()`) → Finishing (lifetime doldu veya `Stop()`) → Returning (son particle söndü) → Pooled

### Interactions with Other Systems

| Sistem | Yön | Arayüz |
|--------|-----|--------|
| **Combat System** | ← talep alır | `Play(HitSpark, hitPosition, ...)` — her hasar olayında. Combat ayrıca `RequestScreenShake(intensity, duration)` çağırır → VFX System parametreleri uygular → `CameraSystem.ShakeCamera()` çağırır |
| **Player Controller** | ← talep alır | `Play(DashTrail, playerPosition, ...)` — dash sırasında |
| **Pact System** | ← talep alır | `PlayLooping(PactAura, playerTransform)` — pakt aktifken sürekli |
| **Status Effect System** | ← talep alır | `PlayLooping(PoisonVFX, targetTransform)` — status effect süresince |
| **Enemy System** | ← talep alır | `Play(DeathVFX, enemyPosition, ...)` — düşman ölümünde |
| **Room/Tilemap System** | ← talep alır | Environmental efektler oda yüklendiğinde başlar |
| **Camera System** | → çağırır | `CameraSystem.ShakeCamera(intensity, duration)` — VFX System screen shake parametrelerini (ScreenShakeIntensity, ScreenShakeDuration) uygulayarak Camera System'a iletir. Çağrı zinciri: Combat System → VFX System → Camera System |
| **Game Manager** | ← dinler | `OnGameStateChanged` → Paused'da tüm particle'lar pause edilir |

## Formulas

- **Pool boyutu** = `MaxConcurrentEffects[type] × 1.5` (headroom). Varsayılan MaxConcurrent: Hit=10, Spell=5, Aura=3, Environmental=20
- **Particle bütçesi** = Toplam aktif particle sayısı < 2000 (16x16 sprite particle, mobil port düşünülerek)
- **Efekt lifetime** = VFX tipine göre ScriptableObject'te tanımlı (0.2s–5.0s arası)

## Edge Cases

| Durum | Ne olur |
|-------|---------|
| **Pool tükenirse (tüm efektler aktif)** | En eski aktif efekt zorla pool'a döndürülür, yeni efekt onun yerine başlar. Log uyarısı yazılır. |
| **Pause sırasında aktif efektler** | `ParticleSystem.Pause()` çağrılır, resume'da `Play()` ile devam. Lifetime timer da durur. |
| **Looping efekt sahibi (oyuncu/düşman) ölürse** | Efekt `Stop()` ile kapatılır, kalan particle'lar söner, pool'a döner. Orphan efekt kalmaz. |
| **Aynı pozisyona çok sayıda efekt istenirse** | Efektler stack edilir. Görsel kirlilik riski → Combat System hit frequency ile sınırlanır, VFX System'ın sorumluluğu değil. |
| **Oda geçişinde aktif efektler** | Eski odanın environmental efektleri stop edilir. Player efektleri (aura, trail) devam eder. |
| **Screen shake ile birlikte particle pozisyonu kayarsa** | Particle'lar world-space'te render edilir — screen shake kamerayı oynatır, particle pozisyonu etkilenmez. |

## Dependencies

**Upstream:** Yok — Foundation layer.

**Downstream:**

| Sistem | Bağımlılık Tipi | Arayüz |
|--------|----------------|--------|
| Combat System | Hard | `VFXManager.Play()` API ile hit/spell efektleri |
| Player Controller | Soft | Dash trail (olmasa da dash çalışır) |
| Pact System | Soft | Pact aura (olmasa da pakt çalışır) |
| Status Effect System | Soft | Status VFX (olmasa da efekt uygulanır) |
| Enemy System | Soft | Death VFX (olmasa da düşman ölür) |

## Tuning Knobs

| Knob | Tip | Varsayılan | Güvenli Aralık | Etki |
|------|-----|-----------|----------------|------|
| `MaxTotalParticles` | int | 2000 | 500–5000 | Toplam aktif particle limiti. Düşük → efektler erken kırpılır. Yüksek → FPS düşer. |
| `PoolSizeMultiplier` | float | 1.5 | 1.0–3.0 | Pool headroom. 1.0 → sık pool tükenmesi. 3.0 → fazla bellek kullanımı. |
| `HitStopDuration` | float (saniye) | 0.05 | 0.0–0.15 | Hit anında kısa duraklama (juice). 0 → hit hissedilmez. 0.15 → combat yavaşlar. |
| `ScreenShakeIntensity` | float | 0.3 | 0.0–1.0 | Hit/spell screen shake gücü. 0 → shake yok. 1.0 → aşırı sarsıntı. |
| `ScreenShakeDuration` | float (saniye) | 0.1 | 0.05–0.3 | Screen shake süresi. |

## Visual/Audio Requirements

- Tüm particle sprite'lar pixel-art stilinde, ELV pack renk paletine uyumlu
- Hit spark: Turuncu/sarı tonları (Crypt), yeşil tonları (Graveyard), kırmızı/mor (Hell)
- Pakt auraları: Pakt rengine göre (Katliam=kırmızı, Kan Kalkanı=koyu kırmızı, Gölge Adımı=mor, Lanetli Dokunuş=yeşil, Açgözlülük=altın)
- Screen shake ve hitstop audio ile senkronize olmalı

## UI Requirements

- VFX System'ın doğrudan UI gereksinimleri yok
- UI feedback efektleri (level up flash, loot glow) screen-space overlay olarak render edilir
- Ayarlar menüsünde "VFX yoğunluğu" slider'ı (Low/Medium/High → particle sayısı çarpanı)

## Acceptance Criteria

| # | Kriter | Doğrulama Yöntemi |
|---|--------|-------------------|
| 1 | Object pool çalışır, runtime'da Instantiate/Destroy yapılmaz | Profiler: GC allocation olmadan 100 efekt oynat |
| 2 | Hit spark doğru pozisyonda ve doğru sorting layer'da render edilir | Visual test: düşmana vur, spark isabetnoktasında görünür |
| 3 | Looping efektler (aura) sahibine bağlı kalır ve Stop ile durur | Integration test: pact aura başlat, oyuncu hareket et, aura takip eder, stop et, söner |
| 4 | Pause'da tüm particle'lar durur, resume'da devam eder | Visual test: efekt ortasında pause → particle donuk, resume → devam |
| 5 | Pool tükendiğinde en eski efekt recycle edilir | Unit test: pool boyutu=1, 2 efekt talep et, ilki kapatılır |
| 6 | Toplam aktif particle < MaxTotalParticles | Performance test: yoğun combat'ta particle count monitor |
| 7 | Screen shake ve hitstop doğru süre/yoğunlukta çalışır | Visual test: tuning knob değerlerini değiştir, farkı gözlemle |
| 8 | Efekt lifetime sonunda otomatik pool'a döner | Unit test: 0.5s lifetime efekt → 0.5s sonra pooled state |

## Open Questions

1. VFX Graph mı Particle System mı? (Önerilen: Particle System — 2D pixel-art için yeterli, VFX Graph overkill ve URP 2D desteği sınırlı)
2. Hitstop (time freeze) global mi yoksa sadece vuran/vurulan için mi? (Önerilen: Sadece vuran+vurulan — global freeze diğer düşmanları etkiler, kalabalık sahnelerde kötü hisseder)
3. Destruction particle'ları (kırılan sandık, patlayan vazo) MVP'de mi? (Önerilen: MVP sonrası, Room/Tilemap destructible'lar ile birlikte)
