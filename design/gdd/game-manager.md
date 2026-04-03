# Game Manager

> **Status**: Designed
> **Author**: User + Claude
> **Last Updated**: 2026-04-03
> **Implements Pillar**: Core Infrastructure

## Overview

Game Manager, Dark Pact'in global oyun durumunu (Main Menu, Playing, Paused, Game Over) ve sahne geçişlerini yöneten altyapı sistemidir. Oyuncu doğrudan etkileşmez; sistem arka planda çalışarak oyunun hangi durumda olduğunu belirler ve diğer sistemlerin bu duruma göre davranmasını sağlar. Game Manager olmadan hiçbir sistem oyunun aktif mi, duraklatılmış mı yoksa bitmiş mi olduğunu bilemez. Minimal tasarımdır — sadece state ve sahne yönetimi yapar; run akışı Run Manager'a, combat akışı Combat System'e aittir.

## Player Fantasy

Game Manager oyuncuya dönük bir fantezi sunmaz. Bu bir "infrastructure you don't notice" sistemidir — oyuncu menüden oyuna geçtiğinde, pause'a bastığında veya öldüğünde geçişlerin pürüzsüz ve anlık olmasını sağlar. Başarısı görünmezliğindedir: oyuncu hiçbir zaman "yükleniyor" beklemez, state hatası yaşamaz, ya da oyunun hangi durumda olduğu konusunda kafası karışmaz.

## Detailed Design

### Core Rules

1. **Singleton** — `DontDestroyOnLoad` ile sahneler arası yaşar, oyun boyunca tek instance bulunur
2. **State Machine** — Her an tek bir `GameState` aktif olur, geçersiz state geçişleri sessizce reddedilir ve loglanır
3. **Sahne Geçişleri** — `SceneManager.LoadSceneAsync` ile async sahne yükleme, loading screen desteği
4. **Time Control** — Pause durumunda `Time.timeScale = 0` uygulanır, UI input'ları pause'da da çalışır
5. **Event-Driven** — State değişikliklerini `OnGameStateChanged(oldState, newState)` event'i ile duyurur, diğer sistemler bu event'i dinler
6. **Initialization Sırası** — Boot state'inde diğer singleton servislerin initialize olmasını bekler, hepsi hazır olunca MainMenu'ye geçer

### States and Transitions

| State | Açıklama | Geçerli Geçişler |
|-------|----------|-----------------|
| **Boot** | Oyun ilk açılıyor, servisler initialize ediliyor | → MainMenu |
| **MainMenu** | Ana menü ekranı | → Loading |
| **Loading** | Sahne async yükleniyor | → Playing, → MainMenu (iptal) |
| **Playing** | Aktif gameplay | → Paused, → GameOver, → Loading |
| **Paused** | Oyun duraklatılmış (`timeScale = 0`) | → Playing (resume), → MainMenu (quit) |
| **GameOver** | Run bitti (ölüm veya zafer) | → Loading (yeni run), → MainMenu |

**Geçiş kuralları:**
- Boot → MainMenu: Otomatik, tüm servisler initialize olduktan sonra
- MainMenu → Loading: Oyuncu "Play" butonuna bastığında
- Loading → Playing: Sahne yüklemesi tamamlandığında otomatik
- Playing ↔ Paused: Oyuncunun ESC tuşuyla tetikler
- Playing → GameOver: Sadece Run Manager tetikler (ölüm veya son boss yenilgisi)
- GameOver → Loading: Oyuncu "Yeni Run" seçtiğinde
- GameOver → MainMenu: Oyuncu "Ana Menü" seçtiğinde
- Paused → MainMenu: Oyuncu "Oyundan Çık" seçtiğinde
- Diğer tüm geçişler geçersizdir ve loglanır

### Interactions with Other Systems

| Sistem | Yön | Arayüz |
|--------|-----|--------|
| **Run Manager** | ← talep eder | `RequestStateChange(GameState)` ile yeni run başlatır (Loading) veya run bitirir (GameOver) |
| **Player Controller** | → bildirir | `OnGameStateChanged` event'ini dinler; Paused/GameOver'da input devre dışı |
| **Camera System** | → bildirir | `OnGameStateChanged` dinler; Loading'de kamerayı resetler |
| **Health & Damage** | → bildirir | `OnGameStateChanged` dinler; Paused'da damage hesabı durur |
| **Item Database** | → bildirir | Boot state'inde initialize olur, hazır olduğunu Game Manager'a bildirir |
| **Save/Load System** | → bildirir | `OnGameStateChanged` dinler; GameOver'da meta progression auto-save tetikler |

## Formulas

Game Manager matematiksel formül içermez. State geçişleri deterministiktir ve hesaplama gerektirmez. Tek performans metrikleri:

- **Maksimum sahne yükleme süresi**: < 3 saniye (Loading state süresi)
- **State geçiş overhead'i**: < 1 frame (16.6ms)

## Edge Cases

