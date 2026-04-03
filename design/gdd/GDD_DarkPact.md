# Dark Pact — Game Design Document v0.1

**Tür:** Top-down hack-and-slash roguelite  
**Platform:** PC (Steam) — Mobile port sonraki faz  
**Motor:** Unity 2D  
**Geliştirici:** Solo  
**Asset:** ELV Games — Ultimate TopDown Adventure (16x16 pixel art)  
**Durum:** Pre-production

---

## 1. Vizyon

Dark Pact, her run başında karanlık varlıklarla pakt kurarak güç kazandığın bir top-down hack-and-slash roguelite'tır. Her pakt güçlü bir buff ve anlamlı bir kısıt getirir; paktlar birbirleriyle etkileşime girerek her run'ı benzersiz kılar. Güç ile kontrol arasındaki gerilim oyunun özüdür.

**Tek cümle pitch:** "Şeytanla anlaş, bedelini öde — her seferinde farklı bir kaos."

---

## 2. Özgün Mekanik: Karanlık Pakt Sistemi

### 2.1 Temel Konsept

Her run başında oyuncu 3 rastgele pakt arasından birini seçer. Paktlar iki boyutludur:

- **Boon (Nimet):** Güçlü bir buff ya da pasif yetenek
- **Bane (Bela):** Bunu dengeleyen anlamlı bir kısıt

### 2.2 Pakt Etkileşimleri

Paktlar tek başına değil, diğer paktlarla birleşince şekil alır. Her run'da oyuncu milestone'larda ek paktlar seçebilir. İki pakt birlikte *Sinerjik Etki* yaratabilir — bu run'a özgü güçlü kombinasyonlar.

**Örnek Paktlar:**

| Pakt Adı | Boon | Bane |
|---|---|---|
| Katliam Paktı | +%60 hasar | Düşmanlar bir kez yeniden dirilir |
| Kan Kalkanı | Her öldürme = +5 geçici can | Can iksiri yoktur bu run'da |
| Gölge Adımı | Dash sınırsız, cooldown yok | Durduğunda yavaş yavaş hasar alırsın |
| Lanetli Dokunuş | Saldırılar zehirler | Kendi saldırıların da seni hafifçe etkiler |
| Açgözlülük Paktı | Altın %200 fazla düşer | Ekipman satın alınamazın, sadece düşmanlardan loota bak |

### 2.3 Sinerjik Etki Örnekleri

- **Katliam + Kan Kalkanı:** Dirilen düşmanları tekrar öldürmek ekstra can verir → savaş döngüsü değişir
- **Gölge Adımı + Lanetli Dokunuş:** Sürekli hareket ederek hasar almayı engelle, zehirli dash ile düşmana sürekli hasar ver

---

## 3. Core Gameplay Loop

```
RUN BAŞLANGICI
    ↓
Pakt Seçimi (3 seçenek, 1 seçilir)
    ↓
Dungeon Giriş (Procedural tilemap)
    ↓
Combat → Loot → Level Up → Skill Tree
    ↓
Milestone (Yeni Pakt teklifi)
    ↓
Akt Sonu Boss
    ↓
[Kaybettiysen] → Meta progression + yeni run
[Kazandıysan] → Sonraki Akt / New Game+
```

---

## 4. Oyun Yapısı

### 4.1 Aktlar

| Akt | Tileset (ELV Pack) | Boss |
|---|---|---|
| 1 — Kript | Crypt Tileset | Boss Minotaur |
| 2 — Mezarlık | Graveyard Tileset | Boss Werewolf |
| 3 — Cehennem Tapınağı | Hell + Temple Tileset | Boss Osiris |

### 4.2 Dungeon Yapısı

