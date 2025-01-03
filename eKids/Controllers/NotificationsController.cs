using Database.Context;
using Database.DTOs;
using Database.Models;
using Database.Repository;
using eKids.Hubs;
using eKids.Shared.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace eKids.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly ILogger<NotificationsController> _logger;
        private readonly IRepository<Notifications> _notificationsRepository;
        private readonly IHubContext<NotificationsHub> _notificationsHub;
        private readonly IRepository<Friendships> _friendshipRepository;
        private readonly ApplicationDbContext _context;
        private readonly IRepository<Users> _userRepository;
        private static readonly ConnectionMapping _connectionMapping = new();

        public NotificationsController(
            ILogger<NotificationsController> logger,
            ApplicationDbContext context,
            IRepository<Notifications> notificationsRepository,
            IHubContext<NotificationsHub> notificationshHub,
            IRepository<Friendships> friendshipRepository,
            IRepository<Users> userRepository
            )
        {
            _logger = logger;
            _context = context;
            _notificationsRepository = notificationsRepository;
            _notificationsHub = notificationshHub;
            _friendshipRepository = friendshipRepository;
            _userRepository = userRepository;
        }

        [HttpPost("/api/Notifications/Info")]
        public async Task<IActionResult> CreateNotificationInternal(CreateNotificationDto notificationDto, CancellationToken token)
        {
            try
            {
                //var username = User?.Identity?.Name;
                var username = await _userRepository.Get(notificationDto.ReceiverId, token);
                var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = int.Parse(userId);

                var notification = new Notifications
                {
                    //UserId = notificationDto.UserId, KJO MUNET MU KON NULL PER SHKAK QE MUJN MU BO NOTIFICATIONSA PSH PREJ SISTEMIT
                    ReceiverId = notificationDto.ReceiverId, //KJO SMUN ME KON NULL SE E MERR NOTIFICATIONIN
                    Information = notificationDto.Information,
                    Type = NotificationsType.Info,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };
                _notificationsRepository.Add(notification);
                await _notificationsRepository.SaveAsync(token);

                if (!string.IsNullOrEmpty(username.Username))
                {
                    var userConnected = _connectionMapping.GetConnectionId(username.Username);
                    if(userConnected != null)
                    {
                        //notification.IsRead = true;
                        //_notificationsRepository.Update(notification);
                        //await _notificationsRepository.SaveAsync(token);
                        await _notificationsHub.Clients.Client(userConnected).SendAsync("ReceiveNotification", notification);
                        var unreads = await _notificationsRepository.GetAll().AsNoTracking().Where(c => c.ReceiverId == notificationDto.ReceiverId && c.IsRead == false).CountAsync(token);
                        await _notificationsHub.Clients.Client(userConnected).SendAsync("UnreadNotifications", unreads);
                    }
                }

                return Ok(new { Message = "Succesfully made action" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in creating notification");
                return BadRequest(new { Message = "Error creating notification" });
            }
        }

        [HttpPost("/api/Notifications/Warning")]
        public async Task<IActionResult> CreateNotificationInternalWarning(CreateNotificationDto notificationDto, CancellationToken token)
        {
            try
            {
                //var username = User?.Identity?.Name;
                var username = await _userRepository.Get(notificationDto.ReceiverId, token);
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = int.Parse(userId);

                var notification = new Notifications
                {
                    //UserId = notificationDto.UserId,
                    ReceiverId = notificationDto.ReceiverId,
                    Information = notificationDto.Information,
                    Type = NotificationsType.Warning,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };
                _notificationsRepository.Add(notification);
                await _notificationsRepository.SaveAsync(token);

                if (!string.IsNullOrEmpty(username.Username))
                {
                    var userConnected = _connectionMapping.GetConnectionId(username.Username);
                    if (userConnected != null)
                    {
                        //notification.IsRead = true;
                        //_notificationsRepository.Update(notification);
                        //await _notificationsRepository.SaveAsync(token);
                        await _notificationsHub.Clients.Client(userConnected).SendAsync("ReceiveNotification", notification);

                        var unreads = await _notificationsRepository.GetAll().AsNoTracking().Where(c => c.ReceiverId == notificationDto.ReceiverId && c.IsRead == false).CountAsync(token);
                        await _notificationsHub.Clients.Client(userConnected).SendAsync("UnreadNotifications", unreads);
                    }
                }

                return Ok(new { Message = "Sended action" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " error in sending action");
                return BadRequest(new { Message = "Error in sending action" });
            }
        }

        [HttpPost("/api/Notifications/UserFriendReq")]
        public async Task<IActionResult> CreateNotificationUserRequest(CreateNotificationDto notificationDto, CancellationToken token)
        {
            try
            {
                var username = await _userRepository.Get(notificationDto.ReceiverId, token);
                var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = int.Parse(userId);

                var notification = new Notifications
                {
                    UserId = notificationDto.UserId,
                    ReceiverId = notificationDto.ReceiverId,
                    Information = "Kerkese miqesie",
                    IsRead = false,
                    Type = NotificationsType.UserFriendReq,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                };

                _notificationsRepository.Add(notification);
                await _notificationsRepository.SaveAsync(token);

                var friendship = new Friendships
                {
                    SenderId = notificationDto.UserId.Value,
                    ReceiverId = notificationDto.ReceiverId,
                    NotificationId = notification.ID,
                    Status = Database.Shared.Enums.FriendshipStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };
                _friendshipRepository.Add(friendship);
                //_context.Attach(friendship);
                _context.Entry(friendship).State = EntityState.Added;
                await _friendshipRepository.SaveAsync(token);

                if (!string.IsNullOrEmpty(username.Username))
                {
                    var userConnected = _connectionMapping.GetConnectionId(username.Username);
                    if (userConnected != null)
                    {
                        //notification.IsRead = true;
                        //_notificationsRepository.Update(notification);
                        //await _notificationsRepository.SaveAsync(token);
                        await _notificationsHub.Clients.Client(userConnected).SendAsync("ReceiveNotification", notification);
                        var unreads = await _notificationsRepository.GetAll().AsNoTracking().Where(c => c.ReceiverId == notificationDto.ReceiverId && c.IsRead == false).CountAsync(token);
                        await _notificationsHub.Clients.Client(userConnected).SendAsync("UnreadNotifications", unreads);
                    }
                }

                return Ok(new { Message = "Action sended" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in creating notification with user req");
                return BadRequest(new { Message = "Error in sending action" });
            }
        }

        [HttpPost("/api/Notifications/UserActionReq")]
        public async Task<IActionResult> CreateNotificationUserActionReq(CreateNotificationDto notificationDto, CancellationToken token)
        {
            try
            {
                var username = await _userRepository.Get(notificationDto.ReceiverId, token);
                var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = int.Parse(userId);
                var notification = new Notifications
                {
                    UserId = notificationDto.UserId,
                    ReceiverId = notificationDto.ReceiverId,
                    Information = "Action to be made",
                    Type = NotificationsType.UserActionReq,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                };
                _notificationsRepository.Add(notification);
                await _notificationsRepository.SaveAsync(token);

                if (!string.IsNullOrEmpty(username.Username))
                {
                    var userConnected = _connectionMapping.GetConnectionId(username.Username);
                    if (userConnected != null)
                    {
                        //notification.IsRead = true;
                        //_notificationsRepository.Update(notification);
                        //await _notificationsRepository.SaveAsync(token);
                        await _notificationsHub.Clients.Client(userConnected).SendAsync("ReceiveNotification", notification);
                        var unreads = await _notificationsRepository.GetAll().AsNoTracking().Where(c => c.ReceiverId == notificationDto.ReceiverId && c.IsRead == false).CountAsync(token);
                        await _notificationsHub.Clients.Client(userConnected).SendAsync("UnreadNotifications", unreads);
                    }
                }

                return Ok(new { Message = "Action completed" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in creating notification for user actiokn req");
                return BadRequest(new { Message = "Error in creating notification user action req" });
            }
        }

        [HttpGet("/api/Notifications/MakeReadNotifications")]
        public async Task<IActionResult> MakeReads(CancellationToken token)
        {
            try
            {
                var username = User?.Identity?.Name;
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = int.Parse(userId);
                var unReads = await _notificationsRepository.GetAll().Where(c => c.ReceiverId == user && c.IsRead == false).ToListAsync(token);
                if(unReads.Count != 0)
                {
                    foreach (var item in unReads)
                    {
                        item.IsRead = true;
                    }
                    _notificationsRepository.UpdateRange(unReads);
                    await _notificationsRepository.SaveAsync(token);

                    if (!string.IsNullOrEmpty(username))
                    {
                        var userConnected = _connectionMapping.GetConnectionId(username);
                        if(userConnected != null)
                        {
                            var countNotifications = await _notificationsRepository.GetAll().AsNoTracking().Where(c => c.ReceiverId == user && c.IsRead == false).CountAsync(token);
                            await _notificationsHub.Clients.Client(userConnected).SendAsync("UnreadNotifications", countNotifications);
                        }
                    }
                }


                return Ok(new {Message = "Notifications readed"});
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in making reads");
                return BadRequest(new { Message = "Error in making reads" });
            }
        }

        [HttpGet("/api/Notifications/{userId}")]
        public async Task<IActionResult> GetNotificationByUser(int userId, CancellationToken token)
        {
            try
            {
                var notifications = await _notificationsRepository
                    .GetAll()
                    .AsNoTracking()
                    .Where(c => c.ReceiverId == userId)
                    .Include(c => c.User)
                    .Include(c => c.NotificationReceiver)
                    .Select(c => new
                    {
                        c.ID,
                        c.Information,
                        c.UserId,
                        c.ReceiverId,
                        c.Type,
                        c.IsRead,
                        NotificationSender = new
                        {
                            Name = c.User.Firstname + " " + c.User.Lastname,
                            ProfilePicture = c.User.ProfilePictureUrl
                        },
                        NotificationReceiver = new
                        {
                            Name = c.NotificationReceiver.Firstname + " " + c.NotificationReceiver.Lastname,
                            ProfilePicture = c.NotificationReceiver.ProfilePictureUrl
                        },
                        c.CreatedAt
                    })
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync(token);

                if (notifications.Count == 0)
                {
                    return NotFound(new { Message = "No notifications found" });
                }
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in retriving notifications for user {userId}");
                return BadRequest(new { Message = "Error in retriving notifications" });
            }
        }

        [HttpGet("/api/Notificaions/GetById/{id}")]
        public async Task<IActionResult> GetNotificationsById(int id, CancellationToken token)
        {
            try
            {
                var notifications = await _notificationsRepository.Get(id, token, c => c.User, c => c.NotificationReceiver);
                if (notifications == null)
                {
                    return NotFound(new { Message = "No notifications found" });
                }
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in retriving notifications with id {id}");
                return BadRequest(new { Message = "Error in retriving notifications by id" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(int id, [FromQuery] int userId, CancellationToken token)
        {
            try
            {
                var notification = await _notificationsRepository.GetAll().FirstOrDefaultAsync(c => c.ID == id && c.ReceiverId == userId, token);
                if(notification == null)
                {
                    return NotFound(new { Message = "No notification found" });
                }
                await _notificationsRepository.Delete(id, token);
                await _notificationsRepository.SaveAsync(token);
                return Ok(new { Message = "Notification deltetd" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in deleting notification with id {id}");
                return BadRequest(new { Message = "Error in deleting notification" });
            }
        }
    }
}
