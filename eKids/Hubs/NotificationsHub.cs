using Database.Context;
using Database.Models;
using eKids.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace eKids.Hubs
{
    [Authorize]
    public class NotificationsHub : Hub
    {
        private readonly ApplicationDbContext _context;
        private static readonly ConnectionMapping _connectionMapping = new ();
        private readonly ILogger<NotificationsHub> _logger;

        public NotificationsHub(ApplicationDbContext context, ILogger<NotificationsHub> logger)
        {
            _context = context;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            string username = Context?.User?.Identity?.Name;
            //var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier).Value;
            //var convertedUser = int.TryParse(userId, out var user);
            //var notificationsCount = await _context.Notifications.Where(c => c.ReceiverId == user && c.IsRead == false).CountAsync(default);

            if (!string.IsNullOrEmpty(username))
            {
                _connectionMapping.Add(username, Context.ConnectionId);
                //await Clients.Client(username).SendAsync("UnreadNotifications", notificationsCount);
            }
            await base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            string userId = Context?.User?.Identity?.Name;
            if (!string.IsNullOrEmpty(userId))
            {
                _connectionMapping.Remove(userId);
            }
            return base.OnDisconnectedAsync(exception);
        }

        public async Task NotificationsUnreadCount()
        {
            try
            {
                var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier).Value;
                var convertedUser = int.TryParse(userId, out var user);

                var username = Context?.User?.Identity?.Name;
                var notificationsCount = await _context.Notifications.Where(c => c.ReceiverId == user && c.IsRead == false).CountAsync(default);
                var connectedUser = _connectionMapping.GetConnectionId(username);
                if(connectedUser != null)
                {
                    await Clients.Client(connectedUser).SendAsync("UnreadNotifications", notificationsCount);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Error in sendign user notification bc it is unauthorized");
                throw new UnauthorizedAccessException("user not authorized", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in sending notification to user connected");
                throw new ApplicationException("Error in sending notification", ex);
            }
        }

        public async Task SendNotification(Notifications notifications)
        {
            try
            {
                var username = Context?.User?.Identity?.Name;
                var userId = Context.User?.FindFirst(JwtRegisteredClaimNames.Sub);
                if(userId != null && username != null)
                {
                    if(notifications.UserId.ToString() == userId.Value.ToString())
                    {
                        var userConnected = _connectionMapping.GetConnectionId(username);
                        if (userConnected != null)
                        {
                            await Clients.Client(userConnected).SendAsync("ReceiveNotification", notifications);
                        }
                    }
                }
                else
                {
                    _logger.LogError($"userid or username missing userID:{userId}, username:{username}");
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Error in sendign user notification bc it is unauthorized");
                throw new UnauthorizedAccessException("user not authorized", ex);
            }catch (Exception ex)
            {
                _logger.LogError(ex, "Error in sending notification to user connected");
                throw new ApplicationException("Error in sending notification", ex);
            }
        }
    }
}
