using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using VisitorReg.Application.DTOs;
using VisitorReg.Application.UseCases;
using VisitorReg.Domain.Enums;

namespace VisitorReg.Web.Pages.Visitors;

public class IndexModel : PageModel
{
    private readonly SearchVisitorsUseCase _searchVisitorsUseCase;

    public IndexModel(SearchVisitorsUseCase searchVisitorsUseCase)
    {
        _searchVisitorsUseCase = searchVisitorsUseCase;
    }

    [BindProperty(SupportsGet = true)]
    public SearchCriteriaModel SearchCriteria { get; set; } = new();

    public PagedResult<VisitorDto>? Visitors { get; set; }

    public class SearchCriteriaModel
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Name { get; set; }
        public string? Company { get; set; }
        public string? HostName { get; set; }
        public VisitorStatus? Status { get; set; }
        public int PageIndex { get; set; } = 0;
    }

    public async Task OnGetAsync()
    {
        var searchDto = new VisitorSearchDto
        {
            StartDate = SearchCriteria.StartDate,
            EndDate = SearchCriteria.EndDate,
            Name = SearchCriteria.Name,
            Company = SearchCriteria.Company,
            HostName = SearchCriteria.HostName,
            Status = SearchCriteria.Status,
            PageIndex = SearchCriteria.PageIndex,
            PageSize = 20
        };

        Visitors = await _searchVisitorsUseCase.ExecuteAsync(searchDto);
    }
}
