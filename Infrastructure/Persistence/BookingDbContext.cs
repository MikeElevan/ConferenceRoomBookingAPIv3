using ConferenceRoomBookingAPIv3.DomainModels;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRoomBookingAPIv3.Infrastructure.Persistence;

public sealed class BookingDbContext(DbContextOptions<BookingDbContext> options) : DbContext(options)
{
    public DbSet<ConferenceRoom> Rooms => Set<ConferenceRoom>();
    public DbSet<RoomService> RoomServices => Set<RoomService>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConferenceRoom>(entity =>
        {
            entity.HasKey(room => room.Id);
            entity.Property(room => room.Name).IsRequired().HasMaxLength(100);
            entity.Property(room => room.BaseHourlyRate).HasPrecision(18, 2);
            entity.Property(room => room.RowVersion).IsRowVersion();
            entity.HasMany(room => room.Services)
                .WithOne()
                .HasForeignKey("ConferenceRoomId")
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RoomService>(entity =>
        {
            entity.HasKey(service => service.Id);
            entity.Property(service => service.Name).IsRequired().HasMaxLength(100);
            entity.Property(service => service.Price).HasPrecision(18, 2);
            entity.HasIndex("ConferenceRoomId", nameof(RoomService.Name)).IsUnique();
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(booking => booking.Id);
            entity.HasOne<ConferenceRoom>()
                .WithMany()
                .HasForeignKey(booking => booking.RoomId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(booking => booking.Services)
                .WithOne()
                .HasForeignKey(snapshot => snapshot.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(booking => new { booking.RoomId, booking.StartsAt, booking.EndsAt });
            entity.Property(booking => booking.RoomCost).HasPrecision(18, 2);
            entity.Property(booking => booking.ServicesCost).HasPrecision(18, 2);
            entity.Ignore(booking => booking.TotalCost);
        });

        modelBuilder.Entity<BookingServiceSnapshot>(entity =>
        {
            entity.HasKey(snapshot => new { snapshot.BookingId, snapshot.ServiceId });
            entity.Property(snapshot => snapshot.Name).IsRequired().HasMaxLength(100);
            entity.Property(snapshot => snapshot.Price).HasPrecision(18, 2);
        });
    }
}
