using Database.Context;
using Database.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Repository
{
    public class MeetingStatusBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MeetingStatusBackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

        public MeetingStatusBackgroundService(IServiceScopeFactory scopeFactory, ILogger<MeetingStatusBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Meeting Status Background Service is starting;");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        await CheckAndUpdateMeetingStatuses(dbContext);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while checking for meetings");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        private async Task CheckAndUpdateMeetingStatuses(ApplicationDbContext dbContext)
        {
            var now = DateTime.Now;
            var pendingMeetings = await dbContext.OnlineMeetings
                .Where(m => m.Status == MeetingStatus.Scheduled && m.ScheduleDateTime <= now)
                .ToListAsync();

            foreach (var meeting  in pendingMeetings)
            {
                var meetingEndTime = meeting.ScheduleDateTime.AddMinutes(meeting.DurationTime.Value);
                if (now < meetingEndTime && meeting.Status == MeetingStatus.Scheduled)
                {
                    //meeting.Status = MeetingStatus.Started;
                    _logger.LogInformation($"Meeting {meeting.ID} has not started yet");
                }
                else if (now == meetingEndTime || now > meetingEndTime && meeting.Status == MeetingStatus.Scheduled)
                {
                    meeting.Status = MeetingStatus.NotTookPlace;
                    _logger.LogInformation($"Meeting {meeting.ID} has not took place");
                }
                else if (now < meetingEndTime && meeting.Status == MeetingStatus.Started)
                {
                    _logger.LogInformation($"Meeting {meeting.ID} has started");
                }
                else if (now == meetingEndTime || now > meetingEndTime && meeting.Status == MeetingStatus.Started)
                {
                    meeting.Status = MeetingStatus.Completed;
                    _logger.LogInformation($"Meeting {meeting.ID} is completed");
                }
            }
            if (pendingMeetings.Count > 0)
            {
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
