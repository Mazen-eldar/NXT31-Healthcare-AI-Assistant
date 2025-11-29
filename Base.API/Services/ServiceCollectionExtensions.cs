using Base.API.Authorization;
using Base.API.Filters;
using Base.DAL.Contexts;
using Base.DAL.Models.BaseModels;
using Base.Repo.Implementations;
using Base.Repo.Interfaces;
using Base.Services.Implementations;
using Base.Services.Interfaces;
using Hangfire;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity.UI.Services;
using Base.DAL.Models.SystemModels;
using Base.Services.HangFireJobs;

namespace Base.API.Services
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            // تجزئة التسجيل إلى مجموعات منطقية
            services.AddDatabase(configuration);
            services.AddIdentityServices(configuration);
            services.AddAuthenticationAndJwt(configuration);
            services.AddInfrastructureServices(configuration);
            services.AddApiBehavior(configuration);
            services.AddSwaggerWithJwt();
            services.AddHangfire(configuration);
            services.AddResponseAndCaching(configuration);
            services.AddAuthorizationPolicies();

            return services;
        }

        // ---------------------------------------------------------------------------------
        // Database
        // - ملاحظة: لا تستخدم LazyLoadingProxies افتراضياً (N+1, serialization loops, performance)
        // ---------------------------------------------------------------------------------
        private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseLazyLoadingProxies();
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
                #if DEBUG
                // مفيد في بيئة التطوير فقط
                options.EnableSensitiveDataLogging();
                #endif
            });

            return services;
        }

        // ---------------------------------------------------------------------------------
        // Identity
        // - وضع كل إعدادات Identity في مكان واحد
        // - فصل خيارات Token lifespans (مثال)
        // ---------------------------------------------------------------------------------
        private static IServiceCollection AddIdentityServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                // Sign-in
                options.SignIn.RequireConfirmedEmail = true;

                // Password policy - اضبطها حسب متطلبات الأمن بالمؤسسة
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;
                options.Password.RequiredUniqueChars = 0;

                // Lockout
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                // User
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

            // Token lifespan الافتراضي المشترك (يمكن استبداله بمزودات مخصصة بحسب الحاجة)
            services.Configure<DataProtectionTokenProviderOptions>(opt =>
            {
                opt.TokenLifespan = TimeSpan.FromHours(3); // الافتراضي الآمن لبعض الحالات
            });

            // إن احتجت lifespans مختلفة (مثال توضيحي) يمكنك تسجيل مزود توكن مخصص
            // services.Configure<CustomEmailTokenProviderOptions>(...) // مثال متقدم

            return services;
        }

        // ---------------------------------------------------------------------------------
        // Authentication + JWT + External Providers
        // - استخدم UTF8 للمفتاح
        // - ClockSkew = TimeSpan.Zero
        // - Events مع logging (غير حساس في الإنتاج)
        // ---------------------------------------------------------------------------------
        private static IServiceCollection AddAuthenticationAndJwt(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtKey = configuration["Auth:Jwt:Key"];
            if (string.IsNullOrWhiteSpace(jwtKey))
            {
                throw new InvalidOperationException("Jwt:Key is not configured. Set it in configuration securely (e.g. user secrets / vault).");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = true;
                options.SaveToken = true;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Auth:Jwt:Issuer"],
                    ValidAudience = configuration["Auth:Jwt:Audience"],
                    IssuerSigningKey = key,

                    // لا تسمح بالـ clock skew الافتراضي (لتقليل نافذة استغلال توكن منتهي)
                    ClockSkew = TimeSpan.Zero
                };

                // رسائل مصممة للـ API - لا تظهر تفاصيل كثيرة في الإنتاج
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";

                        var payload = new
                        {
                            statusCode = 401,
                            message = "Unauthorized - Invalid or missing token",
                            traceId = context.HttpContext.TraceIdentifier
                        };

                        var json = JsonSerializer.Serialize(payload);
                        await context.Response.WriteAsync(json);
                    },
                    OnForbidden = async context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/json";

                        var message = context.HttpContext.Items.ContainsKey("UserIsInactive")
                            ? "User account is inactive"
                            : "You do not have the required permissions";

                        var payload = new
                        {
                            statusCode = 403,
                            message,
                            traceId = context.HttpContext.TraceIdentifier
                        };

                        var json = JsonSerializer.Serialize(payload);
                        await context.Response.WriteAsync(json);
                    }
                };
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddGoogle(googleOptions =>
            {
                googleOptions.ClientId = configuration["Authentication:Google:ClientId"];
                googleOptions.ClientSecret = configuration["Authentication:Google:ClientSecret"];
                googleOptions.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddFacebook(facebookOptions =>
            {
                facebookOptions.AppId = configuration["Authentication:Facebook:AppId"];
                facebookOptions.AppSecret = configuration["Authentication:Facebook:AppSecret"];
                facebookOptions.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            });

            return services;
        }

        // ---------------------------------------------------------------------------------
        // Business / Infrastructure services registration
        // - استخدام HttpClientFactory
        // - توضيح lifetimes المناسبة
        // ---------------------------------------------------------------------------------
        private static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {

            // -----------------------
            // Memory Cache
            // -----------------------
            services.AddMemoryCache();

            // -----------------------
            // Logging (مهم لكل service محتاجة ILogger)
            // -----------------------
            services.AddLogging();

            // -----------------------
            // Business Services (معظمها تعتمد على DbContext → Scoped)
            // -----------------------
            services.AddScoped<IEmailSender, EmailSender>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<IOtpService, OtpService>();
            services.AddScoped<IRefreshTokenService, RefreshTokenService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IUserProfileService, UserProfileService>();
            services.AddScoped<IAuthorizationHandler, ActiveUserHandler>();
            services.AddScoped<IClinicServices, ClinicServices>();
            services.AddScoped<IUploadImageService, UploadImageService>();
            services.AddScoped<AppointmentSlotGeneratorJob>();

            // -----------------------
            // إذا كان لديك أي service صغيرة stateless → استخدم Transient
            // -----------------------
            // services.AddTransient<IMyHelperService, MyHelperService>();

            // -----------------------
            // Configuration
            // -----------------------
            services.AddSingleton(configuration);

            return services;
        }

        // ---------------------------------------------------------------------------------
        // API behavior, controllers and CORS
        // ---------------------------------------------------------------------------------
        private static IServiceCollection AddApiBehavior(this IServiceCollection services, IConfiguration configuration)
        {
            // Controllers + JSON options
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                    options.JsonSerializerOptions.PropertyNamingPolicy = new LowerCaseNamingPolicy();
                    options.JsonSerializerOptions.DictionaryKeyPolicy = new LowerCaseNamingPolicy();
                    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
                    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                    options.JsonSerializerOptions.WriteIndented = true;
                });

            services.AddEndpointsApiExplorer();

            // CORS - استخدم قائمة origins من الـ config (آمن وقابل للتغيير لكل بيئة)
            var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new string[0];

            services.AddCors(options =>
            {
                options.AddPolicy("DefaultCorsPolicy", builder =>
                {
                    if (allowedOrigins.Any())
                    {
                        builder.WithOrigins(allowedOrigins)
                            .AllowAnyHeader()
                            .AllowAnyMethod();
                    }
                    else
                    {
                        // في حالة التطوير المؤقت فقط - لا تترك هذا في الإنتاج
                        builder.SetIsOriginAllowed(_ => true)
                               .AllowAnyHeader()
                               .AllowAnyMethod();
                    }
                });
            });

            return services;
        }

        // ---------------------------------------------------------------------------------
        // Swagger with JWT
        // ---------------------------------------------------------------------------------
        private static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Health Care",
                    Version = "v1",
                    Description = "API documentation with unified response format"
                });

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme (Example: \"Bearer {token}\")",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT"
                });

                //options.AddSecurityRequirement(new OpenApiSecurityRequirement {
                //    {
                //        new OpenApiSecurityScheme{
                //            Reference = new OpenApiReference{
                //                Type = ReferenceType.SecurityScheme,
                //                Id = "Bearer"
                //            }
                //        },
                //        Array.Empty<string>()
                //    }
                //});

                // Operation filter to show [Authorize] endpoints (if you have one implemented)
                options.OperationFilter<AuthorizeCheckOperationFilter>();
                options.OperationFilter<AllowAnonymousOperationFilter>();

                // XML comments are optional - include if you generate XML docs
                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (System.IO.File.Exists(xmlPath))
                    options.IncludeXmlComments(xmlPath);
            });

            return services;
        }

        // ---------------------------------------------------------------------------------
        // Hangfire
        // - استخدم connection string منفصل للحد من التأثير على الـ App DB
        // ---------------------------------------------------------------------------------
        private static IServiceCollection AddHangfire(this IServiceCollection services, IConfiguration configuration)
        {
            var hangfireConn = configuration.GetConnectionString("HangfireConnection") ?? configuration.GetConnectionString("DefaultConnection");

            services.AddHangfire(config =>
            {
                config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseSqlServerStorage(hangfireConn);
            });

            services.AddHangfireServer();

            return services;
        }

        // ---------------------------------------------------------------------------------
        // Response caching & compression
        // ---------------------------------------------------------------------------------
        private static IServiceCollection AddResponseAndCaching(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddResponseCaching();

            services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.MimeTypes = new[]
                {
                    "application/json",
                    "text/plain",
                    "text/html",
                    "application/javascript",
                    "text/css"
                };
            });

            return services;
        }

        // ---------------------------------------------------------------------------------
        // Authorization policies
        // ---------------------------------------------------------------------------------
        private static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy("ActiveUserOnly", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.Requirements.Add(new ActiveUserRequirement());
                });
            });

            return services;
        }
    }

    // ---------------------------------------
    // LowerCaseNamingPolicy (مثال بسيط)
    // ---------------------------------------
    public class LowerCaseNamingPolicy : System.Text.Json.JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            return string.IsNullOrEmpty(name) ? name : name.ToLowerInvariant();
        }
    }
}