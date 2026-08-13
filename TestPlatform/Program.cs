using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using TestPlatform.Data.Context;
using TestPlatform.Data.Repositories;
using TestPlatform.Service.Interfaces;
using TestPlatform.Service.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TestPlatform.Domain.Entities;
using TestPlatform.Domain.Enums;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// 0. Cloud Port binding (Render, Railway, Heroku)
var port = Environment.GetEnvironmentVariable("PORT") ?? "5005";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// 1. PostgreSQL ma'lumotlar bazasini ro'yxatdan o'tkazish
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var envDbUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

if (!string.IsNullOrWhiteSpace(envDbUrl))
{
    try
    {
        var databaseUri = new Uri(envDbUrl);
        var userInfo = databaseUri.UserInfo.Split(':');
        var user = userInfo.Length > 0 ? userInfo[0] : "";
        var pass = userInfo.Length > 1 ? userInfo[1] : "";
        var dbName = databaseUri.AbsolutePath.TrimStart('/');
        var dbHost = databaseUri.Host;
        var dbPort = databaseUri.Port > 0 ? databaseUri.Port : 5432;

        connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={user};Password={pass};SSL Mode=Require;Trust Server Certificate=true;";
    }
    catch
    {
        // Fallback to default connection string if parse fails
    }
}

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString);
    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});

// ====== DI (Dependency Injection) QISMI ======
// Generic Repository-ni ro'yxatdan o'tkazish
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// Biznes mantiq xizmatlarini (Servislarni) ro'yxatdan o'tkazish
builder.Services.AddScoped<ISubjectService, SubjectService>();
builder.Services.AddScoped<ITestService, TestService>();
builder.Services.AddScoped<IQuestionService, QuestionService>();
builder.Services.AddScoped<IAttemptService, AttemptService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ITopicService, TopicService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<ILeaderboardService, LeaderboardService>();
builder.Services.AddScoped<ICertificateService, CertificateService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IEmailService, EmailService>();
// ============================================

// CORS sozlamasi (GitHub Pages va boshqa manbalardan so'rovlarni qabul qilish uchun)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 2. JWT Authentication va Authorization sozlamalari
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "TestPlatformIssuer",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "TestPlatformAudience",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "SuperSecretKeyForTestPlatformAppWithAtLeast256BitsLongKeySecurity!"))
    };
});
builder.Services.AddAuthorization();

// 3. Kontrollerlar va API hujjatlashtirish xizmatlarini qo'shish
builder.Services.AddControllers();
builder.Services.AddOpenApi(); // .NET 10 OpenAPI qo'llab-quvvatlashi

var app = builder.Build();

