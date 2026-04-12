// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : ReportingDbContext.cs
//  Project         : BookFast.Reporting.Functions
// ******************************************************************************

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookFast.Reporting.Functions.Persistence;

public sealed class ReportingDbContext : DbContext
{
    public ReportingDbContext(DbContextOptions<ReportingDbContext> options)
        : base(options)
    {
    }

    public DbSet<RoomEntity> Rooms => this.Set<RoomEntity>();

    public DbSet<ReportingReservationSyncEntity> ReportingReservationSyncs => this.Set<ReportingReservationSyncEntity>();

    public DbSet<IntegrationConsumerStateEntity> IntegrationConsumerStates => this.Set<IntegrationConsumerStateEntity>();

    public DbSet<IntegrationConsumerDeadLetterEntity> IntegrationConsumerDeadLetters => this.Set<IntegrationConsumerDeadLetterEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureRoomEntity(modelBuilder.Entity<RoomEntity>());
        ConfigureReportingReservationSyncEntity(modelBuilder.Entity<ReportingReservationSyncEntity>());
        ConfigureIntegrationConsumerStateEntity(modelBuilder.Entity<IntegrationConsumerStateEntity>());
        ConfigureIntegrationConsumerDeadLetterEntity(modelBuilder.Entity<IntegrationConsumerDeadLetterEntity>());
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
}
