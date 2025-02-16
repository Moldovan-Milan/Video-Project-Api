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
using OmegaStreamWebAPI.WebSockets;

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

            // Db connection
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite("Data Source=omega_stream.sqlite"));

            // Identity
            builder.Services.Configure<IdentityOptions>(options =>
                {
                    // Majd az éles verzióban lesz engedélyezve
                    // Password settings.
                    //options.Password.RequireDigit = true;
                    //options.Password.RequireLowercase = true;
                    //options.Password.RequireNonAlphanumeric = true;
                    //options.Password.RequireUppercase = true;
                    //options.Password.RequiredLength = 6;
                    //options.Password.RequiredUniqueChars = 1;

                    // Lockout settings.
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.AllowedForNewUsers = true;



                }
            );

            builder.Services.AddIdentity<User, IdentityRole>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            // Authentication
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
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Issuer"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
                };
            });

            // Repositories
            builder.Services.AddScoped<IVideoRepository, VideoRepository>();
            builder.Services.AddScoped<IImageRepository, ImageRepository>();
            builder.Services.AddScoped<IVideoLikesRepository, VideoLikesRepository>();
            builder.Services.AddScoped<ICommentRepositroy, CommentRepository>();
            builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

            // Custom services
            builder.Services.AddScoped<IVideoUploadService, VideoUploadService>();
            builder.Services.AddScoped<IFileManagerService, FileManagerService>();
            builder.Services.AddScoped<IVideoStreamService, VideoStreamService>();
            builder.Services.AddScoped<ICloudService, CloudService>();
            builder.Services.AddScoped<IVideoProccessingService, VideoProccessingService>();
            builder.Services.AddScoped<IAvatarService, AvatarService>();
            builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IVideoMetadataService, VideoMetadataService>();
            builder.Services.AddScoped<IVideoLikeService, VideoLikeService>();
            builder.Services.AddScoped<ICommentService, CommentService>();

            // Websocket for chat
            builder.Services.AddSingleton<ChatWebsocketHanlder>();

            builder.Services.AddSingleton<IEncryptionHelper, EncryptionHelper>();

            // Mapper
            builder.Services.AddAutoMapper(typeof(MappingProfile));

            // R2 settings
            builder.Services.Configure<R2Settings>(builder.Configuration.GetSection("R2"));


            // CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigin",
                    builder => builder.WithOrigins("http://localhost:5173", "http://localhost:8081", "http://192.168.1.72:8081")
                                      .AllowAnyMethod()
                                      .WithHeaders("Authorization", "Content-Type", "Content-Range") // Authorization header-t engedélyezi
                                      .AllowCredentials());
            });

            var app = builder.Build();

            // For private chat
            app.UseWebSockets();

            var webSocketHandler = app.Services.GetRequiredService<ChatWebsocketHanlder>();
            app.Map("/ws", async context => await webSocketHandler.HandleWebsocketAsync(context));


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
