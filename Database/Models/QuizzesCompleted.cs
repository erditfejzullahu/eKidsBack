using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Models
{
    public class QuizzesCompleted : BaseModel
    {
        [Column("Id")]
        public override int ID { get; set; }

        public int UserId { get; set; }

        public int QuizId { get; set; }

        public bool Completed { get; set; }

        public int Mistakes { get; set; }

        public int Duration { get; set; }

        [ForeignKey("UserId")]
        public Users User { get; set; }

        [ForeignKey("QuizId")]
        public Quizzes Quiz { get; set; }
    }
}
