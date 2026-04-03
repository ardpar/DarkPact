# Equipment System

> **Status**: Designed
> **Author**: User + Claude
> **Last Updated**: 2026-04-04
> **Implements Pillar**: Core Gameplay

## Overview

Equipment System, oyuncunun silah, zırh ve aksesuar yönetimini sağlayan sistemdir. Item Database'den item tanımlarını alır, oyuncunun üzerindeki ekipman slotlarını yönetir ve ekipmanların stat modifier'larını Player Controller ve Combat System'e iletir. Equip/unequip, stat karşılaştırma ve run boyunca ekipman değişimini kapsar.

## Player Fantasy

"Daha iyi bir kılıç buldum!" — Loot heyecanının somutlaştığı yer. Ekipman değiştirmek anında hissedilir: daha hızlı saldırı, daha fazla hasar, daha sağlam zırh. Legendary item bulup pakt sinerji bonusunu görmek "build'im tamamlanıyor" tatmini verir.

## Detailed Design

### Core Rules

1. **Ekipman slotları**: Weapon (1), Armor (1), Accessory (1), SpellGem (4). Run başında sınıfa göre başlangıç ekipmanı.
2. **Equip**: Item slota yerleştirilir → stat modifier'lar anında uygulanır. Eski item envantere döner veya yere düşer.
3. **Stat uygulama**: Tüm ekipman stat'ları toplanır, Player Controller ve Combat System'e iletilir.
4. **Run-scoped**: Ekipman run boyunca kalır. Run sonunda kaybolur (roguelite).
5. **Pakt kısıtı**: Açgözlülük Paktı shop'tan satın almayı engeller, loot'tan bulma serbest.

### Slot Yapısı

```
EquipmentSlots
├── weapon: ItemInstance (silah — damage, attackSpeed, range, weaponType)
├── armor: ItemInstance (zırh — defense, maxHPBonus, moveSpeedModifier)
├── accessory: ItemInstance (aksesuar — pasif bonuslar)
└── spellGems[4]: ItemInstance[] (büyü taşları — skill slotlarına bağlı)
```

```
ItemInstance
├── definition: ItemDefinition (SO referansı)
├── rarity: Rarity (drop anında belirlenen)
├── finalBaseStats: StatBlock (rarity çarpanı uygulanmış)
├── bonusStats: List<BonusStat> (random roll edilmiş)
└── isPactBonusActive: bool (Legendary + doğru pakt)
```

### States and Transitions

| State | Açıklama |
|-------|----------|
| **Empty** | Slot boş (run başı, slota göre) |
| **Equipped** | Item slotta, stat'lar aktif |
| **Swapping** | Yeni item ile eski yer değiştiriyor |

### Interactions with Other Systems

| Sistem | Yön | Arayüz |
|--------|-----|--------|
| **Item Database** | ← veri alır | ItemDefinition → stat tanımları |
| **Combat System** | → stat sağlar | weapon.damage, weapon.attackSpeed, weapon.range, weapon.weaponType |
| **Player Controller** | → stat sağlar | armor.moveSpeedModifier, armor.defense |
| **Health & Damage** | → stat sağlar | armor.maxHPBonus, armor.defense |
| **Pact System** | ← kontrol alır | Shop kısıtı, Legendary pakt bonus aktivasyonu |
| **Loot System** | ← item alır | Yerden item aldığında equip veya envantere |
| **Inventory/Equipment UI** | → veri sağlar | Slot durumları, stat karşılaştırma |

## Formulas

### Toplam Stat Hesabı

```
totalDamage = weapon.finalDamage + sum(weapon.bonusStats[damage]) + accessory.damageBonus
totalDefense = armor.finalDefense + sum(armor.bonusStats[defense]) + accessory.defenseBonus
totalMaxHP = baseMaxHP + armor.maxHPBonus + accessory.hpBonus
totalMoveSpeed = baseMoveSpeed × armor.moveSpeedModifier × accessory.speedModifier
```

### Legendary Pakt Bonus

```
if (item.rarity == Legendary AND item.pactAffinity == activePact):
    finalStats = baseStats × LegendaryPactBonusMultiplier (1.3)
```

## Edge Cases

