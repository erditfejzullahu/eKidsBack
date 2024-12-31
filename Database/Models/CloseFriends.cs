using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Models
{
    public class CloseFriends : BaseModel
    {
        [Column("Id")]
        public override int ID { get; set; }
        public int UserId { get; set; }
        public int CloseFriendId { get; set; }

        [ForeignKey("UserId")]
        public Users User { get; set; }

        [ForeignKey("CloseFriendId")]
        public Users CloseFriend { get; set; }
    }
}
