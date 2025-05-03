using Database.Shared.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Models
{
    public class OnlineMeetings : BaseModel
    {
        [Column("Id")]
        public override int ID { get; set; }
        public int? CourseId { get; set; }
        public int? LessonId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string MeetingUrl { get; set; }
        [Column(TypeName ="datetime")]
        public virtual DateTime ScheduleDateTime { get; set; }
        public int? DurationTime { get; set; }
        public int InstructorId { get; set; }
        public MeetingStatus Status { get; set; }

        [ForeignKey("CourseId")]
        public InstructorCourses Course { get; set; }

        [ForeignKey("LessonId")]
        public InstructorLessons Lesson { get; set; }

        [ForeignKey("InstructorId")]
        public Instructors Instructor { get; set; }

        public virtual ICollection<OnlineMeetingsParticipants> OnlineMeetingsParticipants { get; set; } = new List<OnlineMeetingsParticipants>();
        public virtual ICollection<StudentCourseLessonProgress> StudentCourseLessonProgresses { get; set; } = new List<StudentCourseLessonProgress>();
    }
}
