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
        public string Content { get; set; }
        public string Video_Url { get; set; }

        [ForeignKey("Section_Id")]
        public InstructorCourseSections InstructorCourseSections { get; set; }

        public virtual ICollection<StudentCourseLessonProgress> StudentCourseLessonsProgresses { get; set; } = new List<StudentCourseLessonProgress>();

        public virtual ICollection<OnlineMeetings> OnlineMeetings { get; set; } = new List<OnlineMeetings>();
    }
}
