using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.DTOs
{
    public class ProcessQuizDto
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string QuizTitle { get; set; }

        [Required]
        [StringLength(500)]
        public string QuizDescription { get; set; }

        [Range(1, int.MaxValue)]
        public int UserId { get; set; }

        [Range(1, int.MaxValue)]
        public int QuizCategory { get; set; }

        [Required]
        [MinLength(1)]
        [MaxLength(100)] // Adjust based on your needs
        public Dictionary<string, object> QuizData { get; set; }
    }
}
