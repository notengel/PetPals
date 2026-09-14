using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetPals.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ClinicShelterPhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BannerUrl",
                table: "Shelters",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "Shelters",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BannerUrl",
                table: "Clinics",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "Clinics",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BannerUrl",
                table: "Shelters");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "Shelters");

            migrationBuilder.DropColumn(
                name: "BannerUrl",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "Clinics");
        }
    }
}
