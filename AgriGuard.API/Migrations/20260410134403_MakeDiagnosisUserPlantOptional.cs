using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriGuard.API.Migrations
{
    /// <inheritdoc />
    public partial class MakeDiagnosisUserPlantOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Diagnoses_UserPlants_UserPlantId",
                table: "Diagnoses");

            migrationBuilder.AlterColumn<int>(
                name: "UserPlantId",
                table: "Diagnoses",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Diagnoses_UserPlants_UserPlantId",
                table: "Diagnoses",
                column: "UserPlantId",
                principalTable: "UserPlants",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Diagnoses_UserPlants_UserPlantId",
                table: "Diagnoses");

            migrationBuilder.AlterColumn<int>(
                name: "UserPlantId",
                table: "Diagnoses",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Diagnoses_UserPlants_UserPlantId",
                table: "Diagnoses",
                column: "UserPlantId",
                principalTable: "UserPlants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
