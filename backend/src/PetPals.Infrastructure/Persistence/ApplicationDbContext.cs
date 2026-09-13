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
    }
}
