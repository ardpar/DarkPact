# Sprint 003 — 2026-05-02 to 2026-05-16

## Sprint Goal
Prototipi baştan sona oynanabilir hale getir: sahne geçişlerini stabilize et, oda prefab'larını cilalanmış tasarla, pakt/loot/boss mekaniklerini playtest-ready yap — "bu demo gösterilebilir mi?" sorusunu cevapla.

## Capacity
- Total days: 14
- Buffer (20%): 3 gün
- Available: 11 gün

## Tasks

### Must Have (Critical Path)

| ID | Task | Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------|-----------|-------------|-------------------|
| 9.1 | Sahne geçişi stabilizasyonu — MainMenu→Gameplay arası ServiceLocator reset, state cleanup, DontDestroyOnLoad düzgün çalışması | — | 1 | — | MainMenu→Gameplay→GameOver→MainMenu→Gameplay döngüsü hatasız çalışır, memory leak yok |
| 9.2 | PactSelectionUI stabilizasyonu — 3 kart görünüyor, seçim çalışıyor, deaktif/aktif geçişleri sorunsuz | — | 0.5 | 9.1 | Pakt seçim ekranı her run'da düzgün açılır, kart butonları tıklanabilir, seçim sonrası dungeon başlar |
| 9.3 | Oda prefab'larını tasarla — her room prefab'ında tile decoration, iç engeller, görsel çeşitlilik | — | 2 | — | Room_Start, Room_Combat_Small, Room_Treasure, Room_Boss_Large prefab'ları elle cilalanmış, ELV Crypt tile'ları ile |
| 9.4 | DungeonManager prefab lifecycle — oda geçişinde eski prefab temizleme, yeni prefab yükleme, düşman/loot cleanup | — | 1 | 9.1 | 7 oda boyunca geçiş yapılabilir, spawn point'ler çalışır, boss odasına ulaşılır |
| 9.5 | Loot pickup görsel feedback — ikon gösterimi, magnet efekti, toplama sesi (placeholder) | — | 0.5 | — | Loot yere düşünce sprite görünür, oyuncuya yaklaşınca magnet, toplama anında kısa flash |
| 9.6 | Pakt efektleri tam çalışsın — Lanetli Dokunuş zehir uygulama, Açgözlülük loot modifier, Kan Kalkanı kill→TempHP | — | 1 | — | Her 5 paktın hem boon hem bane'i gözlemlenebilir şekilde çalışır |
| 9.7 | Game Over UI — ölüm ekranı run istatistikleri (kills, rooms, damage, score), Tekrar Dene / Ana Menü butonları | — | 0.5 | 9.1 | Ölünce istatistik paneli görünür, butonlar çalışır |
| 9.8 | HUD tamamlama — HP bar fill renk, aktif pakt ikonları, oda sayacı (X/7), minimap placeholder | — | 0.5 | 9.6 | HP bar renk geçişi, pakt ikonu, "Oda 3/7" yazısı |

### Should Have

| ID | Task | Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------|-----------|-------------|-------------------|
| 10.1 | Oda temizleme mekaniği — düşmanlar ölünce kapılar açılır (görsel feedback), temizlenmemiş odaya geri dönüş | — | 1 | 9.4 | Kapılar combat sırasında kilitli (duvar tile), düşmanlar bitince açılır (flash), temiz odaya dönüşte tekrar spawn yok |
| 10.2 | Node Editor iyileştirme — room prefab preview, drag-drop prefab atama, connection delete | — | 1 | — | Editor'da oda prefab thumbnail'ı görünür, prefab sürüklenip node'a atanır |
| 10.3 | 2+ farklı combat oda prefab'ı — Room_Combat_Medium (L-şekil), Room_Combat_Corridor (uzun dar) | — | 1 | 9.3 | En az 3 farklı combat oda şekli, layout SO'da karışık kullanım |
| 10.4 | Ses entegrasyonu — temel SFX: saldırı, hasar, ölüm, loot, kapı, pakt seçimi (ELV SFX) | — | 1 | — | 6+ farklı ses efekti oyunda duyulur |

### Nice to Have

| ID | Task | Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------|-----------|-------------|-------------------|
| 11.1 | Pakt sinerji sistemi — 2 pakt combo efektleri (Katliam+Kan Kalkanı vb.) | pact-system.md | 1 | 9.6 | En az 2 sinerji combo tanımlı ve çalışır |
| 11.2 | Equipment System — basit weapon/armor equip, stat uygulaması | equipment-system.md | 1 | — | Item equip edilir, stat değişir, UI'da görünür |
| 11.3 | Playtest session + prototype README findings | — | 0.5 | all | 3+ tam run oynanmış, findings kaydedilmiş |

## Carryover from Sprint 002

| Task | Reason | New Estimate |
|------|--------|-------------|
| Sahne geçişi sorunları | MainMenu→Gameplay arası state management kararsız | 1 gün (Task 9.1) |
| PactSelectionUI deaktif/aktif sorunu | Subscribe timing problemi | 0.5 gün (Task 9.2) |
| Oda prefab'ları ham | Runtime tile generation yerine prefab'a geçildi, henüz tasarlanmadı | 2 gün (Task 9.3) |
| Pakt efektleri kısmen çalışıyor | Lanetli Dokunuş, Açgözlülük tam implement değil | 1 gün (Task 9.6) |
| Playtest yapılmadı | Sprint 002'de sisteme odaklanıldı | 0.5 gün (Task 11.3) |

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| Sahne geçişi memory leak (DontDestroyOnLoad objeleri birikmesi) | Orta | Uzun session'da crash | ServiceLocator.Reset() her geçişte, profiler ile kontrol |
| Prefab lifecycle bug (eski oda kalıntıları, ghost collider) | Yüksek | Oyuncu görünmez duvara takılır | Her geçişte explicit cleanup, null check'ler |
| 5 paktın birbirleriyle etkileşim bug'ları | Orta | Broken run | Her pakt izole test, sonra ikili combo test matrisi |
| Node editor UX yetersiz (hand-craft oda tasarımı yavaş) | Düşük | İterasyon yavaşlar | Fallback: runtime tile generation hâlâ çalışır |

## Dependencies on External Factors
- Sprint 002 code base'inin compile edilebilir olması
- ELV asset pack SFX dosyalarının projeye import edilmiş olması

## Definition of Done for this Sprint
- [ ] All Must Have tasks completed
- [ ] MainMenu → Pakt Seçimi → 7 oda dungeon → Boss → Game Over → Restart tam döngü hatasız
- [ ] 5 paktın tümü boon+bane ile çalışır
- [ ] Loot düşer, toplanır, stat uygulanır
- [ ] HUD: HP bar, pakt ikonu, oda sayacı, game over stats
- [ ] No S1 or S2 bugs
- [ ] 60 FPS maintained throughout run
- [ ] En az 1 tam playtest run kaydedilmiş

## Effort Summary

| Tier | Tasks | Total Days |
|------|-------|-----------|
| Must Have | 8 | 7 |
| Should Have | 4 | 4 |
| Nice to Have | 3 | 2.5 |
| **Total** | **15** | **13.5** |
| **Available (with buffer)** | — | **11** |

**Not:** Must Have (7 gün) rahat capacity içinde. Kalan 4 gün Should Have'lere yetmeli — oda temizleme mekaniği (10.1) ve ses entegrasyonu (10.4) en yüksek öncelikli.
