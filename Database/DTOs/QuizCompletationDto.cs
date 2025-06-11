using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.DTOs
{
    public class QuizCompletationDto
    {
        [Required]
        public int UserId { get; set; }
        [Required]
        public int QuizId { get; set; }
        //[Required]
        public bool Completed { get; set; }
        public int? Mistakes { get; set; }
        public int? Duration { get; set; }

    }

    public class QuizCompStartDto
    {
        [Required]
        public int UserId { get; set; }
        [Required]
        public int QuizId { get; set; }
    }

}
