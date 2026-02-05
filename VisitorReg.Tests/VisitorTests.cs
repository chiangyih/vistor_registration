using VisitorReg.Domain.Entities;
using VisitorReg.Domain.Enums;

namespace VisitorReg.Tests;

/// <summary>
/// Visitor 實體單元測試
/// </summary>
public class VisitorTests
{
    [Fact]
    public void Checkout_WhenInSite_ShouldSucceed()
    {
        // Arrange
        var visitor = new Visitor
        {
            Id = Guid.NewGuid(),
            RegisterNo = "V202602040001",
            Name = "測試訪客",
            Purpose = "測試目的",
            HostName = "測試受訪者",
            CheckInAt = DateTime.Now,
            Status = VisitorStatus.InSite,
            CreatedAt = DateTime.Now,
            CreatedBy = "System"
        };

        var checkoutTime = DateTime.Now;
        var updatedBy = "TestUser";

        // Act
        visitor.Checkout(checkoutTime, updatedBy);

        // Assert
        Assert.Equal(VisitorStatus.CheckedOut, visitor.Status);
        Assert.Equal(checkoutTime, visitor.CheckOutAt);
        Assert.Equal(updatedBy, visitor.UpdatedBy);
        Assert.NotNull(visitor.UpdatedAt);
    }

    [Fact]
    public void Checkout_WhenAlreadyCheckedOut_ShouldThrowException()
    {
        // Arrange
        var visitor = new Visitor
        {
            Id = Guid.NewGuid(),
            RegisterNo = "V202602040002",
            Name = "測試訪客",
            Purpose = "測試目的",
            HostName = "測試受訪者",
            CheckInAt = DateTime.Now,
            CheckOutAt = DateTime.Now,
            Status = VisitorStatus.CheckedOut,
            CreatedAt = DateTime.Now,
            CreatedBy = "System"
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            visitor.Checkout(DateTime.Now, "TestUser"));
        
        Assert.Contains("已離場或已作廢", exception.Message);
    }

    [Fact]
    public void Checkout_WhenVoided_ShouldThrowException()
    {
        // Arrange
        var visitor = new Visitor
        {
            Id = Guid.NewGuid(),
            RegisterNo = "V202602040003",
            Name = "測試訪客",
            Purpose = "測試目的",
            HostName = "測試受訪者",
            CheckInAt = DateTime.Now,
            Status = VisitorStatus.Voided,
            CreatedAt = DateTime.Now,
            CreatedBy = "System"
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            visitor.Checkout(DateTime.Now, "TestUser"));
        
        Assert.Contains("已離場或已作廢", exception.Message);
    }

    [Fact]
    public void CanCheckout_WhenInSite_ShouldReturnTrue()
    {
        // Arrange
        var visitor = new Visitor
        {
            Status = VisitorStatus.InSite
        };

        // Act
        var result = visitor.CanCheckout();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanCheckout_WhenCheckedOut_ShouldReturnFalse()
    {
        // Arrange
        var visitor = new Visitor
        {
            Status = VisitorStatus.CheckedOut
        };

        // Act
        var result = visitor.CanCheckout();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Void_ShouldChangeStatusToVoided()
    {
        // Arrange
        var visitor = new Visitor
        {
            Id = Guid.NewGuid(),
            RegisterNo = "V202602040004",
            Name = "測試訪客",
            Purpose = "測試目的",
            HostName = "測試受訪者",
            CheckInAt = DateTime.Now,
            Status = VisitorStatus.InSite,
            CreatedAt = DateTime.Now,
            CreatedBy = "System"
        };

        var updatedBy = "TestUser";

        // Act
        visitor.Void(updatedBy);

        // Assert
        Assert.Equal(VisitorStatus.Voided, visitor.Status);
        Assert.Equal(updatedBy, visitor.UpdatedBy);
        Assert.NotNull(visitor.UpdatedAt);
    }
}
