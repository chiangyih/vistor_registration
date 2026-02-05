using VisitorReg.Infrastructure.Services;

namespace VisitorReg.Tests;

/// <summary>
/// IdNumberMasker 工具測試
/// </summary>
public class IdNumberMaskerTests
{
    [Theory]
    [InlineData("A123456789", "A12****789")]
    [InlineData("B987654321", "B98****321")]
    [InlineData("C111222333", "C11****333")]
    public void Mask_WithValidTaiwanId_ShouldMaskCorrectly(string input, string expected)
    {
        // Act
        var result = IdNumberMasker.Mask(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("ABC", "A**")]
    [InlineData("ABCDEF", "A*****")]
    [InlineData("AB", "A*")]
    public void Mask_WithShortInput_ShouldMaskAllExceptFirst(string input, string expected)
    {
        // Act
        var result = IdNumberMasker.Mask(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Mask_WithNullOrWhiteSpace_ShouldReturnNull(string? input)
    {
        // Act
        var result = IdNumberMasker.Mask(input);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Mask_WithWhiteSpace_ShouldTrimAndMask()
    {
        // Arrange
        var input = "  A123456789  ";

        // Act
        var result = IdNumberMasker.Mask(input);

        // Assert
        Assert.Equal("A12****789", result);
    }

    [Theory]
    [InlineData("A123456789", true)]
    [InlineData("B987654321", true)]
    [InlineData("Z000000000", true)]
    public void IsValidTaiwanId_WithValidId_ShouldReturnTrue(string input, bool expected)
    {
        // Act
        var result = IdNumberMasker.IsValidTaiwanId(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("123456789", false)]  // 缺少英文字母
    [InlineData("AB12345678", false)] // 兩個英文字母
    [InlineData("A12345678", false)]  // 只有9個字元
    [InlineData("A1234567890", false)] // 11個字元
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidTaiwanId_WithInvalidId_ShouldReturnFalse(string? input, bool expected)
    {
        // Act
        var result = IdNumberMasker.IsValidTaiwanId(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsValidTaiwanId_WithLowerCase_ShouldAccept()
    {
        // Arrange
        var input = "a123456789";

        // Act
        var result = IdNumberMasker.IsValidTaiwanId(input);

        // Assert
        Assert.True(result);
    }
}
