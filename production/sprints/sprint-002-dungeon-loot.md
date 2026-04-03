# Sprint 002 — 2026-04-18 to 2026-05-02

## Sprint Goal
Procedural dungeon generation ve loot sistemiyle tek odalı prototipi gerçek bir run deneyimine dönüştür: birden fazla oda, düşman loot'u, 1 boss, 5 pakt — "bu tekrar oynanır mı?" sorusunu cevapla.

## Capacity
- Total days: 14
- Buffer (20%): 3 gün (bug fix / tuning)
- Available: 11 gün

## Tasks

### Must Have (Critical Path)

| ID | Task | Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------|-----------|-------------|-------------------|
| 5.1 | Sprint 001 bug fix — PlayerController.FlashWhite (renk restore), HitstopManager/pause çakışması, VFXManager singleton→ServiceLocator | — | 0.5 | — | FlashWhite orijinal renge döner, hitstop pause sırasında çakışmaz, VFXManager ServiceLocator kullanır |
| 5.2 | Legacy Input temizliği — GameManager/SimpleHUD/GameBootstrap'ta Input.GetKeyDown → Input Action Asset | — | 0.5 | — | Tüm input Input Action Asset üzerinden, UnityEngine.Input referansı sıfır |
| 5.3 | Dash trail VFX (Sprint 001 carryover) | vfx-system.md | 0.5 | 5.1 | Dash sırasında afterimage trail görünür, pool'dan çıkar, GC allocation yok |
| 5.4 | Damage number renkleri (Sprint 001 carryover) | hud.md | 0.25 | — | Normal=beyaz, crit=sarı/büyük, heal=yeşil |
| 6.1 | Procedural Dungeon Generator — BSP/random walk oda layout, koridor bağlantıları | procedural-dungeon-generator.md | 2 | — | 5-8 oda üretir, tüm odalar erişilebilir, duvar collider'ları doğru, son oda=boss |
| 6.2 | Oda geçiş sistemi — kapı trigger, oda yükleme, kamera geçişi | procedural-dungeon-generator.md, camera-system.md | 1 | 6.1 | Kapıdan geçince yeni oda yüklenir, kamera smooth geçiş yapar, önceki oda deaktif |
| 6.3 | Item Database (ScriptableObject) — temel item tanımları | item-database.md | 0.5 | — | ItemDefinition SO: isim, ikon, rarity, stat bonusları. 5+ test item tanımlı |
| 6.4 | Loot System — düşman ölünce drop, pickup collider, envanter yok (direkt stat) | loot-system.md | 1 | 6.3, 3.2 | Düşman ölünce %50 loot drop, yere düşer, oyuncu dokunca alınır, stat uygulanır |
| 6.5 | Status Effect System — poison, slow, buff/debuff framework | status-effect-system.md | 1 | 2.3 | Efekt uygulanır/süresi biter, tick damage çalışır, UI ikonu gösterir |
| 6.6 | 4 ek pakt (toplam 5) — Kan Kalkanı, Gölge Adımı, Lanetli Dokunuş, Açgözlülük | pact-system.md | 1.5 | 6.5, 6.4 | Her pakt boon+bane çalışır, PactSelectionUI 3 seçenek gösterir |

### Should Have

| ID | Task | Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------|-----------|-------------|-------------------|
| 7.1 | Boss System — Minotaur (pattern-based AI: charge, stomp, idle) | boss-system.md | 1.5 | 6.1, 3.1 | Boss odasında spawn, 3 attack pattern, HP bar ayrı gösterilir, ölünce akt tamamlanır |
| 7.2 | Kamera oda sınır clamp'i (Sprint 001 eksik) | camera-system.md | 0.25 | 6.2 | Kamera oda dışına çıkmaz, geçişte smooth lerp |
| 7.3 | A* Pathfinding — grid-based, oda walkability | enemy-ai.md | 1 | 6.1 | Düşmanlar duvarlardan kaçınır, 8-yönlü hareket, köşe kesme yok, 0.3s cache |
| 7.4 | SimpleHUD iyileştirme — pakt ikonu göster, oda/boss bilgisi | hud.md | 0.5 | 6.6 | Aktif pakt ikonu HUD'da, mevcut oda/toplam oda sayısı |

### Nice to Have

| ID | Task | Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------|-----------|-------------|-------------------|
| 8.1 | Pakt sinerji sistemi (2 pakt combo) — Katliam+Kan Kalkanı | pact-system.md | 1 | 6.6 | 2 pakt seçildiğinde sinerjik etki aktif, UI'da gösterilir |
| 8.2 | Playtest + prototype README sonuçları | — | 0.5 | all | README.md findings bölümü dolu, run istatistikleri kaydedilmiş |
| 8.3 | Equipment System — basit silah/zırh equip | equipment-system.md | 1 | 6.3, 6.4 | Loot'tan gelen item equip edilir, stat uygulanır |

## Carryover from Sprint 001

| Task | Reason | New Estimate |
|------|--------|-------------|
| 2.2 Dash trail VFX | VFXManager'da trail metodu eksik | 0.5 gün (Task 5.3) |
| 2.6 Damage number renkleri | Renk tipi implement edilmedi | 0.25 gün (Task 5.4) |
| 1.6 Kamera oda clamp | Clamp kodu yazılmadı | 0.25 gün (Task 7.2) |
| 4.2 Playtest + notlar | Prototip henüz test edilmedi | 0.5 gün (Task 8.2) |
| Bug fixes | FlashWhite, HitstopManager, VFX singleton, legacy input | 1 gün (Task 5.1 + 5.2) |

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| Procedural dungeon oda bağlantı hataları (ulaşılamaz oda) | Yüksek | Run oynanamaz | BFS/flood fill ile connectivity garantisi, erken prototip |
| 5 pakt balans sorunu (aşırı güçlü/zayıf combo) | Yüksek | Deneyim bozulur | Tuning knob'lar SO'da, playtest ile ayarla, buffer günü ayır |
| Boss pattern AI karmaşıklığı (scope creep) | Orta | Sprint gecikir | MVP'de 2-3 basit pattern yeterli, polish sonraki sprint |
| Loot drop rate dengesizliği | Orta | Ekonomi bozuk | Basit başla (%50 drop, 3 item tipi), playtest ile ayarla |
| Status effect + pakt etkileşim bug'ları | Orta | Pakt sistemi kırılır | Her pakt isolation test, sonra combo test |

## Dependencies on External Factors
- Sprint 001 prototype'ın çalışır durumda olması (scene setup tamamlandı)
- ELV asset pack'ten boss sprite (Minotaur veya benzeri) mevcut olmalı

## Definition of Done for this Sprint
- [ ] All Must Have tasks completed
- [ ] Procedural dungeon 5+ oda üretir, tüm odalar erişilebilir
- [ ] 5 pakt çalışır, 3 seçenek sunulur
- [ ] Loot düşer ve alınır
- [ ] Sprint 001 bug'ları giderilmiş
- [ ] No S1 or S2 bugs in delivered features
- [ ] 60 FPS maintained with 8 active enemies
- [ ] Prototype README findings güncellenmiş

## Effort Summary

| Tier | Tasks | Total Days |
|------|-------|-----------|
| Must Have | 10 | 8.75 |
| Should Have | 4 | 3.25 |
| Nice to Have | 3 | 2.5 |
| **Total** | **17** | **14.5** |
| **Available (with buffer)** | — | **11** |

**Not:** Must Have (8.75 gün) available capacity (11 gün) içinde. Kalan ~2.25 gün Should Have'lere ayrılır. Boss (7.1) ve A* pathfinding (7.3) Should Have'de en yüksek öncelikli.
