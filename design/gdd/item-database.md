# Item Database

> **Status**: Designed
> **Author**: User + Claude
> **Last Updated**: 2026-04-04
> **Implements Pillar**: Core Infrastructure

## Overview

Item Database, Dark Pact'teki tüm item'ların (silahlar, zırhlar, büyü ekipmanları, tüketilebilirler) veri tanımlarını barındıran ScriptableObject tabanlı merkezi veri katmanıdır. Her item bir SO olarak tanımlanır: stat'lar, rarity, ikon, açıklama, pakt sinerjisi bilgisi burada yaşar. Equipment System item'ları oyuncuya giydirirken, Loot System düşürmek için item seçerken bu veritabanını sorgular. Runtime'da item oluşturmaz — tüm item'lar tasarım zamanında tanımlıdır.

## Player Fantasy

Oyuncu yere düşen bir item gördüğünde "bu ne, güçlü mü?" merak eder. Item'ın adı, rarity rengi ve stat'ları bir bakışta okunabilir. Altın (Paktlı) rarity item'lar "paktımla sinerji yapıyor" heyecanı yaratır. Item çeşitliliği her run'da farklı build denemeleri teşvik eder.

## Detailed Design

### Core Rules

1. **ScriptableObject tabanlı**: Her item bir `ItemDefinition` SO. Yeni item eklemek = yeni SO oluşturmak, kod değişikliği gerektirmez.
2. **Item kategorileri**:
   - `Weapon` — Kılıç, balta, mızrak, asa (saldırı stat'ları, saldırı hızı, menzil)
   - `Armor` — Gövde zırhı (savunma, HP bonus, hareket hızı modifier)
   - `Accessory` — Yüzük, kolye (pasif bonus stat'lar)
   - `Consumable` — HP iksiri, buff iksiri (tek kullanımlık efekt)
   - `SpellGem` — Skill slotlarına takılan büyü taşları (ateş, buz, zehir, şimşek, karanlık)
3. **Rarity sistemi**: 5 kademe, her kademe stat çarpanı uygular
4. **Pakt affinity**: Altın rarity item'lar belirli bir pakta bağlı bonus taşır
5. **Item ID**: Her item benzersiz string ID (ör: `weapon_sword_iron`, `armor_leather_basic`)
6. **Tag sistemi**: Item'lar tag'lerle etiketlenir (ör: `melee`, `fire`, `heavy`) — Loot System filtreleme için kullanır

### Item Rarity

| Rarity | Renk | Stat Çarpanı | Drop Weight | Özellik |
|--------|------|-------------|-------------|---------|
| Common (Gri) | #808080 | ×1.0 | 50% | Temel stat'lar |
| Uncommon (Yeşil) | #00CC00 | ×1.2 | 25% | +1 bonus stat |
| Rare (Mavi) | #0066FF | ×1.5 | 15% | +2 bonus stat |
| Epic (Mor) | #9900CC | ×2.0 | 8% | +3 bonus stat + özel efekt |
| Legendary (Altın) | #FFD700 | ×2.5 | 2% | +3 bonus stat + pakt sinerji bonusu |

### ItemDefinition ScriptableObject Yapısı

```
ItemDefinition (ScriptableObject)
├── itemId: string (benzersiz)
├── itemName: string (görünen ad)
├── description: string (açıklama metni)
├── category: ItemCategory enum
├── icon: Sprite (ELV pack ikonu)
├── baseRarity: Rarity enum (minimum drop rarity)
├── tags: List<string>
├── baseStats: StatBlock
│   ├── damage: int (sadece weapon)
│   ├── attackSpeed: float (sadece weapon)
│   ├── range: float (sadece weapon)
│   ├── defense: int (sadece armor)
│   ├── maxHPBonus: int
│   ├── moveSpeedModifier: float
│   └── customStats: Dictionary<string, float>
├── bonusStatPool: List<BonusStat> (rarity'ye göre random seçilir)
├── pactAffinity: PactType enum (sadece Legendary)
├── pactBonus: StatBlock (Legendary + doğru pakt aktifken uygulanır)
├── weaponType: WeaponType enum (kılıç, balta, mızrak, asa)
├── damageType: DamageType enum (Physical, Fire, Ice, Poison, Lightning, Dark)
└── spellEffect: SpellDefinition ref (sadece SpellGem)
```

### States and Transitions

Item Database statik veri katmanı — state'i yok. Boot'ta yüklenir, runtime boyunca readonly kalır.

| Durum | Açıklama |
|-------|----------|
| **Unloaded** | Uygulama başlamadan önce |
| **Loading** | Boot state'inde SO'lar yükleniyor |
| **Ready** | Tüm SO'lar bellekte, sorgulanabilir |

### Interactions with Other Systems

| Sistem | Yön | Arayüz |
|--------|-----|--------|
| **Game Manager** | ← tetikler | Boot state'inde yükleme başlar, Ready olunca bildirir |
| **Equipment System** | → sorgular | `GetItem(itemId)` → ItemDefinition döner |
| **Loot System** | → sorgular | `GetItemsByFilter(category, tags, minRarity)` → filtrelenmiş liste |
| **Pact System** | → sorgular | `GetLegendaryItemsForPact(pactType)` → pakt sinerji item'ları |
| **HUD / Inventory UI** | → sorgular | Item icon, name, description, stat bilgileri |

## Formulas

### Stat Scaling (Rarity'ye göre)

```
finalBaseStat = baseStatValue × rarityMultiplier
bonusStatCount = rarityTier - 1  (Common=0, Uncommon=1, Rare=2, Epic=3, Legendary=3)
```

| Rarity | rarityMultiplier | bonusStatCount |
|--------|-----------------|----------------|
| Common | 1.0 | 0 |
| Uncommon | 1.2 | 1 |
| Rare | 1.5 | 2 |
| Epic | 2.0 | 3 |
| Legendary | 2.5 | 3 + pakt bonus |

**Örnek:** Iron Sword (baseDamage=15), Rare drop → finalDamage = 15 × 1.5 = 22 (floor) + 2 random bonus stat

### Bonus Stat Seçimi

```
bonusStats = random.sample(item.bonusStatPool, bonusStatCount)
each bonusStat.value = random(bonusStat.minValue, bonusStat.maxValue)
```

## Edge Cases

| Durum | Ne olur |
|-------|---------|
| **Aynı item ID'li iki SO varsa** | Boot'ta validasyon hatası loglanır, ikincisi yok sayılır |
| **Item'ın icon'u null ise** | Placeholder "missing" ikonu gösterilir |
| **Legendary item aktif pakta uymuyorsa** | Pakt bonusu uygulanmaz, item Epic gibi davranır (base stat'lar + 3 bonus) |
| **Item stat'ı negatif hesaplanırsa** | Minimum 0 garanti (defense, maxHP gibi stat'lar negatif olamaz). Damage minimum 1. |
| **Bonus stat pool'da yeterli stat yoksa** | Mevcut kadar stat verilir, eksik slotlar boş kalır, uyarı loglanır |
| **Runtime'da yeni item eklenmek istenirse** | Desteklenmez — tüm item'lar tasarım zamanında tanımlı. Modding desteği Full Vision'da değerlendirilir. |

## Dependencies

**Upstream:**

| Sistem | Bağımlılık Tipi | Arayüz |
|--------|----------------|--------|
| Game Manager | Hard | Boot sırasında initialization tetiklemesi |

**Downstream:**

| Sistem | Bağımlılık Tipi | Arayüz |
|--------|----------------|--------|
| Equipment System | Hard | `GetItem()` API |
| Loot System | Hard | `GetItemsByFilter()` API |
| Pact System | Soft | `GetLegendaryItemsForPact()` API |
| Inventory/Equipment UI | Soft | Item display bilgileri |

## Tuning Knobs

| Knob | Tip | Varsayılan | Güvenli Aralık | Etki |
|------|-----|-----------|----------------|------|
| `RarityMultipliers` | float[5] | [1.0, 1.2, 1.5, 2.0, 2.5] | her biri 0.5–5.0 | Rarity başına stat çarpanı. Çok yüksek fark → Common anlamsız. Çok düşük fark → Legendary heyecansız. |
| `BonusStatRanges` | per-stat min/max | stat'a göre değişir | — | Her bonus stat'ın min-max değerleri. Geniş aralık → daha fazla RNG. Dar aralık → öngörülebilir. |
| `LegendaryPactBonusMultiplier` | float | 1.3 | 1.1–2.0 | Doğru pakt aktifken Legendary'nin ek çarpanı. |

## Visual/Audio Requirements

- Item rarity renkleri: Gri, Yeşil, Mavi, Mor, Altın — isim metni ve ikon çerçevesi bu renkte
- ELV pack ikonları doğrudan kullanılır (32x32 silah/zırh/büyü ikonları)
- Legendary item'lar ikon etrafında parlama (glow) efekti
- Item pickup SFX: Rarity'ye göre farklı ses (Common → basit tık, Legendary → dramatik chime)

## UI Requirements

- Item tooltip: İsim (rarity renginde), kategori, base stat'lar, bonus stat'lar (yeşil renkte), pakt bonusu (altın renkte), açıklama
- Item karşılaştırma: Mevcut ekipman vs yeni item, stat farkları yeşil/kırmızı renkte
- Item Database'in kendisi UI gerektirmez — tüm görsel Inventory/Equipment UI'a ait

## Acceptance Criteria

| # | Kriter | Doğrulama Yöntemi |
|---|--------|-------------------|
| 1 | Boot'ta tüm SO'lar yüklenir, Ready state'e geçilir | Integration test: Boot → tüm item'lar sorgulanabilir |
| 2 | `GetItem(id)` doğru SO'yu döner | Unit test: bilinen ID → beklenen ItemDefinition |
| 3 | `GetItemsByFilter` doğru filtreleme yapar | Unit test: category=Weapon, minRarity=Rare → sadece Rare+ silahlar |
| 4 | Rarity stat çarpanı doğru uygulanır | Unit test: baseDamage=10, Rare → 15 |
| 5 | Bonus stat sayısı rarity'ye göre doğru | Unit test: Epic → 3 bonus stat |
| 6 | Duplicate ID boot'ta tespit edilir | Unit test: aynı ID'li 2 SO → hata log |
| 7 | Legendary pakt bonusu sadece doğru paktta aktif | Unit test: Katliam paktı item + Katliam aktif → bonus var, farklı pakt → bonus yok |
| 8 | Null icon durumunda placeholder gösterilir | Unit test: icon=null → placeholder sprite döner |

## Open Questions

1. Item sayısı MVP'de kaç olmalı? (Önerilen: Silah 5, Zırh 3, Accessory 2, Consumable 2, SpellGem 5 = ~17 base item × rarity)
2. Set bonus sistemi (aynı setten 3 parça = ekstra bonus) olacak mı? (Önerilen: MVP'de yok, VS'de değerlendirilir)
3. Silah tipi saldırı animasyonunu belirleyecek mi? (Önerilen: Evet — kılıç=yay, balta=overhead, mızrak=thrust, asa=cast)
