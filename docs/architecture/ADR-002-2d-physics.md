# ADR-002: 2D Physics Approach

**Status**: Accepted
**Date**: 2026-04-04
**Decision makers**: User + Claude

## Context

Dark Pact top-down 2D oyun. Fizik ihtiyaçları:
- Oyuncu/düşman hareketi (Rigidbody2D velocity)
- Duvar collision (TilemapCollider2D + CompositeCollider2D)
- Hitbox/hurtbox tespiti (Trigger Collider2D)
- Projectile collision

Unity 6.3 LTS'te Box2D v3 entegrasyonu mevcut — `UnityEngine.LowLevelPhysics2D` namespace'i ile multi-threaded performans sunuyor.

## Decision

**Standart Unity Physics2D API** kullanılacak (Box2D v3 backend üzerinde otomatik çalışır). Low-level API kullanılmayacak.

## Rationale

- **Standart API yeterli**: Top-down hack-and-slash'te fizik simülasyonu basit — hareket, collision, trigger. Bunlar standart `Rigidbody2D`, `Collider2D`, `Physics2D.OverlapCircle` ile karşılanır.
- **Box2D v3 otomatik**: Unity 6.3'te Physics2D zaten Box2D v3 backend kullanır — multi-threaded performans avantajı standart API'den de alınır.
- **Low-level API riski**: `LowLevelPhysics2D` yeni ve LLM training data'sında yok. Dokümantasyon sınırlı. Standart API denenmiş ve bilinen.
- **Performans yeterli**: 50-100 aktif collider (düşmanlar + oyuncu + projectile) standart API için sorun değil.

## Physics Layer Yapısı

| Layer | Collision | Kullanım |
|-------|-----------|----------|
| Player | Wall, EnemyHurtbox, Pickup | Oyuncu Rigidbody2D + CapsuleCollider2D |
| Enemy | Wall, PlayerHitbox, EnemyBody | Düşman Rigidbody2D + CapsuleCollider2D |
| Wall | Player, Enemy, Projectile | TilemapCollider2D + CompositeCollider2D |
| PlayerHitbox | EnemyHurtbox | Saldırı trigger collider (kısa ömürlü) |
| EnemyHurtbox | PlayerHitbox | Düşman hasar alma trigger |
| EnemyAttack | PlayerHurtbox | Düşman saldırı trigger |
| PlayerHurtbox | EnemyAttack | Oyuncu hasar alma trigger |
| Projectile | Wall, EnemyHurtbox, PlayerHurtbox | Mermi collider |
| Pickup | Player | Yerdeki loot trigger |

## Consequences

- Collision matrix Unity Editor'da Layer Collision Matrix ile yapılandırılır
- Tüm GDD'lerdeki hitbox/hurtbox tanımları bu layer yapısına referans verir
- Low-level API'ye geçiş gerekirse (performans darboğazı) sadece physics query'leri değişir, gameplay kodu aynı kalır

## Alternatives Considered

| Approach | Pro | Con | Neden reddedildi |
|----------|-----|-----|-----------------|
| LowLevelPhysics2D | Multi-threaded control, custom solver | Yeni API, dokümantasyon az, LLM bilgisi yok | Risk/fayda oranı düşük |
| Custom physics (no Unity Physics2D) | Tam kontrol | Tekerleği yeniden icat, hata riski çok yüksek | Gereksiz |
