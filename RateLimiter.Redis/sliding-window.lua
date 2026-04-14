  -- Sliding Window Rate Limiter
  -- KEYS[1] = current window key  (e.g. "rl:127.0.0.1:1712900000000")
  -- KEYS[2] = previous window key (e.g. "rl:127.0.0.1:1712899940000")
  -- ARGV[1] = limit           (max requests allowed per window)
  -- ARGV[2] = elapsed_ms      (milliseconds elapsed in the current window)
  -- ARGV[3] = window_ms       (total window size in milliseconds)
  -- ARGV[4] = ttl_seconds     (how long to keep keys in Redis, 2x window)
  --
  -- Returns: { allowed, requests_remaining, retry_after_seconds }
  --   allowed = 1 (request is OK) or 0 (limit exceeded)

  local curr_count = tonumber(redis.call('GET', KEYS[1])) or 0
  local prev_count = tonumber(redis.call('GET', KEYS[2])) or 0
  local limit      = tonumber(ARGV[1])
  local elapsed_ms = tonumber(ARGV[2])
  local window_ms  = tonumber(ARGV[3])
  local ttl        = tonumber(ARGV[4])

  local weight    = 1 - (elapsed_ms / window_ms)
  local estimated = math.floor(prev_count * weight + curr_count)

  if estimated >= limit then
      local retry_after = math.ceil((window_ms - elapsed_ms) / 1000)
      return {0, 0, retry_after}
  end

  redis.call('INCR', KEYS[1])
  redis.call('EXPIRE', KEYS[1], ttl)

  return {1, limit - estimated - 1, 0}