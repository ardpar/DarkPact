# ADR-003: Pathfinding Approach

**Status**: Accepted
**Date**: 2026-04-04
**Decision makers**: User + Claude

## Context

Enemy AI'da düşmanlar oyuncuya doğru hareket ederken duvarlardan kaçınmalı. Seçenekler:

1. **Grid-based A*** — Tile grid üzerinde A* pathfinding
2. **Unity NavMesh2D** — NavMeshSurface2D bake + NavMeshAgent
3. **Flowfield** — Tüm grid için tek hesaplama, çok düşmanda verimli

## Decision

**Grid-based A*** kullanılacak — custom implementation, tile grid ile doğal uyumlu.

## Rationale

- **Tile uyumu**: Dungeon 16x16 tile grid üzerine kurulu. A* pathfinding bu grid'i doğrudan walkability grid olarak kullanır — ek veri yapısı gerekmez.
- **NavMesh dezavantajı**: NavMesh2D procedural dungeon'da her oda yüklendiğinde bake gerektirir. Bake süresi ve runtime NavMesh birleştirme karmaşıklık ekler.
- **Flowfield overkill**: Flowfield çok sayıda aynı hedefe giden agent'ta verimli (tower defense). Dark Pact'te oda başına 3-8 düşman var — A* yeterli.
- **Basitlik**: Grid A* implement etmesi kolay, debug etmesi kolay (grid overlay ile path görselleştirme).

## Implementation Notes

```
- Walkability grid: Room/Tilemap System'dan — Wall tile = impassable, Ground tile = walkable
- Grid çözünürlüğü: 1:1 tile (16x16 piksel = 1 grid hücresi)
- Path cache: Düşman başına path 0.3s cache, her 0.3s'de recalculate (AI tick rate ile uyumlu)
- Max path length: 50 node (oda boyutu max 32x32 = yeterli)
- Diagonal movement: İzin verilir (8-yönlü), köşe kesme yasak (diagonal wall clip önleme)
```

## Performance Budget

- Oda başına max 10 düşman × path recalculate 0.3s = ~33 A* query/s
- 32x32 grid'de A* = ~1000 node worst case = < 0.1ms per query
- Toplam: < 3.3ms/s = ihmal edilebilir

## Consequences

- Her oda yüklendiğinde walkability grid oluşturulmalı (Room/Tilemap → bool[,] grid)
- Dynamic obstacle (hareket eden engel) desteği yok — MVP'de gerekmez
- Path smoothing opsiyonel (düşmanlar grid hareketinden funnel algorithm ile yumuşatılabilir)

## Alternatives Considered

| Approach | Pro | Con | Neden reddedildi |
|----------|-----|-----|-----------------|
| NavMesh2D | Unity native, agent avoidance built-in | Procedural dungeon'da bake karmaşık, runtime overhead | Tile grid ile doğal uyumsuz |
| Flowfield | Çok agent'ta O(1) per agent | Tek hedefli (oyuncu), setup maliyeti yüksek | Oda başına 3-8 düşman için overkill |
| No pathfinding (direct chase) | Basit | Düşmanlar duvara takılır | Oynanamaz |
