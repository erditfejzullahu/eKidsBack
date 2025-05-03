using Database.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Context
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<RefreshToken> RefreshToken { get; set; }
        public DbSet<Courses> Courses { get; set; }
        public DbSet<Lessons> Lessons { get; set; }
        public DbSet<Categories> Categories { get; set; }
        public DbSet<Comments> Comments { get; set; }
        public DbSet<CommentLikes> CommentLikes { get; set; }
        public DbSet<LessonLikes> LessonLikes { get; set; }
        public DbSet<UserProgress> UserProgress { get; set; }
        public DbSet<CourseCompleted> CourseCompleted { get; set; }
        public DbSet<Quizzes> Quizzes { get; set; }
        public DbSet<QuizzesCompleted> QuizzesCompleted { get; set; }
        public DbSet<Conversations> Conversations { get; set; }
        public DbSet<Notifications> Notifications { get; set; }
        public DbSet<Users> Users { get; set; }
        public DbSet<Friendships> Friendships { get; set; }
        public DbSet<Friends> Friends { get; set; }
        public DbSet<CloseFriends> CloseFriends { get; set; }
        public DbSet<UserInformations> UserInformations { get; set; }
        public DbSet<UserJobs> UserJobs { get; set; }
        public DbSet<UserEducations> UserEducations { get; set; }
        public DbSet<Tags> Tags { get; set; }
        public DbSet<Blogs> Blogs { get; set; }
        public DbSet<BlogLikes> BlogLikes { get; set; }
        public DbSet<BlogComments> BlogComments { get; set; }
        public DbSet<BlogCommentLikes> BlogCommentLikes { get; set; }
        public DbSet<Commits> Commits { get; set; }
        public DbSet<Discussions> Discussions { get; set; }
        public DbSet<DiscussionTags> DiscussionTags { get; set; }
        public DbSet<DiscussionsWithTags> DiscussionsWithTags { get; set; }
        public DbSet<DiscussionAnswers> DiscussionAnswers { get; set; }
        public DbSet<DiscussionAnswerVotes> DiscussionAnswerVotes { get; set; }
        public DbSet<DiscussionVotes> DiscussionVotes { get; set; }
        public DbSet<Instructors> Instructors { get; set; }
        public DbSet<InstructorCourses> InstructorCourses { get; set; }
        public DbSet<InstructorCourseSections> InstructorCourseSections { get; set; }
        public DbSet<InstructorLessons> InstructorLessons { get; set; }
        public DbSet<StudentCourseLessonProgress> StudentCourseLessonProgress { get; set; }
        public DbSet<InstructorStudents> InstructorStudents { get; set; }
        public DbSet<OnlineMeetings> OnlineMeetings { get; set; }
        public DbSet<OnlineMeetingsParticipants> OnlineMeetingsParticipants { get; set; }
        public DbSet<Packages> Packages { get; set; }
        public DbSet<Payments> Payments { get; set; }
        public DbSet<PasswordResetTokens> PasswordResetTokens { get; set; }


        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }


        //to fixxxxx
        //public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken)
        //{
        //    var entries = ChangeTracker.Entries<Friendships>()
        //        .Where(c => c.State == EntityState.Modified);
        //    var entries1 = ChangeTracker.Entries();
        //    var pending = ChangeTracker.Entries<Friendships>()
        //        .Where(c => c.State == EntityState.Added);

        //    foreach (var item in entries)
        //    {
        //        var oldValue = item.OriginalValues["Status"]?.ToString(); 
        //        var newValue = item.CurrentValues["Status"]?.ToString();

        //        if(int.TryParse(newValue, out int status) && status == 2)
        //        {
        //            if(!string.Equals(oldValue, newValue, StringComparison.Ordinal))
        //            {
        //                await NotifyTheChangeInEntity(item.Entity, "accepted", cancellationToken);
        //            }
        //        }else if(int.TryParse(newValue, out int otherStatus) && otherStatus == 3)
        //        {
        //            if(!string.Equals(oldValue, newValue, StringComparison.Ordinal))
        //            {
        //                await NotifyTheChangeInEntity(item.Entity, "rejected", cancellationToken);
        //            }
        //        }
        //    }

        //    return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

        //}
        //public async Task NotifyTheChangeInEntity(Friendships entity, string status, CancellationToken token)
        //{
        //    if(string.Equals(status, "accepted", StringComparison.Ordinal))
        //    {
        //        var friend1 = new Friends
        //        {
        //            UserId = entity.SenderId,
        //            FriendId = entity.ReceiverId,
        //            CreatedAt = DateTime.UtcNow,
        //            LastModified = DateTime.UtcNow
        //        };
        //        var friend2 = new Friends
        //        {
        //            UserId = entity.ReceiverId,
        //            FriendId = entity.SenderId,
        //            CreatedAt = DateTime.UtcNow,
        //            LastModified = DateTime.UtcNow
        //        };
        //        base.Set<Friends>().AddRange(friend1, friend2);
        //        await base.SaveChangesAsync(token);

        //    }else if(string.Equals(status, "rejected", StringComparison.Ordinal))
        //    {
        //        var friendshipToRemove = await base.Set<Friendships>().FindAsync(entity.ID);
        //        if(friendshipToRemove != null)
        //        {
        //            base.Set<Friendships>().Remove(friendshipToRemove);
        //            await base.SaveChangesAsync(token);
        //        }
        //    }
        //    await Task.CompletedTask;
        //}
        ////to fixx

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //indexes

            modelBuilder.Entity<CourseCompleted>()
                .HasIndex(c => new { c.UserId, c.CourseId });
            //.HasName("IX_CourseCompleted_SomeProperty"); // not working

            modelBuilder.Entity<DiscussionsWithTags>()
                .HasKey(c => new { c.DiscussionId, c.TagId });

            modelBuilder.Entity<DiscussionAnswerVotes>()
                .HasKey(c => new { c.DiscussionCommentId, c.UserId });

            modelBuilder.Entity<Instructors>()
                .HasKey(c => c.UserId);

            modelBuilder.Entity<StudentCourseLessonProgress>()
                .HasKey(c => new { c.UserId, c.OnlineMeetId });

            modelBuilder.Entity<InstructorStudents>()
                .HasKey(c => new { c.UserId, c.CourseId });

            modelBuilder.Entity<OnlineMeetingsParticipants>()
                .HasKey(c => new {c.OnlineMeetId, c.UserId});
            //indexes


            modelBuilder.Entity<PasswordResetTokens>()
                .HasOne(c => c.User)
                .WithMany(c => c.PasswordResetTokens)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OnlineMeetingsParticipants>()
                .HasOne(c => c.OnlineMeeting)
                .WithMany(c => c.OnlineMeetingsParticipants)
                .HasForeignKey(c => c.OnlineMeetId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OnlineMeetingsParticipants>()
                .HasOne(c => c.User)
                .WithMany(c => c.OnlineMeetingParticipated)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OnlineMeetings>()
                .HasOne(c => c.Instructor)
                .WithMany(c => c.OnlineMeetings)
                .HasForeignKey(c => c.InstructorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OnlineMeetings>()
                .HasOne(c => c.Course)
                .WithMany(c => c.OnlineMeetings)
                .HasForeignKey(c => c.CourseId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<OnlineMeetings>()
                .HasOne(c => c.Lesson)
                .WithMany(c => c.OnlineMeetings)
                .HasForeignKey(c => c.LessonId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<InstructorStudents>()
                .HasOne(c => c.User)
                .WithMany(c => c.InstructorStudents)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<InstructorStudents>()
                .HasOne(c => c.InstructorCourse)
                .WithMany(c => c.InstructorStudents)
                .HasForeignKey(c => c.CourseId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<InstructorStudents>()
                .HasOne(c => c.Instructor)
                .WithMany(c => c.InstructorStudents)
                .HasForeignKey(c => c.InstructorId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<StudentCourseLessonProgress>()
                .HasOne(c => c.OnlineMeeting)
                .WithMany(c => c.StudentCourseLessonProgresses)
                .HasForeignKey(c => c.OnlineMeetId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StudentCourseLessonProgress>()
                .HasOne(c => c.User)
                .WithMany(c => c.StudentCourseLessonProgresses)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Instructors>()
                .HasOne(c => c.User)
                .WithOne(c => c.Instructor)
                .HasForeignKey<Instructors>(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);


            modelBuilder.Entity<InstructorCourses>()
                .HasOne(c => c.Instructor)
                .WithMany(c => c.InstructorCourses)
                .HasForeignKey(c => c.InstructorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InstructorCourseSections>()
                .HasOne(c => c.InstructorCourses)
                .WithMany(c => c.InstructorCourseSections)
                .HasForeignKey(c => c.Course_Id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InstructorLessons>()
                .HasOne(c => c.InstructorCourseSections)
                .WithMany(c => c.InstructorLessons)
                .HasForeignKey(c => c.Section_Id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DiscussionVotes>()
                .HasOne(c => c.User)
                .WithMany(c => c.DiscussionVotes)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DiscussionVotes>()
                .HasOne(c => c.Discussion)
                .WithMany(c => c.DiscussionVotes)
                .HasForeignKey(c => c.DiscussionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DiscussionAnswerVotes>()
                .HasOne(c => c.User)
                .WithMany(c => c.DiscussionAnswerVotes)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DiscussionAnswerVotes>()
                .HasOne(c => c.DiscussionAnswer)
                .WithMany(c => c.DiscussionAnswerVotes)
                .HasForeignKey(c => c.DiscussionCommentId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<DiscussionAnswers>()
                .HasOne(c => c.Discussion)
                .WithMany(c => c.DiscussionAnswers)
                .HasForeignKey(c => c.DiscussionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DiscussionAnswers>()
                .HasOne(c => c.User)
                .WithMany(c => c.DiscussionAnswers)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DiscussionAnswers>()
                .HasOne(c => c.Parent)
                .WithMany(c => c.Replies)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Discussions>()
                .HasOne(c => c.User)
                .WithMany(c => c.UserDiscussions)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DiscussionsWithTags>()
                .HasOne(c => c.Discussion)
                .WithMany(c => c.DiscussionWithTags)
                .HasForeignKey(c => c.DiscussionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DiscussionsWithTags>()
                .HasOne(c => c.DiscussionTag)
                .WithMany(c => c.DiscussionWithTags)
                .HasForeignKey(c => c.TagId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Conversations>()
                .HasOne(c => c.Discussion)
                .WithMany(c => c.DiscussionConversations)
                .HasForeignKey(c => c.DiscussionId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Conversations>()
                .HasOne(c => c.Blog)
                .WithMany(c => c.BlogConversations)
                .HasForeignKey(c => c.BlogId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Commits>()
                .HasOne(c => c.User)
                .WithMany(c => c.Commits)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BlogCommentLikes>()
                .HasOne(c => c.BlogComment)
                .WithMany(c => c.BlogCommentLikes)
                .HasForeignKey(c => c.CommentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BlogCommentLikes>()
                .HasOne(c => c.User)
                .WithMany(c => c.BlogCommentLikes)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BlogComments>()
                .HasOne(c => c.User)
                .WithMany(c => c.BlogComments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BlogComments>()
                .HasOne(c => c.Blog)
                .WithMany(c => c.BlogComments)
                .HasForeignKey(c => c.BlogId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BlogComments>()
                .HasOne(c => c.Parent)
                .WithMany(c => c.Replies)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
                

            modelBuilder.Entity<BlogLikes>()
                .HasOne(c => c.Blog)
                .WithMany(c => c.BlogLikes)
                .HasForeignKey(c => c.BlogId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BlogLikes>()
                .HasOne(c => c.User)
                .WithMany(c => c.BlogLikes)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Users>()
                .HasMany(c => c.Blogs)
                .WithOne(c => c.User)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Blogs>()
                .HasOne(c => c.Category)
                .WithMany(c => c.Blogs)
                .HasForeignKey(c => c.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Blogs>()
                .HasOne(c => c.Tag)
                .WithMany(c => c.Blogs)
                .HasForeignKey(c => c.TagId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Tags>()
                .HasMany(c => c.Blogs)
                .WithOne(c => c.Tag)
                .HasForeignKey(c => c.TagId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Tags>()
                .HasOne(c => c.Parent)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.Parent_Id)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<UserJobs>()
                .HasOne(c => c.UserInformation)
                .WithMany(c => c.UserJobs)
                .HasForeignKey(c => c.UserInformationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserEducations>()
                .HasOne(c => c.UserInformation)
                .WithMany(c => c.UserEducations)
                .HasForeignKey(c => c.UserInformationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserEducations>()
                .HasOne(c => c.User)
                .WithMany(c => c.UserEducations)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserJobs>()
                .HasOne(c => c.User)
                .WithMany(c => c.UserJobs)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            modelBuilder.Entity<UserInformations>()
                .HasOne(c => c.User)
                .WithOne(c => c.UserInformations)
                .HasForeignKey<UserInformations>(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);


            modelBuilder.Entity<Friends>()
                .HasOne(c => c.User)
                .WithMany(c => c.UsersWithFriends)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Friends>()
                .HasOne(c => c.Friend)
                .WithMany(c => c.Friends)
                .HasForeignKey(c => c.FriendId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CloseFriends>()
                .HasOne(c => c.User)
                .WithMany(c => c.UsersWithCloseFriends)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CloseFriends>()
                .HasOne(c => c.CloseFriend)
                .WithMany(c => c.CloseFriends)
                .HasForeignKey(c => c.CloseFriendId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Friendships>()
                .HasIndex(f => new { f.SenderId, f.ReceiverId })
                .IsUnique();

            modelBuilder.Entity<Friendships>()
                .HasOne(c => c.Sender)
                .WithMany(c => c.FriendshipSenders)
                .HasForeignKey(c => c.SenderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Friendships>()
                .HasOne(c => c.Receiver)
                .WithMany(c => c.FriendshipReceivers)
                .HasForeignKey(c => c.ReceiverId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Notifications>()
                .HasOne(c => c.User)
                .WithMany(c => c.Notifications)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Notifications>()
                .HasOne(c => c.NotificationReceiver)
                .WithMany(c => c.NotificationsReceived)
                .HasForeignKey(c => c.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Conversations>()
                .HasOne(c => c.Quiz)
                .WithMany(c => c.QuizConversations)
                .HasForeignKey(c => c.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Conversations>()
                .HasOne(c => c.Lesson)
                .WithMany(c => c.LessonConversations)
                .HasForeignKey(c => c.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Conversations>()
                .HasOne(c => c.Course)
                .WithMany(c => c.CourseConversations)
                .HasForeignKey(c => c.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Conversations>()
                .HasOne(c => c.Sender)
                .WithMany(u => u.SentMessages) // Assuming the Users model has a collection of SentMessages
                .HasForeignKey(c => c.SenderUsername)
                .HasPrincipalKey(u => u.Username)
                .OnDelete(DeleteBehavior.Restrict);  // Prevent cascading deletes

            modelBuilder.Entity<Conversations>()
                .HasOne(c => c.Receiver)
                .WithMany(u => u.ReceivedMessages) // Assuming the Users model has a collection of ReceivedMessages
                .HasForeignKey(c => c.ReceiverUsername)
                .HasPrincipalKey(u => u.Username) // fix per shkak qe e lypke primary key e userit, a un ekom bo me username
                .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<QuizzesCompleted>()
                .HasOne(c => c.User)
                .WithMany(c => c.QuizzesCompleted)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuizzesCompleted>()
                .HasOne(c => c.Quiz)
                .WithMany(c => c.QuizzesCompleted)
                .HasForeignKey(c => c.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Quizzes>()
                .HasOne(c => c.User)
                .WithMany(c => c.Quizzes)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Quizzes>()
                .HasOne(c => c.Category)
                .WithMany(c => c.Quizzes)
                .HasForeignKey(c => c.QuizCategory)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Quizzes>()
                .HasMany(c => c.Questions)
                .WithOne(c => c.Quiz)
                .HasForeignKey(c => c.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuizQuestions>()
                .HasMany(c => c.Answers)
                .WithOne(c => c.QuizQuestion)
                .HasForeignKey(c => c.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
            
            modelBuilder.Entity<QuizAnswers>()
                .HasOne(c => c.QuizQuestion)
                .WithMany(c => c.Answers)
                .HasForeignKey(c => c.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);



            modelBuilder.Entity<CourseCompleted>()
                .HasOne(c => c.User)
                .WithMany(c => c.CourseCompleted)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CourseCompleted>()
                .HasOne(c => c.Course)
                .WithMany(c => c.CourseCompleted)
                .HasForeignKey(c => c.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Users>();

            modelBuilder.Entity<Lessons>()
                .HasOne(c => c.Course)
                .WithMany(c => c.Lessons)
                .HasForeignKey(c => c.CourseID);

            modelBuilder.Entity<Stories>();

            modelBuilder.Entity<Packages>()
                .HasMany(c => c.Payments)
                .WithOne(c => c.Package)
                .HasForeignKey(c => c.PackageID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Payments>()
                .HasOne(c => c.User)
                .WithMany(c => c.Payments)
                .HasForeignKey(c => c.UserID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Comments>()
                .HasOne(c => c.Parent)
                .WithMany(c => c.Replies)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict); //restrict per edhe nese fshihet parenti psh Parenti nket rast apo Useri qe osht parent i komentit me u fshi veq kometnat


            modelBuilder.Entity<Comments>()
                .HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade); //cascade nese useri fshihet me u fshi krejt komentat

            modelBuilder.Entity<CommentLikes>()
                .HasOne(c => c.Users)
                .WithMany(c => c.CommentLikes)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CommentLikes>()
                .HasOne(c => c.Comments)
                .WithMany(c => c.CommentLikes)
                .HasForeignKey(c => c.CommentID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LessonLikes>()
                .HasOne(c => c.Users)
                .WithMany(c => c.LessonLikes)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LessonLikes>()
                .HasOne(c => c.Lessons)
                .WithMany(c => c.LessonLikes)
                .HasForeignKey(c => c.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Usermeta>()
                .HasOne(c => c.User)
                .WithMany(c => c.UserMeta)
                .HasForeignKey(c => c.UserID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RefreshToken>();

            modelBuilder.Entity<Categories>();
            modelBuilder.Entity<MediaLibrary>();

            modelBuilder.Entity<Courses>()
                .HasOne(c => c.Category)
                .WithMany(c => c.Courses)
                .HasForeignKey(c => c.CourseCategory);

            modelBuilder.Entity<Courses>()
                .HasOne(c => c.User)
                .WithMany(c => c.CoursesCreated)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<UserProgress>()
                .HasOne(c => c.User)
                .WithMany(c => c.UserProgress)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserProgress>()
                .HasOne(c => c.Courses)
                .WithMany(c => c.CoursesProgress)
                .HasForeignKey(c => c.CourseId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<UserProgress>()
                .HasOne(c => c.Lessons)
                .WithMany(c => c.LessonProgress)
                .HasForeignKey(c => c.LessonId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Bookmarks>();
        }
    }
}
