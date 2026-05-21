using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyClinicOnline.Migrations
{
    /// <inheritdoc />
    public partial class AddLockoutAndPasswordReset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FailedLoginAttempts",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockoutUntil",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetExpiry",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetToken",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FailedLoginAttempts",
                table: "Doctors",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockoutUntil",
                table: "Doctors",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetExpiry",
                table: "Doctors",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetToken",
                table: "Doctors",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailedLoginAttempts",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LockoutUntil",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordResetExpiry",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordResetToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FailedLoginAttempts",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "LockoutUntil",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "PasswordResetExpiry",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "PasswordResetToken",
                table: "Doctors");
        }
    }
}
