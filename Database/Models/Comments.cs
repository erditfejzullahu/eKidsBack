using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Models
{
    public class Comments : BaseModel
    {
        public int CommentID { get; set; }

        [Required]
        public int LessonID { get; set; }

        [Required]
        public required string Firstname { get; set; }

        [Required]
        public required string Lastname { get; set; }

        [Required]
        public required string UserURL { get; set; }

        //public DateTime CreatedAt { get; set; }
        //public DateTime UpdatedAt { get; set;}

    }
}
