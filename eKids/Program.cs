
using Microsoft.EntityFrameworkCore;
using NLog.Web;
using Database.Context;
using Database.Repository;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using eKids.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using eKids.Mapping;
using StackExchange.Redis;
using FluentValidation.AspNetCore;
using FluentValidation;
using eKids.Validators;
using Database.DTOs;
using eKids.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Net;

namespace eKids
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.Configure<KestrelServerOptions>(options =>
            {
                options.Listen(IPAddress.Any, 5194);
                options.Limits.MaxRequestBodySize = 500 * 1024 * 1024; // 10 MB
            });

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAllOrigins", builder =>
                {
                    builder.AllowAnyOrigin()  // Allow any origin
                           .AllowAnyMethod()  // Allow any HTTP method
                           .AllowAnyHeader();  // Allow any header
                });
            });

            builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            context.Response.Headers.Add("Token-Expired", "true");
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
            });



            // Add services to the container.
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddScoped<IFileUploadService, FileUploadService>();
            builder.Services.AddScoped<IFileChecker, FileChecker>();
            builder.Services.AddScoped<ICommentService, CommentService>();
            builder.Services.AddScoped<ICommentLikesService, CommentLikesService>();
            builder.Services.AddScoped<ILessonLikesService, LessonLikesService>();
            builder.Services.AddScoped<ILessonNavigationService, LessonNavigationService>();
            builder.Services.AddScoped<ICourseCompletationService, CourseCompletationService>();
            builder.Services.AddScoped<IVideoFileService, VideoFileService>();
            builder.Services.AddScoped(typeof(ISorterService<>), typeof(SorterService<>));
            builder.Services.AddSignalR(options =>
            {
                options.MaximumReceiveMessageSize = 64 * 1024 * 1024;
            });
            builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var configuration = sp.GetRequiredService<IConfiguration>();

                var redisConnectionString = configuration.GetValue<string>("Redis:ConnectionString");
                var configOptions = ConfigurationOptions.Parse(redisConnectionString);
                return ConnectionMultiplexer.Connect(configOptions);
            });

            builder.Services.AddSingleton<IViewCountService, ViewCountService>();
            builder.Services.AddHostedService<ViewCountSyncService>();
            builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            builder.Services.AddControllers()
                .AddNewtonsoftJson(x => x.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore);

            builder.Services.AddFluentValidationAutoValidation()
                .AddFluentValidationClientsideAdapters();
            //builder.Services.AddValidatorsFromAssemblyContaining<UpdateUserValidator>(); this is for entire controllers
            builder.Services.AddSingleton<IValidator<UpdateUser>, UpdateUserValidator>();

            builder.Services.AddAutoMapper(typeof(MappingProfile));
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            //builder.Services.AddAuthentication(JwtBearerDefaults.);

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Host.UseNLog(new NLogAspNetCoreOptions { RemoveLoggerFactoryFilter = false });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }


            // Add custom services for authorization
           /* builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
            });*/

            app.UseHttpsRedirection();
            app.UseCors("AllowAllOrigins");
            //app.UseAuthentication();
            app.UseAuthorization();
            app.UseStaticFiles();

            app.MapControllers();
            app.MapHub<VideoUploadHub>("/videouploadhub");
            app.MapHub<ChatHub>("/chatHub");
            app.MapHub<NotificationsHub>("/notificationsHub");

            app.Run();
        }
    }
}
