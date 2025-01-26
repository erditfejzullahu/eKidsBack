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

        [Required]
        [JsonIgnore]
        public int PackageID { get; set; }

        [Required]
        [JsonIgnore]
        public int PaymentID { get; set; }

        [JsonProperty("PackageInfo")]
        public virtual Packages Package { get; set; }

        [JsonProperty("UserMeta")]
        public virtual ICollection<Usermeta> UserMeta { get; set; }


        public virtual ICollection<Comments> Comments { get; set; }

        [JsonProperty("PaymentInfo")]
        public virtual Payments Payment { get; set; }

        [Required]
        public int Age { get; set; }

        public string ProfilePictureUrl { get; set; }

        public virtual ICollection<CommentLikes> CommentLikes { get; set; } = new List<CommentLikes>();
        public virtual ICollection<LessonLikes> LessonLikes { get; set; } = new List<LessonLikes>();
        public virtual ICollection<CourseCompleted> CourseCompleted { get; set; } = new List<CourseCompleted>();
        public virtual ICollection<Quizzes> Quizzes { get; set; } = new List<Quizzes>();
        public virtual ICollection<QuizzesCompleted> QuizzesCompleted { get; set; } = new List<QuizzesCompleted>();


        public virtual ICollection<Conversations> SentMessages { get; set; } = new List<Conversations>();
        public virtual ICollection<Conversations> ReceivedMessages { get; set; } = new List<Conversations>();

        public virtual ICollection<Notifications> Notifications { get; set; } = new List<Notifications>();
        public virtual ICollection<Notifications> NotificationsReceived { get; set; } = new List<Notifications>();

        public virtual ICollection<Friendships> FriendshipSenders {  get; set; } = new List<Friendships>();
        public virtual ICollection<Friendships> FriendshipReceivers { get; set; } = new List<Friendships>();

        public virtual ICollection<CloseFriends> UsersWithCloseFriends { get; set; } = new List<CloseFriends>();
        public virtual ICollection<CloseFriends> CloseFriends { get; set; } = new List<CloseFriends>();

        public virtual ICollection<Friends> UsersWithFriends { get; set; } = new List<Friends>();
        public virtual ICollection<Friends> Friends { get; set; } = new List<Friends>();

        public virtual ICollection<Courses> CoursesCreated { get; set; } = new List<Courses>();

        public virtual UserInformations UserInformations { get; set; } // if one on one
        //public virtual ICollection<UserInformations> UserInformations { get; set; }
        public virtual ICollection<UserEducations> UserEducations { get; set; } = new List <UserEducations>();
        public virtual ICollection<UserJobs> UserJobs { get; set; } = new List<UserJobs>();
        public virtual ICollection<Blogs> Blogs { get; set; } = new List<Blogs>();
        public virtual ICollection<BlogLikes> BlogLikes { get; set; } = new List<BlogLikes>();
        public virtual ICollection<BlogComments> BlogComments { get; set; } = new List<BlogComments>();
        public virtual ICollection<BlogCommentLikes> BlogCommentLikes { get; set; } = new List<BlogCommentLikes>();
        public virtual ICollection<Commits> Commits { get; set; } = new List<Commits>();

        public virtual ICollection<UserProgress> UserProgress { get; set; } = new List<UserProgress>();

    }
}
