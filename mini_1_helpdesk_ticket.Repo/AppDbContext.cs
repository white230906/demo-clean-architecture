using Microsoft.EntityFrameworkCore;

namespace mini_1_helpdesk_ticket.Repo;

public class AppDbContext : DbContext
{
    public  AppDbContext(DbContextOptions options) : base(options)
    {}
    
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        
        //modelBuilder.SeedData();
    }
}