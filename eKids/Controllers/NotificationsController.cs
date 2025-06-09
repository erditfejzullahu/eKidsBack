using Database.Context;
using Database.DTOs;
using Database.Models;
using Database.Repository;
using eKids.Hubs;
using eKids.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
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
        private static readonly ConnectionMapping _connectionMapping = new ConnectionMapping();

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


        [Authorize]
        [HttpPost("/api/Notifications/UserFriendReq")]
        public async Task<IActionResult> CreateNotificationUserRequest(CreateNotificationDto notificationDto, CancellationToken token)
        {
            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync(token);
                var user = User?.FindFirstValue(ClaimTypes.NameIdentifier);
                if(string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int currentUserId))
                {
                    return Unauthorized();
                }

                var friendRequestReceiver = await _context.Users.AsNoTracking().Where(c => c.ID == notificationDto.ReceiverId).FirstOrDefaultAsync(token);
                if(friendRequestReceiver == null)
                {
                    return NotFound(new {Message = "Receiver user not found"});
                }

                var sender = await _context.Users.AsNoTracking().FirstOrDefaultAsync(c => c.ID == currentUserId);
                if (sender == null) {
                    return NotFound(new { Message = "Sender user not found" });
                }

                var notification = new Notifications
                {
                    UserId = currentUserId,
                    ReceiverId = notificationDto.ReceiverId,
                    Information = "Kerkese miqesie",
                    IsRead = false,
                    Type = NotificationsType.FriendRequestReceived,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                };

                await _context.Notifications.AddAsync(notification, token);
                //await _context.SaveChangesAsync(token);

                var friendship = new Friendships
                {
                    SenderId = currentUserId,
                    ReceiverId = notificationDto.ReceiverId,
                    Status = Database.Shared.Enums.FriendshipStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };
                await _context.Friendships.AddAsync(friendship, token);
                await _context.SaveChangesAsync(token);

                if (!string.IsNullOrEmpty(friendRequestReceiver.Username))
                {
                    var userConnected = ConnectionMapping.GetConnectionId(friendRequestReceiver.Username);
                    if (userConnected != null)
                    {
                        //notification.IsRead = true;
                        //_notificationsRepository.Update(notification);
                        //await _notificationsRepository.SaveAsync(token);
                        var query = _context.Notifications.AsNoTracking();
                        var sendNotification = await query
                            .Where(c => c.ID == notification.ID)
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
                            .FirstOrDefaultAsync();
                        if(sendNotification != null)
                        {
                            await _notificationsHub.Clients.Client(userConnected).SendAsync("ReceiveNotification", sendNotification);
                        }
                        var unreads = await query.Where(c => c.ReceiverId == notificationDto.ReceiverId && c.IsRead == false).CountAsync(token);
                        await _notificationsHub.Clients.Client(userConnected).SendAsync("UnreadNotifications", unreads);
                    }
                }
                await transaction.CommitAsync(token);
                return Ok(new { Message = "Action sended" });
            }
            catch (Exception ex)
            {
                if(_context.Database.CurrentTransaction != null)
                {
                await _context.Database.RollbackTransactionAsync(token);
                }
                _logger.LogError(ex, "Error in creating notification with user req");
                return BadRequest(new { Message = "Error in sending action" });
            }
        }


        //??? testing duhet me hek ose me bo admin only
        [Authorize(Roles = "Admin")]
        [HttpPost("/api/Notifications/UserActionReq")]
        public async Task<IActionResult> CreateNotificationUserActionReq(CreateNotificationDto notificationDto, CancellationToken token)
        {
            try
            {
                var username = await _userRepository.Get(notificationDto.ReceiverId, token);
                var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = int.TryParse(userId, out var currentUserId);
                var notification = new Notifications
                {
                    UserId = notificationDto.UserId,
                    ReceiverId = notificationDto.ReceiverId,
                    Information = notificationDto.Information,
                    Type = NotificationsType.UserActionReq,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                };
                _notificationsRepository.Add(notification);
                await _notificationsRepository.SaveAsync(token);

                if (!string.IsNullOrEmpty(username.Username))
                {
                    var userConnected = ConnectionMapping.GetConnectionId(username.Username);
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

        [Authorize]
        [HttpGet("/api/Notifications/MakeReadNotifications")]
        public async Task<IActionResult> MakeReads(CancellationToken token)
        {
            try
            {
                var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                if(string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int userId))
                {
                    return Unauthorized();
                }
                var userLogged = await _context.Users.AsNoTracking().FirstOrDefaultAsync(c => c.ID == userId);
                if(userLogged == null)
                {
                    return NotFound(new {Message ="no user found"});
                }
                var unReads = await _context.Notifications.Where(c => c.ReceiverId == userId && c.IsRead == false).ToListAsync(token);
                if(unReads.Count != 0)
                {
                    foreach (var item in unReads)
                    {
                        item.IsRead = true;
                    }
                    _context.Notifications.UpdateRange(unReads);
                    await _context.SaveChangesAsync(token);

                    var userConnected = ConnectionMapping.GetConnectionId(userLogged.Username);
                    if (userConnected != null)
                    {
                        await _notificationsHub.Clients.Client(userConnected).SendAsync("UnreadNotifications", 0);
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

        [Authorize]
        [HttpGet("/api/Notifications/")]
        public async Task<IActionResult> GetNotificationByUser(, CancellationToken token)
        {
                var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
            try
            {
                var username = User.FindFirstValue("Username");
                if(string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int userId))
                {
                    return Unauthorized();
                }

                var notifications = await _context.Notifications
                    .AsNoTracking()
                    //.AsSplitQuery()
                    .Where(c => c.ReceiverId == userId)
                    .OrderByDescending(c => c.CreatedAt)
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
                    .ToListAsync(token);

                if (notifications.Count == 0)
                {
                    return NotFound(new { Message = "No notifications found" });
                }

                if (!string.IsNullOrEmpty(username))
                {
                    var connectedUser = ConnectionMapping.GetConnectionId(username);
                    if (connectedUser != null)
                    {
                        await _notificationsHub.Clients.Client(connectedUser).SendAsync("UnreadNotifications", 0);   
                    }
                }

                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in retriving notifications for user {user}");
                return BadRequest(new { Message = "Error in retriving notifications" });
            }
        }

        [Authorize(Roles = "Admin")]
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

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(int id, CancellationToken token)
        {
            try
            {
                var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if(string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int userId))
                {
                    return Unauthorized();
                }

                var notification = await _context.Notifications.Where(c => c.ID == id && c.ReceiverId == userId).FirstOrDefaultAsync();
                if(notification == null)
                {
                    return NotFound(new { Message = "No notification found" });
                }
                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync(token);
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
