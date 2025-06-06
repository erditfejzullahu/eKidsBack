using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Models
{
    public class Conversations : BaseModel
    {
        [Column("Id")]
        public override int ID { get; set; }
        public string? Content { get; set; }

        public string SenderUsername { get; set; }

        [ForeignKey("SenderUsername")]
        public Users Sender { get; set; }

        public string ReceiverUsername { get; set; }

        [ForeignKey("ReceiverUsername")]
        public Users Receiver{ get; set; }

        public string? FileUrl { get; set; }
        public bool IsRead { get; set; }

        public int? QuizId { get; set; }
        public int? LessonId { get; set; }
        public int? CourseId { get; set; }
        public int? BlogId { get; set; }
        public int? DiscussionId { get; set; }
        public int? OnlineMeetingId { get; set; }
        public int? InstructorCourseId { get; set; }
        public int? InstructorLessonId { get; set; }
        public int? InstructorId { get; set; }

        [ForeignKey("OnlineMeetingId")]
        public OnlineMeetings OnlineMeeting { get; set; }

        [ForeignKey("InstructorCourseId")]
        public InstructorCourses InstructorCourse { get; set; }

        [ForeignKey("InstructorLessonId")]
        public InstructorLessons InstructorLesson { get; set; }

        [ForeignKey("InstructorId")]
        public Instructors Instructor { get; set; }

        [ForeignKey("BlogId")]
        public Blogs Blog { get; set; }

        [ForeignKey("QuizId")]
        public Quizzes Quiz { get; set; }

        [ForeignKey("LessonId")]
        public Lessons Lesson { get; set; }

        [ForeignKey("DiscussionId")]
        public Discussions Discussion { get; set; }

        [ForeignKey("CourseId")]
        public Courses Course { get; set; }
    }
}
