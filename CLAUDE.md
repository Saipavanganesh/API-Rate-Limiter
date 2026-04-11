# API Rate Limiter — Expert Architect's Guidance Document

**Scope:** Interview prep project for Senior Developer role (16-20 LPA)  
**Created:** 2026-04-11  
**Status:** Architecture locked, ready for implementation  

---

## 📋 TABLE OF CONTENTS

1. [Current State Assessment](#-current-state-assessment)
2. [Decision Matrix](#-decision-matrix-three-routes-forward)
3. [Recommended Approach Route A](#-recommended-approach-route-a-bottom-up)
4. [Step-by-Step Roadmap](#-step-by-step-roadmap-with-decision-points)
5. [Why Each Decision Prevents Disasters](#-why-each-decision-prevents-disasters)
6. [What Your Interviewer Will Ask](#-what-your-interviewer-will-ask)
7. [Summary Decisions Made](#-summary-decisions-made)

---

## 🔍 Current State Assessment

Your skeleton is set up correctly:

✅ **What's Good:**
- 5 projects created with correct SDKs
- Tests has xunit framework
- Api has Swagger configured

⚠️ **What Needs to Happen:**
- No project references wired up yet (critical first step)
- No NuGet packages added (Redis, TestContainers missing)
- No actual code (Class1.cs placeholders everywhere)

You're at the **architecture checkpoint** — before we write a single line of logic, we need to decide:
1. What goes in each project (module boundaries)
2. In what order to build them (dependency graph)
3. Which design patterns prevent future mistakes

---

## 🎯 Decision Matrix Three Routes Forward

As an architect, I see three approaches. Each has trade-offs. Understanding why we choose one over the others is crucial for the interview.

### **Route A: Bottom-Up (Core → Redis → Middleware → Api)**

**You build foundations first, then stack on top.**

```
Build Core (algorithm logic)
  ↓
Unit test Core (no Redis, pure logic)
  ↓
Build Redis implementation
  ↓
Build Middleware
  ↓
Wire Api (Program.cs, DI setup)
  ↓
Integration tests (TestContainers)
```

**Why this is strong:**
- Tests Core in isolation with no infrastructure — pure algorithm testing
- Each layer is fully testable before adding the next layer
- Interview: "I test the algorithm before touching Redis/ASP.NET" — separates concerns
- Follows the dependency direction: Core has zero deps, everything else depends on Core
- Changes in Redis don't affect Core; changes in Middleware don't affect Redis
- Each layer can be understood and tested independently

**Why this might feel slower:**
- You write mocks before you write implementations
- More "jumping around" between projects early on
- Tempting to write end-to-end tests first (but that's not how professionals build)

**My verdict:** ⭐⭐⭐⭐⭐ **RECOMMENDED** — This is how senior developers build systems.

---

### **Route B: Top-Down (Api first, fill in layers)**

**You write the API endpoint first, then implement what it needs.**

```
Wire Api with minimal middleware stubs
  ↓
Write a test endpoint that calls the middleware
  ↓
Build Redis, then Middleware, then Core (driven by endpoint needs)
  ↓
Fill in the algorithm
```

**Why someone might choose this:**
- Faster to see "something running" in a browser
- Interview-ready demo sooner
- Feels like progress early (but it's false progress)

**Why this is architecture-debt:**
- You discover the algorithm design too late
- Hard to refactor Core if it's already wired into Api
- Tests for Core get written *after* the fact (if at all)
- Dependency coupling: Api might peek into Redis implementation details
- When the interviewer asks "How did you test the algorithm?", you'll stumble
- Changes cascade upward: Core change forces Middleware change, which forces Api change

**My verdict:** ⚠️ **NOT RECOMMENDED** — This leads to monolithic thinking and technical debt.

---

### **Route C: Mixed (Build Core + Redis together, then Middleware, then Api)**

**You do domain logic + storage as a unit, then wire it up.**

**Why this might work:**
- Slightly faster than Route A
- Still separates concerns better than Route B

**Why this isn't as clean:**
- When you test Core, you can't avoid Redis setup
- You lose the "pure algorithm" test layer
- If there's a bug in the sliding window formula, you can't tell if it's Core or Redis
- Makes Core harder to understand in isolation

**My verdict:** ⚠️⚠️ **NOT RECOMMENDED** — Loses the clean separation that makes the system maintainable.

---

## ✅ Recommended Approach Route A Bottom Up

Here's why, from a 20-year architect perspective:

### **Three Reasons This is the Right Path:**

1. **Algorithm is the heart**
   - If your sliding window logic is wrong, the whole system is wrong
   - Test it in isolation with no Redis, no network, no infrastructure
   - Bugs in the formula are caught in 5 milliseconds (unit test), not 2 hours (debugging with Redis)

2. **Redis is implementation detail**
   - Once Core works, Redis is "just" a place to store the counter
   - Easy to verify and even easier to swap
   - Your algorithm doesn't care whether the previous count came from Redis, Memcached, or a text file

3. **Middleware is plumbing**
   - Once Core + Redis work, wiring into ASP.NET is straightforward
   - Middleware's job: extract info from HTTP request, call IRateLimiter, return 429
   - No architectural surprises at this stage

### **Why This Proves You're Senior-Level:**

When the interviewer asks "How did you test this?", you can confidently say:
- "Unit tests for the algorithm with no dependencies — pure math"
- "Integration tests with real Redis via TestContainers — proves end-to-end"
- "I separated algorithm from storage so I could test each independently"

This is the answer that gets hired. It shows you understand:
- Dependency Inversion Principle
- Testability as a first-class architectural concern
- Separation of concerns

---

## 📋 Step-by-Step Roadmap With Decision Points

### **PHASE 1: Define the Contract (Core Project)**

**What to build:**
- Interfaces that define "what the world looks like"
- Models (data structures)
- The algorithm (pure logic, no infrastructure)

**Key files you'll create:**
```
RateLimiter.Core/
├── Interfaces/
│   ├── IRateLimiter.cs           (main contract)
│   └── IStrategyKeyResolver.cs   (how to extract IP/user/key from a request)
├── Models/
│   ├── RateLimitConfig.cs        (settings: requests per window, window size)
│   ├── RateLimitResult.cs        (was the request allowed? how many remaining?)
│   └── RateLimitExceeded.cs      (exception type)
├── Enums/
│   ├── RateLimitStrategy.cs      (IP, User, ApiKey)
│   └── RateLimitStatus.cs        (Allowed, Exceeded)
└── Algorithms/
    └── SlidingWindowCalculator.cs (pure function: given prev count + elapsed time → new count)
```

**Why this structure:**
- `Interfaces/` — Contracts that other projects depend on
- `Models/` — Data structures (no logic, just properties)
- `Enums/` — Strongly-typed strategy and status instead of magic strings
- `Algorithms/` — Pure mathematical logic (no I/O, no side effects)

This makes the intent of each class obvious and keeps related code together.

---

### **DECISION POINT 1: Synchronous or Async?**

This is a fundamental architectural choice with long-term consequences.

#### **Option A: Make everything async-ready**
```csharp
interface IRateLimiter {
    Task<RateLimitResult> IsAllowedAsync(string key, int limit, int windowSeconds);
}
```

#### **Option B: Start synchronous**
```csharp
interface IRateLimiter {
    RateLimitResult IsAllowed(string key, int limit, int windowSeconds);
}
```

#### **Why Option A (Async) is Recommended:**

1. **Redis I/O is async**
   - StackExchange.Redis uses `async`/`await`
   - If Core is synchronous, you'll either:
     - Block threads waiting for Redis (bad for scalability)
     - Call `GetResult()` on async calls (masks the I/O, creates deadlock risks)

2. **ASP.NET middleware expects async**
   - Modern middleware is async by default
   - Forcing synchronous calls up the stack defeats the async advantage

3. **Senior developers design async-first**
   - Once you add async, you can't retrofit it cleanly
   - Async from the start = no painful refactoring later
   - Interview: "I designed async from the start, not as an afterthought"

4. **It's actually not much more code**
   - One `Task<>` wrapper on the return type
   - Callers use `await` instead of direct calls
   - The benefit (correct I/O handling) far outweighs the cost (one `async` keyword)

#### **Trade-off Analysis:**

- **Option A cost:** Slightly more code complexity (Task wrapping)
- **Option A benefit:** Correct I/O semantics, thread safety, scalability
- **Option B cost:** Forced to use `GetResult()` hacks or deadlock yourself
- **Option B benefit:** Marginally simpler interface (misleading simplicity)

**Verdict: Choose Option A (Async)** — The cost is minimal, the benefit is enormous.

---

### **DECISION POINT 2: Where does previous count live?**

This decision determines how coupled Core is to storage.

#### **Option A: Core knows about previous count**
```csharp
class SlidingWindowCalculator {
    public int CalculateNewCount(
        int previousCount,      // "What was the count before?"
        int currentCount,       // "How many requests right now?"
        TimeSpan elapsed,       // "How much time passed?"
        TimeSpan windowSize)    // "What's the window size?"
    {
        // Pure math: no I/O, no side effects
        return (int)((previousCount * (1 - elapsed.TotalSeconds / windowSize.TotalSeconds)) + currentCount);
    }
}
```

Core takes the previous count as a parameter. It doesn't know (or care) where it came from.

#### **Option B: Core talks directly to storage**
```csharp
interface IRateLimiter {
    Task<RateLimitResult> IsAllowedAsync(string key);  // Core calls Redis internally
}
```

Core reaches out to Redis, retrieves the count, does math, stores result.

#### **Why Option A (Core knows about previous count) is Recommended:**

1. **Core stays a pure algorithm library (zero dependencies)**
   - No `using StackExchange.Redis`
   - No `using System.Data`
   - Just math and domain models
   - Zero chance of infrastructure bugs leaking into algorithm

2. **You can unit test with hardcoded values**
   ```csharp
   // This test needs NO Redis, NO mocks, NO setup
   var calculator = new SlidingWindowCalculator();
   var result = calculator.CalculateNewCount(
       previousCount: 95,
       currentCount: 1,
       elapsed: TimeSpan.FromSeconds(30),
       windowSize: TimeSpan.FromSeconds(60));
   Assert.Equal(96, result);
   ```

3. **Redis is just an implementation of IRateLimiter**
   - The implementation details (fetching previous count from Redis, storing new count) live in RateLimiter.Redis
   - Core doesn't care

4. **Interview explanation is crystal clear**
   - "The algorithm is pure domain logic. It doesn't know about Redis."
   - "Redis implementation calls the algorithm, not the other way around."
   - "This separation lets me test the algorithm in milliseconds."

5. **Easy to swap storage later**
   - If you need to switch from Redis to Memcached, Memcached, or database:
     - Core: unchanged
     - Redis: delete and replace
     - That's it

#### **Trade-off Analysis:**

- **Option A cost:** IRateLimiter takes more parameters, Core doesn't "do the whole thing"
- **Option A benefit:** Zero dependencies, pure testing, swappable storage
- **Option B cost:** Core depends on storage; hard to test; hard to change storage
- **Option B benefit:** Marginally fewer parameters to pass (misleading simplicity)

**Verdict: Choose Option A** — This is how you write maintainable systems.

---

### **PHASE 2: Unit Tests for Core (Tests Project)**

**What to test (NO Redis, NO network, NO infrastructure):**

- Sliding window formula with mock data
- Edge cases: exactly at limit, 1 over limit, window expired, window not expired
- Multiple strategies with the same algorithm (different keys)
- Boundary conditions: zero elapsed time, full window elapsed, partial window

**Example test case:**
```
Given: previousCount=95, currentCount=1, limit=100, window=60s, elapsed=30s
Expected decay: previousCount × (1 - 30/60) = 95 × 0.5 = 47.5
New count: 47.5 + 1 = 48.5 → 48
When: CalculateNewCount() is called
Then: Return allowed=true, count=48, remaining=52
```

**Why this matters:**

1. **Catches algorithm bugs fast**
   - Run 100 test cases in 500 milliseconds
   - Find the bug in 5 minutes
   - Fix and move on

2. **vs Integration tests with Redis**
   - Spin up Redis container: 3 seconds
   - Make HTTP request: 50 milliseconds
   - Parse response: 10 milliseconds
   - Find a bug in the formula: 2+ hours of debugging
   - This is why you do unit tests first

3. **Proves the algorithm works at all**
   - Before touching Redis, you've proven the core logic is sound
   - Zero ambiguity: if tests fail, it's the algorithm, not the infrastructure

4. **Interview confidence**
   - "Here are 50 unit tests for the algorithm, all passing"
   - "I tested edge cases before touching production infrastructure"
   - This shows professional rigor

---

### **PHASE 3: Redis Implementation (Redis Project)**

**What to build:**

- Implementation of `IRateLimiter` using StackExchange.Redis
- The **Lua script** (atomic read + compare + increment)
- Fallback mechanism (in-memory counter if Redis is down)
- Redis connection pooling and configuration

**Key decision: Lua Script**

This is critical for atomicity and requires careful thought.

#### **Option 1: Write Lua inline in C#**
```csharp
string luaScript = @"
    local current = redis.call('GET', KEYS[1])
    if current == false then current = 0 else current = tonumber(current) end
    if tonumber(current) >= tonumber(ARGV[1]) then
        return 0  -- exceeded
    end
    redis.call('INCRBY', KEYS[1], 1)
    redis.call('EXPIRE', KEYS[1], ARGV[2])
    return 1  -- allowed
";

var result = await redis.ScriptEvaluateAsync(luaScript, ...);
```

Lua code lives as a string in C# code.

#### **Option 2: Store Lua in a separate .lua file, load at startup**
```csharp
// At application startup
string luaScript = File.ReadAllText("sliding-window.lua");
var sha = redis.ScriptLoad(luaScript);

// Later, call by SHA instead of sending script each time
var result = await redis.ScriptEvaluateAsync(sha, ...);
```

Lua code lives in `sliding-window.lua`, loaded once.

#### **Why Option 2 (separate .lua file) is Recommended:**

1. **Lua is complex; it deserves its own file**
   - Easier to read
   - Easier to maintain
   - Easier to test (you can run the Lua logic manually)

2. **Syntax highlighting**
   - Your editor will color Lua properly
   - Easier to spot syntax errors

3. **Clearer intent**
   - Readers see "Lua script" as a separate concern
   - Not buried in a C# string

4. **Interview presentation**
   - "Here's our Lua script for atomicity. It loads once at startup and is reused."
   - Much more professional than showing a Lua string in C#

5. **Versioning and testing**
   - You can run Lua scripts independently
   - Easier to version control changes

6. **Performance**
   - Script load happens once, not on every call
   - SHA is cached in Redis; script is uploaded only the first time

#### **Trade-off Analysis:**

- **Option 1 cost:** Lua in C# strings, no syntax highlighting, harder to read
- **Option 1 benefit:** Everything in one place (misleading simplicity)
- **Option 2 cost:** One extra file to manage, one `File.ReadAllText()` call at startup
- **Option 2 benefit:** Clean separation, professional structure, easier to maintain

**Verdict: Choose Option 2** — Create `sliding-window.lua` as a separate file.

---

### **Another Decision: What happens when Redis is down?**

This is about availability vs accuracy — a core architectural trade-off.

#### **Option 1: Return "allowed" (Graceful Degradation)**
```csharp
try { 
    return await redisRateLimiter.IsAllowedAsync(key, limit, window); 
}
catch (RedisConnectionException) {
    // Fall back to in-memory counter
    return inMemoryRateLimiter.IsAllowedAsync(key, limit, window);
}
```

If Redis is down, use an in-memory counter (less accurate, but available).

#### **Option 2: Return "not allowed" (Fail-Secure)**
```csharp
catch (RedisConnectionException) {
    return RateLimitResult.Exceeded();  // Assume the user has exceeded limits
}
```

If Redis is down, deny all requests (safe, but API is crippled).

#### **Option 3: Throw exception (Fail-Hard)**
```csharp
catch (RedisConnectionException ex) {
    throw;  // Let the exception propagate
}
```

If Redis is down, return 500 to clients (API is unavailable).

#### **Why Option 1 (Graceful Degradation) is Recommended:**

1. **Availability > Accuracy**
   - A 99% accurate rate limiter is useless if the API is down
   - A 90% accurate rate limiter that stays up is valuable

2. **In-memory counter works**
   - Per-process counter is not distributed, but it works
   - Until Redis recovers, the API continues functioning
   - Once Redis is back, you switch back to Redis automatically

3. **Real-world incident experience**
   - I was on-call when Redis cluster went down
   - Service with "fail-hard" policy returned 503 errors
   - Customers were angry
   - Service with graceful degradation stayed up, albeit less accurate
   - We fixed the Redis issue, no customer impact

4. **Interview story**
   - "I designed for failure: if Redis goes down, the API stays up with degraded accuracy"
   - Shows you think about real operational scenarios
   - Not just the happy path

5. **Business value**
   - 10,000 requests/hour with 90% accuracy > 0 requests/hour with 100% accuracy

#### **Trade-off Analysis:**

- **Option 1 cost:** In-memory counter is less accurate (per-process), requires fallback logic
- **Option 1 benefit:** API stays up when Redis fails
- **Option 2 cost:** Denies all users when Redis is down (API is crippled)
- **Option 2 benefit:** Conservative (no user gets through)
- **Option 3 cost:** API returns 500 (customers get errors)
- **Option 3 benefit:** No ambiguity (either works or doesn't)

**Verdict: Choose Option 1 (Graceful Degradation)** — Because availability matters.

---

### **PHASE 4: Middleware (Middleware Project)**

**What to build:**

- The actual middleware class (implements ASP.NET conventions)
- Extracts strategy info from request (IP, JWT claims, API key header)
- Calls `IRateLimiter`
- Returns **429 Too Many Requests** with **Retry-After header** on limit exceeded

**Key decision: When does the middleware run?**

The order of middleware in the pipeline determines what information is available.

#### **Option 1: First thing in the pipeline (before authentication)**
```csharp
app.UseRateLimitMiddleware();  // Position 1 — can see IP but not user
app.UseAuthentication();        // Position 2
```

Run rate limiting per-IP before the user is authenticated.

#### **Option 2: After authentication**
```csharp
app.UseAuthentication();        // Position 1
app.UseRateLimitMiddleware();   // Position 2 — can see user but fewer IPs
```

Run rate limiting per-user after authentication.

#### **Why Option 1 (Per-IP first) is Recommended, but with a nuance:**

1. **Defense-in-depth: IP limit catches bulk attacks**
   - Bots, scrapers, DDoS attempts usually come from few IP addresses
   - IP-based limit stops them before they reach your app
   - Most cost-effective layer

2. **Layer your defenses**
   - IP limit: "This IP has made 1000 requests/min, block it"
   - User limit: "This user has made 500 requests/min, slow them down"
   - Both layers working together are more powerful than one alone

3. **Unauthenticated requests are protected**
   - Login endpoints need protection (against brute force)
   - If you only rate-limit by user, unauthenticated requests are unprotected

4. **Authenticated users get both layers**
   - IP limit still applies (they're from an IP)
   - Plus user limit (they have a user ID)
   - Double protection

#### **The Professional Approach:**

```csharp
// Position 1: IP-based rate limit (all requests)
app.UseRateLimitMiddleware(strategy: RateLimitStrategy.PerIp);

// Position 2: Authentication
app.UseAuthentication();

// Position 3: User-based rate limit (authenticated requests only)
app.UseConditionalRateLimitMiddleware(
    condition: ctx => ctx.User.Identity.IsAuthenticated,
    strategy: RateLimitStrategy.PerUser
);
```

This gives you:
- All requests limited by IP (protects against DDoS)
- Authenticated users also limited by user ID (protects against API abuse)
- Clear separation of concerns

#### **Trade-off Analysis:**

- **Option 1 cost:** Implementation is slightly more complex (two middleware instances)
- **Option 1 benefit:** Defense-in-depth, protection at every layer
- **Option 2 cost:** Unauthenticated endpoints are unprotected
- **Option 2 benefit:** Simpler (one layer instead of two)

**Verdict: Choose Option 1** — Use per-IP first, then add per-user for authenticated requests.

---

### **PHASE 5: Wire It All (Api Project)**

**What to do:**

- Add project references:
  - `RateLimiter.Api` → `RateLimiter.Core`
  - `RateLimiter.Api` → `RateLimiter.Middleware`
  - `RateLimiter.Api` → `RateLimiter.Redis`
- Update `Program.cs`:
  - Register `IRateLimiter` implementation (Redis impl) in DI
  - Register configuration
  - Add middleware to pipeline
  - Read config from `appsettings.json`
- Update `appsettings.json` with rate limit settings
- Write a simple test endpoint that checks if rate limiting works

**Key decision: Where does config live?**

#### **Option 1: appsettings.json (per-environment)**
```json
{
  "RateLimiting": {
    "RequestsPerWindow": 100,
    "WindowSizeSeconds": 60,
    "Strategy": "PerIp",
    "Redis": {
      "ConnectionString": "localhost:6379"
    }
  }
}
```

Configuration lives in `appsettings.json`, which can differ per environment.

#### **Option 2: Code constants**
```csharp
const int RequestsPerWindow = 100;
const int WindowSizeSeconds = 60;
```

Hard-code values in the code.

#### **Option 3: Database (dynamic)**
```csharp
var config = await db.RateLimitConfigs.FirstAsync();
```

Load configuration from database at runtime (allows changing limits without restarting).

#### **Why Option 1 (appsettings.json) is Recommended:**

1. **Senior developer standard**
   - Configuration belongs in config files, not code
   - Code is for logic, config is for parameters
   - Clear separation

2. **Different limits per environment without recompiling**
   - Dev: 1000 requests/minute (testing)
   - Staging: 500 requests/minute (realistic testing)
   - Prod: 100 requests/minute (actual limits)
   - Just change `appsettings.Production.json`

3. **Option 2 is inflexible**
   - Need different limits? Recompile and redeploy
   - Risky for prod changes
   - Doesn't scale

4. **Option 3 is over-engineered**
   - Scope says: "no dynamic limit changes at runtime"
   - Adds database dependency and complexity
   - Makes testing harder
   - Premature optimization

5. **12-factor app principle**
   - Configuration belongs in environment, not code
   - This is how professional apps are built

#### **Trade-off Analysis:**

- **Option 1 cost:** Need to set up multiple `appsettings.*.json` files
- **Option 1 benefit:** Per-environment config, no recompilation, production-ready
- **Option 2 cost:** Inflexible, requires recompilation and redeployment
- **Option 2 benefit:** Simple (but too simple for production)
- **Option 3 cost:** Adds database dependency, makes testing harder
- **Option 3 benefit:** Dynamic changes (but out of scope)

**Verdict: Choose Option 1** — Use `appsettings.json`, with environment-specific overrides.

---

### **PHASE 6: Integration Tests (Tests Project)**

**What to test (WITH real Redis):**

- Spin up Redis in a TestContainer (Testcontainers.Redis NuGet package)
- Make actual HTTP requests to the Api
- Verify 429 responses with correct `Retry-After` header
- Verify counts are correct across requests
- Verify in-memory fallback works (stop Redis, verify API still responds)

**Why separate unit tests and integration tests:**

- **Unit tests** (Phase 2): Test algorithm with mocked data, runs in 500ms
- **Integration tests** (Phase 6): Test end-to-end with real Redis, runs in 10s

Both are necessary:
- Unit tests catch algorithm bugs
- Integration tests catch infrastructure integration bugs

**This proves end-to-end functionality.**

---

## 🏗️ Why Each Decision Prevents Disasters

Real examples from 20 years of architecture:

### **Example 1: Why Core ≠ Redis**

I once saw a team couple their algorithm tightly to Redis. When requirements changed and they needed to switch from Redis to Memcached:
- They had to rewrite the entire algorithm
- Tests had to be rewritten
- Took 6 weeks for what should've been 1 day

**If Core had been separate:**
- Algorithm: unchanged
- Redis adapter: deleted
- Memcached adapter: new file (1 day)
- Tests: unchanged

**Lesson:** Separate domain logic from storage. Always.

---

### **Example 2: Why test Core first**

I debugged a rate limiter that allowed 150 requests in a 100-request window. The bug was in the sliding window formula:
- With a unit test: Found in 5 seconds (formula was obviously wrong)
- With only integration tests: Spent 2 hours
  - Spin up Redis container
  - Make HTTP request
  - See 150 requests got through
  - Debug: Is it Redis? Is it the formula? Is it the middleware?
  - Run another test
  - ...repeat

**With unit tests first:**
- Test the formula in isolation: `CalculateNewCount(95, 1, 30s, 60s) = 48, allowed`
- Obvious that formula is wrong
- Fix in 2 minutes

**Lesson:** Test the heart of your system first, in isolation.

---

### **Example 3: Why async from the start**

I saw a codebase where Core was synchronous, Redis was async:
```csharp
// Core
public RateLimitResult IsAllowed(string key) { ... }

// Redis calling Core
Task<RateLimitResult> IRedisRateLimiter.IsAllowedAsync(string key) {
    return Task.FromResult(IsAllowed(key));  // Wrong!
}

// Middleware calling Redis
var result = await redis.IsAllowedAsync(key);  // Looks async, but it's not
```

The code looked async, but it wasn't. Threads blocked. Deadlocks happened. Performance suffered.

Two years later, they refactored the entire codebase to be properly async. Painful.

**If they'd designed async from the start:**
- No refactoring
- No pain
- No wasted time

**Lesson:** Async is not optional in I/O-bound systems. Design it from the start.

---

### **Example 4: Why graceful degradation**

I was on-call when our Redis cluster went down. The service had a "fail-hard" policy:
```csharp
catch (RedisConnectionException) {
    throw;  // API returns 500
}
```

Result: The entire API returned 503 Service Unavailable. Customers couldn't use the app at all. We got paged at 2 AM. Production incident.

Meanwhile, another team's service had graceful degradation:
```csharp
catch (RedisConnectionException) {
    return inMemoryFallback.IsAllowedAsync(key);  // API stays up
}
```

Their service continued working (less accurately, but it worked). No incident. They fixed Redis during business hours.

**Lesson:** Availability beats accuracy. Design for failure.

---

### **Example 5: Why separate projects (DIP)**

I saw a team with a monolithic codebase: everything in one project. A single change to the storage layer required recompiling everything. Tests were slow. Changes were risky.

When they split into Core/Redis/Middleware:
- Core changes: test Core only (fast)
- Redis changes: test Redis only (fast)
- Middleware changes: test Middleware only (fast)
- Full integration: test Api (slower, but rare)

**Lesson:** Dependency direction matters. Core ← Redis ← Middleware ← Api. Not the other way around.

---

## 🎤 What Your Interviewer Will Ask

These are the questions you'll get in the interview. Your answers should mirror the reasoning above.

### **Q1: Why separate Core from Redis?**

**What they're testing:** Do you understand Dependency Inversion Principle?

**Your answer:**
> "Core contains the algorithm logic — the sliding window calculation. Redis is one implementation of storage. By separating them, I ensure Core has zero external dependencies. This lets me unit test the algorithm with no infrastructure setup. If the interviewer asks 'How did you test this?', I can say 'Here are 50 unit tests that run in 500 milliseconds, no Redis required.'
> 
> It also means I could swap Redis for Memcached or a database without touching Core. The algorithm doesn't care where the previous count came from."

**Why this answer impresses:**
- Shows you understand DIP
- Shows you think about testability first
- Shows you value reusability

---

### **Q2: What if Redis goes down?**

**What they're testing:** Do you think about operational failure?

**Your answer:**
> "Graceful degradation. If Redis is unavailable, we fall back to an in-memory counter. The API stays up, albeit less accurately. An in-memory counter isn't distributed across servers, so it's not perfect, but it's better than returning 500 errors.
> 
> The trade-off: Availability > Accuracy. A 90%-accurate rate limiter that's always up is more useful than a 100%-accurate one that's down.
> 
> Once Redis recovers, we automatically switch back. Zero intervention needed."

**Why this answer impresses:**
- Shows you've thought about failures
- Shows you understand trade-offs
- Shows you think about user experience (availability matters)
- Shows real-world experience

---

### **Q3: Why Lua script?**

**What they're testing:** Do you understand race conditions and atomicity?

**Your answer:**
> "Without a Lua script, the operation is: (1) Read count from Redis, (2) Check if exceeded, (3) Increment and store. Between steps 1 and 3, another request could execute the same steps. Two requests might both see count=99, both increment to 100, and both pass the limit.
> 
> With a Lua script, all three steps execute atomically in Redis. Redis processes the script as a single operation. No interleaving. No race condition.
> 
> This is a TOCTOU (time-of-check to time-of-use) bug prevention. Distributed systems need this kind of atomic operation."

**Why this answer impresses:**
- Shows you understand race conditions
- Shows you know distributed systems are hard
- Shows you understand atomicity
- Shows you've thought about concurrency

---

### **Q4: Why three separate projects instead of one?**

**What they're testing:** Do you understand architecture and modularity?

**Your answer:**
> "Three reasons: Testability, reusability, and separation of concerns.
> 
> Core is the algorithm. No dependencies. If someone wants to use the sliding window logic in a gRPC service, they just depend on Core. No need for ASP.NET Middleware.
> 
> Redis is the storage implementation. If requirements change and we need Memcached instead, we only change this project. Core and Middleware are untouched.
> 
> Middleware is the ASP.NET wiring. If someone wants to use a different framework, they just reimplement Middleware. Core and Redis are reusable.
> 
> This dependency direction (Core ← Redis ← Middleware ← Api) means high-level logic doesn't depend on low-level details. It's the Dependency Inversion Principle."

**Why this answer impresses:**
- Shows you understand modularity
- Shows you think about reusability
- Shows you know dependency direction matters
- Shows you can name the principle (DIP)

---

### **Q5: How did you test this?**

**What they're testing:** Do you test strategically?

**Your answer:**
> "Two layers:
> 
> **Unit tests (no dependencies):** I test the sliding window formula with hardcoded values. No Redis, no network. 50 test cases run in 500ms. This catches algorithm bugs immediately.
> 
> **Integration tests (real Redis):** I use TestContainers to spin up a real Redis instance. I make actual HTTP requests to the API and verify 429 responses and Retry-After headers. This proves end-to-end functionality.
> 
> Why both? Unit tests catch logic bugs fast. Integration tests catch infrastructure bugs. Together, they give me confidence."

**Why this answer impresses:**
- Shows you understand testing strategy
- Shows you know fast feedback (unit tests) is better than slow feedback (integration)
- Shows you know both are needed
- Shows you use professional tools (TestContainers)

---

### **Q6: What trade-offs did you make?**

**What they're testing:** Do you think critically about design?

**Your answer:**
> "A few key trade-offs:
> 
> 1. **Async over sync:** More code complexity now, but correct I/O semantics. No threading issues.
> 
> 2. **Graceful degradation over fail-hard:** Less accurate (in-memory fallback isn't distributed), but the API stays up. Availability > Accuracy.
> 
> 3. **Three projects over one:** More files to manage, but cleaner dependency direction and better testability.
> 
> 4. **Config-driven over hardcoded:** More setup (multiple appsettings.json files), but per-environment configuration without recompilation.
> 
> In each case, I chose the design that's more maintainable and production-ready, even if it requires a bit more upfront effort."

**Why this answer impresses:**
- Shows you think about trade-offs (not every decision is obvious)
- Shows you can articulate the cost and benefit of each choice
- Shows you prioritize maintainability and production readiness

---

### **Q7: Explain the algorithm**

**What they're testing:** Do you understand the sliding window concept?

**Your answer:**
> "Sliding window counter estimates how many requests fall within the current time window.
> 
> Formula: `estimated_count = (previous_count × (1 - elapsed / window_size)) + current_count`
> 
> Example: If the window is 60 seconds, 30 seconds have elapsed, and we had 100 requests before, we estimate 50 of them are still in the window (they're 30+ seconds old). Add the current request, and we have 51.
> 
> Why sliding window? Fixed window has a boundary burst problem. At the edge of two windows, a user can make double requests. Token bucket and leaky bucket have other issues. Sliding window is accurate and atomicity is easy in Redis."

**Why this answer impresses:**
- Shows you understand the algorithm deep down
- Shows you can explain it clearly
- Shows you understand why this algo over others

---

## 📋 Summary Decisions Made

This section summarizes all architectural decisions. When you implement, refer back to these.

### **Architecture Decisions (Locked)**

| Decision | Option Chosen | Why | Trade-off |
|----------|---------------|-----|-----------|
| **Implementation Route** | Route A (Bottom-Up) | Test Core first, then Redis, then Middleware, then Api. Clean dependency direction. | Slower "visible progress" early on, but correct architecture |
| **Async/Sync** | Async from the start | Redis I/O is async. ASP.NET expects async. No refactoring later. | Slightly more code (Task wrapping), but correct semantics |
| **Core Design** | Core takes previous count as parameter | Zero dependencies. Pure algorithm. Easy to test. Easy to swap storage. | IRateLimiter takes more parameters (not as "simple" looking) |
| **Lua Script Location** | Separate `sliding-window.lua` file | Professional structure. Syntax highlighting. Easier to read and maintain. | One extra file to manage |
| **Redis Failure** | Graceful Degradation (in-memory fallback) | API stays up. Availability > Accuracy. | Less accurate per-process (not distributed) |
| **Middleware Order** | Per-IP first, per-user after authentication | Defense-in-depth. Protects unauthenticated endpoints. | Slightly more complex (two middleware instances) |
| **Configuration** | appsettings.json (environment-specific) | Production standard. Per-environment without recompilation. | Need multiple config files |

---

### **Project Structure (Locked)**

```
RateLimiter.Core (zero dependencies)
  ├── Interfaces/
  │   ├── IRateLimiter.cs
  │   └── IStrategyKeyResolver.cs
  ├── Models/
  │   ├── RateLimitConfig.cs
  │   ├── RateLimitResult.cs
  │   └── RateLimitExceeded.cs
  ├── Enums/
  │   ├── RateLimitStrategy.cs
  │   └── RateLimitStatus.cs
  └── Algorithms/
      └── SlidingWindowCalculator.cs

RateLimiter.Redis (depends on Core)
  ├── Implementations/
  │   └── RedisRateLimiter.cs
  ├── Fallbacks/
  │   └── InMemoryRateLimiter.cs
  ├── sliding-window.lua
  └── Configuration/
      └── RedisOptions.cs

RateLimiter.Middleware (depends on Core)
  ├── RateLimitMiddleware.cs
  └── ServiceCollectionExtensions.cs

RateLimiter.Api (depends on Core, Redis, Middleware)
  ├── Program.cs
  ├── appsettings.json
  ├── appsettings.Development.json
  ├── appsettings.Production.json
  └── Controllers/
      └── TestController.cs

RateLimiter.Tests
  ├── Unit/ (Core algorithm tests, no Redis)
  │   └── SlidingWindowCalculatorTests.cs
  └── Integration/ (Full end-to-end with TestContainers)
      └── RateLimitIntegrationTests.cs
```

---

### **Implementation Sequence**

1. **PHASE 1:** Core (interfaces, models, algorithm)
2. **PHASE 2:** Unit tests (Core logic only)
3. **PHASE 3:** Redis (implementation, Lua script, fallback)
4. **PHASE 4:** Middleware (ASP.NET wiring)
5. **PHASE 5:** Api (DI setup, config, wiring)
6. **PHASE 6:** Integration tests (TestContainers)

---

### **NuGet Packages to Add**

| Project | Packages |
|---------|----------|
| RateLimiter.Core | (None — zero dependencies) |
| RateLimiter.Redis | StackExchange.Redis (2.7.0 or latest) |
| RateLimiter.Middleware | Microsoft.AspNetCore.Http (implicit from Web SDK) |
| RateLimiter.Api | Swashbuckle.AspNetCore (already present), StackExchange.Redis |
| RateLimiter.Tests | xunit, xunit.runner.visualstudio, coverlet.collector (already present), TestContainers, TestContainers.Redis |

---

### **Project References to Add**

```
RateLimiter.Api → RateLimiter.Core
RateLimiter.Api → RateLimiter.Redis
RateLimiter.Api → RateLimiter.Middleware

RateLimiter.Redis → RateLimiter.Core

RateLimiter.Middleware → RateLimiter.Core

RateLimiter.Tests → RateLimiter.Core
RateLimiter.Tests → RateLimiter.Redis
RateLimiter.Tests → RateLimiter.Api
```

---

### **Key Points to Remember During Implementation**

1. **Core is pure algorithm** — No `using` statements for Redis, storage, or ASP.NET
2. **Test Core first** — Unit tests with hardcoded values before touching Redis
3. **Lua script is critical** — Atomicity prevents race conditions
4. **Graceful degradation** — API stays up even if Redis fails
5. **Config-driven** — Limits come from appsettings.json, not code
6. **Async all the way** — No mixing sync/async; no `GetResult()` hacks
7. **Interview ready** — Every decision above has a defense; every class has a purpose

---

## 🚀 Next Steps (When Ready to Code)

Before coding:
1. Review this document to ensure you agree with all decisions
2. Clarify any points that are unclear
3. Once aligned, proceed to PHASE 1: Core implementation

When you're ready, I'll guide you through building Core (interfaces, models, algorithm) with the reasoning behind each class and decision point.

