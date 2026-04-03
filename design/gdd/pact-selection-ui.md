# Pact Selection UI

> **Status**: Designed
> **Author**: User + Claude
> **Last Updated**: 2026-04-04
> **Implements Pillar**: Presentation — Core Identity

## Overview

Pact Selection UI, run başında ve milestone'larda oyuncuya 3 pakt teklif eden ekranı yöneten sistemdir. Oyunun en kritik karar anıdır — burada seçilen paktlar run'ın tamamını şekillendirir. Karanlık, dramatik atmosfer ile paktların "şeytanla anlaşma" hissini görsel olarak destekler. Seçim zorunludur — çıkış yok.

## Player Fantasy

Ekran kararır, 3 gizemli kart belirir. Her kartı incelediğinde güçlü bir nimet ve korkutucu bir bela görürsün. "Hangisini seçmeliyim?" gerilimi doruk noktasında. Seçim anında dramatik bir flash — geri dönüş yok. Bu an oyunun mantra'sını somutlaştırır: "Şeytanla anlaş, bedelini öde."

## Detailed Design

### Core Rules

1. **Modal ekran**: Gameplay durur (Paused-like), sadece pakt seçimi aktif
2. **3 kart layout**: Ortada yan yana 3 pakt kartı
3. **Hover detay**: Kart üzerine gelince detaylı boon/bane açıklaması
4. **Seçim zorunlu**: 1 kart seçilmeli — kapatma/kaçış yok
5. **Seçim sonrası**: Kart efekti, diğer kartlar kaybolur, gameplay devam
6. **Sınıf affinity**: Uyumlu paktlarda özel gösterge ("+%20 güçlü" gibi)

### UI Layout

```
┌────────────────────────────────────────────────────┐
│                                                    │
│              "Karanlık bir güç seni çağırıyor..."   │  ← Flavor text
│                                                    │
│    ┌──────────┐  ┌──────────┐  ┌──────────┐       │
│    │  [ICON]  │  │  [ICON]  │  │  [ICON]  │       │
│    │          │  │          │  │          │       │
│    │ Katliam  │  │Kan Kalkanı│  │ Gölge   │       │
│    │  Paktı   │  │          │  │ Adımı    │       │
│    │          │  │          │  │          │       │
│    │ BOON:    │  │ BOON:    │  │ BOON:    │       │
│    │ +%60     │  │ +5 TempHP│  │ Sınırsız │       │
│    │ hasar    │  │ /kill    │  │ Dash     │       │
│    │          │  │          │  │          │       │
│    │ BANE:    │  │ BANE:    │  │ BANE:    │       │
│    │ Düşmanlar│  │ İyileşme │  │ Durma    │       │
│    │ dirilir  │  │ yok      │  │ hasarı   │       │
│    │          │  │          │  │          │       │
│    │ [SEÇ]   │  │ [SEÇ]   │  │ [SEÇ]   │       │
│    └──────────┘  └──────────┘  └──────────┘       │
│                                                    │
│            "Bir pakt seçmelisin."                  │  ← Instruction
└────────────────────────────────────────────────────┘
```

### Kart Yapısı

```
PactCard
├── pactDef: PactDefinition
├── cardFrame: Sprite (pakt tier'ına göre: Common=gri, Rare=mor, Mythic=altın)
├── icon: Sprite (pakt ikonu)
├── nameText: string (pakt adı)
├── boonText: string (yeşil renk, boon açıklaması)
├── baneText: string (kırmızı renk, bane açıklaması)
├── affinityBadge: GameObject (sınıf uyumu varsa görünür)
├── synergyIndicator: GameObject (mevcut paktla sinerji varsa görünür — VS)
└── selectButton: Button
```

### Animasyon Akışı

| Adım | Süre | Olay |
|------|------|------|
| 1 | 0.5s | Ekran kararır (fade to dark overlay) |
| 2 | 0.3s | Flavor text belirme (fade in) |
| 3 | 0.2s × 3 | Kartlar sırayla belirme (soldan sağa, slide up + fade in) |
| 4 | — | Oyuncu inceleme ve seçim |
| 5 | 0.3s | Seçilen kart büyür + glow, diğerleri fade out |
| 6 | 0.5s | Pakt rengiyle ekran flash |
| 7 | 0.3s | Overlay fade out → gameplay devam |
| **Toplam** | ~2.5s + seçim süresi | |

### States and Transitions

| State | Açıklama | Geçiş |
|-------|----------|-------|
| **Hidden** | Ekran kapalı | → Appearing (milestone/run start tetikler) |
| **Appearing** | Animasyon: overlay + kartlar beliriyor | → Selecting (animasyon bitince) |
| **Selecting** | Oyuncu kartları inceliyor, hover aktif | → Selected (kart seçildiğinde) |
| **Selected** | Seçim animasyonu oynuyor | → Hidden (animasyon bitince → gameplay) |

### Interactions with Other Systems

