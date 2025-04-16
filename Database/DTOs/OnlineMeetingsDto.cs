using Database.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.DTOs
{
    public class RemoveStudentDto
    {
        public int StudentId { get; set; }
        public int InstructorId { get; set; }
    }
    public class ChangeMeetingStatusDto
    {
        public int MeetingId { get; set; }
        public MeetingStatus Status { get; set; }
    }
    public class OnlineMeetingsDto
    {
        public int? CourseId { get; set; }
        public int? LessonId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime ScheduleDateTime { get; set; }
        public int? DurationTime { get; set; }
        public int? UserId { get; set; }
        public MeetingStatus Status { get; set; }

    }
}
