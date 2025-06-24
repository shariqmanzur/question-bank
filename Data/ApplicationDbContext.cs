using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QuestionBank.Models;

namespace QuestionBank.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets for your entities
        public DbSet<Theme> Themes { get; set; }
        public DbSet<Topic> Topics { get; set; }
        public DbSet<CompetencyLevel> CompetencyLevels { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<BCQAnswer> BCQAnswers { get; set; }
        public DbSet<QuestionReview> QuestionReviews { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Paper> Papers { get; set; }
        public DbSet<PaperQuestion> PaperQuestions { get; set; }

        public DbSet<PaperFilter> PaperFilters { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // NOTE: Removed ApplyConfigurationsFromAssembly since all configurations are inline

            // Configure Topic -> Theme (Cascade Delete)
            builder.Entity<Topic>()
                .HasOne(t => t.Theme)
                .WithMany(th => th.Topics)
                .HasForeignKey(t => t.ThemeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Question -> Topic (Cascade Delete)
            builder.Entity<Question>()
                .HasOne(q => q.Topic)
                .WithMany(t => t.Questions)
                .HasForeignKey(q => q.TopicId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Question -> CompetencyLevel (Restrict Delete)
            builder.Entity<Question>()
                .HasOne(q => q.CompetencyLevel)
                .WithMany(cl => cl.Questions)
                .HasForeignKey(q => q.CompetencyLevelId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure BCQAnswer -> Question (Cascade Delete)
            builder.Entity<BCQAnswer>()
                .HasOne(a => a.Question)
                .WithMany(q => q.BCQAnswers)
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Notification -> Question (Cascade Delete)
            builder.Entity<Notification>()
                .HasOne(n => n.Question)
                .WithMany(q => q.Notifications)
                .HasForeignKey(n => n.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure composite primary key for PaperQuestion
            builder.Entity<PaperQuestion>()
                .HasKey(pq => new { pq.PaperId, pq.QuestionId });

            // Configure relationships for PaperQuestion
            builder.Entity<PaperQuestion>()
                .HasOne(pq => pq.Paper)
                .WithMany(p => p.PaperQuestions)
                .HasForeignKey(pq => pq.PaperId);

            builder.Entity<PaperQuestion>()
                .HasOne(pq => pq.Question)
                .WithMany()
                .HasForeignKey(pq => pq.QuestionId);
        }
    }
}