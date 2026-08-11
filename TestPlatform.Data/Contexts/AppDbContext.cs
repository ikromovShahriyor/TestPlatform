using Microsoft.EntityFrameworkCore;
using TestPlatform.Domain.Configuration;
using TestPlatform.Domain.Entities;

namespace TestPlatform.Data.Context;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Subject> Subjects { get; set; }
    public DbSet<Test> Tests { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<AnswerOption> AnswerOptions { get; set; }
    public DbSet<TestAttempt> TestAttempts { get; set; }
    public DbSet<StudentAnswer> StudentAnswers { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Topic> Topics { get; set; }
    public DbSet<TestTopic> TestTopics { get; set; }
    public DbSet<Certificate> Certificates { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Subject 1 --- * Test (Subject o'chganda testlari bo'lsa taqiqlash)
        modelBuilder.Entity<Subject>()
            .HasMany(s => s.Tests)
            .WithOne(t => t.Subject)
            .HasForeignKey(t => t.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        // Subject Name Unique bo'lishi shart
        modelBuilder.Entity<Subject>()
            .HasIndex(s => s.Name)
            .IsUnique();

        // 2. Test 1 --- * Question (Test o'chganda savollari o'chib ketadi)
        modelBuilder.Entity<Test>()
            .HasMany(t => t.Questions)
            .WithOne(q => q.Test)
            .HasForeignKey(q => q.TestId)
            .OnDelete(DeleteBehavior.Cascade);

        // 3. Test 1 --- * TestAttempt (Urinish bo'lsa testni o'chirish taqiqlanadi)
        modelBuilder.Entity<Test>()
            .HasMany(t => t.Attempts)
            .WithOne(ta => ta.Test)
            .HasForeignKey(ta => ta.TestId)
            .OnDelete(DeleteBehavior.Restrict);

        // 4. Question 1 --- * AnswerOption (Savol o'chsa variantlari o'chadi)
        modelBuilder.Entity<Question>()
            .HasMany(q => q.Options)
            .WithOne(o => o.Question)
            .HasForeignKey(o => o.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        // 5. TestAttempt 1 --- * StudentAnswer (Urinish o'chsa javoblar o'chadi)
        modelBuilder.Entity<TestAttempt>()
            .HasMany(ta => ta.Answers)
            .WithOne(sa => sa.TestAttempt)
            .HasForeignKey(sa => sa.TestAttemptId)
            .OnDelete(DeleteBehavior.Cascade);

        // 6. Kombinatsiya Unique: Bitta urinishda bitta savolga faqat bitta javob bo'lishi shart
        modelBuilder.Entity<StudentAnswer>()
            .HasIndex(sa => new { sa.TestAttemptId, sa.QuestionId })
            .IsUnique();

        // 7. User configurations
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .Property(u => u.Role)
            .HasConversion<string>();

        // 8. User 1 --- * TestAttempt
        modelBuilder.Entity<TestAttempt>()
            .HasOne(ta => ta.User)
            .WithMany()
            .HasForeignKey(ta => ta.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // 9. Topic and TestTopic configuration
        modelBuilder.Entity<Topic>()
            .HasIndex(t => t.Name)
            .IsUnique();

        modelBuilder.Entity<TestTopic>()
            .HasIndex(tt => new { tt.TestId, tt.TopicId })
            .IsUnique();

        modelBuilder.Entity<TestTopic>()
            .HasOne(tt => tt.Test)
            .WithMany(t => t.TestTopics)
            .HasForeignKey(tt => tt.TestId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TestTopic>()
            .HasOne(tt => tt.Topic)
            .WithMany(t => t.TestTopics)
            .HasForeignKey(tt => tt.TopicId)
            .OnDelete(DeleteBehavior.Cascade);

        // 10. Certificate configuration
        modelBuilder.Entity<Certificate>()
            .HasIndex(c => c.CertificateNumber)
            .IsUnique();

        modelBuilder.Entity<Certificate>()
            .HasOne(c => c.Attempt)
            .WithMany()
            .HasForeignKey(c => c.AttemptId)
            .OnDelete(DeleteBehavior.Cascade);

        // 11. AuditLog configuration
        modelBuilder.Entity<AuditLog>()
            .HasOne(al => al.User)
            .WithMany()
            .HasForeignKey(al => al.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // 12. Enum configurations
        modelBuilder.Entity<Test>()
            .Property(t => t.Difficulty)
            .HasConversion<string>();
    }

    // 5-band talabi: CreatedAt va UpdatedAt serverda avtomatik boshqarilishi shart
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is Auditable && (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entityEntry in entries)
        {
            var entity = (Auditable)entityEntry.Entity;
            var utcNow = DateTime.UtcNow;

            if (entityEntry.State == EntityState.Added)
            {
                entity.CreatedAt = utcNow;
            }
            else if (entityEntry.State == EntityState.Modified)
            {
                entity.UpdatedAt = utcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}