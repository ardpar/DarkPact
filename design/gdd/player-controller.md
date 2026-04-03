# Player Controller

> **Status**: Designed
> **Author**: User + Claude
> **Last Updated**: 2026-04-03
> **Implements Pillar**: Core Gameplay

## Overview

Player Controller, oyuncunun fiziksel varlığını ve hareketini yöneten sistemdir. Input System'dan gelen aksiyonları (Move, Attack, Dash, Skill) karakter davranışlarına çevirir: 8 yönlü hareket, mouse yönüne saldırı, dash mekaniği ve skill tetikleme. Rigidbody2D tabanlı fizik hareketi kullanır. Animasyon state'lerini yönetir ve diğer sistemlere (Combat, Pact, Equipment) oyuncu durumu hakkında bilgi sağlar.

## Player Fantasy

Oyuncu kendini çevik, tepkisel ve güçlü hisseder. Her input anında karşılık bulur. Dash oyuncuya "kaçış her zaman mümkün" güvenini verir. Saldırı yönü mouse ile kontrol edildiğinden oyuncu "nereye vurduğumu ben seçiyorum" hisseder. Hack-and-slash'in özü: hareket et, saldır, kaç — hepsi pürüzsüz.

## Detailed Design

### Core Rules

1. **Hareket**: Rigidbody2D.velocity ile 8 yönlü hareket. Input normalize edilir (çapraz = düz hız). Hareket hızı `MoveSpeed` stat'ı ile belirlenir.
2. **Saldırı yönü**: Mouse world pozisyonu ile oyuncu pozisyonu arasındaki yön. 360 derece serbestlik.
3. **Dash**: Space tuşuyla tetiklenir. Oyuncu `DashSpeed` ile `DashDuration` süresince belirtilen yöne hareket eder. Dash sırasında invincible. Cooldown süresi `DashCooldown` ile belirlenir.
4. **Skill kullanımı**: 1-2-3-4 tuşları ile tetiklenir. Player Controller skill'i aktive eder, efekt hesaplaması Combat/Skill Tree sistemlerine aittir.
5. **Animasyon**: Animator component ile yönetilir. State'ler: Idle, Run, Attack, Dash, Hit, Death. Yön parametresi (4 yön sprite) hareket veya aim yönüne göre güncellenir.
6. **Collision**: CapsuleCollider2D. Duvarlarla fiziksel çarpışma (Rigidbody2D ile otomatik). Düşmanlarla trigger collision (hasar ayrı sistemde).

### States and Transitions

| State | Açıklama | Geçerli Geçişler | Input |
|-------|----------|-----------------|-------|
| **Idle** | Hareketsiz, her aksiyona açık | → Run, → Attack, → Dash, → Skill, → Hit, → Death | Tüm input aktif |
| **Run** | 8 yönlü hareket | → Idle (input kesilince), → Attack, → Dash, → Skill, → Hit, → Death | Tüm input aktif |
| **Attack** | Saldırı animasyonu oynatılıyor | → Idle (anim bitince), → Hit, → Death | Move kısıtlı (yavaş), Dash aktif (cancel), Attack buffered |
| **Dash** | Dash hareketi, invincible | → Idle (dash bitince), → Death | Input yok (momentum ile hareket) |
| **Skill** | Skill cast animasyonu | → Idle (cast bitince), → Hit, → Death | Move kısıtlı, Dash aktif (cancel) |
| **Hit** | Hasar alma stagger | → Idle (stagger bitince), → Death | Input yok (kısa süre: 0.2s) |
| **Death** | Ölüm animasyonu → Run Manager'a bildir | Terminal state | Input yok |

**Önemli kurallar:**
- Attack ve Skill, Dash ile cancel edilebilir (kaçış her zaman mümkün)
- Hit state kısa (0.2s) — uzun stagger hack-and-slash'te kötü hissettirir
- Attack sırasında yeni Attack input'u buffer'lanır → combo potansiyeli
- Death → Run Manager `OnPlayerDeath` event'i fırlatır

### Interactions with Other Systems

