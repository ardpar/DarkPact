# Combat System

> **Status**: Designed
> **Author**: User + Claude
> **Last Updated**: 2026-04-04
> **Implements Pillar**: Core Gameplay

## Overview

Combat System, Dark Pact'in hack-and-slash combat'ını yöneten merkezi gameplay sistemidir. Saldırı tetikleme, hitbox/hurtbox tespiti, hasar iletimi, silah davranışları ve skill kullanımını orkestre eder. Player Controller'dan gelen aksiyon sinyallerini alır, Health & Damage'e hasar gönderir, VFX/Audio'ya feedback tetikler. Oyunun "eğlenceli mi?" sorusunun cevabı bu sistemde yatar — her vuruş ağırlıklı, her kaçış heyecanlı hissettirmelidir.

## Player Fantasy

Oyuncu bir ölüm makinesi gibi hisseder — düşman gruplarına dalıp biçer. Her vuruş "impact" hissi verir (hitstop + screen shake + VFX + SFX senkron). Dash ile ölümden kıl payı kurtulmak adrenalin yaratır. Silah tipi combat stilini değiştirir — kılıç hızlı ama kısa, mızrak yavaş ama uzun, asa uzaktan vurur. Paktlar combat'ı kökten değiştirir: Katliam Paktı seni acımasız yapar, Gölge Adımı seni durdurulamaz.

## Detailed Design

### Core Rules

