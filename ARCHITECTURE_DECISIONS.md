# API Rate Limiter — Architecture Decisions & Reasoning

Deep-dive rationale behind every design choice. Read this to understand *why*, not just *what*.

---

## Why Route A (Bottom-Up) Over Routes B & C

**Route A:** Core → Unit Tests → Redis → Middleware → Api → Integration Tests

**Why not Route B (Top-Down, Api first):**
- You discover algorithm design too late
- Hard to refactor Core once wired into Api
- Tests for Core get written after the fact (if at all)
- When the interviewer asks "How did you test the algorithm?", you'll stumble
- Changes cascade upward: Core change → Middleware change → Api change

**Why not Route C (Core + Redis together):**
- Can't test Core without Redis setup
- Lose the "pure algorithm" test layer
- If there's a bug in the formula, can't tell if it's Core or Redis

---

## Decision: Async from the Start

**Chose:** `Task<RateLimitResult> IsAllowedAsync()`

**Why not sync:**
- Redis I/O is inherently async (StackExchange.Redis)
- Sync interface forces `GetResult()` hacks → deadlock risks
- Retrofitting async later is painful and breaks everything
- ASP.NET middleware expects async

**Real-world lesson:** Saw a codebase where Core was sync, Redis was async. Looked async but wasn't. Threads blocked. Deadlocks happened. Two years later they refactored the entire codebase. Painful. Design async from the start.

---

## Decision: Core Takes Previous Count as Parameter

**Chose:** `CalculateNewCount(previousCount, currentCount, elapsed, windowSize)` — pure function

**Why not have Core call Redis:**
- Core would need StackExchange.Redis dependency (destroys zero-dep goal)
- Can't unit test without Redis spinning
- Couples algorithm to storage — can't swap without rewriting algorithm

**Real-world lesson:** Saw a team couple algorithm to Redis. When they needed to switch to Memcached: 6 weeks of rewrites. If Core had been separate: 1 day (just replace the Redis adapter).

---

## Decision: Lua Script in Separate File

**Chose:** `sliding-window.lua` loaded at startup, called by SHA

**Why not inline C# string:**
- No syntax highlighting, harder to read, harder to maintain
- SHA-based calls are faster (script uploaded once, referenced by hash after)
- Separate file = separate concern = easier to version and test

**Why Lua at all:** Without it, read-check-increment is three separate Redis commands. Another request can slip in between steps 1 and 3. Both requests see count=99, both pass, both increment to 100. TOCTOU race condition. Lua executes atomically — Redis processes it as a single operation.

---

## Decision: Graceful Degradation When Redis Fails

**Chose:** Fall back to in-memory counter if Redis throws `RedisConnectionException`

**Why not fail-hard (throw):** API returns 503. Customers can't use it. On-call gets paged at 2 AM.

**Why not fail-secure (deny all):** Same result — API is crippled. All users blocked.

**Why graceful degradation:**
- In-memory counter is per-process (not distributed), so less accurate
- But the API stays up
- Availability > Accuracy: 90%-accurate limiter that's up beats 100%-accurate one that's down
- Once Redis recovers, automatically switches back

**Real-world lesson:** Was on-call when Redis went down. Service with fail-hard policy → 503 everywhere, production incident. Another team's service with graceful degradation → stayed up, fixed during business hours.

---

## Decision: Middleware Order — Per-IP First, Per-User After Auth

```csharp
app.UseRateLimitMiddleware(strategy: RateLimitStrategy.PerIp);   // position 1
app.UseAuthentication();                                           // position 2
app.UseConditionalRateLimitMiddleware(                            // position 3
    condition: ctx => ctx.User.Identity.IsAuthenticated,
    strategy: RateLimitStrategy.PerUser
);
```

**Why IP first:**
- Bots/DDoS come from few IPs — stop them before they reach your app
- Login endpoints need protection even without user identity
- Unauthenticated endpoints are fully unprotected if you only rate-limit by user

**Defense-in-depth:** IP limit catches bulk attacks. User limit catches per-account abuse. Both together are more powerful than either alone.

---

## Decision: Config in appsettings.json

**Chose:** Environment-specific `appsettings.*.json` files

**Why not code constants:** Need different limits per env? Recompile and redeploy. Risky for prod.

**Why not database:** Out of scope. Adds DB dependency, makes testing harder, premature optimization.

**12-factor app principle:** Configuration belongs in environment, not code. Dev uses 1000 req/min. Prod uses 100 req/min. Just change the config file.

---

## Decision Points Summary

| Decision | Chose | Key Reason |
|----------|-------|------------|
| Build order | Route A (Bottom-Up) | Test algorithm before touching infrastructure |
| Async/Sync | Async from start | Redis I/O requires it; can't retrofit cleanly |
| Core design | Pure function (no storage calls) | Zero deps, testable in isolation |
| Lua location | Separate `.lua` file | Syntax highlighting, clear separation, SHA caching |
| Redis failure | Graceful degradation | Availability > Accuracy |
| Middleware order | IP first, User after auth | Defense-in-depth, protects unauthenticated endpoints |
| Config | appsettings.json | Per-environment without recompilation |