| Sistem | Yön | Arayüz |
|--------|-----|--------|
| **Pact System** | ← veri alır | `GeneratePactOptions(3)` → 3 PactDefinition |
| **Pact System** | → seçim bildirir | `SelectPact(pactDef)` → boon/bane uygulanır |
| **Run Manager** | ← tetikler | Milestone/run start → UI açılır |
| **Game Manager** | → state değiştirir | UI açılınca oyun pause-like duruma geçer |
| **Input System** | ← UI action map | Mouse hover, click, keyboard nav |
| **Class System** | ← bilgi alır | Aktif sınıf → affinity badge gösterimi |
| **Audio Manager** | → ses tetikler | Kart hover SFX, seçim SFX, dramatik ambient |

## Formulas

Pact Selection UI formül içermez — pure presentation. Pakt teklif mantığı Pact System'da.

## Edge Cases

| Durum | Ne olur |
|-------|---------|
| **3'ten az pakt kaldıysa** | Kalan kadar kart gösterilir (1 veya 2). Boş slotlar görünmez. |
| **Oyuncu seçmeden bekler** | Timeout yok — istediği kadar düşünebilir. Bu stratejik bir karar. |
| **Hover sırasında mouse karttan çıkarsa** | Detay tooltip kapanır, kart normal boyutuna döner. |
| **Seçim animasyonu sırasında input gelirse** | Input yok sayılır — animasyon tamamlanana kadar input lock. |
| **Sınıf affinity + sinerji aynı kartta** | İkisi de gösterilir: affinity badge + sinerji indicator (farklı köşelerde). |

## Dependencies

**Upstream:**

| Sistem | Bağımlılık Tipi | Arayüz |
|--------|----------------|--------|
| Pact System | Hard | Pakt verileri ve seçim API |
| Run Manager | Hard | Tetikleme zamanlaması |

**Downstream:** Yok — pure presentation.

## Tuning Knobs

| Knob | Tip | Varsayılan | Güvenli Aralık | Etki |
|------|-----|-----------|----------------|------|
| `CardAppearDelay` | float (s) | 0.2 | 0.1–0.5 | Kartlar arası belirme gecikmesi. Düşük → hızlı. Yüksek → dramatik. |
| `SelectionFlashDuration` | float (s) | 0.5 | 0.2–1.0 | Seçim flash süresi. |
| `OverlayFadeDuration` | float (s) | 0.5 | 0.2–1.0 | Karartma fade süresi. |
| `CardHoverScale` | float | 1.1 | 1.0–1.2 | Hover'da kart büyüme oranı. |

## Visual/Audio Requirements

- **Arka plan**: Koyu, duman/sis particle overlay, hafif turuncu/mor ışık kaynağı
- **Kart tasarımı**: Gotik çerçeve, pakt ikonu ortada, boon yeşil metin, bane kırmızı metin
- **Hover efekti**: Kart hafifçe büyür + çerçeve parlaması + düşük "hum" sesi
- **Seçim efekti**: Pakt renginde ekran flash + dramatik "boom" SFX + kart patlaması particle
- **Ambient ses**: Fısıltı/mırıltı (karanlık varlık sesi) — gerilim atmosferi
- **Font**: ELV Neatpixels Boss (7×7) pakt isimleri, Standard (5×5) açıklamalar

## UI Requirements

- Minimum 1280×720 desteklenir
- Kart boyutu ekranın %25 genişliği, yan yana 3 kart (%75 toplam + boşluklar)
- Keyboard navigasyonu: Sol/sağ ok ile kart seçimi, Enter ile onay
- Gamepad desteği (VS): D-pad navigasyonu, A butonu seçim

## Acceptance Criteria

| # | Kriter | Doğrulama Yöntemi |
|---|--------|-------------------|
| 1 | 3 pakt kartı doğru verilerle gösterilir | Integration test: GeneratePactOptions → 3 kart, doğru boon/bane text |
| 2 | Hover'da kart büyür ve detay gösterilir | Visual test: mouse hover → scale up + tooltip |
| 3 | Seçim sonrası pakt uygulanır | Integration test: kart seç → Pact System'da pakt aktif |
| 4 | Seçim zorunlu — çıkış yok | Visual test: ESC → hiçbir şey olmaz |
| 5 | Animasyon akışı doğru sırada oynar | Visual test: fade → text → kartlar → seçim → flash → gameplay |
| 6 | Sınıf affinity badge doğru gösterilir | Visual test: Ölüm Şövalyesi + Kan Kalkanı → badge görünür |
| 7 | Keyboard navigasyonu çalışır | Visual test: ok tuşları ile kart değiştir, Enter ile seç |
| 8 | Gameplay seçim sonrası sorunsuz devam eder | Integration test: seçim → overlay fade → düşmanlar hareket ediyor |

## Open Questions

1. Pakt geçmişi (önceki run'larda seçilen paktlar) gösterilecek mi? (Önerilen: Meta Progression ile birlikte — Alpha.)
2. Kart çevirme animasyonu (arka yüz → ön yüz) olacak mı? (Önerilen: VS'de — ek polish.)
3. Seçim geri alma (5s içinde iptal) mekanığı? (Önerilen: Hayır — geri alınamazlık oyunun gerilim kaynağı.)
