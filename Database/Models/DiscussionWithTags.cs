using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Models
{
    public class DiscussionWithTags
    {
        public int DiscussionId { get; set; }
        public int TagId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastModified { get; set; }

        [ForeignKey("DiscussionId")]
        public Discussions Discussion { get; set; }
        [ForeignKey("TagId")]
        public DiscussionTags DiscussionTag { get; set; }
    }
}
