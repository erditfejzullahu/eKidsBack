using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.DTOs
{
    public class DiscussionDto
    {
        [Required]
        public string Title { get; set; }
        [Required]
        public string Content { get; set; }
        [Required]
        public int UserId { get; set; }
        [Required]
        public bool PreferAnonimity { get; set; }
        public List<DiscussionTagsDto> Tags { get; set; } = new List<DiscussionTagsDto>();
    }

    public class DiscussionTagsDto
    {
        public int? TagId { get; set; }
        [Required]
        public string Title { get; set; }
        public string? Description { get; set; }
    }
}