| Durum | Ne olur |
|-------|---------|
| **Oyuncu pause menüsündeyken GameOver tetiklenirse** | GameOver öncelikli — Paused state'i atlanır, direkt GameOver'a geçilir. Run Manager'ın `RequestStateChange` çağrısı Paused'dan GameOver'a geçişe izin verir. |
| **Sahne yüklenirken oyuncu ESC basarsa** | Loading state'inde ESC yok sayılır. Loading iptal sadece "MainMenu'ye dön" butonu ile mümkün. |
| **Boot sırasında bir servis initialize olamazsa** | Game Manager hata loglar ve fallback ile devam eder. Kritik servis başarısızsa (Run Manager gibi) → hata ekranı gösterilir, MainMenu'ye geçilmez. |
| **Aynı state'e geçiş talep edilirse** | Geçiş reddedilir, loglanır. Event fırlatılmaz. |
| **Çok hızlı art arda state geçişi** | Geçiş queue'lanmaz — mevcut geçiş tamamlanmadan yeni geçiş reddedilir. `IsTransitioning` flag'i kontrol edilir. |
| **Playing'den Loading'e geçerken aktif combat varsa** | Game Manager `OnGameStateChanged` fırlatır → Combat System cleanup yapar → sahne yükleme başlar. Sıra garanti edilir. |

## Dependencies

**Upstream (Game Manager bunlara bağımlı):** Yok — Foundation layer, bağımlılığı olmayan en temel sistem.

**Downstream (bunlar Game Manager'a bağımlı):**

| Sistem | Bağımlılık Tipi | Arayüz |
|--------|----------------|--------|
| Camera System | Hard | `OnGameStateChanged` event'i, `CurrentState` property |
| Player Controller | Hard | `OnGameStateChanged` event'i, `CurrentState` property |
| Health & Damage | Hard | `OnGameStateChanged` event'i |
| Item Database | Hard | Boot sırasında initialization sırası |
| Run Manager | Hard | `RequestStateChange()` metodu, `OnGameStateChanged` event'i |
| Save/Load System | Hard | `OnGameStateChanged` event'i |

Tüm bağımlılıklar hard — bu sistemler Game Manager olmadan çalışamaz.

## Tuning Knobs

| Knob | Tip | Varsayılan | Güvenli Aralık | Etki |
|------|-----|-----------|----------------|------|
| `SceneLoadTimeout` | float (saniye) | 10.0 | 5.0–30.0 | Bu sürede sahne yüklenmezse hata loglanır ve MainMenu'ye dönülür. Çok düşük → normal yüklemelerde timeout. Çok yüksek → oyuncu bekler. |
| `MinLoadingScreenDuration` | float (saniye) | 0.5 | 0.0–2.0 | Loading ekranının minimum gösterim süresi. Çok hızlı yüklemelerde flash effect'i önler. 0 → anında geçiş. |
| `PauseTimeScale` | float | 0.0 | 0.0–0.1 | Pause'daki `Time.timeScale`. 0 = tam duraklama. 0.1 = ağır çekim pause (stilistik tercih). |

## Visual/Audio Requirements

- Loading ekranında basit bir progress bar veya spinner animasyonu
- State geçişlerinde fade-to-black / fade-from-black efekti (0.3s)
- Pause aktifken ekrana yarı-saydam karartma overlay'i
- Ses: Pause'da müzik low-pass filter ile kısılır, resume'da normal döner

## UI Requirements

- **Loading Screen**: Progress bar + "Loading..." metni (ELV Neatpixels font)
- **Pause Menu**: Resume, Ayarlar, Ana Menüye Dön butonları, yarı-saydam arka plan
- **GameOver Screen**: Run istatistikleri (süre, öldürme, toplanan altın), Yeni Run / Ana Menü butonları
- Tüm UI elementleri pause state'inde input alabilmeli (`Time.timeScale = 0` iken)

## Acceptance Criteria

| # | Kriter | Doğrulama Yöntemi |
|---|--------|-------------------|
| 1 | Boot → MainMenu geçişi tüm servisler initialize olduktan sonra otomatik gerçekleşir | Unit test: tüm servisleri mockla, Boot'tan MainMenu'ye geçişi doğrula |
| 2 | Her geçerli state geçişi `OnGameStateChanged` event'i fırlatır | Unit test: her geçişte event subscriber'ın doğru `oldState`/`newState` aldığını doğrula |
| 3 | Geçersiz state geçişleri reddedilir ve loglanır | Unit test: Playing → Boot gibi geçersiz geçişlerde state değişmediğini doğrula |
| 4 | Pause `Time.timeScale = 0` uygular, resume `1` döndürür | Unit test: Paused state'inde timeScale kontrolü |
| 5 | Sahne yükleme async çalışır, `SceneLoadTimeout` içinde tamamlanır | Integration test: test sahnesini yükle, süreyi ölç |
| 6 | Eşzamanlı iki geçiş talep edildiğinde ikincisi reddedilir | Unit test: `IsTransitioning = true` iken ikinci `RequestStateChange` false döner |
| 7 | GameOver state'inde pause talebi reddedilir | Unit test: GameOver'dan Paused'a geçiş denemesi, state değişmez |
| 8 | State geçiş overhead'i < 1 frame (16.6ms) | Performance test: geçiş süresini `Stopwatch` ile ölç |

## Open Questions

1. Loading screen'de ipucu/lore metinleri gösterilecek mi? (Karar: Vertical Slice'da değerlendirilecek)
2. Pause menüsünde ayarlar (ses, görüntü) MVP'de mi yoksa sonra mı? (Önerilen: MVP sonrası)
3. GameOver ekranında pakt seçimlerinin özeti gösterilmeli mi? (Pact System GDD'si yazıldığında kararlaştırılacak)
