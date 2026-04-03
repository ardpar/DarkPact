# Pact System

> **Status**: Designed
> **Author**: User + Claude
> **Last Updated**: 2026-04-04
> **Implements Pillar**: Core Identity — Oyunun Özgün Mekaniği

## Overview

Pact System, Dark Pact'in oyunu diğer roguelite'lardan ayıran çekirdek mekaniğidir. Her run başında ve milestone'larda oyuncuya 3 rastgele pakt teklif edilir; her pakt güçlü bir Boon (nimet) ve anlamlı bir Bane (bela) taşır. Paktlar ScriptableObject tabanlıdır ve runtime'da stat modifier'lar, status effect'ler ve özel davranış kuralları olarak uygulanır. Pakt etkileşimleri (synergy) run'ı benzersiz kılar. Oyunun mantrası: "Şeytanla anlaş, bedelini öde."

## Player Fantasy

"Bu paktı alırsam çok güçlenirim ama... o bane çok acıtır. Risk alayım mı?" — Her pakt kararı gerilim yaratır. İkinci pakt birincisiyle sinerji yaratınca "deha gibi hissediyorum" anı oluşur. Bane'ler oyuncuyu farklı oynamaya zorlar — standart oyun stili yoktur, her run benzersizdir. Paktlar "güç ile kontrol arasındaki gerilimi" somutlaştırır.

## Detailed Design

### Core Rules

1. **Pakt teklifi**: Run Manager milestone'ında `GeneratePactOptions(count)` çağrılır → 3 rastgele, henüz seçilmemiş pakt döner
2. **Pakt seçimi**: Oyuncu 1 pakt seçer → Boon ve Bane anında uygulanır. Geri alınamaz.
3. **Boon uygulama**: Stat modifier, status effect veya özel kural olarak uygulanır
4. **Bane uygulama**: Stat modifier, status effect, kısıt veya özel kural olarak uygulanır
5. **Çoklu pakt**: Run boyunca birden fazla pakt birikmeli. Tüm boon ve bane'ler aynı anda aktif.
6. **Pakt havuzu**: Tüm paktlar havuzda tanımlı. Seçilmiş paktlar havuzdan çıkar (tekrar teklif edilmez).
7. **Maksimum pakt**: Run başına max pakt sayısı = milestone sayısı + 1 (başlangıç)

### PactDefinition ScriptableObject

```
PactDefinition (ScriptableObject)
├── pactId: string
├── pactName: string
├── description: string
├── icon: Sprite
├── pactTier: PactTier enum (Common, Rare, Mythic)
├── boon: PactEffect
│   ├── statModifiers: Dictionary<StatType, float>
│   ├── statusEffects: List<StatusEffectDefinition>
│   ├── specialRules: List<PactRule> (custom behavior)
│   └── description: string
├── bane: PactEffect
│   ├── statModifiers: Dictionary<StatType, float>
│   ├── statusEffects: List<StatusEffectDefinition>
│   ├── specialRules: List<PactRule>
│   └── description: string
├── synergyPartners: List<PactSynergy>
│   ├── partnerPactId: string
│   ├── synergyEffect: PactEffect
│   └── description: string
├── classAffinity: ClassType enum (None, DeathKnight, Witch)
└── affinityBonus: float (sınıf uyumunda boon çarpanı, ör: 1.2 = %20 boost)
```

### MVP Paktlar (5 adet)

| Pakt | Boon | Bane | Uygulama |
|------|------|------|----------|
| **Katliam Paktı** | +%60 hasar (DamageMultiplier=1.6) | Düşmanlar 1 kez dirilir (Enemy System respawn flag) | Stat modifier + Enemy System special rule |
| **Kan Kalkanı** | Her öldürme = +5 geçici can (TemporaryHP) | Can iksiri kullanılamaz (CanHeal=false) | Kill event → Health & Damage TempHP + heal restriction |
| **Gölge Adımı** | Dash sınırsız, cooldown yok (DashCooldown=0) | Durduğunda yavaş yavaş hasar (SelfDamage status effect) | Stat modifier + conditional status effect |
| **Lanetli Dokunuş** | Saldırılar zehirler (saldırıya Poison efekt eklenir) | Kendi saldırıların seni hafifçe etkiler (SelfDamage on attack) | Combat System on-hit effect + self-damage rule |
| **Açgözlülük Paktı** | Altın %200 fazla düşer (GoldMultiplier=3.0) | Ekipman satın alınamaz (CanBuyEquipment=false) | Loot System gold modifier + shop restriction |

### PactRule Özel Davranışlar

Bazı boon/bane'ler basit stat modifier değil, özel kurallar gerektirir:

| Rule | Sistem | Davranış |
|------|--------|----------|
| `EnemyRespawn` | Enemy System | `canRespawn=true` tüm düşmanlara uygulanır |
| `HealRestriction` | Health & Damage | `CanHeal()` false döner |
| `UnlimitedDash` | Player Controller | DashCooldown override = 0 |
| `StationaryDamage` | Status Effect | Durma > 1s → DoT başlar (bkz. Status Effect GDD) |
| `OnHitPoison` | Combat System | Her saldırı hit'inde hedefe Poison efekti |
| `SelfDamageOnAttack` | Combat System | Her saldırıda oyuncuya `SelfDamagePerAttack` hasar |
| `GoldMultiplier` | Loot System | Altın drop miktarı çarpanı |
| `ShopRestriction` | Equipment/Shop | Satın alma devre dışı |
| `TempHPOnKill` | Health & Damage | Düşman ölüm event'inde TemporaryHP kazanım |

### States and Transitions

Pact System run-scoped — run başında boş, run boyunca paktlar birikir.

| State | Açıklama |
|-------|----------|
| **Empty** | Run başı, pakt yok |
| **Offering** | 3 pakt teklif ediliyor (PactSelection state) |
| **Active** | Seçilen paktlar uygulanmış, boon+bane aktif |

### Interactions with Other Systems

| Sistem | Yön | Arayüz |
|--------|-----|--------|
| **Run Manager** | ← tetikler | Milestone → `GeneratePactOptions(3)`, run start → ilk pakt teklifi |
| **Health & Damage** | → modifier uygular | DamageMultiplier, CanHeal restriction, TempHP on kill |
| **Player Controller** | → modifier uygular | DashCooldown override, MoveSpeed modifier |
| **Combat System** | → modifier uygular | DamageMultiplier, on-hit effects (Poison), SelfDamageOnAttack |
| **Status Effect System** | → efekt uygular | Sürekli status effect'ler (StationaryDamage, SelfDamage) |
| **Enemy System** | → kural uygular | EnemyRespawn flag |
| **Loot System** | → modifier uygular | GoldMultiplier |
| **Item Database** | ← sorgular | Legendary item pakt affinity kontrolü |
| **Pact Selection UI** | → veri sağlar | Pakt listesi, boon/bane açıklamaları, sinerji bilgisi |
| **HUD** | → bildirir | Aktif pakt ikonları |
| **Class System** | ← affinity alır | Sınıf-pakt uyumu → boon çarpanı |

## Formulas

### Pakt Boon Çarpanı (Sınıf Affinitesi ile)

```
effectiveBoonMultiplier = boon.statModifier × classAffinityBonus
classAffinityBonus = pact.classAffinity == activeClass ? pact.affinityBonus : 1.0
```

**Örnek:** Kan Kalkanı boon (+5 TempHP/kill), Ölüm Şövalyesi affinity bonus=1.2 → +6 TempHP/kill

### Pakt Teklif Ağırlıkları

```
pactPool = allPacts.filter(p => !selectedPacts.contains(p))
weights = pactPool.map(p => p.pactTier == Common ? 50 : p.pactTier == Rare ? 35 : 15)
offered = weightedRandomSample(pactPool, weights, count=3)
```

| Tier | Ağırlık | Açıklama |
|------|---------|----------|
| Common | 50 | Sık teklif, dengeli boon/bane |
| Rare | 35 | Nadir, güçlü boon, ağır bane |
| Mythic | 15 | Çok nadir, oyun değiştirici — MVP'de yok |

### Kümülatif Etki

```
totalDamageMultiplier = product(all active pact damageMultipliers)
```

**Örnek:** Katliam (×1.6) + Lanetli Dokunuş (×1.0, hasar bonus yok ama zehir ekler) → toplam hasar ×1.6 + zehir DoT

## Edge Cases

| Durum | Ne olur |
|-------|---------|
| **Havuzda 3'ten az pakt kaldıysa** | Kalan kadar teklif edilir (1 veya 2). 0 kaldıysa milestone atlanır. |
| **Kan Kalkanı + Lanetli Dokunuş (SelfDamage + NoHeal)** | Tehlikeli combo — oyuncu sadece TempHP'den yaşar. Sinerjik: kill → TempHP, saldırı → zehir+self-damage. Dengelemek zor ama bu roguelite'ın ruhu. |
| **Gölge Adımı + Yavaşlama debuff** | Oyuncu yavaşlarsa ama hareket ederse durma hasarı tetiklenmez. Tam yavaşlama (stun) = durma hasarı başlar. |
| **Katliam Paktı + AoE saldırı** | Dirilen düşmanları AoE ile tekrar öldürmek kolay → combat döngüsü değişir ama XP/loot normal (dirilen düşman tekrar loot vermez). |
| **Açgözlülük + Legendary item** | Satın alamazsın ama drop'tan bulabilirsin. Bane sadece shop'u kısıtlar. |
| **Oyuncu hiç pakt seçmek istemezse** | Pakt seçimi zorunlu — ekranda "Seç" butonu dışında çıkış yok. Bu oyunun core mechanic'i. |
| **Aynı stat'ı etkileyen birden fazla pakt** | Çarpımsal: DamageMult pakt1 × DamageMult pakt2. Toplamsal değil. |

## Dependencies

**Upstream:**