| Sistem | Yön | Arayüz |
|--------|-----|--------|
| **Input System** | ← alır | `OnMove`, `OnAttack`, `OnDash`, `OnSkill`, `OnAimPosition` callback'leri |
| **Game Manager** | ← dinler | `OnGameStateChanged` → Paused'da tüm hareket/input durur |
| **Combat System** | → sağlar | Saldırı tetikleme (`OnPlayerAttack(direction, position)`), hasar alma (`TakeDamage(amount)`) |
| **Health & Damage** | ↔ çift yönlü | HP takibi Health System'da, hasar hesabı oradan gelir. `OnHealthChanged`, `OnDeath` event'leri |
| **Equipment System** | ← veri alır | Mevcut silah → saldırı animasyonu, hız, menzil. Zırh → hareket hızı modifier |
| **Pact System** | ← modifier alır | Aktif paktlar stat'ları modifier (Gölge Adımı → sınırsız dash vb.) |
| **VFX System** | → talep eder | Dash trail, hit feedback efektleri |
| **Room/Tilemap System** | ← dinler | Room/Tilemap `OnPlayerEnteredRoom(Room)` event'ini fırlatır, Player Controller dinler (kapı trigger'ı Room/Tilemap'e aittir) |
| **Class System** | ← veri alır | Başlangıç stat'ları, animasyon seti, silah tipi |

## Formulas

### Hareket

```
stateModifier = isAttacking ? AttackMoveSpeedMultiplier : 1.0
actualMoveSpeed = baseMoveSpeed × equipmentModifier × pactModifier × statusEffectModifier × stateModifier
velocity = inputDirection.normalized × actualMoveSpeed
```

| Değişken | Tanım | Varsayılan | Aralık |
|----------|-------|-----------|--------|
| `baseMoveSpeed` | Temel hareket hızı (unit/s) | 5.0 | 3.0–8.0 |
| `equipmentModifier` | Zırh ağırlığına göre çarpan | 1.0 | 0.7–1.3 |
| `pactModifier` | Pakt etkisi çarpanı | 1.0 | 0.5–2.0 |
| `statusEffectModifier` | Yavaşlama/hızlanma | 1.0 | 0.3–1.5 |

### Dash

```
dashVelocity = dashDirection.normalized × DashSpeed
dashDirection = moveInput != zero ? moveInput : lastFacingDirection
```

| Değişken | Tanım | Varsayılan | Aralık |
|----------|-------|-----------|--------|
| `DashSpeed` | Dash hızı (unit/s) | 15.0 | 10.0–25.0 |
| `DashDuration` | Dash süresi (s) | 0.15 | 0.1–0.3 |
| `DashCooldown` | Dash bekleme süresi (s) | 1.0 | 0.0–3.0 |
| `DashInvincibilityDuration` | Dash sırasında invincibility (s) | 0.15 | = DashDuration |

**Örnek:** DashSpeed=15, DashDuration=0.15 → Dash mesafesi = 15 × 0.15 = 2.25 unit ≈ 2.25 tile

### Stagger

```
staggerDuration = baseStaggerDuration × (1 - staggerResistance)
```

| Değişken | Tanım | Varsayılan |
|----------|-------|-----------|
| `baseStaggerDuration` | Temel stagger süresi (s) | 0.2 |
| `staggerResistance` | Zırh/pakt ile gelen direnç (0-0.8) | 0.0 |

## Edge Cases

