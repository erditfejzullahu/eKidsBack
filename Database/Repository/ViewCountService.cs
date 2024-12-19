using Microsoft.EntityFrameworkCore.Storage;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Repository
{
    public class ViewCountService : IViewCountService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly StackExchange.Redis.IDatabase _db;

        public ViewCountService(IConnectionMultiplexer redis)
        {
            _redis = redis;
            _db = _redis.GetDatabase();
        }

        public void IncrementViewCount(int id, string entityType)
        {
            // Determine Redis key based on entity type (Course or Lesson)
            string redisKey = $"{entityType}:{id}:views";
            _db.StringIncrement(redisKey);  // Atomic increment in Redis
        }

        public async Task<int> GetViewCountAsync(int id, string entityType)
        {
            // Determine Redis key based on entity type (Course or Lesson)
            string redisKey = $"{entityType}:{id}:views";
            var count = await _db.StringGetAsync(redisKey);
            return (int)count;
        }
    }
}
