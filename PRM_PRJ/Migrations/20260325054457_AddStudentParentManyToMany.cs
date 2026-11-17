using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PRM_PRJ.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentParentManyToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_Users_ParentId",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_ParentId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "Students");

            migrationBuilder.CreateTable(
                name: "StudentParents",
                columns: table => new
                {
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentParents", x => new { x.StudentId, x.ParentUserId });
                    table.ForeignKey(
                        name: "FK_StudentParents_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentParents_Users_ParentUserId",
                        column: x => x.ParentUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentParents_ParentUserId",
                table: "StudentParents",
                column: "ParentUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentParents");

            migrationBuilder.AddColumn<Guid>(
                name: "ParentId",
                table: "Students",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_ParentId",
                table: "Students",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Users_ParentId",
                table: "Students",
                column: "ParentId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
