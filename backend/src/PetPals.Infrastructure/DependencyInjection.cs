using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PetPals.Application.Abstractions.Auth;
using PetPals.Application.Abstractions.Marketplace;
using PetPals.Application.Abstractions.Social;
using PetPals.Infrastructure.Authentication;
using PetPals.Infrastructure.Configuration;
using PetPals.Infrastructure.Identity;
using PetPals.Infrastructure.Marketplace;
using PetPals.Infrastructure.Persistence;
using PetPals.Infrastructure.Social;

namespace PetPals.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(options => options.Secret.Length >= 32,
                "Jwt:Secret must contain at least 32 characters.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer),
                "Jwt:Issuer is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Audience),
                "Jwt:Audience is required.")
            .ValidateOnStart();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ISocialFeedService, SocialFeedService>();
        services.AddScoped<IMarketplaceService, MarketplaceService>();

        return services;
    }
}
