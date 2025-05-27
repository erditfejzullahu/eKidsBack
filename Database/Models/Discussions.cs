using Database.Shared.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Models
{
    public class Discussions : BaseModel
    {
        [Column("Id")]
        public override int ID { get; set; }
        [Required]
        public string Title { get; set; }
        [Required]
        public string Content { get; set; }
        [Required]
        public int UserId { get; set; }
        [Required]
        public DiscussionAnonimityStatus PreferAnonimity { get; set; }

        public int Views { get; set; }
        public int Votes { get; set; }
        public bool IsUrgent { get; set; }
        public bool Edited { get; set; }

        [ForeignKey("UserId")]
        public Users User { get; set; }

        public List<DiscussionsWithTags> DiscussionWithTags { get; set; } = new List<DiscussionsWithTags>();
        public List<DiscussionAnswers> DiscussionAnswers { get; set; } = new List<DiscussionAnswers>();
        public List<DiscussionVotes> DiscussionVotes { get; set; } = new List<DiscussionVotes>();
        public List<Conversations> DiscussionConversations { get; set; } = new List<Conversations>();
    }
}
