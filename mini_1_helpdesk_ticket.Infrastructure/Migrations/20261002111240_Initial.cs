using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace mini_1_helpdesk_ticket.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "ticket_code_seq");

            migrationBuilder.CreateTable(
                name: "labels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_labels", x => x.id);
                    table.CheckConstraint("ck_labels_color_hex", "color ~ '^#[0-9A-Fa-f]{6}$'");
                    table.CheckConstraint("ck_labels_name_not_blank", "length(btrim(name)) > 0");
                    table.CheckConstraint("ck_labels_slug_format", "slug ~ '^[a-z0-9]+(-[a-z0-9]+)*$'");
                });

            migrationBuilder.CreateTable(
                name: "tickets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", maxLength: 5000, nullable: false),
                    priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Medium"),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Open"),
                    assignee_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tickets", x => x.id);
                    table.CheckConstraint("ck_tickets_description_not_blank", "length(btrim(description)) > 0");
                    table.CheckConstraint("ck_tickets_priority_valid", "priority IN ('Low','Medium','High', 'Urgent')");
                    table.CheckConstraint("ck_tickets_status_valid", "status IN ('Open','Closed','Resolved','InProgress')");
                    table.CheckConstraint("ck_tickets_title_not_blank", "length(btrim(title)) > 0");
                });

            migrationBuilder.CreateTable(
                name: "ticket_comments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticket_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    content = table.Column<string>(type: "text", maxLength: 5000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ticket_comments", x => x.id);
                    table.CheckConstraint("ck_ticket_comments_author_not_blank", "length(btrim(author_name)) > 0");
                    table.CheckConstraint("ck_ticket_comments_content_not_blank", "length(btrim(content)) > 0");
                    table.ForeignKey(
                        name: "fk_ticket_comments_tickets_ticket_id",
                        column: x => x.ticket_id,
                        principalTable: "tickets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ticket_labels",
                columns: table => new
                {
                    ticket_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ticket_labels", x => new { x.ticket_id, x.label_id });
                    table.ForeignKey(
                        name: "fk_ticket_labels_labels_label_id",
                        column: x => x.label_id,
                        principalTable: "labels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_ticket_labels_tickets_ticket_id",
                        column: x => x.ticket_id,
                        principalTable: "tickets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_labels_slug",
                table: "labels",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ticket_comments_ticket_id_created_at",
                table: "ticket_comments",
                columns: new[] { "ticket_id", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_ticket_labels_label_id",
                table: "ticket_labels",
                column: "label_id");

            migrationBuilder.CreateIndex(
                name: "ix_tickets_assignee_name",
                table: "tickets",
                column: "assignee_name");

            migrationBuilder.CreateIndex(
                name: "ix_tickets_code",
                table: "tickets",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tickets_created_at_id",
                table: "tickets",
                columns: new[] { "created_at", "id" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_tickets_status_priority",
                table: "tickets",
                columns: new[] { "status", "priority" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ticket_comments");

            migrationBuilder.DropTable(
                name: "ticket_labels");

            migrationBuilder.DropTable(
                name: "labels");

            migrationBuilder.DropTable(
                name: "tickets");

            migrationBuilder.DropSequence(
                name: "ticket_code_seq");
        }
    }
}
