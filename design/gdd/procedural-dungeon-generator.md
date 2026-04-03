# Procedural Dungeon Generator

> **Status**: Designed
> **Author**: User + Claude
> **Last Updated**: 2026-04-04
> **Implements Pillar**: Core Gameplay — Roguelite Replayability

## Overview

Procedural Dungeon Generator, her run'da benzersiz dungeon layout'ları üreten sistemdir. El yapımı oda şablonlarını (Room/Tilemap System prefab'ları) procedural olarak yerleştirir ve birbirine kapılarla bağlar. Run Manager'dan seed alır, belirleyici (deterministic) üretim yapar — aynı seed = aynı dungeon. Oda tipleri (combat, hazine, event, boss), zorluk dağılımı ve pacing bu sistemin kontrolündedir.

## Player Fantasy

Her run'da kapıdan geçtiğinde "arkada ne var?" merakı. Dungeon hiçbir zaman aynı hissetmez ama her zaman "fair" hisseder — çıkmaz sokak yok, imkansız oda dizilimi yok. Haritayı keşfetmek kendi başına ödüllendirici.

## Detailed Design

### Core Rules

1. **Seed-based**: Run Manager seed'i ile deterministic random. `System.Random(seed)` kullanılır.
2. **Şablon tabanlı**: Oda içerikleri el yapımı prefab, yerleşim procedural.
3. **Graph yapısı**: Dungeon bir graph — node'lar oda, edge'ler kapı bağlantıları.
4. **Algoritma**: Random Walk + branching. Ana yol (main path) start→boss arası garanti. Dallar opsiyonel ödüller.
5. **Oda tipi dağılımı**: Combat odaları çoğunluk, hazine/event odaları belirli oranlarda.
6. **Zorluk eğrisi**: Boss'a yaklaştıkça zorluk artar (Run Manager difficulty ile).

### Dungeon Layout Algoritması

```
1. Start odası yerleştir (merkez)
2. Main path oluştur (start → boss, RoomsPerAct uzunluğunda)
   - Her adımda random yön seç (kuzey/güney/doğu/batı)
   - Çakışma varsa yön değiştir
   - Kapı bağlantılarını oluştur
3. Branch noktaları ekle (main path'ten dallanma)
   - BranchChance ile her oda dallanma adayı
   - Branch uzunluğu: 1-3 oda
   - Branch sonları: hazine veya event odası
4. Oda tiplerini ata:
   - İlk oda: Start (düşman yok, güvenli)
   - Son oda: Boss
   - Main path: Combat odaları (zorluk artarak)
   - Branch sonları: Treasure (%60) veya Event (%40)
5. Her odaya uygun şablon (prefab) seç:
   - Kapı yönlerine uygun şablon filtrele
   - Oda tipine uygun şablon seç
   - Akt tileset'ine uygun olmalı
```

### Oda Tipleri

| Tip | Oran (main path) | İçerik |
|-----|-------------------|--------|
| **Start** | 1 (ilk oda) | Güvenli, düşman yok, başlangıç bilgisi |
| **Combat** | ~70% | Düşman spawn, oda temizlenince kapılar açılır |
| **Treasure** | Branch sonları (%60) | Garantili iyi loot, düşman yok veya az |
| **Event** | Branch sonları (%40) | Özel olay: NPC, tuzak, bonus seçimi (MVP'de basit) |
| **Boss** | 1 (son oda) | Boss savaşı, büyük oda |

### Graph Kısıtları

1. Start'tan boss'a her zaman yol var (main path garanti)
2. Dead-end sadece branch sonlarında (main path'te yok)
3. Odalar çakışmaz (grid-based pozisyonlama)
4. Her odanın en az 1 kapısı bağlı (orphan oda yok)
5. Main path uzunluğu = `RoomsPerAct` (tuning knob)
6. Toplam oda = main path + branch odaları

### States and Transitions

| State | Açıklama |
|-------|----------|
| **Idle** | Generator hazır, tetik bekliyor |
| **Generating** | Seed ile layout üretiliyor |
| **Generated** | Layout hazır, Room/Tilemap'e aktarılabilir |

### Interactions with Other Systems

| Sistem | Yön | Arayüz |
|--------|-----|--------|
| **Run Manager** | ← tetikler | `GenerateDungeon(seed, act, difficulty)` |
| **Room/Tilemap System** | → veri sağlar | Oda prefab'ları, pozisyonlar, kapı bağlantıları |
| **Enemy System** | → veri sağlar | Oda başına zorluk, spawn bütçesi |
| **Loot System** | → veri sağlar | Hazine odası bilgisi |

## Formulas

### Main Path Uzunluğu

```
mainPathLength = RoomsPerAct (tuning knob, default: 15)
```

### Branch Sayısı

```
branchCount = floor(mainPathLength × BranchFrequency)
branchLength = random(1, MaxBranchLength) // seed-based
```

| Değişken | Tanım | Varsayılan |
|----------|-------|-----------|
| `RoomsPerAct` | Akt başına main path oda sayısı | 15 |
| `BranchFrequency` | Main path oda başına dallanma şansı | 0.3 |
| `MaxBranchLength` | Maksimum dal uzunluğu (oda) | 3 |

### Zorluk Eğrisi

```
roomDifficulty = baseDifficulty + (roomIndex / mainPathLength) × DifficultyRange
```
Bu değer Run Manager'a iletilir → Enemy System'a aktarılır.

| Değişken | Tanım | Varsayılan | Aralık |
|----------|-------|-----------|--------|
| `baseDifficulty` | Akt başlangıç zorluğu | Akt1=1.0, Akt2=2.0, Akt3=3.0 | 0.5–5.0 |
| `DifficultyRange` | Akt içi zorluk artış aralığı | 1.0 | 0.5–3.0 |

**Örnek (Akt 1):** baseDifficulty=1.0, DifficultyRange=1.0, mainPathLength=15
- Room 0: 1.0 + (0/15) × 1.0 = 1.0
- Room 7: 1.0 + (7/15) × 1.0 = 1.47
- Room 14: 1.0 + (14/15) × 1.0 = 1.93

### Toplam Oda Sayısı (Tahmini)

```
totalRooms ≈ mainPathLength + (mainPathLength × BranchFrequency × avgBranchLength)
```
Default: 15 + (15 × 0.3 × 2) ≈ 24 oda

## Edge Cases

| Durum | Ne olur |
|-------|---------|
| **Random walk kendi üstüne dönerse** | Yön değiştir, 4 yön de doluysa 1 adım geri git ve farklı yön dene |
| **Uygun şablon bulunamazsa** | Fallback: generic şablon (tüm kapı yönlerine sahip). Log uyarısı. |
| **Branch main path'e bağlanırsa (loop)** | İzin verilmez — branch sadece main path'ten dışarı uzar |
| **Çok fazla branch üretilirse** | MaxTotalRooms limiti — limit aşılırsa branch ekleme durur |
| **Boss odası büyük ama grid'e sığmıyorsa** | Boss odası her zaman main path sonuna 2x2 alan olarak reserve edilir |

## Dependencies

**Upstream:**

| Sistem | Bağımlılık Tipi | Arayüz |
|--------|----------------|--------|
| Room/Tilemap System | Hard | Oda prefab kataloğu, tile boyutu |
| Enemy System | Soft | Oda başına spawn bütçesi bilgisi |

**Downstream:**

| Sistem | Bağımlılık Tipi | Arayüz |
|--------|----------------|--------|
| Boss System | Hard | Boss odası pozisyonu |

## Tuning Knobs

| Knob | Tip | Varsayılan | Güvenli Aralık | Etki |
|------|-----|-----------|----------------|------|
| `RoomsPerAct` | int | 15 | 10–25 | Main path uzunluğu. Run süresini doğrudan etkiler. |
| `BranchFrequency` | float | 0.3 | 0.1–0.5 | Dallanma sıklığı. Düşük → lineer. Yüksek → labirent. |
| `MaxBranchLength` | int | 3 | 1–5 | Dal uzunluğu. Uzun dallar → keşif ödülleri. |
| `MaxTotalRooms` | int | 30 | 20–50 | Toplam oda limiti. Bellek ve pacing kontrolü. |
| `TreasureRoomChance` | float | 0.6 | 0.3–0.8 | Branch sonunda hazine olma şansı (vs event). |

## Visual/Audio Requirements

- Dungeon generation görünmez — oyuncu sadece sonucu deneyimler
- Minimap oluşturulan layout'u gösterir (VS priority)
- Her akt farklı tileset kullanır (Room/Tilemap ile koordineli)

## UI Requirements

- **Minimap** (VS priority): Keşfedilen odalar, mevcut oda highlight, kapı bağlantıları
- Generation sırasında loading screen (Room/Tilemap pre-load ile koordineli)

## Acceptance Criteria

| # | Kriter | Doğrulama Yöntemi |
|---|--------|-------------------|
| 1 | Aynı seed aynı layout üretir | Unit test: seed X → 2 çağrı → identical graph |
| 2 | Start'tan boss'a her zaman yol var | Unit test: 100 random seed → pathfinding hepsinde başarılı |
| 3 | Oda çakışması yok | Unit test: tüm oda pozisyonları unique |
| 4 | Orphan oda yok | Unit test: her oda en az 1 bağlantılı |
| 5 | Oda tipleri doğru dağılımda | Unit test: 100 seed → treasure/event oranı ≈ 60/40 |
| 6 | MaxTotalRooms aşılmaz | Unit test: toplam oda ≤ MaxTotalRooms |
| 7 | Boss odası her zaman son oda | Unit test: graph'ın leaf node'u boss tipi |
| 8 | Generation < 100ms | Performance test: 100 seed'lik batch < 10s |

## Open Questions

1. Oda şablonu kaç tane gerekli? (Önerilen: MVP'de akt başına 10-15 combat + 3 treasure + 2 event + 1 boss = ~20 şablon)
2. Backtracking (önceki odalara dönme) serbest mi? (Önerilen: Evet — temizlenmiş odalara dönülebilir, düşman yok.)
3. Secret room mekanığı olacak mı? (Önerilen: VS'de — gizli duvar, bomba ile açılır, rare loot.)
