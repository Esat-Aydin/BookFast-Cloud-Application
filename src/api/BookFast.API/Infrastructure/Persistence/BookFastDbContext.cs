// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : BookFastDbContext.cs
//  Project         : BookFast.API
// ******************************************************************************

using BookFast.API.Domain;
using BookFast.API.Infrastructure.Eventing;

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

    public DbSet<OutboxMessageEntity> OutboxMessages => this.Set<OutboxMessageEntity>();

    public DbSet<IntegrationConsumerStateEntity> IntegrationConsumerStates => this.Set<IntegrationConsumerStateEntity>();

    public DbSet<IntegrationConsumerDeadLetterEntity> IntegrationConsumerDeadLetters => this.Set<IntegrationConsumerDeadLetterEntity>();

    public DbSet<ReportingReservationSyncEntity> ReportingReservationSyncs => this.Set<ReportingReservationSyncEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureRoomEntity(modelBuilder.Entity<RoomEntity>());
        ConfigureReservationEntity(modelBuilder.Entity<ReservationEntity>());
        ConfigureOutboxMessageEntity(modelBuilder.Entity<OutboxMessageEntity>());
        ConfigureIntegrationConsumerStateEntity(modelBuilder.Entity<IntegrationConsumerStateEntity>());
        ConfigureIntegrationConsumerDeadLetterEntity(modelBuilder.Entity<IntegrationConsumerDeadLetterEntity>());
        ConfigureReportingReservationSyncEntity(modelBuilder.Entity<ReportingReservationSyncEntity>());
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

    private static void ConfigureOutboxMessageEntity(EntityTypeBuilder<OutboxMessageEntity> entityBuilder)
    {
        entityBuilder.ToTable("OutboxMessages");
        entityBuilder.HasKey(message => message.Id);
        entityBuilder.Property(message => message.EventType).HasMaxLength(200).IsRequired();
        entityBuilder.Property(message => message.AggregateType).HasMaxLength(100).IsRequired();
        entityBuilder.Property(message => message.PayloadJson).IsRequired();
        entityBuilder.Property(message => message.CorrelationId).HasMaxLength(128);
        entityBuilder.Property(message => message.Status).HasConversion<int>().IsRequired();
        entityBuilder.Property(message => message.LastError).HasMaxLength(4000);
        entityBuilder.HasIndex(message => new
        {
            message.Status,
            message.NextAttemptUtc
        });
        entityBuilder.HasIndex(message => new
        {
            message.AggregateType,
            message.AggregateId
        });
    }

    private static void ConfigureIntegrationConsumerStateEntity(EntityTypeBuilder<IntegrationConsumerStateEntity> entityBuilder)
    {
        entityBuilder.ToTable("IntegrationConsumerStates");
        entityBuilder.HasKey(state => new
        {
            state.ConsumerName,
            state.MessageId
        });
        entityBuilder.Property(state => state.ConsumerName).HasMaxLength(200).IsRequired();
        entityBuilder.Property(state => state.EventType).HasMaxLength(200).IsRequired();
        entityBuilder.Property(state => state.CorrelationId).HasMaxLength(128);
        entityBuilder.HasIndex(state => state.ProcessedUtc);
    }

    private static void ConfigureIntegrationConsumerDeadLetterEntity(EntityTypeBuilder<IntegrationConsumerDeadLetterEntity> entityBuilder)
    {
        entityBuilder.ToTable("IntegrationConsumerDeadLetters");
        entityBuilder.HasKey(deadLetter => deadLetter.Id);
        entityBuilder.Property(deadLetter => deadLetter.ConsumerName).HasMaxLength(200).IsRequired();
        entityBuilder.Property(deadLetter => deadLetter.EventType).HasMaxLength(200).IsRequired();
        entityBuilder.Property(deadLetter => deadLetter.PayloadJson).IsRequired();
        entityBuilder.Property(deadLetter => deadLetter.CorrelationId).HasMaxLength(128);
        entityBuilder.Property(deadLetter => deadLetter.Reason).HasMaxLength(4000).IsRequired();
        entityBuilder.HasIndex(deadLetter => new
        {
            deadLetter.ConsumerName,
            deadLetter.MessageId
        }).IsUnique();
        entityBuilder.HasIndex(deadLetter => deadLetter.FailedUtc);
    }

    private static void ConfigureReportingReservationSyncEntity(EntityTypeBuilder<ReportingReservationSyncEntity> entityBuilder)
    {
        entityBuilder.ToTable("ReportingReservationSyncs");
        entityBuilder.HasKey(sync => sync.ReservationId);
        entityBuilder.Property(sync => sync.RoomCode).HasMaxLength(50).IsRequired();
        entityBuilder.Property(sync => sync.RoomName).HasMaxLength(150).IsRequired();
        entityBuilder.Property(sync => sync.Location).HasMaxLength(150).IsRequired();
        entityBuilder.Property(sync => sync.ReservedBy).HasMaxLength(200).IsRequired();
        entityBuilder.Property(sync => sync.Purpose).HasMaxLength(500);
        entityBuilder.Property(sync => sync.Status).HasMaxLength(50).IsRequired();
        entityBuilder.Property(sync => sync.CorrelationId).HasMaxLength(128);
        entityBuilder.HasIndex(sync => sync.RoomId);
        entityBuilder.HasIndex(sync => sync.LastSyncedUtc);
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
