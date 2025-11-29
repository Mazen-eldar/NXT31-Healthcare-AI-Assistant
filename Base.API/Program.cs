using Base.API.Authorization;
using Base.API.Filters;
using Base.API.MiddleWare;
using Base.API.Services;
using Base.DAL.Contexts;
using Base.DAL.Models.BaseModels;
using Base.DAL.Seeding;
using Base.Services.HangFireJobs;
using Base.Services.Implementations;
using Base.Shared.Responses.Exceptions;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using TimeZoneConverter;
internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            WebRootPath = "wwwroot" // هنا تحددي WebRoot قبل إنشاء الـApp
        });

        // 💡 إضافة الخطوة الوقائية لتعطيل تحويل المطالبات
        // تمنع إعادة تسمية مطالبات 'sub' إلى 'nameidentifier' في ClaimsPrincipal
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        builder.Services.AddHttpContextAccessor();

        // 💡 إضافة خدمات التحكم في الوصول عبر الأصول (CORS)
        // 💡 إضافة خدمات الهوية
        builder.Services.AddApplicationServices(builder.Configuration);


        var app = builder.Build();

        #region Seeding
        // 💡 تنفيذ التهيئة الأولية للبيانات عند بدء التشغيل
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;

            var LoggerFactory = services.GetRequiredService<ILoggerFactory>();

            try
            {
                //await StoreContextSeeding.SeedAsync(dbContext);
                var dbContext = services.GetRequiredService<AppDbContext>();
                await dbContext.Database.MigrateAsync();//Apply Migration

                var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

                await IdentitySeeder.SeedAdminAsync(userManager, roleManager);
                await IdentitySeeder.SeedDataAsync(dbContext);

            }
            catch (Exception ex)
            {
                var logger = LoggerFactory.CreateLogger<Program>();
                logger.LogError(ex, "an error occured during apply Migration");
            }
        }
        #endregion
        // 💡 تكوين الـ Middleware في الـ HTTP Request Pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        app.UseMiddleware<TokenBlacklistMiddleware>();

        app.UseStaticFiles();

        // 🛡️ فرض HTTPS (أفضل ممارسة)
        app.UseHttpsRedirection();

        // 🌐 استخدام CORS
        app.UseCors("AllowSpecificOrigin");

        // 💡 تفعيل Response Compression في الـ Pipeline
        app.UseResponseCompression();

        // 💡 تفعيل Response Caching
        app.UseResponseCaching();
        app.UseRouting();

        // 🛡️ تفعيل المصادقة
        app.UseAuthentication();

        // 🛡️ تفعيل التفويض
        app.UseAuthorization();

        // 💡 إضافة Middleware لمعالجة الأخطاء
        app.UseMiddleware<ErrorHandlingMiddleware>();

        // 💡 إضافة Middleware لتغليف الاستجابات الناجحة
        app.UseMiddleware<SuccessResponseMiddleware>();



        

        // 💡 تعيين الخرائط للمتحكمات
        app.MapControllers();
        //app.UseHangfireDashboard("/hangfire/index.html");
        app.MapHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = new[] { new AllowAllDashboardAuthorizationFilter() }
        });

        // Cairo timezone
        var cairoTimeZone = TZConvert.GetTimeZoneInfo("Africa/Cairo");

        // جدولة الـ Job اليومية
        RecurringJob.AddOrUpdate<AppointmentSlotGeneratorJob>(
            "GenerateAppointmentSlots",
            job => job.GenerateMonthlySlotsAsync(),
            "0 2 * * *", //"Mintues Hours DayInmonth Month DayInWeek"
            new RecurringJobOptions
            {
                TimeZone = cairoTimeZone
            }
        );

        // 2) 💥 Job تنظيف الـ Blacklist (كل ساعة)
        RecurringJob.AddOrUpdate<CleanupBlacklistedTokensService>(
            "CleanupBlacklistedTokens",
            job => job.ExecuteAsync(),
            Cron.Hourly, // لو عايزة كل ساعتين: "0 */2 * * *"
            new RecurringJobOptions
            {
                TimeZone = cairoTimeZone
            }
        );
        // 💡 تعيين نقطة النهاية الافتراضية للتعامل مع الطلبات غير المعروفة
        app.MapFallback(async context =>
        {
            throw new NotFoundException("The requested endpoint does not exist.");
        });
        // Hangfire dashboard
     
        app.Run();
    }
}