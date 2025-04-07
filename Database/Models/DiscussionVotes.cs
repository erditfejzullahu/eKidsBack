using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Models
{
    public class DiscussionVotes : BaseModel
    {
        [Column("Id")]
        public override int ID { get; set; }
        public int UserId { get; set; }
        public int DiscussionId { get; set; }
        public bool IsVotedDown { get; set; }
        public bool IsVotedUp { get; set; }

        [ForeignKey("UserId")]
        public Users User { get; set; }
        [ForeignKey("DiscussionId")]
        public Discussions Discussion { get; set; }
    }
}
