using Microsoft.EntityFrameworkCore;
using VisitorReg.Application.DTOs;
using VisitorReg.Application.UseCases;
using VisitorReg.Domain.Entities;
using VisitorReg.Domain.Enums;
using VisitorReg.Infrastructure.Data;
using VisitorReg.Infrastructure.Repositories;
using VisitorReg.Infrastructure.Services;

namespace VisitorReg.Tests;

/// <summary>
/// CreateVisitorUseCase 整合測試
/// </summary>
public class CreateVisitorUseCaseTests : IDisposable
{
    private readonly VisitorDbContext _context;
    private readonly IVisitorRepository _visitorRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly AuditService _auditService;
    private readonly CreateVisitorUseCase _useCase;

    public CreateVisitorUseCaseTests()
    {
        // 使用 InMemory 資料庫
        var options = new DbContextOptionsBuilder<VisitorDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new VisitorDbContext(options);
        _visitorRepository = new VisitorRepository(_context);
        _auditLogRepository = new AuditLogRepository(_context);
        _auditService = new AuditService(_auditLogRepository);
        _useCase = new CreateVisitorUseCase(_visitorRepository, _auditService);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidData_ShouldCreateVisitor()
    {
        // Arrange
        var dto = new CreateVisitorDto
        {
            Name = "張三",
            Company = "測試公司",
            Purpose = "業務洽談",
            HostName = "李四",
            CheckInAt = DateTime.Now,
            Phone = "0912345678"
        };

        // Act
        var result = await _useCase.ExecuteAsync(dto, "TestUser", "127.0.0.1");

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.NotEmpty(result.RegisterNo);
        Assert.Equal("張三", result.Name);
        Assert.Equal("測試公司", result.Company);
        Assert.Equal(VisitorStatus.InSite, result.Status);

        // 驗證資料庫
        var visitor = await _visitorRepository.GetByIdAsync(result.Id);
        Assert.NotNull(visitor);
        Assert.Equal("張三", visitor.Name);

        // 驗證稽核日誌
        var auditLogs = await _context.AuditLogs.ToListAsync();
        Assert.Single(auditLogs);
        Assert.Equal("CREATE", auditLogs[0].Action);
        Assert.Equal("Visitors", auditLogs[0].TargetType);
    }

    [Fact]
    public async Task ExecuteAsync_WithIdNumber_ShouldMaskIdNumber()
    {
        // Arrange
        var dto = new CreateVisitorDto
        {
            Name = "王五",
            IdNumber = "A123456789",
            Purpose = "技術交流",
            HostName = "趙六",
            CheckInAt = DateTime.Now
        };

        // Act
        var result = await _useCase.ExecuteAsync(dto, "TestUser", "127.0.0.1");

        // Assert
        var visitor = await _visitorRepository.GetByIdAsync(result.Id);
        Assert.NotNull(visitor);
        Assert.Equal("A12****789", visitor.IdNumberMasked);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldGenerateUniqueRegisterNo()
    {
        // Arrange
        var dto1 = new CreateVisitorDto
        {
            Name = "訪客1",
            Purpose = "目的1",
            HostName = "受訪者1",
            CheckInAt = DateTime.Now
        };

        var dto2 = new CreateVisitorDto
        {
            Name = "訪客2",
            Purpose = "目的2",
            HostName = "受訪者2",
            CheckInAt = DateTime.Now
        };

        // Act
        var result1 = await _useCase.ExecuteAsync(dto1, "TestUser", "127.0.0.1");
        var result2 = await _useCase.ExecuteAsync(dto2, "TestUser", "127.0.0.1");

        // Assert
        Assert.NotEqual(result1.RegisterNo, result2.RegisterNo);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSetCreatedByAndCreatedAt()
    {
        // Arrange
        var dto = new CreateVisitorDto
        {
            Name = "測試訪客",
            Purpose = "測試目的",
            HostName = "測試受訪者",
            CheckInAt = DateTime.Now
        };

        var currentUser = "admin@example.com";

        // Act
        var result = await _useCase.ExecuteAsync(dto, currentUser, "127.0.0.1");

        // Assert
        var visitor = await _visitorRepository.GetByIdAsync(result.Id);
        Assert.NotNull(visitor);
        Assert.Equal(currentUser, visitor.CreatedBy);
        Assert.True(visitor.CreatedAt <= DateTime.Now);
        Assert.True(visitor.CreatedAt >= DateTime.Now.AddSeconds(-5));
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
