using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Models
{
    public class DiscussionTags : BaseModel
    {
        [Column("Id")]
        public override int ID { get; set; }
        [Required]
        public string Title { get; set; }
        public string? Description { get; set; }

        public List<DiscussionsWithTags> DiscussionWithTags { get; set; } = new List<DiscussionsWithTags>();
    }
}
