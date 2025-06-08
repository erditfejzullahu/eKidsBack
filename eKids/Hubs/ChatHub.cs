using Azure.Core;
using Database.Context;
using Database.Models;
using Database.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Policy;

namespace eKids.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;
        //private static readonly ConnectionMapping _connections = new ConnectionMapping();
        private readonly ILogger<ChatHub> _logger;
        private IFileUploadService _fileUploadService;

        public ChatHub(ApplicationDbContext context, ILogger<ChatHub> logger, IFileUploadService fileUploadService)
        {
            _context = context;
            _logger = logger;
            _fileUploadService = fileUploadService;
        }
        public override Task OnConnectedAsync()
        {
            //string username = Context?.User?.Identity?.Name;
            string username = Context.User.FindFirstValue("Username");
            //string userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(username))
            {
                ConnectionMapping.Add(username, Context.ConnectionId);
                _logger.LogInformation($"{username} connected with connection ID: {Context.ConnectionId}");
            }
            else
            {
                _logger.LogInformation($"{username} is null ????");
            }

            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            string username = Context.User.FindFirstValue("Username");
            if (!string.IsNullOrEmpty(username))
            {
                ConnectionMapping.Remove(username);
                _logger.LogInformation($"{username} disconnected");
            }
            return base.OnDisconnectedAsync(exception);
        }


        public async Task SendPrivateMessage(string receiver, string? message, string? base64Data)
        {
            try
            {
                string? fileUrl = null;
                if(base64Data != null)
                {
                    try
                    {
                        string relativeUrl = await _fileUploadService.UploadFile(base64Data, FileCategory.Other);
                        var httpContext = Context.GetHttpContext();
                        fileUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}{relativeUrl}";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in file upload section");
                        throw new ApplicationException("Error in upload file", ex);
                    }
                }

                string username = Context.User.FindFirstValue("Username");

                if (username == null)
                {
                    throw new UnauthorizedAccessException("User is not authenticated.");
                }

                var newMessage = new Conversations
                {
                    SenderUsername = username,
                    ReceiverUsername = receiver,
                    Content = message,
                    FileUrl = fileUrl,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow
                };

                await _context.Conversations.AddAsync(newMessage, default);
                await _context.SaveChangesAsync(default);

                var messageData = await _context.Conversations
                    .AsNoTracking()
                    .Where(c => c.ID == newMessage.ID)
                    .Select(c => new
                    {
                        c.ID,
                        c.Content,
                        c.IsRead,
                        c.FileUrl,
                        c.SenderUsername,
                        c.ReceiverUsername,
                        Sender = new
                        {
                            c.Sender.Firstname,
                            c.Sender.Lastname,
                            c.Sender.Username,
                            c.Sender.ProfilePictureUrl
                        },
                        Receiver = new
                        {
                            c.Receiver.Firstname,
                            c.Receiver.Lastname,
                            c.Receiver.Username,
                            c.Receiver.ProfilePictureUrl
                        },
                        c.CreatedAt,
                    })
                    .FirstOrDefaultAsync(default);

                var recipientConnectionId = ConnectionMapping.GetConnectionId(receiver);
                if (recipientConnectionId != null)
                {
                    newMessage.IsRead = true;
                    _context.Conversations.Update(newMessage);
                    await _context.SaveChangesAsync(default);
                    await Clients.Client(recipientConnectionId).SendAsync("ReceiveMessage", messageData);
                }

                var senderConnectionId = ConnectionMapping.GetConnectionId(username);
                if (senderConnectionId != null)
                {
                    await Clients.Client(senderConnectionId).SendAsync("MessageSent", messageData);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Unauthorized access attempt.");
                await Clients.Caller.SendAsync("Error", "You must be authenticated to send messages.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in sending private message");
                throw new ApplicationException("An error occurred while sending the private message.", ex);
            }
        }

    }
}
