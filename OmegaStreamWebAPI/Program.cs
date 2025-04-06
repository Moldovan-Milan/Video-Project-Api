using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using OmegaStreamServices.Data;
using OmegaStreamServices.Models;
using OmegaStreamServices.Services;
using OmegaStreamServices.Services.VideoServices;
using OmegaStreamServices.Services.UserServices;
using Microsoft.Extensions.Configuration;
using System.Runtime;
using OmegaStreamServices.Services.Repositories;
using OmegaStreamWebAPI.Hubs;
using Microsoft.AspNetCore.Authentication.Cookies;
using OmegaStreamWebAPI.BackgroundServices;

namespace OmegaStreamWebAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "VideoProjektAspApi", Version = "v1" });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Please enter into field the word 'Bearer' followed by a space and the JWT value",
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    new string[] { }
                }});
            });

            builder.Services.AddMemoryCache();

            //// Db connection
            //var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            //builder.Services.AddDbContext<AppDbContext>(options =>
            //    options.UseSqlite("Data Source=omega_stream.sqlite"));


            string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<AppDbContext>(opt =>
            {
                opt.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            });

            // Identity
            builder.Services.Configure<IdentityOptions>(options =>
                {
                    // Majd az éles verzióban lesz engedélyezve
                    // Password settings.
                    options.Password.RequireDigit = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequiredLength = 3;
                    options.Password.RequiredUniqueChars = 0;

                    // Lockout settings.
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.AllowedForNewUsers = true;
                }
            );

            builder.Services.AddIdentity<User, IdentityRole>()
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            // Turn off identity redirect on status 401
            builder.Services.ConfigureApplicationCookie(options =>
            {
                // Default cookie options
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // HTTPS
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.MaxAge = TimeSpan.FromMinutes(30); // Cookie élettartama

                options.LoginPath = string.Empty; // Ne irányítson bejelentkezési oldalra
                options.AccessDeniedPath = string.Empty; // Ne irányítson jogosultság megtagadás oldalra

                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.Clear();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };

                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.Clear();
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            });




            // Repositories
            builder.Services.AddScoped<IVideoRepository, VideoRepository>();
            builder.Services.AddScoped<IImageRepository, ImageRepository>();
            builder.Services.AddScoped<IVideoLikesRepository, VideoLikesRepository>();
            builder.Services.AddScoped<ICommentRepositroy, CommentRepository>();
            builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
            builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
            builder.Services.AddScoped<IUserThemeRepository, UserThemeRepository>();

            builder.Services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
            builder.Services.AddScoped<IUserChatsRepository, UserChatsRepository>();
            builder.Services.AddScoped<IVideoViewRepository, VideoViewRepository>();
            builder.Services.AddSingleton<IUserVideoUploadRepositroy, UserVideoUploadRepository>();


            // Custom services
            builder.Services.AddScoped<IVideoUploadService, VideoUploadService>();
            //builder.Services.AddScoped<IFileManagerService, FileManagerService>();
            builder.Services.AddScoped<IVideoStreamService, VideoStreamService>();
            builder.Services.AddScoped<ICloudService, CloudService>();
            builder.Services.AddScoped<IAvatarService, AvatarService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IVideoMetadataService, VideoMetadataService>();
            builder.Services.AddScoped<IVideoLikeService, VideoLikeService>();
            builder.Services.AddScoped<ICommentService, CommentService>();
            builder.Services.AddScoped<IVideoViewService, VideoViewService>();
            builder.Services.AddScoped<IVideoManagementService, VideoManagementService>();
            builder.Services.AddScoped<IImageService, ImageService>();

            builder.Services.AddSingleton<IRoomStateManager, RoomStateManager>();

            builder.Services.AddScoped<TokenGenerator>();

            // Background services
            builder.Services.AddHostedService<CheckExpiredVideoUploadTask>();

            // SingalR
            builder.Services.AddSignalR()
                .AddHubOptions<ChatHub>(options =>
                {
                    options.KeepAliveInterval = TimeSpan.FromSeconds(30);
                    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
                })
                .AddHubOptions<WatchTogetherHub>(options =>
                {
                    options.KeepAliveInterval = TimeSpan.FromSeconds(10);
                    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
                });

            builder.Logging.AddFilter("Microsoft.AspNetCore.SignalR", LogLevel.Debug);
            builder.Logging.AddFilter("Microsoft.AspNetCore.Http.Connections", LogLevel.Debug);


            builder.Services.AddSingleton<IEncryptionHelper, EncryptionHelper>();

            // Mapper
            builder.Services.AddAutoMapper(typeof(MappingProfile));

            // R2 settings
            builder.Services.Configure<R2Settings>(builder.Configuration.GetSection("R2"));


            // CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigin",
                    builder => builder.WithOrigins("https://localhost:5173", "http://localhost:8081", "http://192.168.1.72:8081")
                                      .AllowAnyMethod()
                                      .AllowAnyHeader()
                                      .AllowCredentials());
            });

            var app = builder.Build();

            var scope = app.Services.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            string[] roles = { "Admin", "Verified", "User" };
            foreach (var role in roles)
            {
                if (!roleManager.RoleExistsAsync(role).Result)
                {
                    roleManager.CreateAsync(new IdentityRole(role)).Wait();
                }
            }

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var user = userManager.FindByEmailAsync("admin@omegastream.com").Result;
            if (user != null)
            {
                userManager.AddToRoleAsync(user, "User").Wait();
                userManager.AddToRoleAsync(user, "Verified").Wait();
                userManager.AddToRoleAsync(user, "Admin").Wait();
            }


            // For private chat
            app.UseWebSockets();

            // SignalR endpoint
            app.MapHub<ChatHub>("/chatHub").RequireCors("AllowSpecificOrigin").RequireAuthorization();
            app.MapHub<WatchTogetherHub>("/watch").RequireAuthorization();


            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "VideoProjektAspApi v1");
                });
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseCors("AllowSpecificOrigin");
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
