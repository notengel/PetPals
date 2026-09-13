using Microsoft.AspNetCore.Identity;

namespace PetPals.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
}
