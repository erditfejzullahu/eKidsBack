using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.DTOs
{
    public class EnrollCourseDto
    {
        public int UserId { get; set; }
        public int? CourseId { get; set; }
        public int OnlineMeetId { get; set; }
        public int InstructorId { get; set; }
    }

    public class CreateCourseDto
    {
        public int InstructorId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string TopicsCovered { get; set; }
        public List<CreateCourseSectionDto> sectionDtos { get; set; } = new List<CreateCourseSectionDto>();
    }

    public class CreateCourseSectionDto
    {
        //public int Course_Id { get; set; }
        public string Title { get; set; }
        public List<CreateLessonDto> lessonDtos { get; set; } = new List<CreateLessonDto>();
    }

    public class CreateLessonDto
    {
        //public int Section_Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Video_Url { get; set; }
    }

    public class CompleteLessonDto
    {
        public int UserId { get; set; }
        public int LessonId { get; set; }
        //completed then to be hardcoded to true if smth or false if smth else?
    }

    public class CreateInstructor
    {
        public int UserId { get; set; }
        public string Expertise { get; set; }
        public string Bio { get; set; }
        public List<AcceptedInstructorSocials> Socials { get; set; }

    }

    public class AcceptedInstructorSocials
    {
        public string Label { get; set; }
        public string Link { get; set; }
    }
}
