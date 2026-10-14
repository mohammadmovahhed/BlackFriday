-- KEYS[1] = inventory remaining key
-- KEYS[2] = reservation key
-- KEYS[3] = active set key
-- ARGV[1] = quantity
-- ARGV[2] = reservation TTL seconds
-- ARGV[3] = reservation token

local remaining = redis.call('GET', KEYS[1])
if not remaining then
  return {err = 'PROMO_NOT_INITIALIZED'}
end

remaining = tonumber(remaining)
local qty = tonumber(ARGV[1])

if remaining < qty then
  return {0, remaining}
end

redis.call('DECRBY', KEYS[1], qty)
redis.call('SET', KEYS[2], qty, 'EX', tonumber(ARGV[2]))
redis.call('SADD', KEYS[3], ARGV[3])

return {1, remaining - qty}