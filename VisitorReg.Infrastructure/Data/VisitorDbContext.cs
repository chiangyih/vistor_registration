using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VisitorReg.Domain.Entities;

namespace VisitorReg.Infrastructure.Data;

/// <summary>
/// 訪客登記系統資料庫上下文
/// </summary>
public class VisitorDbContext : IdentityDbContext<IdentityUser>
{
    public VisitorDbContext(DbContextOptions<VisitorDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// 訪客資料集
    /// </summary>
    public DbSet<Visitor> Visitors => Set<Visitor>();

    /// <summary>
    /// 稽核日誌資料集
    /// </summary>
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 設定 Visitor 實體
        modelBuilder.Entity<Visitor>(entity =>
        {
            entity.ToTable("Visitors");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasDefaultValueSql("NEWID()");

            entity.Property(e => e.RegisterNo)
                .IsRequired()
                .HasMaxLength(30);

            entity.HasIndex(e => e.RegisterNo)
                .IsUnique();

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(80);

            entity.Property(e => e.IdNumberMasked)
                .HasMaxLength(30);

            entity.Property(e => e.Company)
                .HasMaxLength(120);

            entity.Property(e => e.Purpose)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.HostName)
                .IsRequired()
                .HasMaxLength(80);

            entity.Property(e => e.CheckInAt)
                .IsRequired()
                .HasColumnType("datetime2");

            entity.Property(e => e.CheckOutAt)
                .HasColumnType("datetime2");

            entity.Property(e => e.Phone)
                .HasMaxLength(30);

            entity.Property(e => e.Note)
                .HasMaxLength(400);

            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<byte>();

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasColumnType("datetime2")
                .HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.CreatedBy)
                .IsRequired()
                .HasMaxLength(80);

            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime2");

            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(80);

            entity.Property(e => e.RowVersion)
                .IsRowVersion();

            // 建立索引
            entity.HasIndex(e => e.CheckInAt);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.CheckInAt, e.Status });
            entity.HasIndex(e => new { e.HostName, e.CheckInAt });
        });

        // 設定 AuditLog 實體
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.OccurredAt)
                .IsRequired()
                .HasColumnType("datetime2")
                .HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.Actor)
                .IsRequired()
                .HasMaxLength(80);

            entity.Property(e => e.Action)
                .IsRequired()
                .HasMaxLength(40);

            entity.Property(e => e.TargetType)
                .IsRequired()
                .HasMaxLength(40);

            entity.Property(e => e.TargetId)
                .HasMaxLength(64);

            entity.Property(e => e.Result)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.Detail)
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.Ip)
                .HasMaxLength(45);

            // 建立索引
            entity.HasIndex(e => e.OccurredAt);
            entity.HasIndex(e => e.Actor);
        });
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is Visitor && 
                       (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var visitor = (Visitor)entry.Entity;

            if (entry.State == EntityState.Added)
            {
                visitor.CreatedAt = DateTime.Now;
            }
            else if (entry.State == EntityState.Modified)
            {
                visitor.UpdatedAt = DateTime.Now;
            }
        }
    }
}
