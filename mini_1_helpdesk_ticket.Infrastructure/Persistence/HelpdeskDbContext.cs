using Microsoft.EntityFrameworkCore;
using mini_1_helpdesk_ticket.Application.Abstractions.Persistence;
using mini_1_helpdesk_ticket.Domain.Common;
using mini_1_helpdesk_ticket.Domain.Labels;
using mini_1_helpdesk_ticket.Domain.Tickets;

namespace mini_1_helpdesk_ticket.Infrastructure.Persistence;

public class HelpdeskDbContext(
    DbContextOptions<HelpdeskDbContext> options): DbContext(options), IUnitOfWork
{
    
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketComment> TicketComments => Set<TicketComment>();
    public DbSet<Label> Labels => Set<Label>();
    public DbSet<TicketLabel> TicketLabels => Set<TicketLabel>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasSequence<long>("ticket_code_seq")
            .StartsAt(1)
            .IncrementsBy(1);
        
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HelpdeskDbContext).Assembly);
        //modelBuilder.SeedData();
    }
    
    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ApplyTimestamp();

        try
        {
            return await base.SaveChangesAsync(
                acceptAllChangesOnSuccess,
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new mini_1_helpdesk_ticket.Application.Common.Exceptions.AppException(
                mini_1_helpdesk_ticket.Application.Common.Exceptions.ErrorType.Conflict,
                "TICKET_CONCURRENCY_CONFLICT",
                "Ticket đã được cập nhật bởi request khác. Hãy tải lại dữ liệu mới nhất.");
        }
    }
    
    private void ApplyTimestamp()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    break;
                
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;

                    //case: update luôn của CreatedAt
                    //Nói với EF core ko được đưa CreatedAt vào trong update
                    entry.Property(nameof(IAuditableEntity.CreatedAt))
                        .IsModified = false;
                    break;
            }
        }
    }
}