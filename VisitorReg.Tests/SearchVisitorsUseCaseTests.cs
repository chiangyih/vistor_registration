using Microsoft.EntityFrameworkCore;
using VisitorReg.Application.DTOs;
using VisitorReg.Application.UseCases;
using VisitorReg.Domain.Entities;
using VisitorReg.Domain.Enums;
using VisitorReg.Infrastructure.Data;
using VisitorReg.Infrastructure.Repositories;

namespace VisitorReg.Tests;

/// <summary>
/// SearchVisitorsUseCase 整合測試
/// </summary>
public class SearchVisitorsUseCaseTests : IDisposable
{
    private readonly VisitorDbContext _context;
    private readonly IVisitorRepository _visitorRepository;
    private readonly SearchVisitorsUseCase _useCase;

    public SearchVisitorsUseCaseTests()
    {
        var options = new DbContextOptionsBuilder<VisitorDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new VisitorDbContext(options);
        _visitorRepository = new VisitorRepository(_context);
        _useCase = new SearchVisitorsUseCase(_visitorRepository);

        // 建立測試資料
        SeedTestData().Wait();
    }

    private async Task SeedTestData()
    {
        var visitors = new List<Visitor>
        {
            new Visitor
            {
                Id = Guid.NewGuid(),
                RegisterNo = "V202602040001",
                Name = "張三",
                Company = "ABC公司",
                Purpose = "業務洽談",
                HostName = "李四",
                CheckInAt = new DateTime(2026, 2, 4, 9, 0, 0),
                Status = VisitorStatus.InSite,
                CreatedAt = DateTime.Now,
                CreatedBy = "System"
            },
            new Visitor
            {
                Id = Guid.NewGuid(),
                RegisterNo = "V202602040002",
                Name = "王五",
                Company = "XYZ科技",
                Purpose = "技術交流",
                HostName = "趙六",
                CheckInAt = new DateTime(2026, 2, 4, 10, 0, 0),
                CheckOutAt = new DateTime(2026, 2, 4, 12, 0, 0),
                Status = VisitorStatus.CheckedOut,
                CreatedAt = DateTime.Now,
                CreatedBy = "System"
            },
            new Visitor
            {
                Id = Guid.NewGuid(),
                RegisterNo = "V202602040003",
                Name = "陳七",
                Company = "ABC公司",
                Purpose = "參觀訪問",
                HostName = "李四",
                CheckInAt = new DateTime(2026, 2, 3, 14, 0, 0),
                Status = VisitorStatus.InSite,
                CreatedAt = DateTime.Now,
                CreatedBy = "System"
            }
        };

        await _context.Visitors.AddRangeAsync(visitors);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task ExecuteAsync_WithNoFilters_ShouldReturnAllVisitors()
    {
        // Arrange
        var searchDto = new VisitorSearchDto
        {
            PageIndex = 0,
            PageSize = 20
        };

        // Act
        var result = await _useCase.ExecuteAsync(searchDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Items.Count);
    }

    [Fact]
    public async Task ExecuteAsync_WithDateRange_ShouldFilterByDate()
    {
        // Arrange
        var searchDto = new VisitorSearchDto
        {
            StartDate = new DateTime(2026, 2, 4),
            EndDate = new DateTime(2026, 2, 4, 23, 59, 59),
            PageIndex = 0,
            PageSize = 20
        };

        // Act
        var result = await _useCase.ExecuteAsync(searchDto);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, v => 
            Assert.True(v.CheckInAt.Date == new DateTime(2026, 2, 4)));
    }

    [Fact]
    public async Task ExecuteAsync_WithName_ShouldFilterByName()
    {
        // Arrange
        var searchDto = new VisitorSearchDto
        {
            Name = "張",
            PageIndex = 0,
            PageSize = 20
        };

        // Act
        var result = await _useCase.ExecuteAsync(searchDto);

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Equal("張三", result.Items[0].Name);
    }

    [Fact]
    public async Task ExecuteAsync_WithCompany_ShouldFilterByCompany()
    {
        // Arrange
        var searchDto = new VisitorSearchDto
        {
            Company = "ABC",
            PageIndex = 0,
            PageSize = 20
        };

        // Act
        var result = await _useCase.ExecuteAsync(searchDto);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, v => Assert.Contains("ABC", v.Company));
    }

    [Fact]
    public async Task ExecuteAsync_WithStatus_ShouldFilterByStatus()
    {
        // Arrange
        var searchDto = new VisitorSearchDto
        {
            Status = VisitorStatus.InSite,
            PageIndex = 0,
            PageSize = 20
        };

        // Act
        var result = await _useCase.ExecuteAsync(searchDto);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, v => Assert.Equal(VisitorStatus.InSite, v.Status));
    }

    [Fact]
    public async Task ExecuteAsync_WithHostName_ShouldFilterByHostName()
    {
        // Arrange
        var searchDto = new VisitorSearchDto
        {
            HostName = "李四",
            PageIndex = 0,
            PageSize = 20
        };

        // Act
        var result = await _useCase.ExecuteAsync(searchDto);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, v => Assert.Equal("李四", v.HostName));
    }

    [Fact]
    public async Task ExecuteAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var searchDto = new VisitorSearchDto
        {
            PageIndex = 0,
            PageSize = 2
        };

        // Act
        var result = await _useCase.ExecuteAsync(searchDto);

        // Assert
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.TotalPages);
        Assert.True(result.HasNextPage);
        Assert.False(result.HasPreviousPage);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
