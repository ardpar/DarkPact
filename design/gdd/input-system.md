# Input System

> **Status**: Designed
> **Author**: User + Claude
> **Last Updated**: 2026-04-03
> **Implements Pillar**: Core Infrastructure

## Overview

Input System, oyuncunun tüm fiziksel girdilerini (klavye, mouse) oyun aksiyonlarına çeviren altyapı katmanıdır. Unity'nin New Input System (Input Action Assets) üzerine kurulur. Ham input'u "Move", "Attack", "Dash", "Skill1-4", "Pause" gibi soyut aksiyonlara eşler — hangi tuşun ne yaptığını bilmek bu sistemin sorumluluğundadır, o aksiyonla ne yapılacağı Player Controller ve diğer sistemlere aittir. Rebinding (tuş değiştirme) desteği buradan sağlanır.

## Player Fantasy

Input System oyuncunun bilinçli olarak fark ettiği bir sistem değildir. Doğru çalıştığında oyuncu "düşündüğüm an hareket ediyorum" hisseder — input ile aksiyon arasında algılanabilir gecikme yoktur. Tuşlar sezgiseldir, rebinding kolaydır. Başarısızlığı ise anında hissedilir: input lag, yanlış tuş eşlemesi veya "bastım ama olmadı" anları oyunu oynanamaz kılar. Hack-and-slash'te input responsiveness her şeydir.

## Detailed Design

### Core Rules

1. **Unity New Input System** kullanılır — Input Action Asset ile tanımlı, event-driven
2. **Action Map'ler** ile context'e göre input ayrışır:
   - `Gameplay` — Hareket, saldırı, dash, skill'ler (Playing state'inde aktif)
   - `UI` — Menü navigasyonu, buton tıklamaları (her zaman aktif)
   - `Global` — Pause toggle (her zaman aktif, Loading hariç)
3. **Input Buffer** — Son 0.1s içindeki input'lar zaman damgasıyla buffer'lanır. Input System sadece buffer'ı tutar ve `GetBufferedAction()` API'si ile son buffer'lı aksiyonu sunar. Buffer'dan ne zaman consume edileceğine karar vermek **Player Controller'ın sorumluluğundadır** — animasyon state'ine göre buffer'ı okur ve uygun anda tetikler (Input System animasyon durumunu bilmez)
4. **Mouse aim** — Mouse pozisyonu sürekli takip edilir, world-space'e çevrilir. Saldırı yönü mouse pozisyonuna göredir

### Action Tanımları

| Action | Varsayılan Binding | Tip | Action Map |
|--------|-------------------|-----|------------|
| Move | WASD | Value (Vector2) | Gameplay |
| Attack | Sol Mouse | Button | Gameplay |
| Dash | Space | Button | Gameplay |
| Skill1 | 1 | Button | Gameplay |
| Skill2 | 2 | Button | Gameplay |
| Skill3 | 3 | Button | Gameplay |
| Skill4 | 4 | Button | Gameplay |
| AimPosition | Mouse Position | Value (Vector2) | Gameplay |
| Pause | ESC | Button | Global |
| Interact | E | Button | Gameplay |
| UINavigate | WASD / Arrow Keys | Value (Vector2) | UI |
| UISubmit | Enter / Sol Mouse | Button | UI |
| UICancel | ESC | Button | UI |

### States and Transitions

| Durum | Aktif Action Map'ler | Tetikleyen |
|-------|---------------------|------------|
| **Playing** | Gameplay + Global | `OnGameStateChanged` → Playing |
| **Paused** | UI + Global | `OnGameStateChanged` → Paused |
| **MainMenu** | UI | `OnGameStateChanged` → MainMenu |
| **Loading** | — (tüm input devre dışı) | `OnGameStateChanged` → Loading |
| **GameOver** | UI | `OnGameStateChanged` → GameOver |

### Interactions with Other Systems

| Sistem | Yön | Arayüz |
|--------|-----|--------|
| **Game Manager** | ← dinler | `OnGameStateChanged` → aktif action map'leri değiştirir |
| **Player Controller** | → sağlar | `OnMove(Vector2)`, `OnAttack()`, `OnDash()`, `OnSkill(int)`, `OnAimPosition(Vector2)` callback'leri |
| **HUD** | → sağlar | UI action map üzerinden menü navigasyonu |
| **Pact Selection UI** | → sağlar | UI action map üzerinden pakt seçimi |

## Formulas

Input System matematiksel formül içermez. Tek performans metriği:

- **Input-to-callback latency**: < 1 frame (16.6ms)
- **Input buffer penceresi**: 0.1s (configurable, bkz. Tuning Knobs)
- **Mouse world-space dönüşümü**: `Camera.ScreenToWorldPoint(mouseScreenPos)` — her frame hesaplanır

## Edge Cases

