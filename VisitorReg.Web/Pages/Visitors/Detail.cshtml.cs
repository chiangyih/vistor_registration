using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using VisitorReg.Application.DTOs;
using VisitorReg.Application.UseCases;

namespace VisitorReg.Web.Pages.Visitors;

public class DetailModel : PageModel
{
    private readonly GetVisitorDetailUseCase _getVisitorDetailUseCase;

    public DetailModel(GetVisitorDetailUseCase getVisitorDetailUseCase)
    {
        _getVisitorDetailUseCase = getVisitorDetailUseCase;
    }

    public VisitorDto? Visitor { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        Visitor = await _getVisitorDetailUseCase.ExecuteAsync(id);

        if (Visitor == null)
        {
            return NotFound();
        }

        return Page();
    }
}
