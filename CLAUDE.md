# API Rate Limiter — Project Instructions

**Scope:** Interview prep project for Senior Developer role (16-20 LPA)
**Last Updated:** 2026-04-15
**Status:** ALL PHASES COMPLETE

> Full architecture rationale → `ARCHITECTURE_DECISIONS.md`
> Interview Q&A → `INTERVIEW_PREP.md`

---

## Current Progress

### All Phases Complete
- **PHASE 1 — Core:** Interfaces, Models, Enums, SlidingWindowCalculator algorithm. Zero external dependencies.
- **PHASE 2 — Unit Tests:** 5 passing tests with [Theory]+[InlineData], AAA pattern. No Redis needed.
- **PHASE 3 — Redis:** sliding-window.lua (atomic Lua), RedisRateLimiter, InMemoryRateLimiter fallback.
- **PHASE 4 — Middleware:** RateLimitMiddleware, ServiceCollectionExtensions, three strategy resolvers (IP/User/ApiKey).
- **PHASE 5 — API Wiring:** Program.cs DI, appsettings config, TestController. Live 429 verified.
- **PHASE 6 — Integration Tests:** TestContainers, 3 tests (200 within limit, 429 exceeded, Retry-After header).
- **Docker:** Dockerfile + docker-compose.yml — `docker-compose up --build` spins everything up.
- **README:** Architecture, algorithm, design decisions, how to run.
- **GitHub:** Pushed to https://github.com/Saipavanganesh/API-Rate-Limiter.git (main branch)

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
  Interfaces/IRateLimiter.cs
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
  IStrategyKeyResolver.cs
  Resolvers.cs (IpKeyResolver, UserKeyResolver, ApiKeyResolver)

RateLimiter.Api (→ Core, Redis, Middleware)
  Program.cs, appsettings.json, Dockerfile
  Controllers/RateLimitTestController.cs

RateLimiter.Tests (→ Core, Api)
  Unit/SlidingWindowCalculatorTests.cs         ← no Redis
  Integration/RateLimitIntegrationTests.cs     ← TestContainers

docker-compose.yml                             ← solution root
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
