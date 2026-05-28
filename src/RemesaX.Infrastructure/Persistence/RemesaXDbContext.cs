using Microsoft.EntityFrameworkCore;
using RemesaX.Core.Entities;

namespace RemesaX.Infrastructure.Persistence;

public class RemesaXDbContext : DbContext
{
    public RemesaXDbContext(DbContextOptions<RemesaXDbContext> options) : base(options) { }

    public DbSet<Remittance> Remittances => Set<Remittance>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Remittance>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.SenderName).IsRequired().HasMaxLength(200);
            e.Property(r => r.DestinationAddress).IsRequired().HasMaxLength(56);
            e.Property(r => r.AmountUsdx).HasPrecision(18, 7);
            e.Property(r => r.ExchangeRate).HasPrecision(18, 6);
            e.Property(r => r.TargetCurrency).HasMaxLength(10);
            e.Property(r => r.TransactionHash).HasMaxLength(100);
            e.Property(r => r.Status).HasConversion<string>();
            e.HasIndex(r => r.CreatedAt);
            e.Ignore(r => r.AmountLocal);
        });
    }
}
