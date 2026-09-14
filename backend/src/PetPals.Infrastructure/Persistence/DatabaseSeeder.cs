using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PetPals.Domain.Entities;
using PetPals.Domain.Enums;
using PetPals.Infrastructure.Identity;

namespace PetPals.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public const string UserEmail = "test.usuario@petpals.local";
    public const string ClinicEmail = "test.veterinaria@petpals.local";
    public const string ShelterEmail = "test.refugio@petpals.local";
    public const string Password = "PetPals123";

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        foreach (var role in Enum.GetNames<UserRole>())
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole<Guid>(role));
                EnsureSucceeded(result, $"creating role {role}");
            }
        }

        var user = await EnsureUserAsync(userManager, UserEmail, "Usuario de prueba", UserRole.User);
        var clinicUser = await EnsureUserAsync(userManager, ClinicEmail, "Veterinaria de prueba", UserRole.Clinic);
        var shelterUser = await EnsureUserAsync(userManager, ShelterEmail, "Refugio de prueba", UserRole.Shelter);

        if (!await db.UserProfiles.AnyAsync(profile => profile.UserId == user.Id, cancellationToken))
        {
            db.UserProfiles.Add(new UserProfile
            {
                UserId = user.Id,
                DisplayName = user.DisplayName,
                Bio = "Usuario de prueba de PetPals."
            });
        }

        if (!await db.UserProfiles.AnyAsync(profile => profile.UserId == clinicUser.Id, cancellationToken))
        {
            db.UserProfiles.Add(new UserProfile
            {
                UserId = clinicUser.Id,
                DisplayName = clinicUser.DisplayName,
                Bio = "Perfil de la veterinaria de prueba."
            });
        }

        if (!await db.UserProfiles.AnyAsync(profile => profile.UserId == shelterUser.Id, cancellationToken))
        {
            db.UserProfiles.Add(new UserProfile
            {
                UserId = shelterUser.Id,
                DisplayName = shelterUser.DisplayName,
                Bio = "Perfil del refugio de prueba."
            });
        }

        if (!await db.Clinics.AnyAsync(clinic => clinic.OwnerUserId == clinicUser.Id, cancellationToken))
        {
            db.Clinics.Add(new Clinic
            {
                OwnerUserId = clinicUser.Id,
                Name = "Veterinaria PetPals Demo",
                Description = "Clínica veterinaria de prueba.",
                Phone = "+34 600 000 001",
                Address = "Calle de las Mascotas 1",
                Latitude = 40.4168,
                Longitude = -3.7038,
                IsVerified = true
            });
        }

        if (!await db.Shelters.AnyAsync(shelter => shelter.OwnerUserId == shelterUser.Id, cancellationToken))
        {
            db.Shelters.Add(new Shelter
            {
                OwnerUserId = shelterUser.Id,
                Name = "Refugio PetPals Demo",
                Description = "Refugio de prueba para adopciones.",
                Phone = "+34 600 000 002",
                Address = "Avenida de los Animales 2",
                Latitude = 40.4168,
                Longitude = -3.7038,
                IsVerified = true
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string displayName,
        UserRole role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                DisplayName = displayName,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(user, Password);
            EnsureSucceeded(createResult, $"creating user {email}");
        }

        if (!await userManager.IsInRoleAsync(user, role.ToString()))
        {
            var roleResult = await userManager.AddToRoleAsync(user, role.ToString());
            EnsureSucceeded(roleResult, $"assigning role {role} to {email}");
        }

        return user;
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Database seeding failed while {operation}: " +
                string.Join("; ", result.Errors.Select(error => error.Description)));
        }
    }
}