| Sistem | Bağımlılık Tipi | Arayüz |
|--------|----------------|--------|
| Run Manager | Hard | Milestone tetikleme, run lifecycle |
| Health & Damage | Hard | Bane/boon stat modifier uygulama |
| Status Effect System | Hard | Sürekli status effect'ler |

**Downstream:**

| Sistem | Bağımlılık Tipi | Arayüz |
|--------|----------------|--------|
| Synergy Calculator | Hard | Pakt kombinasyonlarını değerlendirir (VS priority) |
| Pact Selection UI | Hard | Pakt teklif ekranı veri kaynağı |
| Skill Tree | Soft | Pakt Güçlendirme dalı pakt seçimine bağlı |
| Item Database | Soft | Legendary pakt affinity |

## Tuning Knobs

| Knob | Tip | Varsayılan | Güvenli Aralık | Etki |
|------|-----|-----------|----------------|------|
| `KatliamDamageMultiplier` | float | 1.6 | 1.3–2.0 | Katliam boon gücü. Düşük → zayıf pakt. Yüksek → OP. |
| `KanKalkaniTempHPPerKill` | int | 5 | 3–10 | Öldürmede kazanılan TempHP. |
| `GolgeAdimiStationaryDamage` | float/s | 5.0 | 2.0–10.0 | Durma hasarı. Oyuncuyu hareket etmeye zorlamalı ama fair olmalı. |
| `LanetliDokunusSelfDamagePerAttack` | float | 2.0 | 1.0–5.0 | Her saldırıda self-damage. Çok yüksek → paktı kimse almaz. |
| `AcgozlulukGoldMultiplier` | float | 3.0 | 2.0–5.0 | Altın çarpanı. |
| `ClassAffinityBonus` | float | 1.2 | 1.1–1.5 | Sınıf uyum bonusu. |
| `PactOptionsCount` | int | 3 | 2–5 | Teklif edilen pakt sayısı (Run Manager'da da tanımlı). |

## Visual/Audio Requirements

- Her paktın kendine özel renk paleti: Katliam=kırmızı, Kan Kalkanı=koyu kırmızı, Gölge Adımı=mor, Lanetli Dokunuş=yeşil, Açgözlülük=altın
- Pakt seçim anı: Dramatik ses + ekran flash (pakt rengiyle)
- Aktif pakt auraları oyuncu üzerinde (VFX System looping efektler)
- Pakt seçim ekranı: Karanlık atmosfer, pakt kartları parlayan ikonlarla
- Bane aktifken: Düşük frekanslı ambient ses (gerilim hissi)

## UI Requirements

- **Pact Selection UI**: 3 kart yan yana, her kartta pakt ikonu, ad, boon açıklaması (yeşil), bane açıklaması (kırmızı), sınıf affinity göstergesi
- **HUD Pact Bar**: Aktif paktlar küçük ikonlarla, hover → tooltip
- **Pact Detail Panel**: Aktif paktın detaylı açıklaması, aktif stat modifier'lar

## Acceptance Criteria

| # | Kriter | Doğrulama Yöntemi |
|---|--------|-------------------|
| 1 | 3 rastgele pakt teklif edilir, seçilmiş paktlar hariç | Unit test: 2 pakt seçili → 3 farklı, seçilmemiş pakt teklif |
| 2 | Katliam Paktı hasar ×1.6 uygular | Unit test: baseDamage=10 → effectiveDamage=16 |
| 3 | Kan Kalkanı kill'de TempHP verir ve heal'i engeller | Integration test: düşman öldür → +5 TempHP, heal → 0 |
| 4 | Gölge Adımı dash cooldown=0 yapar ve durma hasarı verir | Integration test: dash spam çalışır, 1s dur → hasar |
| 5 | Lanetli Dokunuş saldırılara zehir ekler ve self-damage yapar | Integration test: vur → düşmanda poison, oyuncuda -2 HP |
| 6 | Açgözlülük altını 3× artırır ve shop'u kilitler | Integration test: gold drop ×3, shop "satın alınamaz" |
| 7 | Sınıf affinity boon'u güçlendirir | Unit test: Ölüm Şövalyesi + Kan Kalkanı → TempHP=6 |
| 8 | Çoklu pakt modifier'ları çarpımsal çalışır | Unit test: Katliam(×1.6) + başka hasar paktı → çarpım |

## Open Questions

1. Pakt reddetme mekanığı olacak mı? (Önerilen: Hayır — zorunlu seçim oyunun core tension'ı. "Hiçbirini istemiyorum" hissi kasıtlıdır.)
2. Pakt kaldırma/takas mekanik olacak mı? (Önerilen: MVP'de yok. Rare event olarak VS'de değerlendirilebilir — "Bir paktını feda et, yenisini al".)
3. Mythic tier paktlar ne zaman eklenir? (Faz 2 — oyun değiştirici paktlar: "Ölümde 1 kez diriliş" gibi.)
4. Sinerji sistemi otomatik mi yoksa keşif tabanlı mı? (Önerilen: VS'de Synergy Calculator — otomatik tespit + UI'da gösterim.)
