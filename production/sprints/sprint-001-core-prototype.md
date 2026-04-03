# Sprint 001 — 2026-04-04 to 2026-04-18

## Sprint Goal
Core combat loop'un oynanabilir prototipini oluştur: 1 odada düşmanlarla dövüş, 1 pakt seçimi, ölüm/restart — "bu eğlenceli mi?" sorusunu cevapla.

## Capacity
- Total days: 14
- Buffer (20%): 3 gün (unplanned work / bug fix)
- Available: 11 gün

## Tasks

### Must Have (Critical Path)

| ID | Task | Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------|-----------|-------------|-------------------|
| 1.1 | Unity 6.3 LTS proje oluştur, URP 2D yapılandır, ELV asset import | — | 0.5 | — | Proje açılır, URP 2D Renderer aktif, ELV sprite'lar import edilmiş |
| 1.2 | ServiceLocator implement et | ADR-001 | 0.5 | — | Register/Get/Reset çalışır, unit test geçer |
| 1.3 | GameManager state machine (Boot→MainMenu→Playing→Paused→GameOver) | game-manager.md | 1 | 1.2 | State geçişleri çalışır, OnGameStateChanged event fırlatılır, ESC pause toggle |
| 1.4 | Input Action Asset — Gameplay/UI/Global action maps | input-system.md | 0.5 | — | WASD, mouse, space, 1-4, ESC binding'leri tanımlı, action map switch çalışır |
| 1.5 | Test odası — 16x16 tile Crypt oda, duvar collider, kapılar | room-tilemap-system.md | 1 | 1.1 | Oda render edilir, duvarlar geçilmez, zemin yürünebilir, CompositeCollider2D aktif |
| 1.6 | Pixel Perfect Camera + SmoothDamp player tracking | camera-system.md | 0.5 | 1.5 | Kamera oyuncuyu takip eder, oda sınırında clamp, pixel shimmer yok |
| 2.1 | Player Controller — WASD 8-yön hareket, Rigidbody2D, 4-yön sprite | player-controller.md | 1 | 1.3, 1.4 | Normalize çapraz hareket, collision ile duvar, sprite yön değişimi |
| 2.2 | Dash — invincibility, cooldown, afterimage trail | player-controller.md | 0.5 | 2.1 | Space → dash, i-frame çalışır, cooldown doğru, VFX trail |
| 2.3 | Health & Damage — HP component, damage pipeline, min 1 hasar, OnDeath | health-damage.md | 1 | 1.2 | Hasar alınır, HP azalır, min 1 garanti, HP=0 → OnDeath event |
| 2.4 | Combat — kılıç 90° yay hitbox, hit detection, hitstop, screen shake | combat-system.md | 1.5 | 2.1, 2.3 | Sol tık → hitbox, düşmana temas → hasar, hitstop 0.05s, screen shake |
| 2.5 | VFX — object pool, hit spark, dash trail particle | vfx-system.md | 0.5 | 2.4 | Pool'dan efekt çıkar/döner, GC allocation yok, hit spark doğru pozisyonda |
| 3.1 | İskelet Savaşçı — melee AI (chase + attack + telegraph) | enemy-system.md, enemy-ai.md | 1.5 | 2.3, 2.4 | Düşman oyuncuyu algılar, kovalayıp saldırır, 0.3s telegraph, hasar verir |
| 3.2 | Düşman spawn + OnRoomCleared | enemy-system.md | 0.5 | 3.1, 1.5 | Oda girişinde 3-5 düşman spawn, hepsi ölünce OnRoomCleared event |
| 3.5 | Basit HUD — HP bar + damage numbers | hud.md | 0.5 | 2.3 | HP bar doğru güncellenir, hasar alınca floating number pop-up |

### Should Have

| ID | Task | Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------|-----------|-------------|-------------------|
| 3.3 | Katliam Paktı — +%60 hasar, düşman respawn | pact-system.md | 1 | 2.4, 3.1 | DamageMultiplier 1.6 uygulanır, düşman 1 kez dirilir, 2. ölümde final |
| 3.4 | Pact Selection UI — basit modal (1 pakt göster, seç) | pact-selection-ui.md | 0.5 | 3.3 | Kart görünür, boon/bane okunur, seçim → pakt aktif |
| 3.6 | Run Manager — basit start/death/restart | run-manager.md | 0.5 | 1.3 | Play → Playing, ölüm → GameOver ekranı, restart → yeni run |
| 2.6 | Damage numbers — renkli floating text | hud.md | 0.5 | 2.3 | Hasar tipi rengi doğru, yukarı float + fade, crit büyük |

### Nice to Have

| ID | Task | Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------|-----------|-------------|-------------------|
| 4.1 | Game feel tuning — hitstop, shake, knockback değer ayarı | combat-system.md | 1 | 2.4 | Değerler ayarlanmış, combat "juicy" hisseder |
| 4.2 | Playtest + notlar | — | 0.5 | all | Prototip README sonuçlarla doldurulmuş |

## Carryover from Previous Sprint

| Task | Reason | New Estimate |
|------|--------|-------------|
| — | İlk sprint, carryover yok | — |

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| ELV asset import sorunları (sprite slice, PPU ayarı) | Orta | Task 1.5 gecikir | İlk gün asset import'u test et, sorun varsa placeholder sprite kullan |
| Combat feel iyi olmaz (hitbox boyutu, hitstop süresi) | Yüksek | Prototip amacını karşılamaz | Buffer günleri tuning'e ayrılmış, tuning knob'lar GDD'de tanımlı |
| Hitbox/hurtbox collision layer sorunları | Orta | Combat çalışmaz | ADR-002 layer matrix'ini 1. gün kur, test sahnesinde doğrula |
| A* pathfinding performansı | Düşük | Düşmanlar yavaş hareket eder | ADR-003'te bütçe hesaplanmış (<0.1ms/query), basit grid yeterli |

## Dependencies on External Factors
- Unity 6.3 LTS kurulu olmalı (Unity Hub'dan indir)
- ELV Games Ultimate TopDown Adventure asset pack projeye import edilmiş olmalı

## Definition of Done for this Sprint
- [x] All Must Have tasks completed
- [ ] All tasks pass acceptance criteria
- [ ] No S1 or S2 bugs in delivered features
- [ ] Design documents updated for any deviations
- [ ] Code reviewed and merged
- [ ] Prototype README filled with playtest results
- [ ] 60 FPS maintained with 5 active enemies

## Effort Summary

| Tier | Tasks | Total Days |
|------|-------|-----------|
| Must Have | 14 | 10.5 |
| Should Have | 4 | 2.5 |
| Nice to Have | 2 | 1.5 |
| **Total** | **20** | **14.5** |
| **Available (with buffer)** | — | **11** |

**Not:** Must Have (10.5 gün) available capacity'yi (11 gün) dolduruyor. Should Have task'ları buffer'dan veya Must Have'lerden kazanılan zamandan yapılacak. Nice to Have'ler sadece erken biterse.
