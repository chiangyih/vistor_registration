using VisitorReg.Application.DTOs;
using VisitorReg.Domain.Entities;
using VisitorReg.Infrastructure.Repositories;

namespace VisitorReg.Application.UseCases;

/// <summary>
/// 查詢訪客使用案例
/// </summary>
public class SearchVisitorsUseCase
{
    private readonly IVisitorRepository _visitorRepository;

    public SearchVisitorsUseCase(IVisitorRepository visitorRepository)
    {
        _visitorRepository = visitorRepository;
    }

    public async Task<PagedResult<VisitorDto>> ExecuteAsync(
        VisitorSearchDto searchDto,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _visitorRepository.SearchAsync(
            searchDto.StartDate,
            searchDto.EndDate,
            searchDto.Name,
            searchDto.Company,
            searchDto.HostName,
            searchDto.Status,
            searchDto.PageIndex,
            searchDto.PageSize,
            cancellationToken);

        return new PagedResult<VisitorDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageIndex = searchDto.PageIndex,
            PageSize = searchDto.PageSize
        };
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
