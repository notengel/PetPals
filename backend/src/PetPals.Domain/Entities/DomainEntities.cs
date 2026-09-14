using PetPals.Domain.Enums;

namespace PetPals.Domain.Entities;

public abstract class Entity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class UserProfile : Entity
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string? BannerUrl { get; set; }
    public bool IsPublic { get; set; } = true;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public class Clinic : Entity
{
    public Guid OwnerUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? LogoUrl { get; set; }
    public string? BannerUrl { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsVerified { get; set; }
}

public class ClinicPhoto : Entity
{
    public Guid ClinicId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
}

public class ClinicReview : Entity
{
    public Guid ClinicId { get; set; }
    public Guid AuthorUserId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
}

public class Shelter : Entity
{
    public Guid OwnerUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? LogoUrl { get; set; }
    public string? BannerUrl { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsVerified { get; set; }
}

public class Pet : Entity
{
    public Guid OwnerUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Species { get; set; } = string.Empty;
    public string? Breed { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? Sex { get; set; }
    public string? Description { get; set; }
    public string? PrimaryImageUrl { get; set; }
}

public class PetPhoto : Entity
{
    public Guid PetId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
}

public class Post : Entity
{
    public Guid AuthorUserId { get; set; }
    public Guid? PetId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? MediaUrl { get; set; }
    public bool IsVisible { get; set; } = true;
}

public class Comment : Entity
{
    public Guid PostId { get; set; }
    public Guid UserId { get; set; }
    public string Content { get; set; } = string.Empty;
}

public class PostLike
{
    public Guid PostId { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class Follow
{
    public Guid FollowerUserId { get; set; }
    public Guid FollowedUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class ClinicService : Entity
{
    public Guid ClinicId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public int? DurationMinutes { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ClinicSchedule : Entity
{
    public Guid ClinicId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly OpensAt { get; set; }
    public TimeOnly ClosesAt { get; set; }
}

public class Appointment : Entity
{
    public Guid UserId { get; set; }
    public Guid ClinicId { get; set; }
    public Guid ClinicServiceId { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    public string? UserNotes { get; set; }
    public string? ClinicNotes { get; set; }
    public List<AppointmentPet> Pets { get; set; } = [];
}

public class AppointmentPet
{
    public Guid AppointmentId { get; set; }
    public Guid PetId { get; set; }
}

public class Product : Entity
{
    public Guid ClinicId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ProductCategory Category { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Cart : Entity
{
    public Guid BuyerUserId { get; set; }
    public List<CartItem> Items { get; set; } = [];
}

public class CartItem : Entity
{
    public Guid CartId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class Order : Entity
{
    public Guid BuyerUserId { get; set; }
    public Guid ClinicId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal Total { get; set; }
    public List<OrderItem> Items { get; set; } = [];
}

public class OrderItem : Entity
{
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal Subtotal { get; set; }
}

public class AdoptablePet : Entity
{
    public Guid ShelterId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Species { get; set; } = string.Empty;
    public string? Breed { get; set; }
    public string? Sex { get; set; }
    public string? ApproximateAge { get; set; }
    public string? Description { get; set; }
    public AdoptablePetStatus Status { get; set; } = AdoptablePetStatus.Available;
    public string? PrimaryImageUrl { get; set; }
}

public class VaccinationRecord : Entity
{
    public Guid AdoptablePetId { get; set; }
    public string VaccineName { get; set; } = string.Empty;
    public DateOnly AppliedOn { get; set; }
    public DateOnly? NextDueOn { get; set; }
    public string? Notes { get; set; }
}

public class AdoptablePetPhoto : Entity
{
    public Guid AdoptablePetId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
}

public class AdoptionRequest : Entity
{
    public Guid AdoptablePetId { get; set; }
    public Guid ApplicantUserId { get; set; }
    public AdoptionRequestStatus Status { get; set; } = AdoptionRequestStatus.Pending;
    public string? ApplicantMessage { get; set; }
    public string? ShelterComment { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
}

public class Conversation : Entity
{
    public DateTime? LastMessageAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public List<ConversationParticipant> Participants { get; set; } = [];
    public List<Message> Messages { get; set; } = [];
}

public class ConversationParticipant
{
    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; }
    public DateTime JoinedAtUtc { get; set; } = DateTime.UtcNow;
}

public class Message : Entity
{
    public Guid ConversationId { get; set; }
    public Guid SenderUserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAtUtc { get; set; }
}
