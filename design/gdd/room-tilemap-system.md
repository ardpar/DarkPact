# Room/Tilemap System

> **Status**: Designed
> **Author**: User + Claude
> **Last Updated**: 2026-04-03
> **Implements Pillar**: Core Infrastructure

## Overview

Room/Tilemap System, Dark Pact'in dungeon yapısının görsel ve fiziksel temelini oluşturur. 16x16 piksel ELV Games tile'ları kullanarak oda şablonlarını Tilemap'lere render eder, duvar/zemin collider'larını yönetir ve odalar arası geçiş mantığını sağlar. Procedural Dungeon Generator bu sistemin üzerine inşa edilir — generator hangi odanın nereye yerleşeceğine karar verir, bu sistem o odayı fiziksel olarak var eder.

## Player Fantasy

Oyuncu her kapıdan geçtiğinde yeni bir odaya adım atar ve anında "burası farklı" hisseder. Oda temaları (Crypt, Graveyard, Hell Temple) tile palette'leri ile desteklenir. Duvarlar gerçek engel gibi hisseder, zemin yürünebilir alan olarak net ve okunabilirdir. Oyuncu haritayı okuyabilir — nereye gidebileceğini, nerenin tehlikeli olduğunu tile'lardan anlayabilir.

## Detailed Design

### Core Rules

1. **Tile boyutu**: 16x16 piksel, Unity Tilemap grid'i ile birebir eşleşir
2. **Tilemap katmanları** (her odada):
   - `Ground` — Zemin tile'ları (yürünebilir alan), Order in Layer: 0
   - `Walls` — Duvar tile'ları (collider'lı, geçilmez), Order in Layer: 1
   - `Decoration` — Dekoratif objeler (meşale, kemik, çatlak), Order in Layer: 2
   - `Overlay` — Üst katman efektleri (gölge, sis), Order in Layer: 3
3. **Oda şablonları**: Her oda bir prefab olarak tanımlıdır. Prefab içinde:
   - Tilemap verileri (ground, walls, decoration, overlay)
   - Spawn point'leri (düşman, hazine, event)
   - Kapı pozisyonları (kuzey, güney, doğu, batı — her yön opsiyonel)
   - Oda tipi etiketi: `Combat`, `Treasure`, `Event`, `Boss`, `Start`
4. **Oda boyutları**: Standart oda 16x16 tile (256x256 piksel). Boss odaları 24x24 veya 32x32 olabilir.
5. **Kapı sistemi**: Kapılar 2 tile genişliğinde açıklık. Oda temizlendiğinde kapılar açılır (collider kaldırılır). Temizlenmeden kapılar kilitli (collider aktif).
6. **Tileset paleti**: Akt'a göre değişir:
   - Akt 1: Crypt Tileset
   - Akt 2: Graveyard Tileset
   - Akt 3: Hell + Temple Tileset
7. **Collision**: Duvar tile'ları `TilemapCollider2D` + `CompositeCollider2D` kullanır (performans için ayrı collider yerine birleşik)

### States and Transitions

| State | Açıklama | Geçiş |
|-------|----------|-------|
| **Unloaded** | Oda henüz oluşturulmamış | → Loading (oyuncu bitişik kapıya yaklaştığında) |
| **Loading** | Oda prefab'ı instantiate ediliyor | → Active (yükleme tamamlandığında) |
| **Active** | Oda görünür ve oynanabilir | → Cleared (tüm düşmanlar öldürüldüğünde) |
| **Cleared** | Oda temizlendi, kapılar açık | → Active (oyuncu geri gelirse düşman yok, kapılar açık kalır) |
| **Unloaded** | Oda bellekten kaldırıldı | ← Active/Cleared (oyuncu 2+ oda uzaklaştığında) |

### Interactions with Other Systems

| Sistem | Yön | Arayüz |
|--------|-----|--------|
| **Procedural Dungeon Generator** | ← veri alır | Oda şablonu, pozisyon, kapı bağlantıları, akt tileset bilgisi |
| **Camera System** | → bildirir | Aktif oda sınırları (`Bounds`) — kamera bu sınırlar içinde kalır |
| **Enemy System** | → sağlar | Spawn point pozisyonları, oda temizlenme event'i (`OnRoomCleared`) |
| **Player Controller** | → bildirir | Oyuncu kapı trigger'ına girdiğinde Room/Tilemap `OnPlayerEnteredRoom(Room)` event'i fırlatır, Player Controller ve Camera System dinler |
| **Combat System** | → bildirir | Oda duvarları fiziksel engel olarak combat'ı etkiler (projectile duvardan geçmez) |

## Formulas

- **Oda bellekte tutulma mesafesi**: Aktif oda + bitişik odalar yüklü kalır. 2+ oda uzaklıktaki odalar unload edilir.
- **Standart oda boyutu**: 16 × 16 tile = 256 × 256 piksel = 16 × 16 × 0.16m = 2.56 × 2.56 Unity unit (PPU: 100)
- **Boss oda boyutu**: 24 × 24 tile = 384 × 384 piksel veya 32 × 32 tile = 512 × 512 piksel
- **Kapı genişliği**: 2 tile = 32 piksel

## Edge Cases