- Procedural tile-based oda sistemi
- Her oda: combat odası, hazine odası, ya da event odası
- Akt sonunda boss odası
- Kasaba/hub ekran yok (Hero Siege'den fark olarak aksiyon kesilmez)

---

## 5. Combat Sistemi

### 5.1 Temel Mekanik

- **Hareket:** WASD
- **Saldırı:** Mouse yönüne doğru, tıklama ile
- **Dash:** Space — Gölge Adımı paktı ile sınırsız olabilir
- **Skill'ler:** 1-2-3-4 kısayolları
- **Top-down perspektif** — ELV asset pack ile birebir uyumlu

### 5.2 Skill Tree

- Her level'da 1 puan
- 3 dal: Saldırı / Savunma / Pakt Güçlendirme
- Pakt Güçlendirme dalı run'a göre değişir — seçilen pakta bağlı özel node'lar açılır

---

## 6. Loot & Progression

### 6.1 Ekipman

ELV pack'teki ikonlar doğrudan kullanılır:
- Silah: Kılıç, balta, balta, mızrak, asa (32x32 ikonlar mevcut)
- Zırh: Armor ikonları mevcut
- Büyü ekipmanları: Spell ikonları (ateş, buz, zehir, şimşek, karanlık)

### 6.2 Item Rarity

- Gri → Yeşil → Mavi → Mor → Altın (Paktlı)
- Altın itemlar seçilen pakta sinerjik bonus taşır

### 6.3 Meta Progression

Run'lar arası kalıcı ilerlemeler:
- Yeni başlangıç class'ları açılır
- Yeni pakt havuzu genişler
- Pasif "ruh gücü" bonusları birikir

---

## 7. Karakterler & Sınıflar

MVP için 2 class, sonraki güncellemelerle genişler:

| Class | Playstyle | Başlangıç Paktı Affinitesi |
|---|---|---|
| Ölüm Şövalyesi | Yakın dövüş, savunma odaklı | Kan Kalkanı paktlarına +%20 etki |
| Cadı | Büyü odaklı, kırılgan | Lanetli Dokunuş paktlarına +%20 etki |

---

## 8. Ses & Müzik

ELV pack'ten doğrudan kullanılacaklar:

**Müzik:**
- Dark Fantasy Music I-V → Dungeon müziği, akt temalarına göre atanır
- Ambient Horror I-V → Boss odaları ve gerilim anları

**SFX:**
- Magic & Spells 1-2 → Skill sesleri
- Crafting & Tinkering → Ekipman equip sesleri
- Achievements → Level up, pakt seçimi

---

## 9. Görsel Stil

- **Tile boyutu:** 16x16 px
- **Karakter/Düşman:** ELV Rogue Adventure sprite'ları
- **UI:** ELV GUI Kit + Weapon/Armor/Spell ikonları
- **Font:** ELV Neatpixels Boss (7x7) başlıklar, Neatpixels Standard metinler
- **Atmosfer:** Karanlık, gotik, turuncu/mor renk paleti

**Eksik — ayrıca temin edilmesi gerekenler:**
- Combat VFX (hit sparks, spell efektleri)
- Unity Particle System ile temel efektler yapılabilir

---

## 10. Teknik Mimari (Unity)

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── GameManager.cs
│   │   └── RunManager.cs
│   ├── Pact/
│   │   ├── PactDefinition.cs (ScriptableObject)
│   │   ├── PactManager.cs
│   │   └── SynergyCalculator.cs
│   ├── Combat/
│   ├── Dungeon/
│   │   └── ProceduralGenerator.cs
│   └── Loot/
│       └── ItemDatabase.cs (ScriptableObject)
├── ScriptableObjects/
│   ├── Pacts/
│   ├── Items/
│   └── Enemies/
└── Art/ (ELV asset pack)
```

**Pact sistemi ScriptableObject tabanlı** — her pakt bir SO, kolayca yeni pakt eklenir.

---

## 11. MVP Scope

MVP'nin içereceği özellikler:

- [x] 1 Akt (Crypt tileset)
- [x] Procedural dungeon generation
- [x] 5 farklı pakt
- [x] 1 class (Ölüm Şövalyesi)
- [x] Temel loot sistemi
- [x] 1 boss
- [x] Skill tree (sadece Saldırı dalı)
- [ ] Ses entegrasyonu
- [ ] Meta progression

MVP hedef: Oynanabilir bir "slice" — 1 run, ~20 dakika içeriği.

---

## 12. Faz Planı

| Faz | İçerik | Hedef |
|---|---|---|
| **Faz 1 — MVP** | 1 akt, 1 class, 5 pakt, temel combat | Oynanabilir prototip |
| **Faz 2 — Derinlik** | 3 akt, 2 class, 15+ pakt, sinerjiler, meta progression | Steam Early Access |
| **Faz 3 — Genişleme** | Co-op, mobile port, ek class'lar | Full release |

---

*GDD v0.1 — Dark Pact — Revize edilecek döküman*
