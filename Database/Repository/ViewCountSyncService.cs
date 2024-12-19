using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Database.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Database.Context;
using Database.Models;

public class ViewCountSyncService : BackgroundService
{
    private readonly IViewCountService _viewCountService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;

    public ViewCountSyncService(IViewCountService viewCountService, IServiceScopeFactory scopeFactory, IConnectionMultiplexer redis)
    {
        _viewCountService = viewCountService;
        _scopeFactory = scopeFactory;
        _redis = redis;
        _db = _redis.GetDatabase();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await SyncViewCountsToDatabaseAsync();
            await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken); // Run every 10 minutes
        }
    }

    public async Task SyncViewCountsToDatabaseAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Get all Redis keys that store view counts for courses and lessons
        var server = _redis.GetServer("192.168.1.16", 6379);
        var redisKeys = server.Keys(pattern: "*:*:views");

        var updateTasks = new List<Task>();

        foreach (var redisKey in redisKeys)
        {
            // Extract entity type (course or lesson) and id from the Redis key
            var keyParts = redisKey.ToString().Split(':');
            var entityType = keyParts[0]; // "course" or "lesson"
            int entityId = int.Parse(keyParts[1]);
            int viewCount = (int)_db.StringGet(redisKey);

            if (entityType == "course")
            {
                // Handle Course view count update
                var course = await dbContext.Courses.FirstOrDefaultAsync(c => c.ID == entityId);
                if (course != null)
                {
                    course.ViewCount += viewCount;
                    updateTasks.Add(dbContext.SaveChangesAsync());
                }
            }
            else if (entityType == "lesson")
            {
                // Handle Lesson view count update
                var lesson = await dbContext.Lessons.FirstOrDefaultAsync(l => l.ID == entityId);
                if (lesson != null)
                {
                    lesson.ViewCount += viewCount;
                    updateTasks.Add(dbContext.SaveChangesAsync());
                }
            }
            else if(entityType == "category")
            {
                var category = await dbContext.Categories.FirstOrDefaultAsync(c => c.ID == entityId);
                if(category != null)
                {
                    category.ViewCount += viewCount;
                    updateTasks.Add(dbContext.SaveChangesAsync());
                }
            }

            // Reset view count in Redis after syncing
            _db.KeyDelete(redisKey);
        }

        // Wait for all update tasks to complete
        await Task.WhenAll(updateTasks);
    }
}
