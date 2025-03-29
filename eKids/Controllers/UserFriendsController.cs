using Database.Context;
using Database.DTOs;
using Database.Models;
using Database.Repository;
using Database.Shared.Enums;
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
    public class UserFriendsController : ControllerBase
    {
        private readonly IRepository<CloseFriends> _closeRepository;
        private readonly IRepository<Users> _usersRepository;
        private readonly IRepository<Friends> _friendsRepository;
        private readonly IRepository<Friendships> _friendShipsRepository;
        private readonly IRepository<Notifications> _notificationRepository;
        private readonly IHubContext<NotificationsHub> _notificationsHub;
        private readonly ApplicationDbContext _context;
        //private static readonly ConnectionMapping _connectionMapping = new();

        private readonly ILogger<UserFriendsController> _logger;

        public UserFriendsController(
            IRepository<CloseFriends> closeRepository,
            ApplicationDbContext context,
            ILogger<UserFriendsController> logger,
            IRepository<Users> usersRepository,
            IRepository<Friends> friendsRepository,
            IRepository<Friendships> friendshipRepository,
            IRepository<Notifications> notificationRepository,
            IHubContext<NotificationsHub> notificationsHub
            )
        {
            _closeRepository = closeRepository;
            _logger = logger;
            _usersRepository = usersRepository;
            _friendsRepository = friendsRepository;
            _friendShipsRepository = friendshipRepository;
            _notificationRepository = notificationRepository;
            _notificationsHub = notificationsHub;
            _context = context;
        }

        [HttpPost("/api/UserFriends/MakeCloseFriend")]
        public async Task<IActionResult> MakeBestFriend([FromBody] CloseFriendDto friendDto, CancellationToken token)
        {
            try
            {
                if(friendDto == null)
                {
                    return BadRequest(new { Message = "Data missing" });
                }

                var closeFriend = new CloseFriends
                {
                    UserId = friendDto.UserId,
                    CloseFriendId = friendDto.CloseFriendId,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                };

                _closeRepository.Add(closeFriend);
                await _closeRepository.SaveAsync(token);
                return Ok(closeFriend);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in making close friend");
                return BadRequest(new { Message = "Error in making close friend" });
            }
        }

        [HttpPost("/api/UserFriends/MakeFriend")]
        public async Task<IActionResult> MakeFriend(FriendDto friendDto, CancellationToken token)
        {
            try
            {
                if(friendDto == null)
                {
                    return BadRequest(new { Message = "No data" });
                }
                var friend1 = new Friends
                {
                    UserId = friendDto.UserId,
                    FriendId = friendDto.FriendId,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };
                _friendsRepository.Add(friend1);
                var friend2 = new Friends
                {
                    UserId = friendDto.FriendId,
                    FriendId = friendDto.UserId,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };
                _friendsRepository.Add(friend2);
                await _friendsRepository.SaveAsync(token);
                return Ok(new {Message = "Friend added"});
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in making friend");
                return BadRequest(new { Message = "Error in making friend" });
            }

        }

        [HttpPut("/api/UserFriends/AcceptFriendRequest")]
        public async Task<IActionResult> AcceptFriend([FromQuery] int senderId, [FromQuery] int receiverId, CancellationToken token)
        {
            try
            {
                var userIdAuth = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = int.TryParse(userIdAuth, out var currentUserID);

                var transaction = await _context.Database.BeginTransactionAsync(token);
                var username = await _context.Users.AsNoTracking().FirstOrDefaultAsync(c => c.ID == senderId, token);
                var friendship = await _context.Friendships.Where(c => c.SenderId == senderId && c.ReceiverId == receiverId).FirstOrDefaultAsync(token);
                var notificationFriendRequest = await _context.Notifications
                    .Where(c => c.UserId == senderId && c.ReceiverId == receiverId && c.Type == NotificationsType.UserFriendReq ||
                    c.UserId == receiverId && c.ReceiverId == senderId && c.Type == NotificationsType.UserFriendReq)
                    .FirstOrDefaultAsync(token);

                if (friendship == null)
                {
                    return NotFound(new { Message = "Friendship not found" });
                }
                friendship.Status = FriendshipStatus.Accepted;
                friendship.LastModified = DateTime.UtcNow;
                _context.Friendships.Update(friendship);
                await _context.SaveChangesAsync(token);

                var newFriend1 = new Friends
                {
                    UserId = senderId,
                    FriendId = receiverId,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };
                await _context.Friends.AddAsync(newFriend1, token);
                var newFriend2 = new Friends
                {
                    UserId = receiverId,
                    FriendId = senderId,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };
                await _context.Friends.AddAsync(newFriend2, token);
                await _context.SaveChangesAsync(token);

                if(notificationFriendRequest != null)
                {
                    _context.Notifications.Remove(notificationFriendRequest);
                }

                var notification = new Notifications
                {
                    UserId = receiverId,
                    ReceiverId = senderId,
                    Information = "Friend accepted",
                    Type = NotificationsType.UserFriendAccepted,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };
                await _context.Notifications.AddAsync(notification, token);
                await _context.SaveChangesAsync(token);
                var connectedUserId = ConnectionMapping.GetConnectionId(username?.Username);
                if(connectedUserId != null)
                {
                    var countNotifications = await _context.Notifications.Where(c => c.ReceiverId == senderId && c.IsRead == false).CountAsync(token);
                    await _notificationsHub.Clients.Client(connectedUserId).SendAsync("UnreadNotifications", countNotifications);
                }
                await transaction.CommitAsync(token);
                return Ok(new { Message = "Friend accepted successfully" });
            }
            catch (Exception ex)
            {
                if (_context.Database.CurrentTransaction != null)
                {
                    await _context.Database.RollbackTransactionAsync(token);
                }
                _logger.LogError(ex, "Error in accepting friend");
                return BadRequest(new { Message = "Error in accepting frined" });
            }
        }

        [HttpGet("/api/UserFriends/GetAllByUser/{userId}")]
        public async Task<IActionResult> GetAllCloseFriends(int userId, [FromQuery] UsersRelationshipTypes types, [FromQuery] PaginationDto paginationDto, CancellationToken token)
        {
            try
            {
                var currentUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(c => c.ID == userId);
                if(currentUser == null)
                {
                    return NotFound(new { Message = "User not found" });
                }

                IQueryable<object>? result = null;

                switch (types)
                {
                    case UsersRelationshipTypes.All:
                        result = _usersRepository
                            .GetAll()
                            .AsSplitQuery()
                            .AsNoTracking()
                            .OrderBy(c => c.ID)
                            .Select(c => new
                            {
                                c.ID,
                                c.Firstname,
                                c.Lastname,
                                c.Email,
                                c.Age,
                                c.ProfilePictureUrl,
                                c.CreatedAt,
                                c.Username,
                                LastMessage = new
                                {
                                    Message = c.ReceivedMessages
                                    .Where(ru => ru.ReceiverUsername == currentUser.Username && ru.SenderUsername == c.Username ||
                                    ru.ReceiverUsername == c.Username && ru.SenderUsername == currentUser.Username)
                                    .Union(c.SentMessages.Where(su => su.SenderUsername == c.Username && su.ReceiverUsername == currentUser.Username ||
                                    su.SenderUsername == currentUser.Username && su.ReceiverUsername == c.Username))
                                    .Select(s => new
                                    {
                                        s.SenderUsername,
                                        s.ReceiverUsername,
                                        s.Content,
                                        s.IsRead,
                                        s.BlogId,
                                        s.LessonId,
                                        s.CourseId,
                                        s.QuizId,
                                        s.CreatedAt
                                    })
                                    .OrderByDescending(c => c.CreatedAt)
                                    .FirstOrDefault()
                                }
                            });
                        break;
                    case UsersRelationshipTypes.Friends:
                        result = _friendsRepository
                            .GetAll()
                            .AsSplitQuery()
                            .AsNoTracking()
                            .OrderBy(c => c.ID)
                            .Where(c => c.UserId == userId)
                            .Include(c => c.Friend)
                            .Include(c => c.User)
                            .Select(c => new
                            {
                                c.Friend.ID,
                                c.Friend.Firstname,
                                c.Friend.Lastname,
                                c.Friend.Email,
                                c.Friend.Age,
                                c.Friend.ProfilePictureUrl,
                                c.Friend.CreatedAt,
                                c.Friend.Username,
                                LastMessage = new
                                {
                                    Message = c.Friend.ReceivedMessages
                                    .Where(ru => ru.ReceiverUsername == currentUser.Username && ru.SenderUsername == c.Friend.Username ||
                                    ru.ReceiverUsername == c.Friend.Username && ru.SenderUsername == currentUser.Username)
                                    .Union(c.Friend.SentMessages.Where(su => su.SenderUsername == c.Friend.Username && su.ReceiverUsername == currentUser.Username ||
                                    su.SenderUsername == currentUser.Username && su.ReceiverUsername == c.Friend.Username))
                                    .Select(s => new
                                    {
                                        s.SenderUsername,
                                        s.ReceiverUsername,
                                        s.Content,
                                        s.IsRead,
                                        s.BlogId,
                                        s.LessonId,
                                        s.CourseId,
                                        s.QuizId,
                                        s.CreatedAt
                                    })
                                    .OrderByDescending(c => c.CreatedAt)
                                    .FirstOrDefault()
                                }
                            });                            
                        break;
                    case UsersRelationshipTypes.CloseFriends:
                        result = _closeRepository
                            .GetAll()
                            .AsSplitQuery()
                            .AsNoTracking()
                            .OrderBy(c => c.ID)
                            .Where(c => c.UserId == userId)
                            .Include(c => c.CloseFriend)
                            .Select(c => new
                            {
                                c.CloseFriend.ID,
                                c.CloseFriend.Firstname,
                                c.CloseFriend.Lastname,
                                c.CloseFriend.Email,
                                c.CloseFriend.Age,
                                c.CloseFriend.ProfilePictureUrl,
                                c.CloseFriend.CreatedAt,
                                c.CloseFriend.Username,
                                LastMessage = new
                                {
                                    Message = c.CloseFriend.ReceivedMessages
                                    .Where(ru => ru.ReceiverUsername == currentUser.Username && ru.SenderUsername == c.CloseFriend.Username ||
                                    ru.ReceiverUsername == c.CloseFriend.Username && ru.SenderUsername == currentUser.Username)
                                    .Union(c.CloseFriend.SentMessages.Where(su => su.SenderUsername == c.CloseFriend.Username && su.ReceiverUsername == currentUser.Username ||
                                    su.SenderUsername == currentUser.Username && su.ReceiverUsername == c.CloseFriend.Username))
                                    .Select(s => new
                                    {
                                        s.SenderUsername,
                                        s.ReceiverUsername,
                                        s.Content,
                                        s.IsRead,
                                        s.BlogId,
                                        s.LessonId,
                                        s.CourseId,
                                        s.QuizId,
                                        s.CreatedAt
                                    })
                                    .OrderByDescending(c => c.CreatedAt)
                                    .FirstOrDefault()
                                }
                            });
                        break;
                    default:
                        break;
                }
                var paginatedQuery = result.Skip(paginationDto.Skip).Take(paginationDto.Take);
                if(await paginatedQuery.AnyAsync(token))
                {
                    var response = await paginatedQuery.ToListAsync(token);
                    return Ok(response);
                }
                else
                {
                    return NotFound(new { Message = "No data found" });
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in retriving all close friends with user id: {userId}");
                return BadRequest(new { Message = "Error in retriving close friends" });
            }
        }

        [HttpGet("/api/UserFriends/SearchUsers/")]
        public async Task<IActionResult> SearchUsers([FromQuery] int userId, [FromQuery] string searchParam, CancellationToken token)
        {
            try
            {
                var users = await _usersRepository
                    .GetAll()
                    .AsNoTracking()
                    .Where(c => c.Firstname.Contains(searchParam) || c.Lastname.Contains(searchParam))
                    .Select(c => new
                    {
                        c.ID,
                        Name = c.Firstname + " " + c.Lastname,
                        c.ProfilePictureUrl,
                        c.Email,
                        IsFriend = c.Friends.Any(f => f.UserId == userId && f.FriendId == c.ID),
                        IsCloseFriend = c.CloseFriends.Any(cl => cl.UserId == userId && cl.CloseFriendId == c.ID)
                    })
                    .ToListAsync(token);
                //var friends = await _friendsRepository.GetAll().AsNoTracking().Where(c => c.UserId == userId).ToListAsync(token);
                //var closeFriends = await _closeRepository.GetAll().AsNoTracking().Where(c => c.UserId == userId).ToListAsync(token);

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in retriving users");
                return BadRequest(new { Message = "Error in retriving users" });
            }
        }

        [HttpGet("/api/UserFriends/GetUserRelationStatus")]
        public async Task<IActionResult> GetUserRelationStatus([FromQuery] FriendshipDto friendDto, CancellationToken token)
        {
            try
            {
                // Validate the input DTO
                if (friendDto == null)
                {
                    return BadRequest(new { Message = "FriendDto is null or missing" });
                }

                if (friendDto.SenderId == null || friendDto.ReceiverId == null)
                {
                    return BadRequest(new { Message = "SenderId or ReceiverId is missing or invalid" });
                }

                // Query the database
                var friendship = await _friendShipsRepository
                    .GetAll()
                    .AsNoTracking()
                    .Select(c => new
                    {
                        c.Status,
                        c.SenderId,
                        c.ReceiverId
                    })
                    .FirstOrDefaultAsync(c => c.SenderId == friendDto.SenderId && c.ReceiverId == friendDto.ReceiverId, token);

                // Check if the friendship exists
                if (friendship == null)
                {
                    return NotFound(new { Message = "No relation found" });
                }

                // Return the friendship data
                return Ok(friendship);
            }
            catch (Exception ex)
            {
                // Log the error and return a generic error response
                _logger.LogError(ex, "Error in retrieving relationshipStatus");
                return BadRequest(new { Message = "Error in retrieving relationshipStatus" });
            }
        }

        [HttpDelete("/api/UserFriends/DeleteCloseFriend/{id}")]
        public async Task<IActionResult> DeleteCloseFriend(int id, CancellationToken token)
        {
            try
            {
                var closefriend = await _closeRepository.Get(id, token);
                if(closefriend == null)
                {
                    return NotFound(new { Message = "CloseFriend not found" });
                }
                await _closeRepository.Delete(closefriend.ID, token);
                await _closeRepository.SaveAsync(token);
                return Ok(new { Message = "Close friend deleted" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in deleting close friend with column ID: {id}");
                return BadRequest(new { Message = "Error in deleting close friend" });
            }
        }

        [HttpDelete("/api/UserFriends/DeleteFriend/")]
        public async Task<IActionResult> DeleteFriend([FromQuery] int userId, [FromQuery] int friendId, CancellationToken token)
        {
            try
            {
                using var transation = await _context.Database.BeginTransactionAsync(token);
                var friend = await _context.Friends.Where(c => c.UserId == userId && c.FriendId == friendId || c.UserId == friendId && c.FriendId == userId).ToListAsync(token);
                var friendships = await _context.Friendships
                    .Where(c => c.SenderId == userId && c.ReceiverId == friendId || c.SenderId == friendId && c.ReceiverId == userId)
                    .FirstOrDefaultAsync(token);
                var notifications = await _context.Notifications
                    .Where(c => c.UserId == userId && c.ReceiverId == friendId && c.Type == NotificationsType.UserFriendAccepted ||
                    c.UserId == friendId && c.ReceiverId == userId && c.Type == NotificationsType.UserFriendAccepted)
                    .FirstOrDefaultAsync(token);
                if (friend.Count == 0 || friendships == null)
                {
                    return NotFound(new { Message = "No friend found" });
                }
                if(notifications != null)
                {
                    _context.Notifications.Remove(notifications);
                }
                _context.Friendships.Remove(friendships);
                _context.Friends.RemoveRange(friend);
                await _context.SaveChangesAsync(token);
                await transation.CommitAsync(token);
                return Ok(new { Message = "Friend Deleted" });
            }
            catch (Exception ex)
            {
                await _context.Database.RollbackTransactionAsync(token);
                _logger.LogError(ex, $"Error deleting friend with");
                return BadRequest(new { Message = "Error deleting friend" });
            }
        }

        [HttpDelete("/api/UserFriends/DeleteFriendRequest")]
        public async Task<IActionResult> DeleteFriendRequest([FromQuery] int userId, [FromQuery] int receiverId, CancellationToken token)
        {
            try
            {
                var transation = await _context.Database.BeginTransactionAsync(token);
                var friendReq = await _context.Friendships.Where(c => c.SenderId == userId && c.ReceiverId == receiverId).FirstOrDefaultAsync(token);
                var notification = await _context.Notifications.Where(c => c.UserId == userId && c.ReceiverId == receiverId && c.Type == NotificationsType.UserFriendReq).FirstOrDefaultAsync(token);
                if(friendReq == null)
                {
                    return NotFound(new { Message = "Not found" });
                }
                _context.Friendships.Remove(friendReq);
                if(notification != null)
                {
                    _context.Notifications.Remove(notification);
                }
                await _context.SaveChangesAsync(token);
                await transation.CommitAsync(token);
                return Ok(new { Message = "Friendship deleted" });
            }
            catch (Exception ex)
            {
                if(_context.Database.CurrentTransaction != null)
                {
                    await _context.Database.RollbackTransactionAsync(token);
                }
                _logger.LogError(ex, "Error in deleting friend request");
                return BadRequest(new { Message = "Error in deleting friend request" });
            }
        }

        
    }
}
