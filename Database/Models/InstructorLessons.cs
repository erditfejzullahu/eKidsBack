using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Models
{
    public class InstructorLessons : BaseModel
    {
        [Column("Id")]
        public override int ID { get; set; }
        public int Section_Id { get; set; }
        public string Title { get; set; }
        public string? Content { get; set; }
        public string? Video_Url { get; set; }

        [ForeignKey("Section_Id")]
        public InstructorCourseSections InstructorCourseSections { get; set; }
        public ICollection<Conversations> InstructorLessonConversations = new List<Conversations>();
        public ICollection<OnlineMeetings> OnlineMeetings { get; set; } = new List<OnlineMeetings>();
        public ICollection<StudentCourseLessonProgress> CourseLessonProgresses { get; set; } = new List<StudentCourseLessonProgress>();
    }
}
