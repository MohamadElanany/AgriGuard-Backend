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

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins("http://localhost:5173")
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            builder.Services.AddControllers();

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddHttpClient<AiDiagnosisService>(client =>
            {
                client.BaseAddress = new Uri("http://127.0.0.1:8000/");
            });

            builder.Services.AddHttpClient<TreatmentService>();

            builder.Services.AddScoped<EmailService>();

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

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCors("AllowFrontend");
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                context.Database.Migrate();

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