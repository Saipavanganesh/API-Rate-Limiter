# API Rate Limiter — Project Instructions

**Scope:** Interview prep project for Senior Developer role (16-20 LPA)
**Last Updated:** 2026-04-12
**Status:** PHASE 1 & 2 COMPLETE | PHASE 3 Next

> Full architecture rationale → `ARCHITECTURE_DECISIONS.md`
> Interview Q&A → `INTERVIEW_PREP.md`

---

## Current Progress

### Done
- **PHASE 1 — Core:** Interfaces, Models, Enums, SlidingWindowCalculator algorithm. Zero external dependencies.
- **PHASE 2 — Unit Tests:** 5 passing tests with [Theory]+[InlineData], AAA pattern. No Redis needed.
- **GitHub:** Pushed to https://github.com/Saipavanganesh/API-Rate-Limiter.git (main branch)

### Up Next — PHASE 3: Redis Implementation
1. Add StackExchange.Redis NuGet to RateLimiter.Redis
2. Create `sliding-window.lua` (atomic read + check + increment)
3. Build `RedisRateLimiter` implementing `IRateLimiter`
4. Build `InMemoryRateLimiter` (graceful degradation fallback)
5. Wire up strategy resolvers (extract IP / user ID / API key)
6. Integration tests with TestContainers

---

## Algorithm (Quick Reference)

```
Formula: estimated_count = (prevCount × (1 - elapsed/window)) + currentCount

Example: prevCount=95, elapsed=30s, window=60s, currentCount=1
  → 95 × 0.5 = 47.5 → 47.5 + 1 = 48.5 → 48
  → limit=100: ALLOWED, remaining=52
```

---

## Architecture Decisions (Locked)

| Decision | Chosen | Trade-off |
|----------|--------|-----------|
| Build order | Route A: Core → Redis → Middleware → Api | Slower visible progress, correct architecture |
| Async/Sync | Async from start (`Task<RateLimitResult>`) | Task wrapping, but correct I/O semantics |
| Core design | Pure function — takes prevCount as param | More params, zero dependencies |
| Lua location | Separate `sliding-window.lua` file | Extra file, but SHA caching + syntax highlighting |
| Redis failure | Graceful degradation → in-memory fallback | Less accurate (per-process), API stays up |
| Middleware order | Per-IP first, Per-User after auth | Slightly complex, defense-in-depth |
| Config | `appsettings.json` per environment | Multiple config files, no recompile needed |

---

## Project Structure

```
RateLimiter.Core (zero dependencies)
  Interfaces/IRateLimiter.cs, IStrategyKeyResolver.cs
  Models/RateLimitConfig.cs, RateLimitResult.cs, RateLimitExceeded.cs
  Enums/RateLimitStrategy.cs, RateLimitStatus.cs
  Algorithms/SlidingWindowCalculator.cs

RateLimiter.Redis (→ Core)
  Implementations/RedisRateLimiter.cs
  Fallbacks/InMemoryRateLimiter.cs
  Configuration/RedisOptions.cs
  sliding-window.lua

RateLimiter.Middleware (→ Core)
  RateLimitMiddleware.cs
  ServiceCollectionExtensions.cs

RateLimiter.Api (→ Core, Redis, Middleware)
  Program.cs, appsettings.json, appsettings.Development.json, appsettings.Production.json
  Controllers/TestController.cs

RateLimiter.Tests
  Unit/SlidingWindowCalculatorTests.cs         ← no Redis
  Integration/RateLimitIntegrationTests.cs     ← TestContainers
```

---

## Project References

```
Api       → Core, Redis, Middleware
Redis     → Core
Middleware → Core
Tests     → Core, Redis, Api
```

---

## NuGet Packages

| Project | Packages |
|---------|----------|
| Core | (none) |
| Redis | StackExchange.Redis |
| Middleware | (implicit from Web SDK) |
| Api | Swashbuckle.AspNetCore (present), StackExchange.Redis |
| Tests | xunit (present), Testcontainers, Testcontainers.Redis |

---

## Implementation Rules (Non-Negotiable)

1. **Core is pure** — no `using` for Redis, ASP.NET, or any infrastructure
2. **Test Core first** — unit tests with hardcoded values before Redis exists
3. **Lua for atomicity** — prevents TOCTOU race conditions
4. **Graceful degradation** — in-memory fallback when Redis throws
5. **Config-driven** — limits in `appsettings.json`, never in code
6. **Async all the way** — no `.GetResult()`, no sync-over-async
