using VisitorReg.Application.DTOs;
using VisitorReg.Domain.Entities;
using VisitorReg.Domain.Enums;
using VisitorReg.Infrastructure.Repositories;
using VisitorReg.Infrastructure.Services;

namespace VisitorReg.Application.UseCases;

/// <summary>
/// 建立訪客使用案例
/// </summary>
public class CreateVisitorUseCase
{
    private readonly IVisitorRepository _visitorRepository;
    private readonly AuditService _auditService;

    public CreateVisitorUseCase(
        IVisitorRepository visitorRepository,
        AuditService auditService)
    {
        _visitorRepository = visitorRepository;
        _auditService = auditService;
    }

    public async Task<VisitorDto> ExecuteAsync(
        CreateVisitorDto dto,
        string currentUser,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        // 驗證輸入
        ValidateInput(dto);

        // 產生登記編號
        var registerNo = await _visitorRepository.GenerateRegisterNoAsync(cancellationToken);

        // 建立訪客實體
        var visitor = new Visitor
        {
            Id = Guid.NewGuid(),
            RegisterNo = registerNo,
            Name = dto.Name.Trim(),
            IdNumberMasked = IdNumberMasker.Mask(dto.IdNumber),
            Company = dto.Company?.Trim(),
            Purpose = dto.Purpose.Trim(),
            HostName = dto.HostName.Trim(),
            CheckInAt = dto.CheckInAt,
            Phone = dto.Phone?.Trim(),
            Note = dto.Note?.Trim(),
            Status = VisitorStatus.InSite,
            CreatedBy = currentUser
        };

        // 儲存
        await _visitorRepository.AddAsync(visitor, cancellationToken);

        // 記錄稽核
        await _auditService.LogSuccessAsync(
            currentUser,
            "CREATE",
            "Visitors",
            visitor.Id.ToString(),
            $"建立訪客登記: {visitor.Name}",
            ipAddress,
            cancellationToken);

        // 轉換為 DTO
        return MapToDto(visitor);
    }

    private void ValidateInput(CreateVisitorDto dto)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(dto.Name))
            errors.Add("訪客姓名為必填");

        if (dto.Name?.Length > 80)
            errors.Add("訪客姓名不可超過 80 字元");

        if (string.IsNullOrWhiteSpace(dto.Purpose))
            errors.Add("來訪目的為必填");

        if (dto.Purpose?.Length > 200)
            errors.Add("來訪目的不可超過 200 字元");

        if (string.IsNullOrWhiteSpace(dto.HostName))
            errors.Add("受訪者為必填");

        if (dto.HostName?.Length > 80)
            errors.Add("受訪者不可超過 80 字元");

        if (dto.Company?.Length > 120)
            errors.Add("公司/單位不可超過 120 字元");

        if (dto.Phone?.Length > 30)
            errors.Add("聯絡方式不可超過 30 字元");

        if (dto.Note?.Length > 400)
            errors.Add("備註不可超過 400 字元");

        if (errors.Any())
        {
            throw new ArgumentException(string.Join("; ", errors));
        }
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
