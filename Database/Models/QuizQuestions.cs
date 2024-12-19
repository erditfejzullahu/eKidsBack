using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Models
{
    public class QuizQuestions : BaseModel
    {
        [Column("id")]
        public override int ID { get; set; }
        public string QuestionText { get; set; }
        public int QuizId { get; set; }
        public string QuestionType { get; set; }

        [ForeignKey("QuizId")]
        public Quizzes Quiz { get; set; }
        public virtual ICollection<QuizAnswers> Answers { get; set; } = new List<QuizAnswers>();
    }
}
