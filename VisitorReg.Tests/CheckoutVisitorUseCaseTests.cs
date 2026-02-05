using Microsoft.EntityFrameworkCore;
using VisitorReg.Application.UseCases;
using VisitorReg.Domain.Entities;
using VisitorReg.Domain.Enums;
using VisitorReg.Infrastructure.Data;
using VisitorReg.Infrastructure.Repositories;
using VisitorReg.Infrastructure.Services;

namespace VisitorReg.Tests;

/// <summary>
/// CheckoutVisitorUseCase 整合測試
/// </summary>
public class CheckoutVisitorUseCaseTests : IDisposable
{
    private readonly VisitorDbContext _context;
    private readonly IVisitorRepository _visitorRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly AuditService _auditService;
    private readonly CheckoutVisitorUseCase _useCase;

    public CheckoutVisitorUseCaseTests()
    {
        var options = new DbContextOptionsBuilder<VisitorDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new VisitorDbContext(options);
        _visitorRepository = new VisitorRepository(_context);
        _auditLogRepository = new AuditLogRepository(_context);
        _auditService = new AuditService(_auditLogRepository);
        _useCase = new CheckoutVisitorUseCase(_visitorRepository, _auditService);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidVisitor_ShouldCheckout()
    {
        // Arrange
        var visitor = new Visitor
        {
            Id = Guid.NewGuid(),
            RegisterNo = "V202602040001",
            Name = "張三",
            Purpose = "業務洽談",
            HostName = "李四",
            CheckInAt = DateTime.Now.AddHours(-2),
            Status = VisitorStatus.InSite,
            CreatedAt = DateTime.Now,
            CreatedBy = "System"
        };

        await _context.Visitors.AddAsync(visitor);
        await _context.SaveChangesAsync();

        var checkoutTime = DateTime.Now;

        // Act
        var result = await _useCase.ExecuteAsync(visitor.Id, checkoutTime, "TestUser", "127.0.0.1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(VisitorStatus.CheckedOut, result.Status);
        Assert.Equal(checkoutTime, result.CheckOutAt);

        // 驗證資料庫
        var updatedVisitor = await _visitorRepository.GetByIdAsync(visitor.Id);
        Assert.NotNull(updatedVisitor);
        Assert.Equal(VisitorStatus.CheckedOut, updatedVisitor.Status);
        Assert.Equal(checkoutTime, updatedVisitor.CheckOutAt);

        // 驗證稽核日誌
        var auditLogs = await _context.AuditLogs.ToListAsync();
        Assert.Single(auditLogs);
        Assert.Equal("CHECKOUT", auditLogs[0].Action);
        Assert.Equal("Visitors", auditLogs[0].TargetType);
        Assert.Equal(visitor.Id.ToString(), auditLogs[0].TargetId);
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentVisitor_ShouldThrowException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _useCase.ExecuteAsync(nonExistentId, DateTime.Now, "TestUser", "127.0.0.1"));
    }

    [Fact]
    public async Task ExecuteAsync_WithAlreadyCheckedOutVisitor_ShouldThrowException()
    {
        // Arrange
        var visitor = new Visitor
        {
            Id = Guid.NewGuid(),
            RegisterNo = "V202602040002",
            Name = "王五",
            Purpose = "技術交流",
            HostName = "趙六",
            CheckInAt = DateTime.Now.AddHours(-3),
            CheckOutAt = DateTime.Now.AddHours(-1),
            Status = VisitorStatus.CheckedOut,
            CreatedAt = DateTime.Now,
            CreatedBy = "System"
        };

        await _context.Visitors.AddAsync(visitor);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _useCase.ExecuteAsync(visitor.Id, DateTime.Now, "TestUser", "127.0.0.1"));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUpdateUpdatedByAndUpdatedAt()
    {
        // Arrange
        var visitor = new Visitor
        {
            Id = Guid.NewGuid(),
            RegisterNo = "V202602040003",
            Name = "測試訪客",
            Purpose = "測試目的",
            HostName = "測試受訪者",
            CheckInAt = DateTime.Now.AddHours(-1),
            Status = VisitorStatus.InSite,
            CreatedAt = DateTime.Now,
            CreatedBy = "System"
        };

        await _context.Visitors.AddAsync(visitor);
        await _context.SaveChangesAsync();

        var currentUser = "admin@example.com";

        // Act
        await _useCase.ExecuteAsync(visitor.Id, DateTime.Now, currentUser, "127.0.0.1");

        // Assert
        var updatedVisitor = await _visitorRepository.GetByIdAsync(visitor.Id);
        Assert.NotNull(updatedVisitor);
        Assert.Equal(currentUser, updatedVisitor.UpdatedBy);
        Assert.NotNull(updatedVisitor.UpdatedAt);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
