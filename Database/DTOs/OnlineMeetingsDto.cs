using Database.Shared.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.DTOs
{
    public class BecomeStudentDto
    {
        [Required]
        public int InstructorId { get; set; }
        public int? CourseId { get; set; }
    }
    public class RemoveStudentDto
    {
        public int StudentId { get; set; }
        public int InstructorId { get; set; }
    }
    public class ChangeMeetingStatusDto
    {
        [Required]
        public int MeetingId { get; set; }
        [Required]
        public MeetingStatus Status { get; set; }
    }
    public class OnlineMeetingsDto
    {
        public int? CourseId { get; set; }
        public int? LessonId { get; set; }
        [Required]
        public string Title { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        public DateTime ScheduleDateTime { get; set; }
        public int? DurationTime { get; set; }
        public int? UserId { get; set; }
        [Required]
        public MeetingStatus Status { get; set; }

    }
}
