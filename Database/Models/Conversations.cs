using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Models
{
    public class Conversations : BaseModel
    {
        [Column("Id")]
        public override int ID { get; set; }
        public string? Content { get; set; }

        public string SenderUsername { get; set; }

        [ForeignKey("SenderUsername")]
        public Users Sender { get; set; }

        public string ReceiverUsername { get; set; }

        [ForeignKey("ReceiverUsername")]
        public Users Receiver{ get; set; }

        public string? FileUrl { get; set; }
        public bool IsRead { get; set; }
    }
}
