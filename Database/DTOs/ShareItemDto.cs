using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.DTOs
{
    public class ShareItemDto
    {
        public string SenderUsername { get; set; }
        public string ReceiverUsername { get; set; }
        public int? LessonId { get; set; }
        public int? CourseId { get; set; }
        public int? QuizId { get; set; }
        public int? BlogId { get; set; }
        public int? DiscussionId { get; set; }
        public int? InstructorId { get; set; }
        public int? InstructorCourseId { get; set; }
        public int? InstructorLessonId { get; set; }
        public int? OnlineMeetingId { get; set; }

    }
}