| Durum | Ne olur |
|-------|---------|
| **Zırh çıkarılırsa MaxHP düşer** | HP oranı korunur (Health & Damage GDD kuralı). MaxHP 120→100, HP 90 → 75. |
| **SpellGem slotu boşsa skill kullanılırsa** | Skill tetiklenmez. HUD'da boş slot gösterilir. |
| **Aynı item tekrar equip edilirse** | İşlem yok sayılır. |
| **Envanter dolu, yeni item bulunursa** | MVP'de envanter yok — sadece slot'lar. Yeni item ya equip edilir ya yerde bırakılır. |
| **Run ortasında pakt değişirse (olmaz ama)** | Legendary bonus anında güncellenir. Pakt kaldırılırsa bonus kaybolur. |

## Dependencies

**Upstream:**

| Sistem | Bağımlılık Tipi | Arayüz |
|--------|----------------|--------|
| Item Database | Hard | Item tanımları |
| Combat System | Hard | Silah stat'larını kullanır |

**Downstream:**

| Sistem | Bağımlılık Tipi | Arayüz |
|--------|----------------|--------|
| Loot System | Hard | Bulunan item'ları equip etme |
| Inventory/Equipment UI | Hard | Slot gösterimi |

## Tuning Knobs

| Knob | Tip | Varsayılan | Güvenli Aralık | Etki |
|------|-----|-----------|----------------|------|
| `StartingWeapon[DeathKnight]` | ItemDefinition | Iron Sword: damage=12, attackSpeed=3.3, range=1.5, type=Kılıç, rarity=Common | — | Ölüm Şövalyesi başlangıç silahı. |
| `StartingWeapon[Witch]` | ItemDefinition | Apprentice Staff: damage=8, attackSpeed=1.67, range=5.0, type=Asa, rarity=Common | — | Cadı başlangıç silahı. |
| `StartingArmor[DeathKnight]` | ItemDefinition | Iron Chainmail: defense=5, maxHPBonus=10, moveSpeedModifier=0.9, rarity=Common | — | Ölüm Şövalyesi başlangıç zırhı. |
| `StartingArmor[Witch]` | ItemDefinition | Cloth Robe: defense=2, maxHPBonus=0, moveSpeedModifier=1.0, rarity=Common | — | Cadı başlangıç zırhı. |
| `LegendaryPactBonusMultiplier` | float | 1.3 | 1.1–2.0 | Pakt uyumu çarpanı. |

## Visual/Audio Requirements

- Equip SFX: Metal giyinme sesi (zırh), kılıç çekme sesi (silah), kristal sesi (SpellGem)
- Stat artışı: Yeşil sayı pop-up, stat düşüşü: kırmızı sayı pop-up
- Legendary item equip: Altın parlama efekti

## UI Requirements

- **Equipment Panel**: Slot ikonları, mevcut stat'lar, karşılaştırma overlay
- **Quick Equip**: Loot pick-up anında "Equip / Bırak" seçimi
- **Stat Comparison**: Yeni vs mevcut, yeşil=artış, kırmızı=azalış

## Acceptance Criteria

| # | Kriter | Doğrulama Yöntemi |
|---|--------|-------------------|
| 1 | Equip stat'ları anında uygulanır | Unit test: silah equip → Combat damage güncellenir |
| 2 | Unequip stat'ları geri alır | Unit test: zırh çıkar → defense düşer |
| 3 | Legendary pakt bonusu doğru paktla aktif | Unit test: Legendary + doğru pakt → ×1.3 |
| 4 | SpellGem equip → skill slotu aktif | Integration test: gem equip → skill kullanılabilir |
| 5 | Başlangıç ekipmanı sınıfa göre doğru | Unit test: Ölüm Şövalyesi → kılıç + ağır zırh |
| 6 | MaxHP değiştiğinde HP oranı korunur | Unit test: zırh çıkar, MaxHP düşer, HP oranı aynı |

## Open Questions

1. MVP'de envanter sistemi gerekli mi? (Önerilen: Hayır — sadece slotlar. Yerdeki item equip ya da bırak. VS'de basit envanter.)
2. Item satma/parçalama mekanığı? (Önerilen: MVP'de yok. Altın sadece düşmanlardan.)
3. Weapon swap hotkey? (Önerilen: MVP'de yok. Tek aktif silah. VS'de 2 silah slotu düşünülebilir.)
