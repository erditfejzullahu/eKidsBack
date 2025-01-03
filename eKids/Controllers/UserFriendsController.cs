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
        private static readonly ConnectionMapping _connectionMapping = new();

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
                var friend = new Friends
                {
                    UserId = friendDto.UserId,
                    FriendId = friendDto.FriendId,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };
                _friendsRepository.Add(friend);
                await _friendsRepository.SaveAsync(token);
                return Ok(friend);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in making friend");
                return BadRequest(new { Message = "Error in making friend" });
            }

        }
        [HttpPost("/api/UserFriends/AcceptFriend")]
        public async Task<IActionResult> AcceptFriend([FromQuery] int senderId, [FromQuery] int receiverId, CancellationToken token)
        {
            try
            {
                var userIdAuth = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = int.Parse(userIdAuth);
                var username = await _usersRepository.Get(senderId, token);

                var friendReq = await _friendShipsRepository.GetAll().FirstOrDefaultAsync(c => c.SenderId == senderId && c.ReceiverId == receiverId, token);
                if(friendReq == null)
                {
                    return NotFound(new { Message = "Friendship not found" });
                }

                friendReq.Status = FriendshipStatus.Accepted;
                friendReq.LastModified = DateTime.UtcNow;
                _friendShipsRepository.Update(friendReq);
                await _friendShipsRepository.SaveAsync(token);

                if(friendReq.NotificationId != null)
                {
                    var notificationId = await _notificationRepository.Get(friendReq.NotificationId.Value, token);
                    await _notificationRepository.Delete(notificationId.ID, token);
                    await _notificationRepository.SaveAsync(token);

                    var newNotification = new Notifications
                    {
                        ReceiverId = senderId,
                        Information = "Friend accepted",
                        Type = NotificationsType.UserFriendAccepted,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow
                    };
                    
                    _notificationRepository.Add(newNotification);
                    await _notificationRepository.SaveAsync(token);

                    var countNotifications = await _notificationRepository.GetAll().AsNoTracking().Where(c => c.UserId == senderId).CountAsync(token);

                    var connectedUserId = _connectionMapping.GetConnectionId(username.Username);
                    if(connectedUserId != null)
                    {
                        await _notificationsHub.Clients.Client(connectedUserId).SendAsync("UnreadNotifications", countNotifications);
                        await _notificationsHub.Clients.Client(connectedUserId).SendAsync("ReceiveNotification", newNotification);
                    }
                }

                return Ok(new { Message = "Successfully accepted friend" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in accepting friend");
                return BadRequest(new { Message = "Error in accepting friend" });
            }
        }

        [HttpGet("/api/UserFriends/GetAllByUser/{userId}")]
        public async Task<IActionResult> GetAllCloseFriends(int userId, [FromQuery] UsersRelationshipTypes types, [FromQuery] PaginationDto paginationDto, CancellationToken token)
        {
            try
            {
                IQueryable<object>? result = null;

                switch (types)
                {
                    case UsersRelationshipTypes.All:
                        result = _usersRepository
                            .GetAll()
                            .AsNoTracking()
                            .Select(c => new
                            {
                                c.ID,
                                c.Firstname,
                                c.Lastname,
                                c.Email,
                                c.Age,
                                c.ProfilePictureUrl,
                                c.CreatedAt,
                                c.Username
                            });
                        break;
                    case UsersRelationshipTypes.Friends:
                        result = _friendsRepository
                            .GetAll()
                            .AsNoTracking()
                            .Where(c => c.UserId == userId)
                            .Include(c => c.Friend)
                            .Select(c => new
                            {
                                c.ID,
                                c.UserId,
                                c.FriendId,
                                c.CreatedAt,
                                UserData = new
                                {
                                    c.Friend.ID,
                                    c.Friend.Firstname,
                                    c.Friend.Lastname,
                                    c.Friend.Email,
                                    c.Friend.Age,
                                    c.Friend.ProfilePictureUrl,
                                    c.Friend.CreatedAt,
                                    c.Friend.Username,
                                }
                            });                            
                        break;
                    case UsersRelationshipTypes.CloseFriends:
                        result = _closeRepository
                            .GetAll()
                            .AsNoTracking()
                            .Where(c => c.UserId == userId)
                            .Include(c => c.CloseFriend)
                            .Select(c => new
                            {
                                c.ID,
                                c.User,
                                c.CloseFriend,
                                c.CreatedAt,
                                UserData = new
                                {
                                    c.CloseFriend.ID,
                                    c.CloseFriend.Firstname,
                                    c.CloseFriend.Lastname,
                                    c.CloseFriend.Email,
                                    c.CloseFriend.Age,
                                    c.CloseFriend.ProfilePictureUrl,
                                    c.CloseFriend.CreatedAt,
                                    c.CloseFriend.Username,
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
                var friend = await _friendsRepository.GetAll().Where(c => c.UserId == userId && c.FriendId == friendId || c.UserId == friendId && c.FriendId == userId).ToListAsync(token);
                if(friend.Count == 0)
                {
                    return NotFound(new { Message = "No friend found" });
                }
                _friendsRepository.DeleteRange(friend);
                await _friendsRepository.SaveAsync(token);
                return Ok(new { Message = "Friend Deleted" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting friend with");
                return BadRequest(new { Message = "Error deleting friend" });
            }
        }

        [HttpPut("/api/UserFriends/DeleteFriendRequest")]
        public async Task<IActionResult> DeleteFriendRequest([FromQuery] int userId, [FromQuery] int receiverId, CancellationToken token)
        {
            try
            {
                var friendReq = await _friendShipsRepository.GetAll().FirstOrDefaultAsync(c => c.SenderId == userId && c.ReceiverId == receiverId, token);
                if(friendReq == null)
                {
                    return NotFound(new { Message = "Not found" });
                }
                friendReq.Status = FriendshipStatus.Rejected;
                _friendShipsRepository.Update(friendReq);
                //_context.Attach(friendReq);
                _context.Entry(friendReq).State = EntityState.Modified;

                await _friendShipsRepository.SaveAsync(token);
                return Ok(new { Message = "Friendship deleted" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in deleting friend request");
                return BadRequest(new { Message = "Error in deleting friend request" });
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

    }
}
