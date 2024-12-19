using Microsoft.AspNetCore.SignalR;

namespace eKids.Hubs
{
    public class VideoUploadHub : Hub
    {
        public async Task SendProgress(string connectionId, int percentage)
        {
            await Clients.Client(connectionId).SendAsync("ReceiveProgress", percentage);
        }
    }
}
