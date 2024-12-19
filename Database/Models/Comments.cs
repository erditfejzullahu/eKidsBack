using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Models
{
    public class Comments : BaseModel
    {
        [Required]
        [Column("CommentID")]
        [JsonProperty("id")]
        public override int ID { get; set; }

        [Required]
        public int LessonId { get; set; }

        [Required]
        public int UserId { get; set; }

        public int Likes { get; set; }

        [Required]
        public string Comment_Content { get; set; }

        public int? ParentId { get; set; }

        public Comments Parent { get; set; }

        public ICollection<Comments> Replies { get; set; }

        [ForeignKey("UserId")]
        public Users User { get; set; }

        public ICollection<CommentLikes> CommentLikes { get; set; } = new List<CommentLikes>();

    }
}
