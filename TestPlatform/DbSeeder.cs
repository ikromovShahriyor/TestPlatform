using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestPlatform.Data.Context;
using TestPlatform.Domain.Entities;
using TestPlatform.Domain.Enums;

namespace TestPlatform.WebApi.Data;

public static class DbSeeder
{
    public static async Task SeedDataAsync(AppDbContext dbContext)
    {
        // 1. Seed Users (Admin and Student)
        try
        {
            var adminEmail = "ikromovshahriyor13@gmail.com";
            var adminUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
            if (adminUser == null)
            {
                adminUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == "admin@test.com");
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

            if (!await dbContext.Users.AnyAsync(u => u.Email == "student@test.com"))
            {
                var studentUser = new User
                {
                    FullName = "Student User",
                    Email = "student@test.com",
                    Role = UserRole.Student,
                    IsEmailVerified = true
                };
                studentUser.PasswordHash = new PasswordHasher<User>().HashPassword(studentUser, "123456");
                dbContext.Users.Add(studentUser);
            }

            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Seed Users Warning] {ex.Message}");
        }

        // 2. Seed Subjects, Topics, Tests, and Questions if no tests exist yet
        try
        {
            if (await dbContext.Tests.AnyAsync(t => t.Questions.Count >= 20))
            {
                return; // Data already seeded
            }

            // Create Subjects
            var subj1 = new Subject { Name = "Dasturlash (C# & .NET Core)", Description = "C# dasturlash tili, OOP, LINQ, EF Core va ASP.NET Core Web API" };
            var subj2 = new Subject { Name = "Python & Sun'iy Intellekt", Description = "Python tili, Data Science, NumPy, Pandas va Machine Learning" };
            var subj3 = new Subject { Name = "Web Dasturlash (Frontend)", Description = "HTML5, CSS3, JavaScript ES6+, DOM manipulation va React.js" };
            var subj4 = new Subject { Name = "PostgreSQL & Ma'lumotlar Bazasi", Description = "SQL so'rovlari, PostgreSQL administration, Indexes, Triggers va ACID" };
            var subj5 = new Subject { Name = "Kompyuter Tarmoqlari & Xavfsizlik", Description = "OSI modeli, TCP/IP, IP subnets, HTTP/HTTPS, Firewall va Kiberxavfsizlik" };
            var subj6 = new Subject { Name = "Ingliz Tili (IELTS & Professional)", Description = "Advanced Grammar, Business Vocabulary, Reading and Contextual Usage" };

            var existingSubjects = await dbContext.Subjects.ToListAsync();
            foreach (var s in new[] { subj1, subj2, subj3, subj4, subj5, subj6 })
            {
                if (!existingSubjects.Any(x => x.Name.Trim().Equals(s.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    dbContext.Subjects.Add(s);
                }
                else
                {
                    var existing = existingSubjects.First(x => x.Name.Trim().Equals(s.Name.Trim(), StringComparison.OrdinalIgnoreCase));
                    s.Id = existing.Id;
                }
            }
            await dbContext.SaveChangesAsync();

            // Create Topics
            var topicOOP = new Topic { Name = "OOP & Architecture" };
            var topicNet = new Topic { Name = ".NET & Memory Management" };
            var topicPyData = new Topic { Name = "Python Data Analysis" };
            var topicWebFE = new Topic { Name = "JavaScript & React" };
            var topicDb = new Topic { Name = "SQL Optimization" };
            var topicSec = new Topic { Name = "Network Security" };
            var topicEng = new Topic { Name = "Advanced English Grammar" };

            var existingTopics = await dbContext.Topics.ToListAsync();
            foreach (var top in new[] { topicOOP, topicNet, topicPyData, topicWebFE, topicDb, topicSec, topicEng })
            {
                if (!existingTopics.Any(x => x.Name.Trim().Equals(top.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    dbContext.Topics.Add(top);
                }
                else
                {
                    var existing = existingTopics.First(x => x.Name.Trim().Equals(top.Name.Trim(), StringComparison.OrdinalIgnoreCase));
                    top.Id = existing.Id;
                }
            }
            await dbContext.SaveChangesAsync();

            // -------------------------------------------------------------
            // TEST 1: C# & .NET Core Professional Sertifikat Imtihoni (30 Questions, 30 Minutes)
            // -------------------------------------------------------------
            var test1 = new Test
            {
                SubjectId = subj1.Id,
                Title = "C# & .NET Core Professional Sertifikat Imtihoni",
                Description = "30 ta savoldan iborat rasmiy sertifikat testi. Har bir savol uchun 1 minut ajratilgan.",
                PassingPercentage = 70,
                DurationMinutes = 30,
                TimeLimitMinutes = 30,
                IsPublished = true,
                Difficulty = DifficultyLevel.Hard,
                MaxAttemptsPerStudent = 5,
                ShowReviewAfterSubmit = true,
                Questions = CreateCSharp30Questions()
            };

            // -------------------------------------------------------------
            // TEST 2: C# OOP va LINQ Amaliy Testi (20 Questions, 20 Minutes)
            // -------------------------------------------------------------
            var test2 = new Test
            {
                SubjectId = subj1.Id,
                Title = "C# OOP va LINQ Amaliy Testi",
                Description = "20 ta savoldan iborat Obyektga Yo'naltirilgan Dasturlash va LINQ bilimlari testi.",
                PassingPercentage = 60,
                DurationMinutes = 20,
                TimeLimitMinutes = 20,
                IsPublished = true,
                Difficulty = DifficultyLevel.Medium,
                MaxAttemptsPerStudent = 5,
                ShowReviewAfterSubmit = true,
                Questions = CreateCSharp20Questions()
            };

            // -------------------------------------------------------------
            // TEST 3: Python & AI Data Science Sertifikat Imtihoni (30 Questions, 30 Minutes)
            // -------------------------------------------------------------
            var test3 = new Test
            {
                SubjectId = subj2.Id,
                Title = "Python & AI Data Science Sertifikat Imtihoni",
                Description = "30 ta savoldan iborat Sun'iy Intellekt va Python dasturlash sertifikat imtihoni.",
                PassingPercentage = 70,
                DurationMinutes = 30,
                TimeLimitMinutes = 30,
                IsPublished = true,
                Difficulty = DifficultyLevel.Hard,
                MaxAttemptsPerStudent = 5,
                ShowReviewAfterSubmit = true,
                Questions = CreatePython30Questions()
            };

            // -------------------------------------------------------------
            // TEST 4: Frontend JavaScript & React Professional Testi (20 Questions, 20 Minutes)
            // -------------------------------------------------------------
            var test4 = new Test
            {
                SubjectId = subj3.Id,
                Title = "Frontend JavaScript & React Professional Testi",
                Description = "20 ta savoldan iborat Zamonaviy Web va React.js injiniring testi.",
                PassingPercentage = 65,
                DurationMinutes = 20,
                TimeLimitMinutes = 20,
                IsPublished = true,
                Difficulty = DifficultyLevel.Medium,
                MaxAttemptsPerStudent = 5,
                ShowReviewAfterSubmit = true,
                Questions = CreateFrontend20Questions()
            };

            // -------------------------------------------------------------
            // TEST 5: PostgreSQL Database Administrator Testi (20 Questions, 20 Minutes)
            // -------------------------------------------------------------
            var test5 = new Test
            {
                SubjectId = subj4.Id,
                Title = "PostgreSQL Database Administrator Testi",
                Description = "20 ta savoldan iborat SQL so'rovlari va Ma'lumotlar bazasi arxitekturasi testi.",
                PassingPercentage = 65,
                DurationMinutes = 20,
                TimeLimitMinutes = 20,
                IsPublished = true,
                Difficulty = DifficultyLevel.Medium,
                MaxAttemptsPerStudent = 5,
                ShowReviewAfterSubmit = true,
                Questions = CreatePostgres20Questions()
            };

            // -------------------------------------------------------------
            // TEST 6: Kompyuter Tarmoqlari & Kiberxavfsizlik Testi (20 Questions, 20 Minutes)
            // -------------------------------------------------------------
            var test6 = new Test
            {
                SubjectId = subj5.Id,
                Title = "Kompyuter Tarmoqlari & Kiberxavfsizlik Testi",
                Description = "20 ta savoldan iborat OSI modeli, Tarmoq protokollari va Xavfsizlik imtihoni.",
                PassingPercentage = 65,
                DurationMinutes = 20,
                TimeLimitMinutes = 20,
                IsPublished = true,
                Difficulty = DifficultyLevel.Medium,
                MaxAttemptsPerStudent = 5,
                ShowReviewAfterSubmit = true,
                Questions = CreateNetwork20Questions()
            };

            // -------------------------------------------------------------
            // TEST 7: English Grammar & Academic Vocabulary Sertifikat Imtihoni (30 Questions, 30 Minutes)
            // -------------------------------------------------------------
            var test7 = new Test
            {
                SubjectId = subj6.Id,
                Title = "English Grammar & Academic Vocabulary Sertifikat Imtihoni",
                Description = "30 ta savoldan iborat Rasmiy ingliz tili sertifikat testi.",
                PassingPercentage = 70,
                DurationMinutes = 30,
                TimeLimitMinutes = 30,
                IsPublished = true,
                Difficulty = DifficultyLevel.Hard,
                MaxAttemptsPerStudent = 5,
                ShowReviewAfterSubmit = true,
                Questions = CreateEnglish30Questions()
            };

            dbContext.Tests.AddRange(test1, test2, test3, test4, test5, test6, test7);
            await dbContext.SaveChangesAsync();

            // Link TestTopics
            dbContext.TestTopics.AddRange(
                new TestTopic { TestId = test1.Id, TopicId = topicOOP.Id },
                new TestTopic { TestId = test1.Id, TopicId = topicNet.Id },
                new TestTopic { TestId = test2.Id, TopicId = topicOOP.Id },
                new TestTopic { TestId = test3.Id, TopicId = topicPyData.Id },
                new TestTopic { TestId = test4.Id, TopicId = topicWebFE.Id },
                new TestTopic { TestId = test5.Id, TopicId = topicDb.Id },
                new TestTopic { TestId = test6.Id, TopicId = topicSec.Id },
                new TestTopic { TestId = test7.Id, TopicId = topicEng.Id }
            );
            await dbContext.SaveChangesAsync();
            Console.WriteLine("[DbSeeder] 170+ savol va 7 ta professional test muvaffaqiyatli saqlandi!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Seed Data Error] {ex.Message}");
        }
    }

    private static Question Q(string text, string correct, string w1, string w2, string w3, int points = 10)
    {
        return new Question
        {
            Text = text,
            Points = points,
            Options = new List<AnswerOption>
            {
                new AnswerOption { Text = correct, IsCorrect = true },
                new AnswerOption { Text = w1, IsCorrect = false },
                new AnswerOption { Text = w2, IsCorrect = false },
                new AnswerOption { Text = w3, IsCorrect = false }
            }
        };
    }

    private static List<Question> CreateCSharp30Questions()
    {
        var list = new List<Question>();
        list.Add(Q("C# tilida OOP ning asosiy 4 ta tamoyilidan biri qaysi?", "Polimorfizm", "Garbage Collection", "Multithreading", "JIT Compilation"));
        list.Add(Q("C# da 'struct' va 'class' o'rtasidagi asosiy farq nimada?", "struct - Value type, class - Reference type", "struct ko'p karrali merosxo'rlikni qo'llaydi", "class statik bo'la olmaydi", "Hech qanday farq yo'q"));
        list.Add(Q("CLR (Common Language Runtime) ning vazifasi nima?", "Kodni xotirada boshqarish va JIT orqali bajarish", "HTML sahifalarni render qilish", "Faqat SQL so'rovlarni bajarish", "CSS stillarni kompilyatsiya qilish"));
        list.Add(Q("LINQ so'rovida ro'yxatni saralash uchun qaysi metod ishlatiladi?", "OrderBy()", "SortList()", "Group()", "Filter()"));
        list.Add(Q("C# 8.0 dan boshlab qaysi kalit so'zi yordamida unhandled xotira resurslari avtomatik yopiladi?", "using", "dispose", "close", "finalize"));
        list.Add(Q("GC (Garbage Collector) qaysi turdagi obyektlarni tozalaydi?", "Managed Heap'dagi foydalanilmayotgan reference type obyektlarni", "Stack'dagi value type'larni", "Fayllar tizimidagi rasmlarni", "Hard diskdagi fayllarni"));
        list.Add(Q(".NET Core da Dependency Injection (DI) xizmatining 'Transient' umr ko'rish muddati nimani bildiradi?", "Har bir so'rovda yangi obyekt yaratiladi", "Butilik ilova bo'yicha yagona singleton yaratiladi", "Faqat bitta HTTP so'rovida bitta obyekt ishlatiladi", "Xotirada hech qachon o'chmaydi"));
        list.Add(Q("ASP.NET Core'da Middleware qanday vazifani bajaradi?", "HTTP so'rov va javoblar oqimini (pipeline) qayta ishlaydi", "Faqat ma'lumotlar bazasiga ulanadi", "HTML fayllarni saqlaydi", "C# sinflarini kompilyatsiya qiladi"));
        list.Add(Q("C# da 'async' va 'await' kalit so'zlari nimaga xizmat qiladi?", "Asinxron operatsiyalarni bloklamasdan bajarish uchun", "Kodni tezroq kompilyatsiya qilish uchun", "Xotirani tozalash uchun", "Thread'ni abadiy to'xtatish uchun"));
        list.Add(Q("EF Core da 'DbSet<T>' nimani ifodalaydi?", "Ma'lumotlar bazasidagi muayyan jadvalni", "HTTP Controller'ni", "JSON faylini", "Kompilyator sozlamalarini"));
        list.Add(Q("Interface va Abstract Class o'rtasidagi asosiy farq nimada?", "Interface ko'p karrali merosxo'rlikni qo'llab-quvvatlaydi", "Abstract class metod konstruktoriga ega emas", "Interface ichida maydonlar (fields) saqlanadi", "Abstract class namuna (instance) oladi"));
        list.Add(Q("C# da 'sealed' kalit so'zi sinfga (class) qo'llansa nima bo'ladi?", "Sinfdan meros (inheritance) olib bo'lmaydi", "Sinfni ishga tushirib bo'lmaydi", "Sinf faqat statik bo'ladi", "Sinf xotiradan o'chmaydi"));
        list.Add(Q("ValueType turlari xotiraning qaysi qismida saqlanadi?", "Stack xotirada", "Heap xotirada", "Virtual Memory'da", "Cache xotirada"));
        list.Add(Q("boxing va unboxing hodisasi nimani bildiradi?", "Value type va Reference type o'rtasidagi o'zaro o'girilish", "Fayllarni arxivlash", "Kodni shifrlash", "Dasturni Docker'ga joylash"));
        list.Add(Q("C# da 'delegate' nima?", "Metodlarga ishora qiluvchi ko'rsatkich (pointer) turi", "O'zgaruvchi turi", "Kompilyator buyrug'i", "Ma'lumotlar bazasi indeksi"));
        list.Add(Q("EF Core da 'Include()' metodi nima uchun ishlatiladi?", "Eager loading - bog'liq jadvallarni birga yuklash uchun", "Jadvalni o'chirish uchun", "Yangi ustun qo'shish uchun", "SQL tranzaksiyani bekor qilish uchun"));
        list.Add(Q("ASP.NET Core da JWT (JSON Web Token) nima uchun ishlatiladi?", "Foydalanuvchilarni autentifikatsiya va avtorizatsiya qilish uchun", "HTML ni shifrlash uchun", "Baza hajmini kichraytirish uchun", "Rasmlarni siqish uchun"));
        list.Add(Q("C# da 'yield return' nimani qaytaradi?", "IEnumerable kolleksiyasini elementma-element iteratsiya qiladi", "Faqat bitta integer qaytaradi", "Dasturni to'xtatadi", "Exception otadi"));
        list.Add(Q("SOLID tamoyillarida 'S' harfi nimani bildiradi?", "Single Responsibility Principle (Yagona Mas'uliyat)", "Simple System Principle", "Secure Socket Principle", "Static Service Principle"));
        list.Add(Q("SOLID tamoyillarida 'O' harfi nimani bildiradi?", "Open/Closed Principle (Kengayishga ochiq, o'zgarishga yopiq)", "Object Oriented Principle", "Overloading Principle", "Operation Principle"));
        list.Add(Q("C# da 'record' turi klassdan nimasi bilan farq qiladi?", "Immutability (o'zgarmaslik) va qiymat bo'yicha tenglikni solishtirish bilan", "Record meros ololmaydi", "Record statik bo'la olmaydi", "Record faqat int saqlaydi"));
        list.Add(Q("System.Threading.Tasks.Task sinfi nimani ifodalaydi?", "Kelajakda bajariladigan asinxron ishni (promise)", "Faqat bir vaqtdagi thread'ni", "Ma'lumotlar strukturasini", "Timer operatsiyasini"));
        list.Add(Q("EF Core da 'AsNoTracking()' metodi qachon ishlatiladi?", "Faqat o'qish uchun mo'ljallangan so'rovlarda tezlikni oshirish uchun", "Ma'lumotni tahrirlashda", "Baza yaratishda", "Tranzaksiyani yoqishda"));
        list.Add(Q("ASP.NET Core Web API da HTTP 200 OK status kodi nimani bildiradi?", "So'rov muvaffaqiyatli bajarildi", "Resurs topilmadi", "Serverda ichki xatolik", "Ruxsat yo'q"));
        list.Add(Q("C# da 'const' va 'readonly' o'rtasidagi farq nimada?", "const kompilyatsiya vaqtida, readonly esa konstruktorda belgilanadi", "const o'zgaradi, readonly o'zgarmaydi", "Hech qanday farq yo'q", "readonly faqat int bo'ladi"));
        list.Add(Q("String va StringBuilder o'rtasidagi farq nima?", "String o'zgarmas (immutable), StringBuilder esa o'zgaruvchan (mutable)", "StringBuilder sekinroq ishlaydi", "String xotira egallamaydi", "Ikkala bir xil"));
        list.Add(Q("C# da Exception xatolarini ushlash uchun qaysi blok ishlatiladi?", "try - catch - finally", "if - else - then", "switch - case", "do - while"));
        list.Add(Q("REST API da HTTP POST metodi nimaga mo'ljallangan?", "Yangi resurs yaratish uchun", "Mavjud resursni o'chirish uchun", "Faqat resursni o'qish uchun", "Serverni o'chirish uchun"));
        list.Add(Q("ASP.NET Core da CORS (Cross-Origin Resource Sharing) nima uchun kerak?", "Boshqa domenlardan so'rovlarni qabul qilish xavfsizligini boshqarish uchun", "Ma'lumotlar bazasini tezlashtirish uchun", "Kodni obfuscation qilish uchun", "JWT token yaratish uchun"));
        list.Add(Q("C# 10.0 dagi 'global using' nimani ta'minlaydi?", "Using importlarini butun loyiha bo'yicha bir marta e'lon qilishni", "Faqat bitta faylda ishlatishni", "Kodni shifrlashni", "Baza ulashni"));
        return list;
    }

    private static List<Question> CreateCSharp20Questions()
    {
        var list = new List<Question>();
        list.Add(Q("Obyektga yo'naltirilgan dasturlashda Encapsulation nima?", "Ma'lumotlarni yashirish va faqat metodlar orqali murojaat qilish", "Kodni ko'paytirish", "Sinfni o'chirish", "Baza bilan ishlash"));
        list.Add(Q("Inheritance (Merosxo'rlik) ning maqsadi nima?", "Mavjud sinf kodi va mantiqini qayta ishlatish (code reuse)", "Xotirani tozalash", "Faqat statik metodlar yaratish", "So'rovlarni sekinlashtirish"));
        list.Add(Q("Polymorphism qanday amalga oshiriladi?", "Method Overriding (virtual/override) va Overloading orqali", "Faqat private o'zgaruvchilar bilan", "Faqat interface o'chirib", "Dasturni to'xtatib"));
        list.Add(Q("Abstraction nimani anglatadi?", "Faqat kerakli xususiyatlarni ko'rsatish va murakkab detallarni yashirish", "Barcha kodni bitta faylga yozish", "Xatolarni inkor qilish", "Kompilyatsiyasiz yurgizish"));
        list.Add(Q("LINQ'da 'Where()' metodi nima qiladi?", "Kolleksiyani shart bo'yicha filtrlash", "Elementlarni saralash", "Guruhlash", "Element qo'shish"));
        list.Add(Q("LINQ'da 'Select()' metodi nima vazifani bajaradi?", "Elementlarni yangi shaklga proyeksiyalash (mapping)", "Filtrlash", "O'chirish", "Hisoblash"));
        list.Add(Q("LINQ'da 'FirstOrDefault()' agar element topilmasa nima qaytaradi?", "Turdagi default qiymatni (masalan null yoki 0)", "Exception otadi", "Dastur qulaydi", "Bo'sh ro'yxat"));
        list.Add(Q("LINQ'da 'GroupBy()' nimaga xizmat qiladi?", "Elementlarni kalit bo'yicha guruhlash uchun", "Saralash uchun", "Elementni o'chirish uchun", "Faqat sana bo'yicha ajratish"));
        list.Add(Q("C# da 'static' metod nima?", "Sinf obyektini olmay turib to'g'ridan-to'g'ri chaqiriladigan metod", "Faqat bir marta ishlaydigan metod", "Private metod", "Virtual metod"));
        list.Add(Q("C# da 'virtual' kalit so'zi nimani bildiradi?", "Metodni voris sinfda 'override' qilish mumkinligini", "Metod o'zgarmasligini", "Metod xotirada saqlanmasligini", "Metod statikligini"));
        list.Add(Q("C# da 'override' kalit so'zi qachon ishlatiladi?", "Ajdod sinfdagi virtual metodni qayta ta'riflashda", "Yangi metod yaratishda", "Konstruktor chaqirishda", "Interface yo'qotishda"));
        list.Add(Q("C# da 'base' kalit so'zi nimani bildiradi?", "Ajdod (parent) sinfga murojaat qilishni", "Joriy obyektga murojaatni", "Statik sinfni", "Baza manzilini"));
        list.Add(Q("C# da 'this' kalit so'zi nimani bildiradi?", "Joriy sinf obyektiga murojaat qilishni", "Ajdod sinfni", "Baza jadvalini", "Exception ni"));
        list.Add(Q("LINQ'da 'Any()' metodi nima qaytaradi?", "Kolleksiyada kamida bitta element mos kelsa true, aks holda false", "Integer son", "String matn", "Exception"));
        list.Add(Q("LINQ'da 'Count()' nima qiladi?", "Kolleksiyadagi elementlar sonini qaytaradi", "Yig'indini beradi", "O'rtachasini topadi", "Filtrlaydi"));
        list.Add(Q("LINQ'da 'Sum()' metodi nimani hisoblaydi?", "Elementlarning sonli yig'indisini", "Ko'paytmasini", "Soni", "Eng kattasini"));
        list.Add(Q("C# da Constructor (Konstruktor) nima?", "Sinf olinganda (instance) avtomatik ishga tushadigan maxsus metod", "Xotirani tozalovchi metod", "Interface metodi", "Statik o'zgaruvchi"));
        list.Add(Q("C# da Destructor (~ClassName) qachon ishlaydi?", "Obyekt xotiradan tozalanganda (GC tomonidan)", "Sinf yaratilganda", "Dastur boshlanganda", "Exception bo'lganda"));
        list.Add(Q("C# da 'params' kalit so'zi nima imkoniyat beradi?", "Metodga o'zgaruvchan sondagi argumentlarni uzatishni", "Parol shifrlashni", "Baza yaratishni", "Async qilishni"));
        list.Add(Q("C# da Nullable types (T?) nimani bildiradi?", "Value type o'zgaruvchisi null qiymat qabul qila olishini", "Faqat string bo'lishini", "Baza o'chishini", "Statik bo'lishini"));
        return list;
    }

    private static List<Question> CreatePython30Questions()
    {
        var list = new List<Question>();
        list.Add(Q("Python tilida o'zgaruvchan (mutable) ma'lumot turi qaysi?", "List (Ro'yxat)", "Tuple (Kortej)", "String (Matn)", "Int (Butun son)"));
        list.Add(Q("Python'da funksiya e'lon qilish uchun qaysi kalit so'z ishlatiladi?", "def", "function", "fn", "create"));
        list.Add(Q("Python'da 'lambda' nima?", "Nomsiz (anonymous) bir qatorli funksiya", "Sinf konstruktori", "Modul nomi", "Sikl turi"));
        list.Add(Q("NumPy kutubxonasida ko'p o'lchamli massiv qanday deyiladi?", "ndarray", "list", "matrix_array", "vector_list"));
        list.Add(Q("Pandas'da 2 o me'moriy jadval ko'rinishidagi ma'lumot strukturasi nima deyiladi?", "DataFrame", "Series", "Panel", "TableSet"));
        list.Add(Q("Python'da 'GIL' (Global Interpreter Lock) nimani cheklaydi?", "Bir vaqtning o'zida faqat bitta OS thread Python bytecode bajarishini", "Xotira hajmini", "Fayllar sonini", "Baza ulashni"));
        list.Add(Q("Machine Learning'da 'Overfitting' nimani anglatadi?", "Model o'quv ma'lumotlarini yodlab olib, yangi ma'lumotda yomon natija berishi", "Model juda sekin ishlashi", "Ma'lumotlar kamligi", "Model aniqligi 0 bo'lishi"));
        list.Add(Q("Supervised Learning (Nazorat ostidagi ta'lim) ning belgilovchi xususiyati nima?", "O'quv ma'lumotlarida belgilangan target (label) larning mavjudligi", "Label yo'qligi", "Faqat rasmlar bilan ishlashi", "Neyron tarmoq ishlatmasligi"));
        list.Add(Q("Scikit-learn kutubxonasida modelni o'rgatish uchun qaysi metod ishlatiladi?", "fit()", "train()", "learn()", "compile()"));
        list.Add(Q("Neural Network (Neyron tarmoq) da Activation Function vazifasi nima?", "Noliziylikni (non-linearity) kiritish", "Vaznlarni nol qilish", "Xotirani tozalash", "Faylga yozish"));
        list.Add(Q("Python'da 'list comprehension' nimaga xizmat qiladi?", "Ro'yxatni ixcham va tezkor yaratish uchun", "Faylni o'chirish uchun", "Sinf yaratish uchun", "Xatoni ushlash uchun"));
        list.Add(Q("Python'da 'decorator' nima?", "Funksiya kodi va xatti-harakatini o'zgartirmasdan kengaytiruvchi funksiya", "Rasm chizuvchi modul", "Baza indeksi", "HTML tegi"));
        list.Add(Q("Python'da 'generator' lar qaysi kalit so'z orqali qiymat qaytaradi?", "yield", "return", "emit", "send"));
        list.Add(Q("Pandas'da ma'lumotlarni fayldan o'qish uchun qaysi metod ishlatiladi?", "pd.read_csv()", "pd.open_file()", "pd.get_csv()", "pd.load()"));
        list.Add(Q("Deep Learning'da 'CNN' (Convolutional Neural Network) asosan nimada qo'llaniladi?", "Tasvirlarga ishlov berish va kompyuter ko'rishi (Computer Vision)", "Faqat matnlarni tarjima qilishda", "Ovoz yozishda", "Baza optimallashtirishda"));
        list.Add(Q("Deep Learning'da 'RNN' (Recurrent Neural Network) qaysi turdagi ma'lumotlarga mos?", "Ketma-ketlik va vaqt qatorlari (Sequence & NLP data)", "Faqat 3D modellarga", "Faqat rasmlarga", "Faqat raqamlarga"));
        list.Add(Q("Python'da '__init__' metodi nima?", "Sinf konstruktori (obyekt initsializatsiyasi)", "Destruktor", "Statik metod", "Modul"));
        list.Add(Q("Python'da 'try-except' bloki nimaga kerak?", "Xatoliklarni (exceptions) ushlash va boshqarish uchun", "Sikl yurgizish uchun", "Fayl ochish uchun", "Kodni o'chirish uchun"));
        list.Add(Q("Matplotlib kutubxonasi nima uchun ishlatiladi?", "Ma'lumotlarni vizuallashtirish va grafiklar chizish uchun", "Baza ulash uchun", "Web server yuritish uchun", "Audio pleyer uchun"));
        list.Add(Q("Python'da 'is' va '==' o'rtasidagi farq nima?", "'is' xotira manzilini (identity), '==' esa qiymat tengligini tekshiradi", "Farq yo'q", "'==' xotira manzilini tekshiradi", "'is' faqat int uchun"));
        list.Add(Q("NLP (Natural Language Processing) da 'Tokenization' nima?", "Matnni so'zlar yoki belgilar bo'laklariga ajratish", "Matnni tarjima qilish", "Matnni o'chirish", "Parol yaratish"));
        list.Add(Q("Machine Learning metrics: 'Accuracy' nimani beradi?", "To'g'ri bashorat qilingan namunalarning umumiy namunalar nisbati", "Faqat xatolar soni", "Model tezligi", "Xotira hajmi"));
        list.Add(Q("Scikit-learn da model bashorat qilish uchun qaysi metod ishlatiladi?", "predict()", "forecast()", "guess()", "evaluate()"));
        list.Add(Q("Python'da 'pip' nima?", "Paket va kutubxonalarni o'rnatuvchi menejer", "Dasturlash tili", "Kompilyator", "Matn muharriri"));
        list.Add(Q("PyTorch va TensorFlow nima?", "Chuqur o'rganish (Deep Learning) freymvorklari", "Web brauzerlar", "Ma'lumotlar bazalari", "Operatsion tizimlar"));
        list.Add(Q("Python'da 'set' (to'plam) ning asosiy xususiyati nima?", "Elementlar unikal (taktiklanmaydigan) va tartibsiz bo me me me me", "Elementlar takrorlanadi", "Faqat kalit-qiymat saqlaydi", "O'zgarmaydi"));
        list.Add(Q("Python'da 'dictionary' qanday struktura?", "Key-Value (Kalit-Qiymat) juftligi", "Faqat indeksli ro'yxat", "Matnlar to'plami", "Baza jadvali"));
        list.Add(Q("Machine Learning da 'Confusion Matrix' nima uchun kerak?", "Klassifikatsiya modeli natijalari (TP, FP, TN, FN) ni baholash uchun", "Xotira xatosini topish uchun", "Rasmni siqish uchun", "Modelni o'chirish uchun"));
        list.Add(Q("Gradient Descent algoritmining maqsadi nima?", "Yo'qotish funksiyasini (Loss function) minimallashtirish", "Vaznlarni oshirish", "Ma'lumotni o'chirish", "Vaqtni oshirish"));
        list.Add(Q("Python 3 da print() nima?", "Ichki o'rnatilgan funksiya", "Kalit so'z", "Sinf", "Modul"));
        return list;
    }

    private static List<Question> CreateFrontend20Questions()
    {
        var list = new List<Question>();
        list.Add(Q("HTML5 da semantik teg qaysi?", "<article>", "<div>", "<span>", "<b>"));
        list.Add(Q("CSS Flexbox da elementlarni gorizontal o'q bo'yicha tekislash uchun qaysi xususiyat ishlatiladi?", "justify-content", "align-items", "flex-direction", "grid-gap"));
        list.Add(Q("JavaScript da 'const' bilan e'lon qilingan o'zgaruvchiga qayta qiymat biriktirish mumkinmi?", "Yo'q, qayta biriktirib bo'lmaydi", "Ha, bemalol", "Faqat number bo'lsa", "Faqat funksiya ichida"));
        list.Add(Q("JavaScript Event Loop ning vazifasi nima?", "Asinxron vazifalarni (micro/macro tasks) call stack ga o'tkazishni boshqarish", "HTML o'qish", "CSS ni kompilyatsiya qilish", "Rasmlarni yuklash"));
        list.Add(Q("DOM (Document Object Model) nima?", "HTML hujjatining brauzer xotirasidagi obyekt ko'rinishidagi daraxtsimon strukturasi", "Ma'lumotlar bazasi", "CSS fayli", "Server protokoli"));
        list.Add(Q("JavaScript da 'Promise' ning 3 ta holati qaysilar?", "Pending, Fulfilled, Rejected", "Start, Run, Stop", "Open, Close, Error", "Init, Active, Done"));
        list.Add(Q("React.js da komponent holatini saqlash uchun qaysi Hook ishlatiladi?", "useState()", "useEffect()", "useContext()", "useRef()"));
        list.Add(Q("React.js da nojo'ya ta'sirlarni (side-effects: API fetch, subscription) bajarish uchun qaysi Hook ishlatiladi?", "useEffect()", "useState()", "useMemo()", "useReducer()"));
        list.Add(Q("Virtual DOM React da nima uchun kerak?", "Haqiqiy DOM ga o'zgarishlarni samarali va tezkor minimal diff orqali tatbiq etish uchun", "Rasmlarni saqlash uchun", "CSS ni o'chirish uchun", "Server yaratish uchun"));
        list.Add(Q("JavaScript da 'closure' (yopilish) nima?", "Ichki funksiyaning tashqi funksiya scope (o'zgaruvchilari) ga kirish huquqi", "Sikl to'xtashi", "DOM o'chishi", "HTML xatosi"));
        list.Add(Q("CSS Grid da ustunlar o me me me o me me o o'lchamini belgilash uchun qaysi xususiyat ishlatiladi?", "grid-template-columns", "grid-columns-width", "flex-columns", "display-grid-size"));
        list.Add(Q("JavaScript da '===' va '==' o'rtasidagi farq nima?", "'===' qiymat va ma'lumot turini solishtiradi, '==' esa turini avto-o'girib solishtiradi", "Farq yo'q", "'==' qat'iyroq", "'===' faqat string uchun"));
        list.Add(Q("JavaScript fetch() API nimani qaytaradi?", "Promise obyektini", "HTML matnini", "Faqat JSON", "Integer kodi"));
        list.Add(Q("React.js da props nima?", "Ota komponentdan bola komponentga uzatiladigan o'zgarmas ma'lumotlar", "Ichki holat", "Server bazasi", "CSS fayli"));
        list.Add(Q("LocalStorage va SessionStorage o'rtasidagi farq nima?", "LocalStorage ma'lumotlari brauzer yopilsa ham saqlanadi, SessionStorage esa tab yopilishi bilan o'chadi", "SessionStorage ko'proq joy oladi", "LocalStorage faqat 1 soat saqlaydi", "Farq yo'q"));
        list.Add(Q("JavaScript ES6 da Arrow Function (=>) ning 'this' kalit so'ziga munosabati qanday?", "Arrow function o'z 'this' iga ega emas, u leksik (tashqi) 'this' ni oladi", "Har doim global window bo'ladi", "This ni o'chiradi", "Faqat statik bo'ladi"));
        list.Add(Q("HTTP GET va POST so'rovlari o'rtasidagi asosiy farq nima?", "GET ma'lumotni URL orqali so'raydi, POST esa so'rov tanasida (body) yuboradi", "POST sekinroq", "GET ma'lumotni o'chiradi", "POST faqat rasmlar uchun"));
        list.Add(Q("CSS da 'box-sizing: border-box' nima qiladi?", "Padding va border ni elementning umumiy eni va bo'yiga kiritadi", "Elementni yashiradi", "Chegarani qizil qiladi", "Fontni kattalashtiradi"));
        list.Add(Q("React.js da 'key' prop prop nima uchun kerak?", "Ro'yxat elementlari o'zgarganda Virtual DOM ularni aniq va tez identifikatsiyalashi uchun", "Elementni bo'yash uchun", "CSS ulash uchun", "API chaqirish uchun"));
        list.Add(Q("Web accessibility (a11y) da 'alt' atributi rasm uchun nima beradi?", "Rasm yuklanmay qolganda yoki ekran o'quvchilar (screen reader) uchun muqobil matn", "Rasm hajmini", "Rasm rangini", "Rasm manzilini"));
        return list;
    }

    private static List<Question> CreatePostgres20Questions()
    {
        var list = new List<Question>();
        list.Add(Q("PostgreSQL ma'lumotlar bazasida jadvaldan ma'lumotni tanlab olish uchun qaysi SQL operatori ishlatiladi?", "SELECT", "GET", "FETCH", "EXTRACT"));
        list.Add(Q("SQL da ma'lumotlarni filtrlashtirish uchun qaysi kalit so'z ishlatiladi?", "WHERE", "HAVING", "GROUP", "ORDER"));
        list.Add(Q("Ikki jadvalni umumiy kalit bo'yicha mos keladigan qatorlarini birlashtirish uchun qaysi JOIN ishlatiladi?", "INNER JOIN", "LEFT JOIN", "RIGHT JOIN", "CROSS JOIN"));
        list.Add(Q("SQL da ma'lumotlarni guruhlash bo'yicha agregat filtr qo'yish uchun qaysi operator ishlatiladi?", "HAVING", "WHERE", "ORDER BY", "LIMIT"));
        list.Add(Q("PostgreSQL da jadvalga yangi qator qo'shish uchun qaysi buyruq ishlatiladi?", "INSERT INTO", "ADD ROW", "APPEND", "PUT"));
        list.Add(Q("PostgreSQL da mavjud ma'lumotlarni tahrirlash uchun qaysi buyruq ishlatiladi?", "UPDATE", "MODIFY", "CHANGE", "SET"));
        list.Add(Q("PostgreSQL da jadvaldan qatorlarni o'chirish uchun qaysi buyruq ishlatiladi?", "DELETE FROM", "REMOVE", "DROP", "TRUNCATE"));
        list.Add(Q("TRUNCATE va DELETE o'rtasidagi farq nima?", "TRUNCATE barcha qatorlarni jadval strukturasi va indekslarni tezda tozalaydi, DELETE esa shart bo me me bo'yicha bittalab o'chiradi", "DELETE tezroq", "TRUNCATE jadvalni yo'qotadi", "Farq yo'q"));
        list.Add(Q("B-Tree Indeks PostgreSQL da nima uchun kerak?", "Qidiruv (SELECT) so'rovlari tezligini sezilarli oshirish uchun", "Baza hajmini kamaytirish uchun", "Parollarni shifrlash uchun", "Fayl yaratish uchun"));
        list.Add(Q("Tranzaksiyalarning ACID xossasidagi 'A' nimani bildiradi?", "Atomicity (Atomlilik - barcha amallar bajariladi yoki hech biri bajarilmaydi)", "Availability", "Accuracy", "Authentication"));
        list.Add(Q("Tranzaksiyani muvaffaqiyatli yakunlab saqlash uchun qaysi buyruq ishlatiladi?", "COMMIT", "ROLLBACK", "SAVEPOINT", "CLOSE"));
        list.Add(Q("Tranzaksiyada xatolik bo'lganda o'zgarishlarni bekor qilish uchun qaysi buyruq ishlatiladi?", "ROLLBACK", "COMMIT", "CANCEL", "UNDO"));
        list.Add(Q("PostgreSQL da Primary Key (Asosiy kalit) ning xususiyati nima?", "Unikal (Unique) va NOT NULL bo'lishi shart", "Null bo'lishi mumkin", "Takrorlanishi mumkin", "Faqat matn bo'ladi"));
        list.Add(Q("Foreign Key (Tashqi kalit) nima uchun ishlatiladi?", "Ikki jadval o'rtasidagi bog'liqlik va relyatsion yaxlitlikni ta'minlash uchun", "Jadvalni o'chirish uchun", "Parol saqlash uchun", "SQL tezlashtirish uchun"));
        list.Add(Q("PostgreSQL da avtomatik ko'payuvchi unikal id yaratish uchun qaysi tur ishlatiladi?", "SERIAL / BIGSERIAL (yoki UUID)", "VARCHAR", "TEXT", "BOOLEAN"));
        list.Add(Q("SQL da takrorlanuvchi (duplicate) natijalarni olib tashlash uchun qaysi kalit so'z ishlatiladi?", "DISTINCT", "UNIQUE", "DIFFERENT", "SINGLE"));
        list.Add(Q("PostgreSQL da View (Tasvir) nima?", "Saqlangan SQL so'rovi natijasida hosil bo'ladigan virtual jadval", "Jismoniy fayl", "Baza foydalanuvchisi", "Indeks turi"));
        list.Add(Q("Trigger PostgreSQL da qachon ishlaydi?", "Jadvalda INSERT, UPDATE yoki DELETE voqeasi (event) sodir bo'lganda avtomatik", "Faqat server o'chganda", "Faqat soat 12 da", "Har bir SELECT da"));
        list.Add(Q("Ma'lumotlar bazasini Normalizatsiya qilishdan maqsad nima?", "Ma'lumotlar takrorlanishini (redundancy) kamaytirish va struktura yaxlitligini oshirish", "Baza hajmini 10 baravar oshirish", "Faqat rasmlarni saqlash", "Jadvallarni o'chirish"));
        list.Add(Q("PostgreSQL da ma'lumotlar bazasi zaxira nusxasini (backup) olish uchun qaysi konsol vositasi ishlatiladi?", "pg_dump", "pg_restore", "psql_copy", "db_export"));
        return list;
    }

    private static List<Question> CreateNetwork20Questions()
    {
        var list = new List<Question>();
        list.Add(Q("OSI (Open Systems Interconnection) modeli necha qavatdan (layer) iborat?", "7 qavatdan", "4 qavatdan", "5 qavatdan", "10 qavatdan"));
        list.Add(Q("OSI modelining 7-qavati (eng yuqori) qaysi?", "Application (Amaliy) qavat", "Transport qavati", "Network qavati", "Physical qavat"));
        list.Add(Q("TCP va UDP o'rtasidagi asosiy farq nima?", "TCP ishonchli va ulanish o'rnatuvchi (connection-oriented), UDP esa tezkor va ulanishsiz (connectionless)", "UDP ishonchliroq", "TCP faqat rasmlar uchun", "Farq yo'q"));
        list.Add(Q("HTTP va HTTPS o'rtasidagi asosiy farq nima?", "HTTPS so'rovlarni SSL/TLS orqali shifrlaydi (Port 443)", "HTTP xavfsizroq", "HTTPS faqat videolar uchun", "HTTP porti 443"));
        list.Add(Q("Standart Web (HTTP) trafigi qaysi port orqali o'tadi?", "Port 80", "Port 443", "Port 22", "Port 21"));
        list.Add(Q("Secure Shell (SSH) masofadan boshqarish protokoli qaysi portda ishlaydi?", "Port 22", "Port 80", "Port 3306", "Port 5432"));
        list.Add(Q("PostgreSQL ma'lumotlar bazasining standart port raqami qaysi?", "5432", "3306", "8080", "1433"));
        list.Add(Q("DNS (Domain Name System) ning asosiy vazifasi nima?", "Domen nomlarini (masalan google.com) IP manzilga o me o'girish", "Fayllar yuklash", "Parollarni shifrlash", "HTML chiqarish"));
        list.Add(Q("IPv4 manzili necha bitdan iborat?", "32 bitdan (4 bayt)", "64 bitdan", "128 bitdan", "16 bitdan"));
        list.Add(Q("IPv6 manzili necha bitdan iborat?", "128 bitdan", "32 bitdan", "64 bitdan", "256 bitdan"));
        list.Add(Q("Localhost (o'z kompyuteringiz) ning standart loopback IP manzili qaysi?", "127.0.0.1", "192.168.1.1", "10.0.0.1", "0.0.0.0"));
        list.Add(Q("Firewall (Xavfsizlik devori) ning tarmoqdagi vazifasi nima?", "Kiralayotgan va chiqayotgan tarmoq trafigini qoidalar bo me me bo'yicha filtrlashtirish", "Kompilyatsiya qilish", "IP manzil sotish", "Dastur yozish"));
        list.Add(Q("DDoS (Distributed Denial of Service) hujumi nimani anglatadi?", "Serverga ko'plab manbalardan bir vaqtda so'rovlar yuborib uni ishdan chiqarish", "Parolni o'g'irlash", "Faylni o'chirish", "Baza nusxalash"));
        list.Add(Q("SQL Injection kiberhujumi nima?", "Foydalanuvchi kiritish maydoniga zararli SQL kodini kiritib bazani buzish", "Tarmoq simini uzish", "DDoS yuborish", "Email buzish"));
        list.Add(Q("XSS (Cross-Site Scripting) hujumining mohiyati nima?", "Veb-sahifaga zararli JavaScript kodini joylashtirib boshqa foydalanuvchilar ma'lumotini o me o'g me o'g'irlash", "SQL o me o'chirish", "Serverni yoqish", "IP o'zgartirish"));
        list.Add(Q("Asimmetrik shifrlashda qanday kalitlar ishlatiladi?", "Juftlik: Public Key (Ochiq) va Private Key (Yopiq)", "Faqat 1 ta umumiy kalit", "Kalit ishlatilmaydi", "Faqat login/parol"));
        list.Add(Q("Simmetrik shifrlash (masalan AES) nimasi bilan ajralib turadi?", "Shifrlash va deshifrlash uchun bitta umumiy maxfiy kalit ishlatiladi", "Ikki kalit ishlatiladi", "Kalitsiz ishlaydi", "Sekin ishlaydi"));
        list.Add(Q("VPN (Virtual Private Network) ning maqsadi nima?", "Internet trafigini shifrlangan xavfsiz tunel orqali yo'naltirish va maxfiylikni ta'minlash", "Internet tezligini 100x oshirish", "Kompyuterni o'chirish", "Baza yaratish"));
        list.Add(Q("DHCP (Dynamic Host Configuration Protocol) tarmoqda nima qiladi?", "Tarmoqdagi qurilmalarga avtomatik IP manzillarni ajratib beradi", "Fayllar ulashadi", "Saytlarni bloklaydi", "Videonikini o me o'zgartiradi"));
        list.Add(Q("Ping buyrug'i tarmoqda nimani tekshirish uchun ishlatiladi?", "Tugunlar o'rtasida tarmoq ulanishi va kechikish (latency/ICMP) vaqtini", "Parol to'g'riligini", "Baza xatosini", "Disk hajmini"));
        return list;
    }

    private static List<Question> CreateEnglish30Questions()
    {
        var list = new List<Question>();
        list.Add(Q("Choose the correct form: 'She _____ to the gym every morning.'", "goes", "go", "is go", "gone"));
        list.Add(Q("Choose the correct form: 'If I _____ more time, I would learn Spanish.'", "had", "have", "will have", "would have"));
        list.Add(Q("Identify the passive voice: 'The report _____ by the team yesterday.'", "was completed", "completed", "is completing", "has complete"));
        list.Add(Q("Choose the correct preposition: 'He is very good _____ solving complex math problems.'", "at", "in", "on", "with"));
        list.Add(Q("Select the synonym for 'METICULOUS':", "Careful and precise", "Lazy", "Quick", "Noisy"));
        list.Add(Q("Choose the correct conditional (3rd): 'If they had arrived earlier, they _____ the train.'", "would not have missed", "will not miss", "do not miss", "had not missed"));
        list.Add(Q("Identify the correct relative pronoun: 'The professor _____ lecture I attended was inspiring.'", "whose", "whom", "which", "who"));
        list.Add(Q("Choose the correct phrasal verb: 'Never _____ giving up on your dreams.'", "give up", "give in", "take off", "look after"));
        list.Add(Q("Select the antonym for 'CANDID':", "Secretive/Deceitful", "Honest", "Frank", "Open"));
        list.Add(Q("Choose the correct form: 'Neither John nor his friends _____ attending the conference.'", "are", "is", "was", "be"));
        list.Add(Q("Select the correct sentence with Subjunctive mood:", "I suggest that he be present at the meeting.", "I suggest that he is present.", "I suggest he was present.", "I suggest he will be present."));
        list.Add(Q("Choose the correct idiom meaning: 'To bite the bullet' means:", "To face a difficult situation with courage", "To eat quickly", "To shoot a gun", "To complain loudly"));
        list.Add(Q("Identify the gerund in the sentence: 'Swimming in the ocean is her favorite activity.'", "Swimming", "ocean", "favorite", "activity"));
        list.Add(Q("Choose the correct connector: 'She worked hard; _____, she passed the exam with distinction.'", "consequently", "however", "although", "despite"));
        list.Add(Q("Choose the correct form: 'By next December, we _____ in this city for ten years.'", "will have lived", "live", "are living", "lived"));
        list.Add(Q("Select the meaning of 'PRAGMATIC':", "Dealing with things sensibly and realistically", "Theoretical", "Emotional", "Unreasonable"));
        list.Add(Q("Choose the correct inverted sentence: 'Hardly _____ entered the room when the phone rang.'", "had I", "I had", "did I", "have I"));
        list.Add(Q("Choose the correct article: 'He is _____ honest man with high moral standards.'", "an", "a", "the", "no article"));
        list.Add(Q("Select the word that means 'Existing everywhere at the same time':", "Ubiquitous", "Ephemeral", "Ambiguous", "Obsolete"));
        list.Add(Q("Choose the correct modal verb: 'You _____ bring an umbrella; it looks like it might rain.'", "should", "must not", "would", "cannot"));
        list.Add(Q("Identify the part of speech of 'swiftly' in 'The eagle flew swiftly across the sky':", "Adverb", "Adjective", "Noun", "Verb"));
        list.Add(Q("Choose the correct spelling:", "Accommodate", "Acommodate", "Accomodate", "Acomodate"));
        list.Add(Q("Select the correct indirect speech: He said, 'I am working on a new project.'", "He said that he was working on a new project.", "He said that he is working.", "He said he works.", "He said I was working."));
        list.Add(Q("Choose the correct usage: 'Despite _____ the rain, we enjoyed the football match.'", "of", "in spite of", "the rain", "although"));
        list.Add(Q("Select the synonym for 'RESILIENT':", "Able to withstand or recover quickly from difficult conditions", "Fragile", "Weak", "Tired"));
        list.Add(Q("Choose the correct tag question: 'You haven't seen my keys, _____?'", "have you", "haven't you", "did you", "don't you"));
        list.Add(Q("Select the word that means 'Short-lived or temporary':", "Ephemeral", "Perpetual", "Eternal", "Constant"));
        list.Add(Q("Choose the correct form: 'I look forward to _____ you at the upcoming summit.'", "meeting", "meet", "have met", "met"));
        list.Add(Q("Select the meaning of 'BENEVOLENT':", "Kind-hearted and well-meaning", "Cruel", "Greedy", "Strict"));
        list.Add(Q("Choose the correct sentence:", "Not only did he finish the project, but he also exceeded all expectations.", "Not only he finished the project.", "He not only did finish.", "Not only finished he the project."));
        return list;
    }
}
