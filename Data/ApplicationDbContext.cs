using IHeard.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IHeard.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<News> Newss { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Bookmark> Bookmarks { get; set; }
        public DbSet<NewsBookmark> NewsBookmarks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            base.OnModelCreating(modelBuilder);

            // definire primary key compus
            modelBuilder.Entity<NewsBookmark>()
                .HasKey(ab => new { ab.NewsBookmarkId, ab.NewsId, ab.BookmarkId });


            // definire relatii cu modelele Bookmark si News (FK)
            modelBuilder.Entity<NewsBookmark>()
                .HasOne(ab => ab.News)
                .WithMany(ab => ab.NewsBookmarks)
                .HasForeignKey(ab => ab.NewsId);

            modelBuilder.Entity<NewsBookmark>()
                .HasOne(ab => ab.Bookmark)
                .WithMany(ab => ab.NewsBookmarks)
                .HasForeignKey(ab => ab.BookmarkId);
        }
    }


}