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
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Users>();
            modelBuilder.Entity<Lessons>();
            modelBuilder.Entity<Stories>();
            modelBuilder.Entity<Packages>();
            modelBuilder.Entity<Payments>();
            modelBuilder.Entity<Comments>();
            modelBuilder.Entity<Usermeta>();
            modelBuilder.Entity<RefreshToken>();
            modelBuilder.Entity<Categories>();
            modelBuilder.Entity<MediaLibrary>();

            /*modelBuilder.Entity<Users>()
            .HasMany(u => u.UserMeta)
            .WithOne(um => um.User)
            .HasForeignKey(um => um.UserID);*/

        }
    }
}
