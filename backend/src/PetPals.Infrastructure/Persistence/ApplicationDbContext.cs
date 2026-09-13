using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PetPals.Domain.Entities;
using PetPals.Infrastructure.Identity;

namespace PetPals.Infrastructure.Persistence;

public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Pet> Pets => Set<Pet>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<PostLike> PostLikes => Set<PostLike>();
    public DbSet<Follow> Follows => Set<Follow>();
    public DbSet<Clinic> Clinics => Set<Clinic>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<ClinicService> ClinicServices => Set<ClinicService>();
    public DbSet<ClinicSchedule> ClinicSchedules => Set<ClinicSchedule>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<AppointmentPet> AppointmentPets => Set<AppointmentPet>();
    public DbSet<Shelter> Shelters => Set<Shelter>();
    public DbSet<AdoptablePet> AdoptablePets => Set<AdoptablePet>();
    public DbSet<VaccinationRecord> VaccinationRecords => Set<VaccinationRecord>();
    public DbSet<AdoptionRequest> AdoptionRequests => Set<AdoptionRequest>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationParticipant> ConversationParticipants => Set<ConversationParticipant>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(profile => profile.Id);
            entity.HasIndex(profile => profile.UserId).IsUnique();
            entity.Property(profile => profile.DisplayName).HasMaxLength(100).IsRequired();
            entity.Property(profile => profile.Bio).HasMaxLength(500);
            entity.Property(profile => profile.AvatarUrl).HasMaxLength(500);
            entity.HasOne<ApplicationUser>()
                .WithOne()
                .HasForeignKey<UserProfile>(profile => profile.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Pet>(entity =>
        {
            entity.HasKey(pet => pet.Id);
            entity.Property(pet => pet.Name).HasMaxLength(100).IsRequired();
            entity.Property(pet => pet.Species).HasMaxLength(50).IsRequired();
            entity.Property(pet => pet.Breed).HasMaxLength(100);
            entity.Property(pet => pet.Sex).HasMaxLength(30);
            entity.Property(pet => pet.Description).HasMaxLength(500);
            entity.Property(pet => pet.PrimaryImageUrl).HasMaxLength(500);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(pet => pet.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Post>(entity =>
        {
            entity.HasKey(post => post.Id);
            entity.Property(post => post.Content).HasMaxLength(2000).IsRequired();
            entity.Property(post => post.MediaUrl).HasMaxLength(500);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(post => post.AuthorUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Pet>()
                .WithMany()
                .HasForeignKey(post => post.PetId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(post => new { post.IsVisible, post.CreatedAtUtc });
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(comment => comment.Id);
            entity.Property(comment => comment.Content).HasMaxLength(1000).IsRequired();
            entity.HasOne<Post>()
                .WithMany()
                .HasForeignKey(comment => comment.PostId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(comment => comment.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(comment => new { comment.PostId, comment.CreatedAtUtc });
        });

        modelBuilder.Entity<PostLike>(entity =>
        {
            entity.HasKey(like => new { like.PostId, like.UserId });
            entity.HasOne<Post>()
                .WithMany()
                .HasForeignKey(like => like.PostId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(like => like.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Follow>(entity =>
        {
            entity.HasKey(follow => new { follow.FollowerUserId, follow.FollowedUserId });
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(follow => follow.FollowerUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(follow => follow.FollowedUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Clinic>(entity =>
        {
            entity.HasKey(clinic => clinic.Id);
            entity.HasIndex(clinic => clinic.OwnerUserId).IsUnique();
            entity.Property(clinic => clinic.Name).HasMaxLength(150).IsRequired();
            entity.Property(clinic => clinic.Description).HasMaxLength(1000);
            entity.Property(clinic => clinic.Phone).HasMaxLength(30);
            entity.Property(clinic => clinic.Address).HasMaxLength(300);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(clinic => clinic.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(product => product.Id);
            entity.Property(product => product.Name).HasMaxLength(150).IsRequired();
            entity.Property(product => product.Description).HasMaxLength(1000);
            entity.Property(product => product.Price).HasPrecision(18, 2);
            entity.Property(product => product.ImageUrl).HasMaxLength(500);
            entity.HasOne<Clinic>()
                .WithMany()
                .HasForeignKey(product => product.ClinicId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(product => new { product.ClinicId, product.IsActive });
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(cart => cart.Id);
            entity.HasIndex(cart => cart.BuyerUserId).IsUnique();
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(cart => cart.BuyerUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.UnitPrice).HasPrecision(18, 2);
            entity.HasIndex(item => new { item.CartId, item.ProductId }).IsUnique();
            entity.HasOne<Cart>()
                .WithMany(cart => cart.Items)
                .HasForeignKey(item => item.CartId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Product>()
                .WithMany()
                .HasForeignKey(item => item.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(order => order.Id);
            entity.Property(order => order.Total).HasPrecision(18, 2);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(order => order.BuyerUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Clinic>()
                .WithMany()
                .HasForeignKey(order => order.ClinicId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(order => new { order.BuyerUserId, order.CreatedAtUtc });
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.ProductName).HasMaxLength(150).IsRequired();
            entity.Property(item => item.UnitPrice).HasPrecision(18, 2);
            entity.Property(item => item.Subtotal).HasPrecision(18, 2);
            entity.HasOne<Order>()
                .WithMany(order => order.Items)
                .HasForeignKey(item => item.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Product>()
                .WithMany()
                .HasForeignKey(item => item.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ClinicService>(entity =>
        {
            entity.HasKey(service => service.Id);
            entity.Property(service => service.Name).HasMaxLength(150).IsRequired();
            entity.Property(service => service.Description).HasMaxLength(1000);
            entity.Property(service => service.Price).HasPrecision(18, 2);
            entity.HasOne<Clinic>()
                .WithMany()
                .HasForeignKey(service => service.ClinicId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(service => new { service.ClinicId, service.IsActive });
        });

        modelBuilder.Entity<ClinicSchedule>(entity =>
        {
            entity.HasKey(schedule => schedule.Id);
            entity.HasOne<Clinic>()
                .WithMany()
                .HasForeignKey(schedule => schedule.ClinicId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(schedule => new
            {
                schedule.ClinicId,
                schedule.DayOfWeek,
                schedule.OpensAt,
                schedule.ClosesAt
            }).IsUnique();
        });

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasKey(appointment => appointment.Id);
            entity.Property(appointment => appointment.UserNotes).HasMaxLength(1000);
            entity.Property(appointment => appointment.ClinicNotes).HasMaxLength(1000);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(appointment => appointment.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Clinic>()
                .WithMany()
                .HasForeignKey(appointment => appointment.ClinicId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ClinicService>()
                .WithMany()
                .HasForeignKey(appointment => appointment.ClinicServiceId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(appointment => new { appointment.ClinicId, appointment.StartsAtUtc });
            entity.HasIndex(appointment => new { appointment.UserId, appointment.StartsAtUtc });
        });

        modelBuilder.Entity<AppointmentPet>(entity =>
        {
            entity.HasKey(item => new { item.AppointmentId, item.PetId });
            entity.HasOne<Appointment>()
                .WithMany(appointment => appointment.Pets)
                .HasForeignKey(item => item.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Pet>()
                .WithMany()
                .HasForeignKey(item => item.PetId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Shelter>(entity =>
        {
            entity.HasKey(shelter => shelter.Id);
            entity.HasIndex(shelter => shelter.OwnerUserId).IsUnique();
            entity.Property(shelter => shelter.Name).HasMaxLength(150).IsRequired();
            entity.Property(shelter => shelter.Description).HasMaxLength(1000);
            entity.Property(shelter => shelter.Phone).HasMaxLength(30);
            entity.Property(shelter => shelter.Address).HasMaxLength(300);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(shelter => shelter.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AdoptablePet>(entity =>
        {
            entity.HasKey(pet => pet.Id);
            entity.Property(pet => pet.Name).HasMaxLength(100).IsRequired();
            entity.Property(pet => pet.Species).HasMaxLength(50).IsRequired();
            entity.Property(pet => pet.Breed).HasMaxLength(100);
            entity.Property(pet => pet.Sex).HasMaxLength(30);
            entity.Property(pet => pet.ApproximateAge).HasMaxLength(50);
            entity.Property(pet => pet.Description).HasMaxLength(1000);
            entity.Property(pet => pet.PrimaryImageUrl).HasMaxLength(500);
            entity.HasOne<Shelter>()
                .WithMany()
                .HasForeignKey(pet => pet.ShelterId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(pet => new { pet.ShelterId, pet.Status });
            entity.HasIndex(pet => pet.Status);
        });

        modelBuilder.Entity<VaccinationRecord>(entity =>
        {
            entity.HasKey(record => record.Id);
            entity.Property(record => record.VaccineName).HasMaxLength(150).IsRequired();
            entity.Property(record => record.Notes).HasMaxLength(500);
            entity.HasOne<AdoptablePet>()
                .WithMany()
                .HasForeignKey(record => record.AdoptablePetId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(record => new { record.AdoptablePetId, record.AppliedOn });
        });

        modelBuilder.Entity<AdoptionRequest>(entity =>
        {
            entity.HasKey(request => request.Id);
            entity.Property(request => request.ApplicantMessage).HasMaxLength(1000);
            entity.Property(request => request.ShelterComment).HasMaxLength(1000);
            entity.HasOne<AdoptablePet>()
                .WithMany()
                .HasForeignKey(request => request.AdoptablePetId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(request => request.ApplicantUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(request => new { request.AdoptablePetId, request.Status });
            entity.HasIndex(request => new { request.ApplicantUserId, request.CreatedAtUtc });
        });

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(conversation => conversation.Id);
            entity.HasIndex(conversation => new { conversation.IsActive, conversation.LastMessageAtUtc });
        });

        modelBuilder.Entity<ConversationParticipant>(entity =>
        {
            entity.HasKey(participant => new { participant.ConversationId, participant.UserId });
            entity.HasOne<Conversation>()
                .WithMany(conversation => conversation.Participants)
                .HasForeignKey(participant => participant.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(participant => participant.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(participant => participant.UserId);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(message => message.Id);
            entity.Property(message => message.Content).HasMaxLength(4000).IsRequired();
            entity.HasOne<Conversation>()
                .WithMany(conversation => conversation.Messages)
                .HasForeignKey(message => message.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(message => message.SenderUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(message => new { message.ConversationId, message.SentAtUtc });
        });
    }
}
