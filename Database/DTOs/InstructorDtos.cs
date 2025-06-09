using Database.Shared.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.DTOs
{
    public class EnrollCourseDto
    {
        public int UserId { get; set; }
        public int CourseId { get; set; }
        public int InstructorId { get; set; }
    }

    public class CreateCourseDto
    {
        //[Required]
        //public int InstructorId { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public int CategoryId { get; set; }
        public string? Image { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        public InstructorCoursesLevels Level { get; set; }
        [Required]
        [MinLength(1)]
        public List<string> TopicsCovered { get; set; } = new List<string>();
        [Required]
        [MinLength(1)]
        public List<string> SectionTitles { get; set; } = new List<string>();
        [Required]
        [MinLength(1)]
        public List<List<string>> SectionLessons { get; set; } = new List<List<string>>();
    }

    public class CompleteLessonDto
    {
        public int UserId { get; set; }
        public int LessonId { get; set; }
        //completed then to be hardcoded to true if smth or false if smth else?
    }

    public class CreateInstructor
    {
        [Required]
        public int UserId { get; set; }
        [Required]
        public string Expertise { get; set; }
        [Required]
        public string Bio { get; set; }
        [Required]
        public List<AcceptedInstructorSocials> Socials { get; set; }

    }

    public class AcceptedInstructorSocials
    {
        public string Label { get; set; }
        public string Link { get; set; }
    }
}