| Durum | Ne olur |
|-------|---------|
| **Dash ile duvara çarparsa** | Rigidbody2D collision ile durur, dash state erken biter, cooldown normal başlar |
| **Attack sırasında hasar alırsa** | Hit state'e geçer (attack cancel). Eğer stagger resistance yüksekse → super armor: attack devam eder, sadece hasar alır |
| **Dash cooldown sırasında Gölge Adımı paktı aktifse** | Pakt DashCooldown'ı 0 yapar → sınırsız dash |
| **Hareket hızı 0'a düşerse (status effect)** | Oyuncu hareket edemez ama saldırı ve dash yapabilir |
| **Ölürken dash aktifse** | Death state öncelikli → dash iptal, ölüm animasyonu başlar |
| **İki düşmandan aynı anda hasar alırsa** | İlk hasar Hit state tetikler, Hit state sırasında ikinci hasar alınır (iframes yok — sadece Dash'te invincible) |
| **Attack animasyonu sırasında oda kapısına yaklaşırsa** | Kapı trigger'ı Attack state'inde devre dışı — sadece Idle/Run'da oda geçişi |

## Dependencies

**Upstream:**

| Sistem | Bağımlılık Tipi | Arayüz |
|--------|----------------|--------|
| Input System | Hard | Hareket ve aksiyon callback'leri |
| Game Manager | Hard | State değişiklik event'i |

**Downstream:**

| Sistem | Bağımlılık Tipi | Arayüz |
|--------|----------------|--------|
| Combat System | Hard | `OnPlayerAttack` event, `TakeDamage` methodu |
| Enemy AI | Hard | Oyuncu pozisyonu ve state bilgisi (takip, kaçma kararları) |
| Class System | Hard | Başlangıç stat'ları ve animasyon seti |
| Camera System | Soft | Oyuncu Transform (takip, doğrudan API bağımlılığı yok) |

## Tuning Knobs

| Knob | Tip | Varsayılan | Güvenli Aralık | Etki |
|------|-----|-----------|----------------|------|
| `BaseMoveSpeed` | float (unit/s) | 5.0 | 3.0–8.0 | Temel hareket hızı. Düşük → ağır, tank hissi. Yüksek → hızlı, çevik. |
| `DashSpeed` | float (unit/s) | 15.0 | 10.0–25.0 | Dash hızı. MoveSpeed'in 2-4x'i olmalı. |
| `DashDuration` | float (s) | 0.15 | 0.1–0.3 | Dash süresi. Kısa → snappy. Uzun → kayma hissi. |
| `DashCooldown` | float (s) | 1.0 | 0.0–3.0 | Dash bekleme. 0 → Gölge Adımı paktı efekti. |
| `BaseStaggerDuration` | float (s) | 0.2 | 0.1–0.5 | Hasar stagger süresi. Kısa → agresif oynanış. Uzun → dikkatli oynanış. |
| `AttackMoveSpeedMultiplier` | float | 0.3 | 0.0–0.5 | Attack sırasında hareket hızı çarpanı. 0 → sabit. 0.5 → yarı hızda kayma. |

## Visual/Audio Requirements

- 4 yönlü sprite animasyonları: Idle, Run, Attack, Dash, Hit, Death (ELV Rogue Adventure pack)
- Dash: Yarı-saydam afterimage trail (VFX System)
- Hit: Karakter kısa beyaz flash (0.1s) + screen shake
- Ayak sesi: Hareket sırasında tile tipine göre (taş, toprak) — Audio Manager ile koordineli
- Ölüm: Yere çökme animasyonu + kan particle

## UI Requirements

- Player Controller'ın doğrudan UI gereksinimleri yok
- HUD'da gösterilecek veriler: HP (Health System'dan), Dash cooldown, Skill cooldown'ları
- Aim direction indicator (opsiyonel, mouse cursor yeterli olabilir)

## Acceptance Criteria

| # | Kriter | Doğrulama Yöntemi |
|---|--------|-------------------|
| 1 | 8 yönlü hareket çalışır, çapraz hız normalize | Integration test: WASD ve çapraz input → eşit hız |
| 2 | Saldırı mouse yönüne doğru tetiklenir | Visual test: mouse farklı yönlerde, saldırı o yöne gider |
| 3 | Dash invincibility çalışır, cooldown doğru | Unit test: dash sırasında hasar = 0, cooldown süresince tekrar dash yapılamaz |
| 4 | Dash ile attack cancel edilebilir | Integration test: attack ortasında dash → dash başlar |
| 5 | Hit stagger doğru sürede, Death terminal | Unit test: stagger süresi ölç, death state'ten çıkış yok |
| 6 | Pause'da hareket durur, resume'da devam eder | Unit test: Paused state'inde velocity = 0 |
| 7 | Equipment modifier hareket hızını etkiler | Unit test: ağır zırh → hız düşer |
| 8 | Oda geçişi sadece Idle/Run state'inde mümkün | Integration test: Attack state'inde kapıya yaklaş → geçiş olmaz |

## Open Questions

1. Combo sistemi MVP'de mi? (Önerilen: MVP'de basit tek saldırı, combo VS'de — Attack state'indeki input buffer altyapısı combo'ya hazır)
2. Dodge roll mu yoksa teleport dash mı? (Şu anki karar: Teleport dash — pixel-art'ta daha net okunur, afterimage trail ile görsel feedback güçlü)
3. Knockback (geri itilme) hasar alınca olacak mı? (Önerilen: Evet, küçük knockback — stagger ile birlikte 0.2s içinde. Tuning knob olarak eklenmeli)
