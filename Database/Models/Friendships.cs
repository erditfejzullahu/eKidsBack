using Database.Shared.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Models
{
    public class Friendships : BaseModel
    {
        [Column("Id")]
        public override int ID { get; set; }
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public FriendshipStatus Status { get; set; }

        [ForeignKey("SenderId")]
        public Users Sender { get; set;}

        [ForeignKey("ReceiverId")]
        public Users Receiver { get; set; }

    }
}
