using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.DTOs
{
    public class QuizCompletationDto
    {
        public int UserId { get; set; }
        public int QuizId { get; set; }
        public bool Completed { get; set; }
        public int? Mistakes { get; set; }
        public int? Duration { get; set; }

    }

    public class QuizCompStartDto
    {
        public int UserId { get; set; }
        public int QuizId { get; set; }
    }

}
