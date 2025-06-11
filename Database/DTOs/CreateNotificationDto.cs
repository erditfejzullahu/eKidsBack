using eKids.Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace Database.DTOs
{
    public class CreateNotificationDto
    {
        public int? UserId {  get; set; }
        [Required]
        public int ReceiverId { get; set; }
        public string? Information { get; set; }
        public NotificationsType Type { get; set; }
    }
}
