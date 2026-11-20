using Microsoft.EntityFrameworkCore;
using Server_ProductsApi.Models;

namespace Server_ProductsApi.Data;

public sealed class BookingDbContext(DbContextOptions<BookingDbContext> options) : DbContext(options)
{
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<AdminAccount> Admins => Set<AdminAccount>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var booking = modelBuilder.Entity<Booking>();
        booking.HasKey(item => item.Id);
        booking.Property(item => item.Name).HasMaxLength(80).IsRequired();
        booking.Property(item => item.Room).HasMaxLength(80).IsRequired();
        booking.Property(item => item.Date).HasMaxLength(10).IsRequired();
        booking.Property(item => item.Time).HasMaxLength(5).IsRequired();
        booking.HasIndex(item => item.CreatedAtUtc);

        var admin = modelBuilder.Entity<AdminAccount>();
        admin.HasKey(item => item.Id);
        admin.Property(item => item.Email).HasMaxLength(254).IsRequired();
        admin.HasIndex(item => item.Email).IsUnique();
        admin.Property(item => item.PasswordHash).HasMaxLength(512).IsRequired();

        var audit = modelBuilder.Entity<AuditEntry>();
        audit.HasKey(item => item.Id);
        audit.Property(item => item.Action).HasMaxLength(80).IsRequired();
        audit.Property(item => item.Actor).HasMaxLength(254);
        audit.HasIndex(item => item.CreatedAtUtc);
    }
}
