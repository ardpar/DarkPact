# ADR-001: State Management Pattern

**Status**: Accepted
**Date**: 2026-04-04
**Decision makers**: User + Claude

## Context

Dark Pact'te birçok sistem (Game Manager, Run Manager, Pact System, Item Database) global erişim gerektiriyor. Unity projelerinde yaygın 3 pattern var:

1. **Singleton MonoBehaviour** — `DontDestroyOnLoad`, static instance
2. **Service Locator** — Merkezi registry, runtime'da servis kaydı/sorgusu
3. **Dependency Injection** — Constructor/field injection (Zenject, VContainer)

## Decision

**Service Locator pattern** kullanılacak — basit, custom, lightweight implementation.

```csharp
public static class ServiceLocator
{
    public static void Register<T>(T service);
    public static T Get<T>();
    public static void Reset(); // Run başında temizlik
}
```

## Rationale

- **Singleton yerine**: Singleton test edilemez (static state), bağımlılıklar gizli. Service Locator'da `Register` ile mock servis inject edilebilir.
- **DI framework yerine**: Zenject/VContainer solo proje için overkill. Öğrenme eğrisi ve boilerplate fazla. Service Locator aynı decoupling'i daha az karmaşıklıkla sağlar.
- **Test edilebilirlik**: Unit test'lerde `ServiceLocator.Register<IHealthSystem>(mockHealth)` ile mock'lanabilir. Her test `Reset()` ile temiz başlar.
- **Bootstrap sırası**: Game Manager Boot state'inde servisleri sırayla register eder. Sıra açık ve kontrol edilebilir.

## Consequences

- Tüm global sistemler interface üzerinden erişilir (ör: `IGameManager`, `IRunManager`)
- `ServiceLocator.Get<T>()` çağrıları compile-time güvenli değil — runtime'da kayıtlı olmayan servis sorgulanırsa exception fırlatılır
- Circular dependency tespiti runtime'da yapılır, compile-time'da değil
- Coding standard'a eklenmeli: "Global servislere doğrudan referans yerine `ServiceLocator.Get<T>()` kullanılır"

## Alternatives Considered

| Pattern | Pro | Con | Neden reddedildi |
|---------|-----|-----|-----------------|
| Singleton | Basit, Unity dostu | Test edilemez, god object riski | GDD'lerde minimal tasarım kararı alındı — singleton bunu desteklemez |
| Zenject DI | Tam DI, compile-time safe | Overkill (solo proje), steep learning curve | Karmaşıklık/fayda oranı düşük |
