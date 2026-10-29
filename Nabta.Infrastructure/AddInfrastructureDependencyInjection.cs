using Infrastructure.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Nabta.Infrastructure.Data;
using Nabta.Infrastructure.identity;
using System.Text;

using Nabta.Application.Featuers.Services;
using Nabta.Infrastructure.Implementations;

namespace Infrastructure
{
    public static class AddInfrastructureDependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Database
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection")));

            // JWT Options
            services.Configure<JwtOptions>(
                configuration.GetSection("Jwt"));

            services.AddSingleton(sp =>
                sp.GetRequiredService<IOptions<JwtOptions>>().Value);

            // Identity + EF Core
            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            // JWT Authentication
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme =
                    JwtBearerDefaults.AuthenticationScheme;

                options.DefaultChallengeScheme =
                    JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;

                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        ValidIssuer = configuration["Jwt:Issuer"],
                        ValidAudience = configuration["Jwt:Audience"],

                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(
                                configuration["Jwt:Key"]!))
                    };
            });

            // Application Services
            services.AddScoped<IAuthService, AuthService>();

            return services;
        }
    }
}
