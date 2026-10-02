using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using mini_1_helpdesk_ticket.Domain.Tickets;

namespace mini_1_helpdesk_ticket.Infrastructure.Persistence.Configurations;

public class TicketLabelConfiguration: IEntityTypeConfiguration<TicketLabel>
{
    public void Configure(EntityTypeBuilder<TicketLabel> builder)
    {
        builder.ToTable("ticket_labels");
        builder.HasKey(x => new { x.TicketId, x.LabelId });

        builder.HasOne(x => x.Ticket)
            .WithMany(x => x.TicketLabels)
            .HasForeignKey(x => x.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Label)
            .WithMany(x => x.TicketLabels)
            .HasForeignKey(x => x.LabelId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}