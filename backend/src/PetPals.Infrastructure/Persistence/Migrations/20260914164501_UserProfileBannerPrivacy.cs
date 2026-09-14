using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetPals.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UserProfileBannerPrivacy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BannerUrl",
                table: "UserProfiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "UserProfiles",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BannerUrl",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "UserProfiles");
        }
    }
}