| Durum | Ne olur |
|-------|---------|
| **Oyuncu kapı eşiğinde oda geçişi sırasında hasar alırsa** | Geçiş sırasında 0.3s invincibility frame — oda geçişi güvenli |
| **Oda şablonunda kapı pozisyonu bitişik odayla eşleşmezse** | Procedural Generator bunu garanti eder. Eşleşme yoksa koridor tile'ları ile bağlanır |
| **Oyuncu çok hızlı odalar arası geçerse** | Oda loading async — bitişik odalar pre-load edildiği için gecikme olmaz. 2+ oda atlama fiziksel olarak mümkün değil (kapılar arası mesafe yeterli) |
| **Boss odası standart kapı boyutuna sığmazsa** | Boss odaları özel kapı genişliği kullanabilir (4 tile). Şablon tanımında belirtilir |
| **Tilemap collider boşluk bırakırsa** | CompositeCollider2D ile tile collider'lar birleştirilir — boşluk kalmaz |
| **Oyuncu temizlenmemiş odadan geri dönerse** | Düşmanlar pozisyonlarını korur, oda state'i Active kalır |

## Dependencies

**Upstream:** Yok — Foundation layer.

**Downstream:**

| Sistem | Bağımlılık Tipi | Arayüz |
|--------|----------------|--------|
| Camera System | Hard | Oda sınırları (`GetActiveRoomBounds()`) |
| Enemy System | Hard | Spawn point'ler, `OnRoomCleared` event |
| Procedural Dungeon Generator | Hard | Oda prefab instantiation API, kapı bağlantı sistemi |
| Combat System | Soft | Duvar collider'lar projectile engellemesi (Physics2D üzerinden, doğrudan bağımlılık yok) |

## Tuning Knobs

| Knob | Tip | Varsayılan | Güvenli Aralık | Etki |
|------|-----|-----------|----------------|------|
| `StandardRoomSize` | Vector2Int | (16, 16) | (12,12)–(20,20) | Standart oda tile boyutu. Çok küçük → combat alanı dar. Çok büyük → boş hisseder. |
| `BossRoomSize` | Vector2Int | (24, 24) | (20,20)–(32,32) | Boss odası tile boyutu. Boss'un hareket alanı ve mekaniklerine bağlı. |
| `DoorWidth` | int | 2 | 2–4 | Kapı genişliği (tile). 2 = dar geçiş (gerilim). 4 = rahat geçiş. |
| `RoomTransitionInvincibility` | float (saniye) | 0.3 | 0.1–0.5 | Oda geçişinde invincibility süresi. 0 → unfair hit. Çok yüksek → exploit. |
| `RoomUnloadDistance` | int | 2 | 1–3 | Kaç oda uzaklıkta unload edilir. 1 → agresif bellek tasarrufu, pop-in riski. 3 → daha fazla RAM kullanır. |

## Visual/Audio Requirements

- Akt'a göre tileset paleti değişir (Crypt → Graveyard → Hell/Temple)
- Kapı açılma animasyonu (2-3 frame sprite swap veya particle effect)
- Kapı açılma SFX'i (taş kayma / zincir kırılma sesi)
- Oda geçişinde kısa fade veya kamera pan (Camera System ile koordineli)
- Dekorasyon tile'ları oda atmosferini destekler (meşale flicker animasyonu, ambient particle)

## UI Requirements

- Minimap'te oda şekillerini ve kapı pozisyonlarını gösterme (HUD ile koordineli, VS priority)
- Aktif oda highlight, temizlenmiş odalar farklı renk, keşfedilmemiş odalar gizli

## Acceptance Criteria

| # | Kriter | Doğrulama Yöntemi |
|---|--------|-------------------|
| 1 | 16x16 tile'lık standart oda doğru render edilir, ELV tileset kullanılır | Visual test: oda prefab'ı sahneye koy, tile'lar 16x16 grid'e oturur |
| 2 | Duvar collider'lar geçilmez, zemin yürünebilir | Integration test: oyuncu duvara yürür → durur, zemine yürür → geçer |
| 3 | Kapılar oda temizlenmeden kilitli, temizlendikten sonra açık | Integration test: düşmanları öldür → kapı collider kaldırılır |
| 4 | Bitişik odalar pre-load edilir, 2+ uzaklıktaki odalar unload edilir | Memory test: 5 odalık dungeon, sadece 3 oda bellekte |
| 5 | Oda geçişinde invincibility frame çalışır | Unit test: geçiş sırasında damage → 0 hasar |
| 6 | CompositeCollider2D ile collider'lar birleşik, boşluk yok | Visual test: collider gizmo'sunda gap kontrolü |
| 7 | Boss odası farklı boyutta doğru render edilir | Visual test: 24x24 boss odası, kapılar doğru pozisyonda |
| 8 | Oda loading < 100ms (pre-load'lu bitişik oda) | Performance test: oda geçiş süresini ölç |

## Open Questions

1. Procedural odalar mı yoksa el yapımı oda şablonları mı? (Önerilen: El yapımı şablonlar, procedural yerleşim — daha kontrollü deneyim)
2. Oda içi destructible objeler (kırılabilir vazo, sandık) MVP'de mi? (Önerilen: MVP'de basit sandık, tam destructible VS'de)
3. Oda ziyaret geçmişi saklanacak mı? (Run içi minimap için gerekli — VS priority)
