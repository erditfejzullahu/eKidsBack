using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Models
{
    public class QuizAnswers : BaseModel
    {
        [Column("id")]
        public override int ID { get; set; }
        public string AnswerText { get; set; }
        public bool IsCorrect { get; set; }
        public int QuestionId { get; set; }

        [ForeignKey("QuestionId")]
        public QuizQuestions QuizQuestion { get; set; }
    }
}
