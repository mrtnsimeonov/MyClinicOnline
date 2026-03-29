using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyClinicOnline.Migrations
{
    /// <inheritdoc />
    public partial class AddMeetingCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MeetingCode",
                table: "Appointments",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MeetingCode",
                table: "Appointments");
        }
    }
}
