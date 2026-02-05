using VisitorReg.Application.DTOs;
using VisitorReg.Domain.Entities;
using VisitorReg.Infrastructure.Repositories;

namespace VisitorReg.Application.UseCases;

/// <summary>
/// 取得訪客詳細資料使用案例
/// </summary>
public class GetVisitorDetailUseCase
{
    private readonly IVisitorRepository _visitorRepository;

    public GetVisitorDetailUseCase(IVisitorRepository visitorRepository)
    {
        _visitorRepository = visitorRepository;
    }

    public async Task<VisitorDto?> ExecuteAsync(
        Guid visitorId,
        CancellationToken cancellationToken = default)
    {
        var visitor = await _visitorRepository.GetByIdAsync(visitorId, cancellationToken);
        
        return visitor == null ? null : MapToDto(visitor);
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
