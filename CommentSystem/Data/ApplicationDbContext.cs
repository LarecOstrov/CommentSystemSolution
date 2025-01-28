using Microsoft.EntityFrameworkCore;
using CommentSystem.Models;
using System.Collections.Generic;
using System.Xml.Linq;

namespace CommentSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Comment> Comments { get; set; }
        //public DbSet<FileAttachment> Files { get; set; }
        //public DbSet<Notification> Notifications { get; set; }
    }
}