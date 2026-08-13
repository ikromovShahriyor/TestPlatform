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
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using TestPlatform.WebApi.Data;

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
    
    // So'nggi migrations qo'llash yoki jadvallarni avtomatik yaratish
    try
    {
        await dbContext.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Migration Notice] {ex.Message}");
    }

    // Guarantee all missing tables (Users, Topics, TestTopics, Certificates, AuditLogs) exist in PostgreSQL
    try
    {
        await dbContext.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""Users"" (
                ""Id"" uuid NOT NULL CONSTRAINT ""PK_Users"" PRIMARY KEY,
                ""FullName"" character varying(100) NOT NULL,
                ""Email"" character varying(100) NOT NULL,
                ""PasswordHash"" character varying(255) NOT NULL,
                ""Role"" text NOT NULL,
                ""AvatarUrl"" text NULL,
                ""IsEmailVerified"" boolean NOT NULL DEFAULT FALSE,
                ""CreatedAt"" timestamp with time zone NOT NULL,
                ""UpdatedAt"" timestamp with time zone NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Users_Email"" ON ""Users"" (""Email"");

            CREATE TABLE IF NOT EXISTS ""Topics"" (
                ""Id"" uuid NOT NULL CONSTRAINT ""PK_Topics"" PRIMARY KEY,
                ""Name"" character varying(100) NOT NULL,
                ""Description"" character varying(500) NULL,
                ""CreatedAt"" timestamp with time zone NOT NULL,
                ""UpdatedAt"" timestamp with time zone NULL
            );

            CREATE TABLE IF NOT EXISTS ""TestTopics"" (
                ""Id"" uuid NOT NULL CONSTRAINT ""PK_TestTopics"" PRIMARY KEY,
                ""TestId"" uuid NOT NULL,
                ""TopicId"" uuid NOT NULL,
                ""CreatedAt"" timestamp with time zone NOT NULL,
                ""UpdatedAt"" timestamp with time zone NULL
            );

            CREATE TABLE IF NOT EXISTS ""Certificates"" (
                ""Id"" uuid NOT NULL CONSTRAINT ""PK_Certificates"" PRIMARY KEY,
                ""CertificateNumber"" text NOT NULL,
                ""StudentName"" text NOT NULL,
                ""TestTitle"" text NOT NULL,
                ""ScorePercentage"" double precision NOT NULL,
                ""IssuedAt"" timestamp with time zone NOT NULL,
                ""AttemptId"" uuid NOT NULL,
                ""CreatedAt"" timestamp with time zone NOT NULL,
                ""UpdatedAt"" timestamp with time zone NULL
            );

            CREATE TABLE IF NOT EXISTS ""AuditLogs"" (
                ""Id"" uuid NOT NULL CONSTRAINT ""PK_AuditLogs"" PRIMARY KEY,
                ""UserId"" uuid NULL,
                ""UserEmail"" text NULL,
                ""Action"" text NOT NULL,
                ""EntityName"" text NOT NULL,
                ""EntityId"" text NULL,
                ""Details"" text NULL,
                ""IpAddress"" text NULL,
                ""Timestamp"" timestamp with time zone NOT NULL
            );

            ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""AvatarUrl"" text NULL;
            ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""IsEmailVerified"" boolean NOT NULL DEFAULT FALSE;
        ");
    }
    catch (Exception exSql)
    {
        Console.WriteLine($"[Table Guarantee Notice] {exSql.Message}");
    }

    // Seed rich data using DbSeeder
    await DbSeeder.SeedDataAsync(dbContext);
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors("AllowAll");
app.UseAuthentication(); // ⬅️ JWT authentication check
app.UseAuthorization();
app.MapControllers();

app.MapGet("/api/health", async (AppDbContext dbContext) =>
{
    try
    {
        var canConnect = await dbContext.Database.CanConnectAsync();
        if (!canConnect)
        {
            return Results.Json(new { status = "Error", dbConnected = false, message = "PostgreSQL bazasiga ulanib bo'lmadi!" }, statusCode: 500);
        }

        var usersCount = 0;
        var usersTableExists = false;
        try
        {
            usersCount = await dbContext.Users.CountAsync();
            usersTableExists = true;
        }
        catch { }

        return Results.Ok(new
        {
            status = "Healthy",
            dbConnected = true,
            usersTableExists = usersTableExists,
            usersCount = usersCount,
            message = "PostgreSQL bazasi ulanishi 100% muvaffaqiyatli ishlayapti!"
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new { status = "Error", dbConnected = false, message = ex.Message }, statusCode: 500);
    }
});

app.Run();