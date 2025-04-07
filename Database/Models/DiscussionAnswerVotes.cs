using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Models
{
    public class DiscussionAnswerVotes
    {
        [ForeignKey("DiscussionCommentId")]
        public int DiscussionCommentId { get; set; }
        [ForeignKey("UserId")]
        public int UserId { get; set; }
        public bool IsVotedUp { get; set; }
        public bool IsVotedDown { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastModified { get; set; }

        public Users User { get; set; }
        public DiscussionAnswers DiscussionAnswer { get; set; }
    }
}
