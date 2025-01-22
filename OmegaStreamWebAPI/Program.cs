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
                options.UseSqlite("Data Source=omega_steam.db"));

            // Identity
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
            builder.Services.AddTransient<IVideoRepository, VideoRepository>();
            builder.Services.AddTransient<IImageRepository, ImageRepository>();

            // Custom services
            builder.Services.AddTransient<IVideoUploadService, VideoUploadService>();
            builder.Services.AddTransient<IFileManagerService, FileManagerService>();
            builder.Services.AddTransient<IUserManagerService, UserManagerService>();
            builder.Services.AddTransient<IVideoStreamService, VideoStreamService>();
            builder.Services.AddTransient<ICloudService, CloudService>();
            builder.Services.AddTransient<IVideoProccessingService, VideoProccessingService>();

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
