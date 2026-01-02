using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyClinicOnline.Migrations
{
    /// <inheritdoc />
    public partial class AddCityAndDoctorCity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CityId",
                table: "Doctors",
                type: "int",
                nullable: true);


            migrationBuilder.CreateTable(
                name: "Cities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cities", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_CityId",
                table: "Doctors",
                column: "CityId");

            migrationBuilder.AddForeignKey(
                name: "FK_Doctors_Cities_CityId",
                table: "Doctors",
                column: "CityId",
                principalTable: "Cities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Doctors_Cities_CityId",
                table: "Doctors");

            migrationBuilder.DropTable(
                name: "Cities");

            migrationBuilder.DropIndex(
                name: "IX_Doctors_CityId",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "CityId",
                table: "Doctors");
        }
    }
}
