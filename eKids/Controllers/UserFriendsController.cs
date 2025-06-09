using Database.Context;
using Database.DTOs;
using Database.Models;
using Database.Repository;
using Database.Shared.Enums;
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

        [Authorize]
        [HttpPost("/api/UserFriends/MakeCloseFriend")]
        public async Task<IActionResult> MakeBestFriend([FromBody] CloseFriendDto friendDto, CancellationToken token)
        {
            try
            {
                var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if(string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int userId))
                {
                    return Unauthorized();
                }

                if(friendDto == null)
                {
                    return BadRequest(new { Message = "Data missing" });
                }

                var closeFriend = new CloseFriends
                {
                    UserId = userId,
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


        [Authorize(Roles = "Admin")]
        [HttpPost("/api/UserFriends/MakeFriend")]
        public async Task<IActionResult> MakeFriend(FriendDto friendDto, CancellationToken token)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(token);
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
                await _context.Friends.AddAsync(friend1);
                var friend2 = new Friends
                {
                    UserId = friendDto.FriendId,
                    FriendId = friendDto.UserId,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };
                await _context.Friends.AddAsync(friend2);
                await _context.SaveChangesAsync(token);
                await transaction.CommitAsync(token);
                return Ok(new {Message = "Friend added"});
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(token);
                _logger.LogError(ex, "Error in making friend");
                return BadRequest(new { Message = "Error in making friend" });
            }

        }

        [Authorize]
        [HttpPut("/api/UserFriends/AcceptFriendRequest")]
        public async Task<IActionResult> AcceptFriend([FromQuery] int senderId, [FromQuery] int receiverId, CancellationToken token)
        {

            using var transaction = await _context.Database.BeginTransactionAsync(token);
            try
            {
                var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int receiverUserId))
                {
                    return Unauthorized();
                }
                
                var friendshipRequestSender = await _context.Users.AsNoTracking().FirstOrDefaultAsync(c => c.ID == senderId, token);
                if(friendshipRequestSender == null)
                {
                    return NotFound(new {Message = "User not found"});
                }

                var friendship = await _context.Friendships.Where(c => c.SenderId == senderId && c.ReceiverId == receiverUserId).FirstOrDefaultAsync(token);
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
                //await _context.SaveChangesAsync(token);

                var newFriend1 = new Friends
                {
                    UserId = senderId,
                    FriendId = receiverUserId,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };
                await _context.Friends.AddAsync(newFriend1, token);

                var newFriend2 = new Friends
                {
                    UserId = receiverUserId,
                    FriendId = senderId,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };

                await _context.Friends.AddAsync(newFriend2, token);
                //await _context.SaveChangesAsync(token);

                if(notificationFriendRequest != null)
                {
                    _context.Notifications.Remove(notificationFriendRequest);
                }

                //njoftim qe ti ke pranu miqesine me senderIdn
                var youAcceptedNotification = new Notifications
                {
                    UserId = senderId,
                    ReceiverId = receiverUserId,
                    Information = "Njoftim mbi pranimin e miqesise",
                    Type = NotificationsType.FriendRequestReceiverAccepted,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };

                //njotim qe receiverid ka pranu miqesine me senderid
                var heGotInformationAboutYourAccept = new Notifications
                {
                    UserId = receiverUserId,
                    ReceiverId = senderId,
                    Information = "Njoftim mbi pranimin e miqesise",
                    Type = NotificationsType.FriendRequestSenderAccepted,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };

                await _context.Notifications.AddAsync(youAcceptedNotification, token);
                await _context.Notifications.AddAsync(heGotInformationAboutYourAccept, token);
                await _context.SaveChangesAsync(token);

                var connectedUserId = ConnectionMapping.GetConnectionId(friendshipRequestSender.Username);
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

                await transaction.RollbackAsync(token);
                _logger.LogError(ex, "Error in accepting friend");
                return BadRequest(new { Message = "Error in accepting frined" });
            }
        }

        [HttpGet("/api/UserFriends/GetAllByUser/{userId}")]
        public async Task<IActionResult> GetAllCloseFriends(int userId, [FromQuery] UsersRelationshipTypes types, [FromQuery] PaginationDto paginationDto, [FromQuery] string? searchParam, CancellationToken token)
        {
            try
            {
                var currentUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(c => c.ID == userId);
                if(currentUser == null)
                {
                    return NotFound(new { Message = "User not found" });
                }

                IQueryable<object>? result = null;

                paginationDto.Validate();

                switch (types)
                {
                    case UsersRelationshipTypes.All:
                        var usersQuery = _context.Users.AsNoTracking();
                        if (!string.IsNullOrEmpty(searchParam))
                        {
                            usersQuery = usersQuery.Where(c => EF.Functions.Contains(c.Firstname, $"\"{searchParam}*\"") || EF.Functions.Contains(c.Lastname, $"\"{searchParam}*\""));
                        }
                        result = usersQuery
                            .AsSplitQuery()
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
                                LastMessage = _context.Conversations
                                    .Where(m => (m.ReceiverUsername == currentUser.Username && m.SenderUsername == c.Username) ||
                                               (m.ReceiverUsername == c.Username && m.SenderUsername == currentUser.Username))
                                    .OrderByDescending(m => m.CreatedAt)
                                    .Select(m => new
                                    {
                                        m.SenderUsername,
                                        m.ReceiverUsername,
                                        m.Content,
                                        m.IsRead,
                                        m.BlogId,
                                        m.LessonId,
                                        m.CourseId,
                                        m.DiscussionId,
                                        m.InstructorId,
                                        m.InstructorCourseId,
                                        m.InstructorLessonId,
                                        m.OnlineMeetingId,
                                        m.QuizId,
                                        m.CreatedAt
                                    })
                                    .FirstOrDefault()
                            });
                        break;
                    case UsersRelationshipTypes.Friends:
                        var friendsQuery = _context.Friends.AsNoTracking();
                        if (!string.IsNullOrEmpty(searchParam))
                        {
                            friendsQuery = friendsQuery.Where(c => EF.Functions.Contains(c.User.Firstname, $"\"{searchParam}*\"") || EF.Functions.Contains(c.User.Lastname, $"\"{searchParam}*\""));
                        }
                        result = friendsQuery
                            .AsSplitQuery()
                            .OrderBy(c => c.ID)
                            .Where(c => c.UserId == userId)
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
                                LastMessage = _context.Conversations
                                    .Where(m => (m.ReceiverUsername == currentUser.Username && m.SenderUsername == c.Friend.Username) ||
                                               (m.ReceiverUsername == c.Friend.Username && m.SenderUsername == currentUser.Username))
                                    .OrderByDescending(m => m.CreatedAt)
                                    .Select(m => new
                                    {
                                        m.SenderUsername,
                                        m.ReceiverUsername,
                                        m.Content,
                                        m.IsRead,
                                        m.BlogId,
                                        m.LessonId,
                                        m.CourseId,
                                        m.DiscussionId,
                                        m.InstructorId,
                                        m.InstructorCourseId,
                                        m.InstructorLessonId,
                                        m.OnlineMeetingId,
                                        m.QuizId,
                                        m.CreatedAt
                                    })
                                    .FirstOrDefault()
                            });                            
                        break;
                    case UsersRelationshipTypes.CloseFriends:
                        var closeFriendQuery = _context.CloseFriends.AsNoTracking();
                        if (!string.IsNullOrEmpty(searchParam))
                        {
                            closeFriendQuery = closeFriendQuery.Where(c => EF.Functions.Contains(c.User.Firstname, $"\"{searchParam}*\"") || EF.Functions.Contains(c.User.Lastname, $"\"{searchParam}*\""));
                        }
                        result = closeFriendQuery
                            .AsSplitQuery()
                            .OrderBy(c => c.ID)
                            .Where(c => c.UserId == userId)
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
                                LastMessage = _context.Conversations
                                    .Where(m => (m.ReceiverUsername == currentUser.Username && m.SenderUsername == c.CloseFriend.Username) ||
                                               (m.ReceiverUsername == c.CloseFriend.Username && m.SenderUsername == currentUser.Username))
                                    .OrderByDescending(m => m.CreatedAt)
                                    .Select(m => new
                                    {
                                        m.SenderUsername,
                                        m.ReceiverUsername,
                                        m.Content,
                                        m.IsRead,
                                        m.BlogId,
                                        m.LessonId,
                                        m.CourseId,
                                        m.InstructorId,
                                        m.DiscussionId,
                                        m.InstructorCourseId,
                                        m.InstructorLessonId,
                                        m.OnlineMeetingId,
                                        m.QuizId,
                                        m.CreatedAt
                                    })
                                    .FirstOrDefault()
                            });
                        break;
                    default:
                        return BadRequest(new { Message = "Bad type provided" });
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
                var users = await _context.Users.AsNoTracking()
                    .Where(c => EF.Functions.Contains(c.Firstname, $"\"{searchParam}*\"") || EF.Functions.Contains(c.Lastname, $"\"{searchParam}*\""))
                    //.Where(c => c.Firstname.Contains(searchParam))
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
                var friendship = await _context.Friendships
                    .AsNoTracking()
                    .Where(c => (c.SenderId == friendDto.SenderId && c.ReceiverId == friendDto.ReceiverId) || (c.SenderId == friendDto.ReceiverId && c.ReceiverId == friendDto.SenderId))
                    .Select(c => new
                    {
                        c.Status,
                        c.SenderId,
                        c.ReceiverId
                    })
                    .FirstOrDefaultAsync();

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

        [Authorize]
        [HttpDelete("/api/UserFriends/DeleteCloseFriend/")]
        public async Task<IActionResult> DeleteCloseFriend(int closeFriendId, CancellationToken token)
        {
            try
            {
                var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if(string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int userId))
                {
                    return Unauthorized();
                }
                var closeFriend = await _context.CloseFriends.Where(c => c.UserId == userId && c.CloseFriendId == closeFriendId).FirstOrDefaultAsync();
                
                if(closeFriend == null)
                {
                    return NotFound(new { Message = "CloseFriend not found" });
                }

                _context.CloseFriends.Remove(closeFriend);
                await _context.SaveChangesAsync(token);
                return Ok(new { Message = "Close friend deleted" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in deleting close friend");
                return BadRequest(new { Message = "Error in deleting close friend" });
            }
        }

        [Authorize]
        [HttpDelete("/api/UserFriends/DeleteFriend/")]
        public async Task<IActionResult> DeleteFriend([FromQuery] int friendId, CancellationToken token)
        {
                using var transation = await _context.Database.BeginTransactionAsync(token);
            try
            {
                var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if(string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int userId))
                {
                    return Unauthorized();
                }

                var friend = await _context.Friends.Where(c => c.UserId == userId && c.FriendId == friendId || c.UserId == friendId && c.FriendId == userId).ToListAsync(token);
                var friendships = await _context.Friendships
                    .Where(c => c.SenderId == userId && c.ReceiverId == friendId || c.SenderId == friendId && c.ReceiverId == userId)
                    .FirstOrDefaultAsync(token);

                var notifications = await _context.Notifications
                    .Where(c => (c.UserId == userId && c.ReceiverId == friendId &&
                     (c.Type == NotificationsType.FriendRequestSenderAccepted ||
                      c.Type == NotificationsType.FriendRequestReceiverAccepted))
                    ||
                    (c.UserId == friendId && c.ReceiverId == userId &&
                     (c.Type == NotificationsType.FriendRequestSenderAccepted ||
                      c.Type == NotificationsType.FriendRequestReceiverAccepted)))
                    .ToListAsync(token);

                if (friend.Count == 0 || friendships == null)
                {
                    return NotFound(new { Message = "No friend found" });
                }

                if(notifications.Count > 0)
                {
                    _context.Notifications.RemoveRange(notifications);
                }

                _context.Friendships.Remove(friendships);
                _context.Friends.RemoveRange(friend);
                await _context.SaveChangesAsync(token);
                await transation.CommitAsync(token);
                return Ok(new { Message = "Friend Deleted" });
            }
            catch (Exception ex)
            {
                await transation.RollbackAsync(token);
                _logger.LogError(ex, $"Error deleting friend with");
                return BadRequest(new { Message = "Error deleting friend" });
            }
        }

        [Authorize]
        [HttpDelete("/api/UserFriends/DeleteFriendRequest")]
        public async Task<IActionResult> DeleteFriendRequest([FromQuery] int receiverId, CancellationToken token)
        {
                var transation = await _context.Database.BeginTransactionAsync(token);
            try
            {
                var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if(string.IsNullOrEmpty(user) || !Int32.TryParse(user, out int userId))
                {
                    return Unauthorized();
                }
                var friendReq = await _context.Friendships.Where(c => c.SenderId == userId && c.ReceiverId == receiverId).FirstOrDefaultAsync(token);
                var notification = await _context.Notifications.Where(c => c.UserId == userId && c.ReceiverId == receiverId && c.Type == NotificationsType.FriendRequestSended).FirstOrDefaultAsync(token);
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
                await transation.RollbackAsync(token);
                _logger.LogError(ex, "Error in deleting friend request");
                return BadRequest(new { Message = "Error in deleting friend request" });
            }
        }

        
    }
}
