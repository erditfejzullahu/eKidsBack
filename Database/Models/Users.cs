using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;

namespace Database.Models
{
    public class Users : BaseModel
    {

        [Required]
        [Column("UserID")]
        [JsonProperty("UserID")]
        public override int ID { get; set; } 

        [Required]
        public string Firstname { get; set; }

        [Required]
        public string Lastname { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string Role { get; set; }

        [JsonProperty("UserMeta")]
        public ICollection<Usermeta> UserMeta { get; set; }


        public ICollection<Comments> Comments { get; set; }

        [JsonProperty("PaymentInfo")]
        public ICollection<Payments> Payments { get; set; } = new List<Payments>();

        [Required]
        public int Age { get; set; }

        public string? ProfilePictureUrl { get; set; }

        public ICollection<CommentLikes> CommentLikes { get; set; } = new List<CommentLikes>();
        public ICollection<LessonLikes> LessonLikes { get; set; } = new List<LessonLikes>();
        public ICollection<CourseCompleted> CourseCompleted { get; set; } = new List<CourseCompleted>();
        public ICollection<Quizzes> Quizzes { get; set; } = new List<Quizzes>();
        public ICollection<QuizzesCompleted> QuizzesCompleted { get; set; } = new List<QuizzesCompleted>();


        public ICollection<Conversations> SentMessages { get; set; } = new List<Conversations>();
        public ICollection<Conversations> ReceivedMessages { get; set; } = new List<Conversations>();

        public ICollection<Notifications> Notifications { get; set; } = new List<Notifications>();
        public ICollection<Notifications> NotificationsReceived { get; set; } = new List<Notifications>();

        public ICollection<Friendships> FriendshipSenders {  get; set; } = new List<Friendships>();
        public ICollection<Friendships> FriendshipReceivers { get; set; } = new List<Friendships>();

        public ICollection<CloseFriends> UsersWithCloseFriends { get; set; } = new List<CloseFriends>();
        public ICollection<CloseFriends> CloseFriends { get; set; } = new List<CloseFriends>();

        public ICollection<Friends> UsersWithFriends { get; set; } = new List<Friends>();
        public ICollection<Friends> Friends { get; set; } = new List<Friends>();

        public ICollection<Courses> CoursesCreated { get; set; } = new List<Courses>();

        public UserInformations UserInformations { get; set; } // if one on one
        //public virtual ICollection<UserInformations> UserInformations { get; set; }
        public ICollection<UserEducations> UserEducations { get; set; } = new List <UserEducations>();
        public ICollection<UserJobs> UserJobs { get; set; } = new List<UserJobs>();
        public ICollection<Blogs> Blogs { get; set; } = new List<Blogs>();
        public ICollection<BlogLikes> BlogLikes { get; set; } = new List<BlogLikes>();
        public ICollection<BlogComments> BlogComments { get; set; } = new List<BlogComments>();
        public ICollection<BlogCommentLikes> BlogCommentLikes { get; set; } = new List<BlogCommentLikes>();
        public ICollection<Commits> Commits { get; set; } = new List<Commits>();

        public ICollection<UserProgress> UserProgress { get; set; } = new List<UserProgress>();


        public ICollection<Discussions> UserDiscussions { get; set; } = new List<Discussions>();
        public ICollection<DiscussionAnswers> DiscussionAnswers { get; set; } = new List<DiscussionAnswers>();

        public ICollection<DiscussionVotes> DiscussionVotes { get; set; } = new List<DiscussionVotes>();
        public ICollection<DiscussionAnswerVotes> DiscussionAnswerVotes { get; set; } = new List<DiscussionAnswerVotes>();

        public Instructors Instructor { get; set; }

        public ICollection<StudentCourseLessonProgress> StudentCourseLessonProgresses { get; set; } = new List<StudentCourseLessonProgress>();
        
        public ICollection<InstructorStudents> InstructorStudents { get; set; } = new List<InstructorStudents>();

        public ICollection<OnlineMeetingsParticipants> OnlineMeetingParticipated { get; set; } = new List<OnlineMeetingsParticipants>();

        public ICollection<PasswordResetTokens> PasswordResetTokens { get; set; } = new List<PasswordResetTokens>();

        public ICollection<ReportTickets> SubmittedTickets { get; set; } = new List<ReportTickets>(); 
        public ICollection<ReportTickets> ReportsAgainstUser { get; set; } = new List<ReportTickets>();
    }
}
