// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : BookFastDbContext.cs
//  Project         : BookFast.API
// ******************************************************************************

using BookFast.API.Domain;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookFast.API.Infrastructure.Persistence;

public sealed class BookFastDbContext : DbContext
{
    public BookFastDbContext(DbContextOptions<BookFastDbContext> options)
        : base(options)
    {
    }

    public DbSet<RoomEntity> Rooms => this.Set<RoomEntity>();

    public DbSet<ReservationEntity> Reservations => this.Set<ReservationEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureRoomEntity(modelBuilder.Entity<RoomEntity>());
        ConfigureReservationEntity(modelBuilder.Entity<ReservationEntity>());
    }

    private static void ConfigureRoomEntity(EntityTypeBuilder<RoomEntity> entityBuilder)
    {
        entityBuilder.ToTable("Rooms");
        entityBuilder.HasKey(room => room.Id);
        entityBuilder.HasIndex(room => room.Code).IsUnique();
        entityBuilder.Property(room => room.Code).HasMaxLength(50).IsRequired();
        entityBuilder.Property(room => room.Name).HasMaxLength(150).IsRequired();
        entityBuilder.Property(room => room.Location).HasMaxLength(150).IsRequired();
        entityBuilder.Property(room => room.Capacity).IsRequired();
        entityBuilder.Property(room => room.AmenitiesJson).IsRequired();
        entityBuilder.ToTable(tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_Rooms_Capacity_Positive", "[Capacity] > 0");
        });
        entityBuilder.HasData(CreateSeedRooms());
    }

    private static void ConfigureReservationEntity(EntityTypeBuilder<ReservationEntity> entityBuilder)
    {
        entityBuilder.ToTable("Reservations");
        entityBuilder.HasKey(reservation => reservation.Id);
        entityBuilder.Property(reservation => reservation.ReservedBy).HasMaxLength(200).IsRequired();
        entityBuilder.Property(reservation => reservation.Purpose).HasMaxLength(500);
        entityBuilder.Property(reservation => reservation.Status).HasConversion<int>().IsRequired();
        entityBuilder.HasIndex(reservation => new
        {
            reservation.RoomId,
            reservation.Status,
            reservation.StartUtc,
            reservation.EndUtc
        });
        entityBuilder.HasIndex(reservation => reservation.CreatedUtc);
        entityBuilder.HasOne(reservation => reservation.Room)
            .WithMany(room => room.Reservations)
            .HasForeignKey(reservation => reservation.RoomId)
            .OnDelete(DeleteBehavior.Restrict);
        entityBuilder.ToTable(tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_Reservations_TimeRange", "[StartUtc] < [EndUtc]");
        });
    }

    private static RoomEntity[] CreateSeedRooms()
    {
        return
        [
            new RoomEntity
            {
                Id = Guid.Parse("8C2D3CFD-2F3A-4C72-9F5B-7397C1D4B901"),
                Code = "AMS-BOARD-01",
                Name = "Amsterdam Boardroom",
                Location = "Amsterdam HQ - Floor 5",
                Capacity = 12,
                AmenitiesJson = "[\"Teams Room\",\"Whiteboard\",\"4K Display\"]"
            },
            new RoomEntity
            {
                Id = Guid.Parse("A8B70B66-676C-4A1D-9EA6-865A0B918A72"),
                Code = "UTR-COLLAB-02",
                Name = "Utrecht Collaboration Hub",
                Location = "Utrecht Office - Floor 2",
                Capacity = 8,
                AmenitiesJson = "[\"Video Conferencing\",\"Whiteboard\"]"
            },
            new RoomEntity
            {
                Id = Guid.Parse("C93B8E11-6D12-4F7C-8BF0-08FA0A1D2C54"),
                Code = "RTM-FOCUS-03",
                Name = "Rotterdam Focus Room",
                Location = "Rotterdam Office - Floor 3",
                Capacity = 4,
                AmenitiesJson = "[\"Quiet Zone\",\"Docking Station\"]"
            }
        ];
    }
}
