using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Models
{
    public class Quizzes : BaseModel
    {
        [Column("id")]
        public override int ID { get; set; }

        public string QuizName { get; set; }

        public string QuizDescription { get; set; }

        public int UserId { get; set; }

        public int QuizCategory { get; set; }
        public int? ViewCount { get; set; }

        [ForeignKey("QuizCategory")]
        public Categories Category { get; set; }

        [ForeignKey("UserId")]
        public Users User { get; set; }

        public ICollection<QuizQuestions> Questions { get; set; } = new List<QuizQuestions>();
        public ICollection<QuizzesCompleted> QuizzesCompleted { get; set; } = new List<QuizzesCompleted>();
        public ICollection<Conversations> QuizConversations { get; set; } = new List<Conversations>();

    }
}
