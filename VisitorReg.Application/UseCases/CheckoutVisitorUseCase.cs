using VisitorReg.Application.DTOs;
using VisitorReg.Domain.Entities;
using VisitorReg.Infrastructure.Repositories;
using VisitorReg.Infrastructure.Services;

namespace VisitorReg.Application.UseCases;

/// <summary>
/// 離場登記使用案例
/// </summary>
public class CheckoutVisitorUseCase
{
    private readonly IVisitorRepository _visitorRepository;
    private readonly AuditService _auditService;

    public CheckoutVisitorUseCase(
        IVisitorRepository visitorRepository,
        AuditService auditService)
    {
        _visitorRepository = visitorRepository;
        _auditService = auditService;
    }

    public async Task<VisitorDto> ExecuteAsync(
        Guid visitorId,
        DateTime checkoutTime,
        string currentUser,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        // 取得訪客
        var visitor = await _visitorRepository.GetByIdAsync(visitorId, cancellationToken);
        if (visitor == null)
        {
            throw new ArgumentException("找不到指定的訪客");
        }

        // 執行離場
        try
        {
            visitor.Checkout(checkoutTime, currentUser);
        }
        catch (InvalidOperationException ex)
        {
            // 記錄失敗稽核
            await _auditService.LogFailureAsync(
                currentUser,
                "CHECKOUT",
                "Visitors",
                visitor.Id.ToString(),
                ex.Message,
                ipAddress,
                cancellationToken);

            throw;
        }

        // 更新
        await _visitorRepository.UpdateAsync(visitor, cancellationToken);

        // 記錄成功稽核
        await _auditService.LogSuccessAsync(
            currentUser,
            "CHECKOUT",
            "Visitors",
            visitor.Id.ToString(),
            $"訪客離場: {visitor.Name}",
            ipAddress,
            cancellationToken);

        // 轉換為 DTO
        return MapToDto(visitor);
    }

    private VisitorDto MapToDto(Visitor visitor)
    {
        return new VisitorDto
        {
            Id = visitor.Id,
            RegisterNo = visitor.RegisterNo,
            Name = visitor.Name,
            IdNumberMasked = visitor.IdNumberMasked,
            Company = visitor.Company,
            Purpose = visitor.Purpose,
            HostName = visitor.HostName,
            CheckInAt = visitor.CheckInAt,
            CheckOutAt = visitor.CheckOutAt,
            Phone = visitor.Phone,
            Note = visitor.Note,
            Status = visitor.Status,
            CreatedAt = visitor.CreatedAt,
            CreatedBy = visitor.CreatedBy
        };
    }
}
