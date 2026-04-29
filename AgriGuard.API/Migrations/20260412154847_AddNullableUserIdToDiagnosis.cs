using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriGuard.API.Migrations
{
    /// <inheritdoc />
    public partial class AddNullableUserIdToDiagnosis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Diagnoses",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Diagnoses_UserId",
                table: "Diagnoses",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Diagnoses_Users_UserId",
                table: "Diagnoses",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Diagnoses_Users_UserId",
                table: "Diagnoses");

            migrationBuilder.DropIndex(
                name: "IX_Diagnoses_UserId",
                table: "Diagnoses");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Diagnoses");
        }
    }
}