// 4. Request pipeline (HTTP so'rovlar oqimi) sozlamalari
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // OpenAPI JSON endpointini yoqish
    app.MapScalarApiReference(); // Scalar UI oynasi (/scalar/v1)
}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    // So'nggi migrations qo'llash
    try
    {
        await dbContext.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Migration Notice] {ex.Message}");
    }

    // Ensure new columns exist on Users table if missing
    try
    {
        await dbContext.Database.ExecuteSqlRawAsync(@"
            ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""AvatarUrl"" text NULL;
            ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""IsEmailVerified"" boolean NOT NULL DEFAULT FALSE;
        ");
    }
    catch { }

    // Default Admin foydalanuvchini seed qilish
    try
    {
        var adminEmail = "ikromovshahriyor13@gmail.com";
        var adminUser = dbContext.Users.FirstOrDefault(u => u.Email == adminEmail);
        if (adminUser == null)
        {
            adminUser = dbContext.Users.FirstOrDefault(u => u.Email == "admin@test.com");
            if (adminUser != null)
            {
                adminUser.Email = adminEmail;
            }
            else
            {
                adminUser = new User { Email = adminEmail };
                dbContext.Users.Add(adminUser);
            }
        }
        adminUser.FullName = "Shahriyor Ikromov";
        adminUser.Role = UserRole.Admin;
        adminUser.IsEmailVerified = true;
        adminUser.PasswordHash = new PasswordHasher<User>().HashPassword(adminUser, "Lenovo0909");

        if (!dbContext.Users.Any(u => u.Email == "student@test.com"))
        {
            var studentUser = new User
            {
                FullName = "Student User",
                Email = "student@test.com",
                Role = UserRole.Student
            };
            studentUser.PasswordHash = new PasswordHasher<User>().HashPassword(studentUser, "123456");
            dbContext.Users.Add(studentUser);
        }

        await dbContext.SaveChangesAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Seed Warning] {ex.Message}");
    }

    // Default fanlar, topiclar va testlarni seed qilish
    try
    {
        if (!dbContext.Subjects.Any())
        {
            var subj1 = new Subject { Name = "Dasturlash (C# & .NET)", Description = "C# dasturlash tili va .NET framework asoslari" };
            var subj2 = new Subject { Name = "Matematika va Algoritmlar", Description = "Oliy matematika va algoritmlar bo'yicha imtihonlar" };
            var subj3 = new Subject { Name = "Ingliz Tili (IELTS Level)", Description = "English Grammar and General Knowledge" };

            dbContext.Subjects.AddRange(subj1, subj2, subj3);

            var topic1 = new Topic { Name = "OOP & SOLID" };
            var topic2 = new Topic { Name = "LINQ & EF Core" };
            var topic3 = new Topic { Name = "Integral va Hosila" };
            var topic4 = new Topic { Name = "Grammar & Vocabulary" };

            dbContext.Topics.AddRange(topic1, topic2, topic3, topic4);
            await dbContext.SaveChangesAsync();

            // 1-Test
            var test1 = new Test
            {
                SubjectId = subj1.Id,
                Title = "C# & .NET Core Asoslari Testi",
                Description = "C# tili, OOP tamoyillari va LINQ bo'yicha bilimingizni sinab ko'ring.",
                PassingPercentage = 60,
                DurationMinutes = 15,
                TimeLimitMinutes = 15,
                IsPublished = true,
                Difficulty = DifficultyLevel.Easy,
                MaxAttemptsPerStudent = 5,
                ShowReviewAfterSubmit = true,
                Questions = new List<Question>
                {
                    new Question
                    {
                        Text = "C# tilida obyektga yo'naltirilgan dasturlashning (OOP) asosiy 4 ta ustunidan biri qaysi?",
                        Points = 10,
                        Options = new List<AnswerOption>
                        {
                            new AnswerOption { Text = "Inkapsulyatsiya (Encapsulation)", IsCorrect = true },
                            new AnswerOption { Text = "Kompilyatsiya (Compilation)", IsCorrect = false },
                            new AnswerOption { Text = "Garbage Collection", IsCorrect = false },
                            new AnswerOption { Text = "Multithreading", IsCorrect = false }
                        }
                    },
                    new Question
                    {
                        Text = "LINQ'da ro'yxatni saralash uchun qaysi metod ishlatiladi?",
                        Points = 10,
                        Options = new List<AnswerOption>
                        {
                            new AnswerOption { Text = "OrderBy()", IsCorrect = true },
                            new AnswerOption { Text = "SortList()", IsCorrect = false },
                            new AnswerOption { Text = "Group()", IsCorrect = false },
                            new AnswerOption { Text = "Filter()", IsCorrect = false }
                        }
                    },
                    new Question
                    {
                        Text = ".NET run-time muhitining nomi nima?",
                        Points = 10,
                        Options = new List<AnswerOption>
                        {
                            new AnswerOption { Text = "CLR (Common Language Runtime)", IsCorrect = true },
                            new AnswerOption { Text = "JVM (Java Virtual Machine)", IsCorrect = false },
                            new AnswerOption { Text = "SDK (Software Dev Kit)", IsCorrect = false },
                            new AnswerOption { Text = "Kestrel Server", IsCorrect = false }
                        }
                    },
                    new Question
                    {
                        Text = "Interface va Abstract Class o'rtasidagi asosiy farq nimada?",
                        Points = 10,
                        Options = new List<AnswerOption>
                        {
                            new AnswerOption { Text = "Interface ko'p karrali merosxo'rlikni qo'llab-quvvatlaydi", IsCorrect = true },
                            new AnswerOption { Text = "Interface constructorga ega bo'lishi shart", IsCorrect = false },
                            new AnswerOption { Text = "Abstract Class private metodlarga ega bo'la olmaydi", IsCorrect = false },
                            new AnswerOption { Text = "Hech qanday farq yo'q", IsCorrect = false }
                        }
                    }
                }
            };

            // 2-Test
            var test2 = new Test
            {
                SubjectId = subj2.Id,
                Title = "Matematik Analiz va Tenglamalar",
                Description = "Oliy matematika, hosila va integral masalalari.",
                PassingPercentage = 70,
                DurationMinutes = 20,
                TimeLimitMinutes = 20,
                IsPublished = true,
                Difficulty = DifficultyLevel.Medium,
                MaxAttemptsPerStudent = 3,
                ShowReviewAfterSubmit = true,
                Questions = new List<Question>
                {
                    new Question
                    {
                        Text = "f(x) = x^2 funksiyaning hosilasi (derivative) nimaga teng?",
                        Points = 10,
                        Options = new List<AnswerOption>
                        {
                            new AnswerOption { Text = "2x", IsCorrect = true },
                            new AnswerOption { Text = "x", IsCorrect = false },
                            new AnswerOption { Text = "x^3 / 3", IsCorrect = false },
                            new AnswerOption { Text = "2", IsCorrect = false }
                        }
                    },
                    new Question
                    {
                        Text = "sin^2(x) + cos^2(x) ning qiymati nechaga teng?",
                        Points = 10,
                        Options = new List<AnswerOption>
                        {
                            new AnswerOption { Text = "1", IsCorrect = true },
                            new AnswerOption { Text = "0", IsCorrect = false },
                            new AnswerOption { Text = "2", IsCorrect = false },
                            new AnswerOption { Text = "tan(x)", IsCorrect = false }
                        }
                    }
                }
            };

            dbContext.Tests.AddRange(test1, test2);
            await dbContext.SaveChangesAsync();

            dbContext.TestTopics.AddRange(
                new TestTopic { TestId = test1.Id, TopicId = topic1.Id },
                new TestTopic { TestId = test1.Id, TopicId = topic2.Id },
                new TestTopic { TestId = test2.Id, TopicId = topic3.Id }
            );
            await dbContext.SaveChangesAsync();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Subject/Topic Seed Warning] {ex.Message}");
    }
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors("AllowAll");
app.UseAuthentication(); // ⬅️ JWT authentication check
app.UseAuthorization();
app.MapControllers();

app.Run();