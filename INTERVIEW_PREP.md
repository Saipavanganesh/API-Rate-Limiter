# API Rate Limiter — Interview Q&A

Reference this before your interview. These are the exact questions you'll get and strong answers for each.

---

## Q1: Why separate Core from Redis?

**What they're testing:** Dependency Inversion Principle

**Your answer:**
> "Core contains the algorithm logic — the sliding window calculation. Redis is one implementation of storage. By separating them, I ensure Core has zero external dependencies. This lets me unit test the algorithm with no infrastructure setup — 50 unit tests run in 500 milliseconds, no Redis required.
>
> It also means I could swap Redis for Memcached or a database without touching Core. The algorithm doesn't care where the previous count came from."

**Why it impresses:** Shows DIP understanding, testability-first thinking, reusability.

---

## Q2: What if Redis goes down?

**What they're testing:** Operational failure thinking

**Your answer:**
> "Graceful degradation. If Redis is unavailable, we fall back to an in-memory counter. The API stays up, albeit less accurately. An in-memory counter isn't distributed across servers, so it's not perfect, but it's better than returning 500 errors.
>
> The trade-off: Availability > Accuracy. A 90%-accurate rate limiter that's always up is more useful than a 100%-accurate one that's down. Once Redis recovers, we automatically switch back. Zero intervention needed."

**Why it impresses:** Shows failure thinking, trade-off awareness, availability mindset.

---

## Q3: Why Lua script?

**What they're testing:** Race conditions and atomicity

**Your answer:**
> "Without a Lua script, the operation is: (1) Read count from Redis, (2) Check if exceeded, (3) Increment and store. Between steps 1 and 3, another request could execute the same steps. Two requests might both see count=99, both increment to 100, and both pass the limit.
>
> With a Lua script, all three steps execute atomically in Redis. Redis processes the script as a single operation. No interleaving. No race condition. This is TOCTOU (time-of-check to time-of-use) bug prevention — distributed systems need this."

**Why it impresses:** Shows concurrency knowledge, atomicity understanding, distributed systems awareness.

---

## Q4: Why three separate projects instead of one?

**What they're testing:** Architecture and modularity

**Your answer:**
> "Three reasons: Testability, reusability, and separation of concerns.
>
> Core is the algorithm — no dependencies. If someone wants sliding window logic in a gRPC service, they just depend on Core.
>
> Redis is the storage implementation. If we need Memcached instead, we only change this project. Core and Middleware are untouched.
>
> Middleware is the ASP.NET wiring. Different framework? Reimplement Middleware. Core and Redis are reusable.
>
> The dependency direction (Core ← Redis ← Middleware ← Api) means high-level logic doesn't depend on low-level details — Dependency Inversion Principle."

**Why it impresses:** Shows modularity, reusability, DIP by name.

---

## Q5: How did you test this?

**What they're testing:** Testing strategy

**Your answer:**
> "Two layers:
>
> Unit tests (no dependencies): I test the sliding window formula with hardcoded values. No Redis, no network. 50 test cases run in 500ms. Catches algorithm bugs immediately.
>
> Integration tests (real Redis): I use TestContainers to spin up a real Redis instance. I make actual HTTP requests and verify 429 responses and Retry-After headers. Proves end-to-end functionality.
>
> Why both? Unit tests catch logic bugs fast. Integration tests catch infrastructure bugs. Together they give me confidence."

**Why it impresses:** Shows testing strategy, fast-feedback thinking, professional tools (TestContainers).

---

## Q6: What trade-offs did you make?

**What they're testing:** Critical design thinking

**Your answer:**
> "A few key ones:
>
> 1. Async over sync — more code complexity now, but correct I/O semantics. No threading issues.
> 2. Graceful degradation over fail-hard — less accurate (in-memory fallback isn't distributed), but the API stays up.
> 3. Three projects over one — more files, but cleaner dependency direction and better testability.
> 4. Config-driven over hardcoded — more setup, but per-environment configuration without recompilation.
>
> In each case, I chose the design that's more maintainable and production-ready, even if it requires more upfront effort."

**Why it impresses:** Shows trade-off thinking, ability to articulate cost vs benefit.

---

## Q7: Explain the algorithm

**What they're testing:** Sliding window mastery

**Your answer:**
> "Sliding window counter estimates how many requests fall within the current time window.
>
> Formula: `estimated_count = (previous_count × (1 - elapsed / window_size)) + current_count`
>
> Example: Window is 60s, 30s have elapsed, we had 100 requests before. We estimate 50 of them are still in the window. Add the current request: 51 total.
>
> Why sliding window? Fixed window has a boundary burst problem — at the edge of two windows a user can make double requests. Token bucket and leaky bucket have other issues. Sliding window is accurate and atomicity is easy in Redis with a Lua script."

**Why it impresses:** Shows deep algorithm understanding, ability to derive formula, comparative knowledge of alternatives.

---

## Algorithm Quick-Recall

```
Formula: estimated_count = (prevCount × (1 - elapsed/window)) + currentCount

Example: prevCount=95, elapsed=30s, window=60s, currentCount=1
  → 95 × (1 - 30/60) = 95 × 0.5 = 47.5
  → 47.5 + 1 = 48.5 → 48
  → If limit=100: allowed, remaining=52
```
