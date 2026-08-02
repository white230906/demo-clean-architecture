using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace mini_1_helpdesk_ticket.Repo.Migrations
{
    /// <inheritdoc />
    public partial class config_helpdesk_dbcontext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "id",
                table: "ticket_labels");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "ticket_labels");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "id",
                table: "ticket_labels",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "ticket_labels",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
