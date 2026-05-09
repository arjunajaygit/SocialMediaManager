using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialMediaManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountUsername : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountUsername",
                table: "SocialAccounts",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountUsername",
                table: "SocialAccounts");
        }
    }
}