| Durum | Ne olur |
|-------|---------|
| **Aynı anda birden fazla yön tuşuna basılırsa** | Vector2 normalize edilir — çapraz hareket düz hareketten hızlı olmaz |
| **Attack ve Dash aynı frame'de basılırsa** | Input buffer sırası korunur — ilk basılan önce işlenir. Aynı frame ise Dash öncelikli (kaçış > saldırı) |
| **Mouse ekran dışına çıkarsa** | Son bilinen world-space pozisyonu korunur, aim yönü değişmez |
| **Rebinding sırasında çakışan tuş atanırsa** | Uyarı gösterilir, eski binding kaldırılır, yeni binding uygulanır |
| **Gameplay action map devre dışıyken buffer'da input varsa** | Buffer temizlenir — state geçişinde eski input'lar yeni state'e taşınmaz |
| **Oyuncu Loading sırasında tuşlara basarsa** | Tüm action map'ler devre dışı, input yok sayılır |
| **ESC pause menüsünde ve UI'da aynı anda dinleniyorsa** | Global map ESC → Pause toggle. UI map ESC → UICancel. Paused state'inde Global ESC resume tetikler, UI ESC ikincil (menü geri navigasyonu). Priority: Global > UI |

## Dependencies

**Upstream (Input System bunlara bağımlı):** Yok — Foundation layer.

**Downstream (bunlar Input System'a bağımlı):**

| Sistem | Bağımlılık Tipi | Arayüz |
|--------|----------------|--------|
| Player Controller | Hard | Gameplay action callback'leri (Move, Attack, Dash, Skill, AimPosition) |

**Cross-system:** Game Manager'ın `OnGameStateChanged` event'ini dinler, ancak Game Manager'a bağımlı değildir — event yoksa tüm action map'ler aktif kalır (graceful degradation).

## Tuning Knobs

| Knob | Tip | Varsayılan | Güvenli Aralık | Etki |
|------|-----|-----------|----------------|------|
| `InputBufferDuration` | float (saniye) | 0.1 | 0.05–0.3 | Buffer penceresi. Çok kısa → input yutulur. Çok uzun → oyuncu istemediği aksiyonlar tetiklenir. |
| `MouseDeadzone` | float (piksel) | 2.0 | 0.0–10.0 | Mouse bu kadar piksel hareket etmeden aim güncellenmez. 0 → micro-jitter aim'i bozar. Çok yüksek → aim sluggish hisseder. |
| `DiagonalNormalization` | bool | true | true/false | Çapraz hareketin normalize edilip edilmeyeceği. false → çapraz %41 daha hızlı (bazı oyuncular bunu tercih eder). |

## Visual/Audio Requirements

- Rebinding ekranında "Bir tuşa basın..." prompt animasyonu
- Başarılı rebinding'de kısa onay SFX'i
- Input System'ın kendisi görsel/audio üretmez — tüm feedback downstream sistemlere aittir

## UI Requirements

- **Rebinding Screen**: Mevcut binding'leri gösteren liste, her satırda "Değiştir" butonu
- Rebinding sırasında modal overlay: "Yeni tuşa basın... (ESC ile iptal)"
- Çakışma uyarısı dialog'u: "[Tuş] zaten [Aksiyon] için kullanılıyor. Değiştirmek istiyor musunuz?"
- Varsayılana dön butonu

## Acceptance Criteria

| # | Kriter | Doğrulama Yöntemi |
|---|--------|-------------------|
| 1 | WASD ile 8 yönlü hareket çalışır, çapraz normalize edilir | Unit test: (1,1) input → magnitude 1.0 |
| 2 | Mouse pozisyonu doğru world-space koordinatına çevrilir | Integration test: bilinen ekran pozisyonu → beklenen world pos |
| 3 | Input buffer 0.1s içindeki input'ları tutar ve sırayla tetikler | Unit test: animasyon sırasında Dash input'u → animasyon bitince Dash tetiklenir |
| 4 | Game state değiştiğinde doğru action map'ler aktif olur | Unit test: her state için aktif map kontrolü |
| 5 | Loading state'inde hiçbir input işlenmez | Unit test: Loading'de tüm action'lar disabled |
| 6 | Rebinding çalışır ve çakışmalar uyarı verir | Integration test: tuş değiştir, çakışma yarat, uyarı gör |
| 7 | Input-to-callback latency < 1 frame | Performance test: input timestamp vs callback timestamp farkı |
| 8 | State geçişinde input buffer temizlenir | Unit test: Playing→Paused geçişinde buffer boşalır |

## Open Questions

1. Gamepad desteği MVP'de mi yoksa sonra mı? (Önerilen: MVP sonrası, PC-first)
2. Hold vs tap ayrımı gerekli mi? (Örn: Attack hold = charged attack) (Karar: Combat System GDD'sinde belirlenecek)
3. Mouse sensitivity ayarı gerekli mi? (Önerilen: Top-down'da mouse hareket hızı değil pozisyon önemli — gereksiz)
