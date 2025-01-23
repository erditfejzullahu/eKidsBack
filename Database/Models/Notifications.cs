using eKids.Shared.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Models
{
    public class Notifications : BaseModel
    {
        [Column("Id")]
        public override int ID {get; set;}
        public int? UserId { get; set; }
        public int ReceiverId { get; set; }
        public string Information { get; set; }
        public NotificationsType Type { get; set; }
        public bool IsRead { get; set; }

        [ForeignKey("UserId")]
        public Users User { get; set; }

        [ForeignKey("ReceiverId")]
        public Users NotificationReceiver { get; set; }

    }
}
