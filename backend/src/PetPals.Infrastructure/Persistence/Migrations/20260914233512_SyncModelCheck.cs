using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetPals.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ClinicReviews_AuthorUserId",
                table: "ClinicReviews",
                column: "AuthorUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ClinicReviews_AuthorUserId",
                table: "ClinicReviews");
        }
    }
}
