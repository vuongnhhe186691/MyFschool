using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRM_PRJ.Migrations
{
    /// <inheritdoc />
    public partial class AddWeekStartDateToTimetable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "WeekStartDate",
                table: "Timetables",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WeekStartDate",
                table: "Timetables");
        }
    }
}
