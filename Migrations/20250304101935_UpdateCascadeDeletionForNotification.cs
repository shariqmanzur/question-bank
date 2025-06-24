using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuestionBank.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCascadeDeletionForNotification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "QuestionId",
                table: "Notifications",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_QuestionId",
                table: "Notifications",
                column: "QuestionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Questions_QuestionId",
                table: "Notifications",
                column: "QuestionId",
                principalTable: "Questions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Questions_QuestionId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_QuestionId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "QuestionId",
                table: "Notifications");
        }
    }
}
