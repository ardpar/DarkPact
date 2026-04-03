# Sprint 001: Core Prototype

**Status**: Not Started
**Goal**: Oynanabilir core loop prototipi — 1 oda, combat, 1 pakt, 1 düşman tipi
**Duration**: ~2 hafta
**Priority**: MVP Foundation + Core + minimal Feature

---

## Sprint Objective

"Bir odada düşmanlarla dövüşüp pakt seçebiliyor muyuz?" sorusunu cevaplar.
Bu prototip production-quality kod DEĞİL — game feel'i test etmek için.

---

## Tasks

### Foundation (Gün 1-3)

| # | Task | GDD Ref | Effort | Status |
|---|------|---------|--------|--------|
| 1.1 | Unity 6.3 LTS proje oluştur, URP 2D yapılandır | — | S | Not Started |
| 1.2 | ServiceLocator implement et | ADR-001 | S | Not Started |
| 1.3 | GameManager — Boot, MainMenu, Loading, Playing, Paused, GameOver state machine | game-manager.md | M | Not Started |
| 1.4 | Input System — Input Action Asset, Gameplay/UI/Global action maps | input-system.md | S | Not Started |
| 1.5 | Basit test odası — 16x16 tile, duvarlar, zemin (ELV Crypt tileset) | room-tilemap-system.md | S | Not Started |
| 1.6 | Pixel Perfect Camera + player tracking | camera-system.md | S | Not Started |

### Core (Gün 4-6)

| # | Task | GDD Ref | Effort | Status |
|---|------|---------|--------|--------|
| 2.1 | Player Controller — WASD hareket, Rigidbody2D, 4-yön animasyon | player-controller.md | M | Not Started |
| 2.2 | Dash mekaniği — invincibility, cooldown, afterimage trail | player-controller.md | S | Not Started |
| 2.3 | Health & Damage — HP component, damage pipeline, OnDeath event | health-damage.md | M | Not Started |
| 2.4 | Combat — kılıç hitbox (90° yay), hit detection, hitstop, screen shake | combat-system.md | M | Not Started |
| 2.5 | VFX — object pool, hit spark, dash trail particle | vfx-system.md | S | Not Started |
| 2.6 | Damage numbers — floating text popup | hud.md | S | Not Started |

### Feature (Gün 7-10)

| # | Task | GDD Ref | Effort | Status |
|---|------|---------|--------|--------|
| 3.1 | Basit düşman — İskelet Savaşçı, melee AI (chase + attack) | enemy-system.md, enemy-ai.md | M | Not Started |
| 3.2 | Düşman spawn — oda girişinde 3-5 düşman, OnRoomCleared event | enemy-system.md | S | Not Started |
| 3.3 | Pact System — 1 pakt (Katliam Paktı: +%60 hasar, düşmanlar dirilir) | pact-system.md | M | Not Started |
| 3.4 | Pact Selection UI — basit 1-kart modal (sadece Katliam test için) | pact-selection-ui.md | S | Not Started |
| 3.5 | Basit HUD — HP bar, pakt ikonu | hud.md | S | Not Started |
| 3.6 | Run Manager — basit run start/death/restart akışı | run-manager.md | S | Not Started |

### Polish & Test (Gün 11-14)

| # | Task | GDD Ref | Effort | Status |
|---|------|---------|--------|--------|
| 4.1 | Game feel tuning — hitstop, screen shake, knockback değerleri | combat-system.md, vfx-system.md | M | Not Started |
| 4.2 | Playtest — core loop'u test et, notlar al | — | M | Not Started |
| 4.3 | Bug fix ve iteration | — | M | Not Started |
| 4.4 | Prototip README yaz (ne test edildi, sonuçlar) | — | S | Not Started |

---

## Scope Sınırları

**Sprint'te VAR:**
- 1 oda (el yapımı, procedural değil)
- 1 silah tipi (kılıç)
- 1 düşman tipi (melee iskelet)
- 1 pakt (Katliam)
- Temel HUD (HP bar)
- Ölüm → restart akışı

**Sprint'te YOK:**
- Procedural dungeon generation
- Loot/equipment sistemi
- Skill tree / level up
- Boss
- Ses
- Birden fazla pakt seçimi
- Save/load

---

## Success Criteria

1. Oyuncu WASD ile hareket edebilir, mouse yönüne saldırabilir, dash yapabilir
2. Düşmanlar oyuncuyu kovalayıp saldırır, öldürülebilir
3. Katliam Paktı aktifken hasar %60 artar ve düşmanlar 1 kez dirilir
4. Oyuncu ölünce GameOver → restart çalışır
5. Combat "juice" hisseder — hitstop, screen shake, hit spark, damage numbers
6. 60 FPS korunur (5 düşman aktifken)

---

## Risk Log

| Risk | Olasılık | Etki | Mitigation |
|------|----------|------|------------|
| ELV asset import sorunları | Orta | Task 1.5 gecikir | İlk gün asset import'u test et |
| Combat feel iyi olmaz | Yüksek | Sprint'in ana amacı | Gün 11-14 tamamen tuning'e ayrılmış |
| Hitbox/hurtbox collision sorunları | Orta | Combat çalışmaz | ADR-002 layer matrix'ini ilk gün kur |
