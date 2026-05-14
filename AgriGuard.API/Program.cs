using AgriGuard.API.Data;
using AgriGuard.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using AgriGuard.API.Services;

namespace AgriGuard.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configure CORS policy for frontend communication
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins("http://localhost:5173")
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            // Register API controllers
            builder.Services.AddControllers();

            // Configure SQL Server database connection
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Register HTTP client for communication with the AI microservice
            builder.Services.AddHttpClient<AiDiagnosisService>(client =>
            {
                client.BaseAddress = new Uri("http://127.0.0.1:8000/");
            });

            // Register HTTP client for Gemini AI treatment service
            builder.Services.AddHttpClient<TreatmentService>();

            // Register email service for password reset functionality
            builder.Services.AddScoped<EmailService>();

            // Configure JWT authentication and authorization
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidAudience = builder.Configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
                    };
                });

            builder.Services.AddEndpointsApiExplorer();

            // Configure Swagger with JWT authentication support
            builder.Services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Description = "Enter 'Bearer' [space] and then your token.\nExample: Bearer abc123"
                });

                options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });
            });

            var app = builder.Build();

            // Enable Swagger only in development environment
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Configure application middleware pipeline
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCors("AllowFrontend");
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            // Seed default locations data on first startup
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                context.Database.Migrate();

                // Add default Egypt governorates if database is empty
                if (!context.Countries.Any())
                {
                    var egypt = new Country
                    {
                        Name = "Egypt",
                        Governorates = new List<Governorate>
                        {
                            new Governorate { Name = "Cairo" },
                            new Governorate { Name = "Giza" },
                            new Governorate { Name = "Alexandria" },
                            new Governorate { Name = "Dakahlia" },
                            new Governorate { Name = "Sharqia" },
                            new Governorate { Name = "Monufia" },
                            new Governorate { Name = "Gharbia" },
                            new Governorate { Name = "Qalyubia" },
                            new Governorate { Name = "Beheira" },
                            new Governorate { Name = "Kafr El Sheikh" },
                            new Governorate { Name = "Fayoum" },
                            new Governorate { Name = "Beni Suef" },
                            new Governorate { Name = "Minya" },
                            new Governorate { Name = "Assiut" },
                            new Governorate { Name = "Sohag" },
                            new Governorate { Name = "Qena" },
                            new Governorate { Name = "Luxor" },
                            new Governorate { Name = "Aswan" },
                            new Governorate { Name = "Ismailia" },
                            new Governorate { Name = "Suez" },
                            new Governorate { Name = "Port Said" },
                            new Governorate { Name = "Damietta" },
                            new Governorate { Name = "North Sinai" },
                            new Governorate { Name = "South Sinai" },
                            new Governorate { Name = "Red Sea" },
                            new Governorate { Name = "New Valley" },
                            new Governorate { Name = "Matrouh" }
                        }
                    };

                    context.Countries.Add(egypt);
                    context.SaveChanges();
                }
            }

            app.Run();
        }
    }
}