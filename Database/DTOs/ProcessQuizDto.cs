using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.DTOs
{
    public class ProcessQuizDto
    {
        public string QuizTitle { get; set; }
        public string QuizDescription { get; set; }
        public int UserId { get; set; }
        public int QuizCategory { get; set; }
        public Dictionary<string, object> QuizData { get; set; }
    }
}
