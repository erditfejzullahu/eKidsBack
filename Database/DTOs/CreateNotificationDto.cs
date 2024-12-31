using eKids.Shared.Enums;

namespace Database.DTOs
{
    public class CreateNotificationDto
    {
        public int? UserId {  get; set; }
        public int ReceiverId { get; set; }
        public string Information { get; set; }
        public NotificationsType Type { get; set; }
    }
}
