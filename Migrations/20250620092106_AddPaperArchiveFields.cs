﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuestionBank.Migrations
{
    /// <inheritdoc />
    public partial class AddPaperArchiveFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Papers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Papers");
        }
    }
}
