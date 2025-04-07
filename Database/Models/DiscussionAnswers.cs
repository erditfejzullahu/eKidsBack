using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Models
{
    public class DiscussionAnswers : BaseModel
    {
        [Column("Id")]
        public override int ID { get; set; }
        public string Content { get; set; }
        public int UserId { get; set; }
        public int DiscussionId { get; set; }
        public int Votes { get; set; }
        public string Item_Url { get; set; }
        public int? ParentId { get; set; }

        public DiscussionAnswers Parent { get; set; }

        [ForeignKey("UserId")]
        public Users User { get; set; }
        [ForeignKey("DiscussionId")]
        public Discussions Discussion { get; set; }

        public ICollection<DiscussionAnswers> Replies { get; set; }

        public virtual ICollection<DiscussionAnswerVotes> DiscussionAnswerVotes { get; set; }
    }
}
