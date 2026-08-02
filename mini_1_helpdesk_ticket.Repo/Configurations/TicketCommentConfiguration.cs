using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Configuration;
using mini_1_helpdesk_ticket.Repo.Entity;

namespace mini_1_helpdesk_ticket.Repo.Configurations;

public class TicketCommentConfiguration: IEntityTypeConfiguration<TicketComment>
{
    public void Configure(EntityTypeBuilder<TicketComment> builder)
    {
        builder.ToTable("ticket_comments", table =>
        {
            table.HasCheckConstraint(
                "ck_ticket_comments_author_not_blank",
                "length(btrim(author_name)) > 0");

            table.HasCheckConstraint(
                "ck_ticket_comments_content_not_blank",
                "length(btrim(content)) > 0");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.AuthorName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Content)
            .HasColumnType("text")
            .HasMaxLength(5000)
            .IsRequired();

        builder.HasOne(x => x.Ticket)
            .WithMany(x => x.TicketComments)
            .HasForeignKey(x => x.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TicketId, x.CreatedAt, x.Id })
            .HasDatabaseName("ix_ticket_comments_ticket_id_created_at");
    }
}