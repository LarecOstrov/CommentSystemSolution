using Common.Models;
using Microsoft.EntityFrameworkCore;

namespace Common.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<FileAttachment> Files { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Comment>()
            .HasOne(c => c.User)
            .WithMany(u => u.Comments)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FileAttachment>()
            .HasOne(c => c.Comment)
            .WithMany(u => u.FileAttachments)
            .HasForeignKey(c => c.CommentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
