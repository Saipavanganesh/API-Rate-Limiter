# API Rate Limiter

A production-ready rate limiting middleware for ASP.NET Core using the **sliding window counter** algorithm, backed by Redis for distributed deployments.

---

## Architecture

```
RateLimiter.Core          (zero dependencies)
  └── Algorithm, Interfaces, Models, Enums

RateLimiter.Redis         (→ Core)
  └── RedisRateLimiter, InMemoryRateLimiter (fallback), Lua script

RateLimiter.Middleware    (→ Core)
  └── RateLimitMiddleware, Strategy resolvers, DI extensions

RateLimiter.Api           (→ Core, Redis, Middleware)
  └── Host project — wires everything together

RateLimiter.Tests         (→ Core, Api)
  └── Unit tests (no Redis), Integration tests (TestContainers)
```

Dependency direction flows inward — Core has zero external dependencies. Redis and Middleware both depend on Core only. Api wires everything together.

---

## Algorithm — Sliding Window Counter

```
estimated_count = (prev_count × (1 - elapsed / window_size)) + current_count
```

**Example:** window=60s, elapsed=30s, prev_count=95, current request=1
```
95 × (1 - 30/60) = 95 × 0.5 = 47.5
47.5 + 1 = 48.5 → ceiling → 49
limit=100 → ALLOWED, remaining=51
```

**Why sliding window over alternatives:**

| Algorithm | Problem |
|-----------|---------|
| Fixed Window | Boundary burst — user can make 2× requests at window edges |
| Token Bucket | Complex atomic Redis implementation |
| Leaky Bucket | Adds queuing latency, better suited to network layer |
| **Sliding Window** | Accurate, no burst problem, atomic Redis ops |

**Atomicity:** Read + check + increment runs as a single Lua script in Redis — prevents TOCTOU race conditions.

---

## Rate Limiting Strategies

Same algorithm — only the Redis key changes:

| Strategy | Redis Key | Source |
|----------|-----------|--------|
| `PerIp` | `rate_limit:ip:{ip}` | `RemoteIpAddress` |
| `PerUser` | `rate_limit:user:{userId}` | JWT `sub` claim |
| `PerApiKey` | `rate_limit:apiKey:{key}` | `X-Api-Key` header |

Configure in `appsettings.json` — no code change needed.

---

## Key Design Decisions

| Decision | Chosen | Why |
|----------|--------|-----|
| Build order | Core → Redis → Middleware → Api | Test algorithm before touching infrastructure |
| Async/Sync | Async from start | Redis I/O requires it — can't retrofit cleanly |
| Core design | Pure function — takes prevCount as param | Zero dependencies, fully testable in isolation |
| Lua location | Separate `sliding-window.lua` file | Syntax highlighting, version control history, clear separation |
| Redis failure | Graceful degradation → in-memory fallback | Availability > Accuracy — API stays up |
| Config | `appsettings.json` per environment | Per-environment limits without recompilation |

---

## How to Run

### Option 1 — Docker Compose (recommended)

Requires Docker Desktop.

```bash
docker-compose up --build
```

Spins up Redis and the API together. API available at `http://localhost:5000`.

### Option 2 — Local

Start Redis:
```bash
docker run -d -p 6379:6379 --name redis-ratelimiter redis:latest
```

Run the API:
```bash
dotnet run --project RateLimiter.Api
```

---

## Configuration

`appsettings.json`:

```json
{
  "RateLimiting": {
    "RequestsPerWindow": 10,
    "WindowSizeSeconds": 60,
    "Strategy": "PerIp",
    "Redis": {
      "ConnectionString": "localhost:6379"
    }
  }
}
```

Supported strategy values: `PerIp`, `PerUser`, `PerApiKey`

---

## API Responses

**Within limit — 200 OK:**
```
X-RateLimit-Remaining: 7
```

**Limit exceeded — 429 Too Many Requests:**
```
Retry-After: 42
Body: Rate limit exceeded. Please try again later.
```

---

## Running Tests

```bash
dotnet test
```

- **Unit tests** — algorithm tested with hardcoded values, no Redis required, runs in milliseconds
- **Integration tests** — TestContainers spins up a real Redis instance, makes actual HTTP requests, verifies 429 and Retry-After header

---

## Tech Stack

- .NET 8, C#
- Redis + StackExchange.Redis
- ASP.NET Core Middleware
- xUnit + TestContainers
- Docker Compose
