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

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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
                .OnDelete(DeleteBehavior.Restrict);



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
            modelBuilder.Entity<Packages>();
            modelBuilder.Entity<Payments>();

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

            modelBuilder.Entity<Usermeta>();
            modelBuilder.Entity<RefreshToken>();
            modelBuilder.Entity<Categories>();
            modelBuilder.Entity<MediaLibrary>();

            modelBuilder.Entity<Courses>()
                .HasOne(c => c.Category)
                .WithMany(c => c.Courses)
                .HasForeignKey(c => c.CourseCategory);

            modelBuilder.Entity<UserProgress>();

            modelBuilder.Entity<Bookmarks>();
        }
    }
}