1. **Hitbox/Hurtbox sistemi**: Saldırı hitbox (Trigger Collider2D) oluşturur, düşmanların hurtbox'una (Trigger Collider2D) temas edince hasar tetiklenir
2. **Saldırı akışı**: Input → Player Controller Attack state → Combat System hitbox spawn → Collision detection → Health & Damage'e hasar gönder → VFX/SFX feedback
3. **Silah tipleri**: Her silah tipi farklı hitbox şekli, hız, menzil ve damage pattern'i tanımlar
4. **Skill sistemi**: 4 skill slotu, her slot bir SpellGem'e bağlı. Cooldown tabanlı, mana yok (roguelite basitliği)
5. **Combo**: MVP'de yok — tek saldırı. Attack state'indeki input buffer altyapısı combo için hazır (VS'de)
6. **Knockback**: Hasar veren saldırılar hedefi geri iter. Knockback gücü silah ve rarity'ye göre değişir
7. **I-frames**: Sadece Dash sırasında invincible. Normal combat'ta yok — pozisyonlama ve dash timing önemli
8. **Critical Hit**: Her saldırıda `CritChance` olasılıkla critical hit olur. Critical hit hasarı `baseDamage × CritMultiplier`. Crit olduğunda büyük damage number + özel VFX + SFX tetiklenir. Default: %10 şans, ×2 hasar.

### Silah Tipleri

| Silah | Hitbox Şekli | Menzil | Hız | Knockback | Davranış |
|-------|-------------|--------|-----|-----------|----------|
| Kılıç | Yay (90°) | 1.5 tile | Hızlı (0.3s) | Orta | Geniş alan, çoklu düşmana vurur |
| Balta | Yay (60°) | 1.2 tile | Yavaş (0.5s) | Yüksek | Dar ama güçlü, stagger şansı yüksek |
| Mızrak | Dikdörtgen (dar, uzun) | 2.5 tile | Orta (0.4s) | Düşük | Uzun menzil, tek hedef (pierce yok MVP'de) |
| Asa | Projectile | 5.0 tile | Yavaş (0.6s) | Yok | Uzaktan, projectile hasar tipi SpellGem'e göre |

### Skill Sistemi

```
SkillSlot (1-4)
├── equippedSpellGem: SpellDefinition (ItemDatabase'den)
├── cooldownTimer: float
├── isReady: bool (cooldownTimer <= 0)
└── Activate() → SpellDefinition.Execute(casterPosition, aimDirection)
```

**Skill tipleri (SpellGem'e göre):**

| Skill Tipi | Davranış | Örnek |
|------------|----------|-------|
| Projectile | Aim yönüne mermi fırlat | Ateş Topu, Buz Mermisi |
| AoE | Oyuncu etrafında alan hasarı | Zehir Bulutu, Şimşek Halkası |
| Buff | Geçici stat boost | Karanlık Güç (+%30 hasar, 5s) |
| Dash Attack | Dash + hasar | Gölge Bıçağı (dash yönünde hasar) |

### States and Transitions

Combat System kendi state'i yok — Player Controller'ın Attack/Skill state'leri ile senkronize çalışır.

**Saldırı akışı (frame-by-frame):**

| Frame | Olay |
|-------|------|
| 0 | Attack input alındı → Player Controller Attack state'e geçer |
| 1-3 | Anticipation (wind-up) — hitbox henüz yok, animasyon başlıyor |
| 4-6 | Active frames — hitbox aktif, temas eden hurtbox'lara hasar |
| 4 (ilk hit) | Hitstop (0.05s freeze) + Screen shake + Hit VFX + Hit SFX |
| 7-10 | Recovery — hitbox kaldırıldı, animasyon bitiyor |
| 10 | Attack state biter → Idle/Run'a dönüş. Buffer'da Attack varsa yeni saldırı |

### Interactions with Other Systems

| Sistem | Yön | Arayüz |
|--------|-----|--------|
| **Player Controller** | ← tetikler | `OnPlayerAttack(weaponType, aimDirection)`, `OnSkillActivate(slotIndex)` |
| **Health & Damage** | → hasar gönderir | `ApplyDamage(target, baseDamage, damageType, source)` |
| **VFX System** | → efekt tetikler | Hit spark, spell efekt, hitstop |
| **Audio Manager** | → ses tetikler | Saldırı swing SFX, hit impact SFX, spell SFX |
| **Equipment System** | ← stat alır | Silah → baseDamage, attackSpeed, range, weaponType |
| **Pact System** | ← modifier alır | Hasar çarpanı, özel saldırı efektleri (Lanetli Dokunuş → zehir) |
| **Status Effect System** | → tetikler | Saldırı ile status effect uygulama (zehir, yavaşlama) |
| **Enemy System** | ↔ çift yönlü | Düşman saldırıları da bu sistemi kullanır (aynı hitbox/hurtbox mantığı) |
| **Item Database** | ← veri alır | SpellGem tanımları → skill davranışları |

## Formulas

### Saldırı Hasarı

```
weaponDamage = equipment.baseDamage × equipment.rarityMultiplier
totalDamage = weaponDamage × pactDamageMultiplier × skillMultiplier
→ Health & Damage pipeline'a gönderilir (elemental, defense, min 1)
```

| Değişken | Tanım | Varsayılan | Aralık |
|----------|-------|-----------|--------|
| `weaponDamage` | Silah base + rarity | silaha bağlı | 5–200 |
| `pactDamageMultiplier` | Pakt bonus (ör: Katliam +%60) | 1.0 | 0.5–3.0 |
| `skillMultiplier` | Skill hasar çarpanı | skill'e bağlı | 0.5–5.0 |

### Critical Hit

```
isCritical = random(0, 1) < CritChance
critMultiplier = isCritical ? CritMultiplier : 1.0
totalDamage = weaponDamage × pactDamageMultiplier × skillMultiplier × critMultiplier
```

| Değişken | Tanım | Varsayılan | Aralık |
|----------|-------|-----------|--------|
| `CritChance` | Critical hit olasılığı | 0.10 | 0.0–0.5 |
| `CritMultiplier` | Critical hasar çarpanı | 2.0 | 1.5–4.0 |

### Saldırı Hızı

```
actualAttackSpeed = baseAttackSpeed × equipmentSpeedModifier × pactSpeedModifier
attackCooldown = 1.0 / actualAttackSpeed
```

| Değişken | Tanım | Varsayılan |
|----------|-------|-----------|
| `baseAttackSpeed` | Silah tipi temel hız (atk/s) | Kılıç=3.3, Balta=2.0, Mızrak=2.5, Asa=1.67 |

### Knockback

```
knockbackForce = baseKnockback × (1 + damageDealt / 100)
knockbackDirection = (targetPos - attackerPos).normalized
knockbackDuration = 0.15s (sabit)
```

| Değişken | Tanım | Varsayılan |
|----------|-------|-----------|
| `baseKnockback` | Silah tipi knockback gücü (unit) | Kılıç=0.5, Balta=1.0, Mızrak=0.3, Asa=0.0 |

### Skill Cooldown

```
actualCooldown = baseCooldown × cooldownReduction
cooldownReduction = 1.0 - totalCDR (max 0.5 = %50 CDR cap)
```

| Değişken | Tanım | Varsayılan |
|----------|-------|-----------|
| `baseCooldown` | Skill temel bekleme (s) | skill'e bağlı: 3–15s |
| `totalCDR` | Toplam cooldown reduction | 0.0 |

### Projectile

```
projectileSpeed = BaseProjectileSpeed × spellSpeedModifier
projectileLifetime = range / projectileSpeed
```

| Değişken | Tanım | Varsayılan |
|----------|-------|-----------|
| `BaseProjectileSpeed` | Mermi hızı (unit/s) | 8.0 |
| `range` | Asa menzili (tile) | 5.0 |

## Edge Cases

| Durum | Ne olur |
|-------|---------|
| **Hitbox birden fazla düşmana temas ederse** | Her düşmana ayrı hasar uygulanır. Kılıç/balta AoE — birden fazla hedef normal. Mızrak sadece ilk temas. |
| **Düşman ve oyuncu aynı frame'de birbirine vurursa** | İkisi de hasar alır — trade hit. Hitstop her iki taraf için uygulanır. |
| **Skill cooldown sırasında tekrar basılırsa** | Input yok sayılır. HUD'da cooldown göstergesi kalan süreyi gösterir. |
| **Projectile duvara çarparsa** | Destroy edilir, duvar hit VFX oynar. Pierce yeteneği varsa (MVP'de yok) devam eder. |
| **Hitstop sırasında başka hit olursa** | Hitstop süreleri stack etmez — en uzun olan uygulanır. |
| **Saldırı hitbox oluşturulurken hedef ölürse** | Ölü hedefler hurtbox'larını devre dışı bırakır → hitbox temas etmez. |
| **Oyuncu asa ile duvara doğru ateş ederse** | Projectile duvardan hemen sonra destroy olur — kısa menzil hasar yok. |
| **Lanetli Dokunuş paktı aktifken saldırı** | Her saldırı hedefe zehir uygular + oyuncuya da hafif hasar (pakt bane'i). Combat System pakt efektini saldırı sonrası tetikler. |

## Dependencies

**Upstream:**

| Sistem | Bağımlılık Tipi | Arayüz |
|--------|----------------|--------|
| Player Controller | Hard | Saldırı ve skill tetikleme sinyalleri |
| Health & Damage | Hard | Hasar uygulama API |
| VFX System | Hard | Combat feedback efektleri |
| Audio Manager | Soft | Combat ses efektleri (olmasa da combat çalışır) |

**Downstream:**

| Sistem | Bağımlılık Tipi | Arayüz |
|--------|----------------|--------|
| Equipment System | Hard | Silah stat'larını Combat'a sağlar |
| Loot System | Soft | Combat kill'leri loot drop tetikler (Enemy System üzerinden) |
| Status Effect System | Hard | Saldırılar status effect uygulayabilir |
| Enemy System | Hard | Düşman saldırıları da combat pipeline kullanır |
| Boss System | Hard | Boss saldırıları combat pipeline kullanır |
| Pact System | Hard | Pakt modifierleri combat'ı değiştirir |
| Skill Tree | Soft | Skill güçlendirmeleri combat stat'larını etkiler |

## Tuning Knobs

| Knob | Tip | Varsayılan | Güvenli Aralık | Etki |
|------|-----|-----------|----------------|------|
| `HitstopDuration` | float (s) | 0.05 | 0.0–0.15 | Hit anında zaman durması. 0 → flat hisseder. 0.15 → her hit dramatik. |
| `HitScreenShakeIntensity` | float | 0.3 | 0.0–1.0 | Hit screen shake gücü. |
| `BaseKnockback[weaponType]` | float | silaha göre | 0.0–2.0 | Silah tipine göre geri itme. |
| `ProjectileSpeed` | float (unit/s) | 8.0 | 4.0–15.0 | Asa projectile hızı. Yavaş → dodge edilebilir. Hızlı → kesin isabet. |
| `CDRCap` | float | 0.5 | 0.3–0.7 | Maksimum cooldown reduction. 0.3 → skill'ler yavaş kalır. 0.7 → spam. |
| `AnticipationFrames` | int | 3 | 1–6 | Saldırı öncesi bekleme frame sayısı. Düşük → anında vuruş. Yüksek → telegraphed, okunabilir. |
| `ActiveFrames` | int | 3 | 2–5 | Hitbox aktif frame sayısı. |
| `RecoveryFrames` | int | 4 | 2–8 | Saldırı sonrası toparlanma. Düşük → hızlı combo. Yüksek → commitment. |

## Visual/Audio Requirements

- **Saldırı animasyonları**: Silah tipine göre farklı (ELV pack sprite'ları + custom hitbox overlay)
- **Hit VFX**: Silah tipine göre (kılıç → spark, balta → impact, mızrak → thrust, asa → element patlama)
- **Hit SFX**: Metal çarpma, kemik kırılma, büyü patlaması — silah ve hedef tipine göre
- **Hitstop**: Hem attacker hem target 0.05s freeze — "weight" hissi
- **Screen shake**: Her hit'te küçük, critical/boss hit'te büyük
- **Damage numbers**: Hasar miktarı hedef üstünde pop-up (Health & Damage GDD'de tanımlı)
- **Skill VFX**: Her SpellGem'e özel efekt (ateş topu, buz patlaması vb.)

## UI Requirements

- **Skill bar** (HUD): 4 slot, her slotta SpellGem ikonu + cooldown overlay (radial wipe)
- **Cooldown göstergesi**: Gri overlay + kalan süre sayısı
- **Weapon slot** (HUD): Aktif silah ikonu
- **Crosshair/Aim indicator**: Mouse pozisyonunda (opsiyonel — default cursor yeterli olabilir)

## Acceptance Criteria

| # | Kriter | Doğrulama Yöntemi |
|---|--------|-------------------|
| 1 | Kılıç 90° yay hitbox ile çoklu düşmana vurur | Integration test: 3 düşman yay içinde → 3 hit |
| 2 | Mızrak dar dikdörtgen hitbox ile sadece ilk hedefe vurur | Integration test: 2 düşman sıralı → sadece ilki hasar alır |
| 3 | Asa projectile oluşturur, aim yönüne gider, duvarda durur | Visual test: ateş et → mermi gider → duvarda patlar |
| 4 | Hitstop + screen shake + VFX senkron çalışır | Visual test: vur → freeze + shake + spark aynı anda |
| 5 | Skill cooldown doğru çalışır, CDR cap uygulanır | Unit test: baseCooldown=10, CDR=0.6 → actualCooldown=5 (cap 0.5) |
| 6 | Knockback hedefi doğru yönde iter | Integration test: sağdan vur → hedef sola kayar |
| 7 | Dash sırasında hitbox'lar oyuncuya temas etmez | Integration test: dash sırasında düşman saldırısı → 0 hasar |
| 8 | Düşman saldırıları da aynı pipeline kullanır | Integration test: düşman saldırısı → aynı hitbox/hurtbox sistemi |

## Open Questions

1. Combo sistemi VS'de nasıl çalışacak? (Önerilen: 3-hit combo, her hit farklı hitbox. Input buffer zaten hazır.)
2. Parry/block mekanik olacak mı? (Önerilen: MVP'de yok. Dash = tek savunma. Parry/block class-specific olabilir.)
3. Friendly fire (co-op'ta) düşünülecek mi? (Faz 3 — Co-op'ta değerlendirilir)
4. Critical hit combat'ta nasıl gösterilecek? (Önerilen: Büyük damage number + özel VFX + SFX)
