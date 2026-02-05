using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using VisitorReg.Application.DTOs;
using VisitorReg.Application.UseCases;

namespace VisitorReg.Web.Pages.Visitors;

public class CheckoutModel : PageModel
{
    private readonly GetVisitorDetailUseCase _getVisitorDetailUseCase;
    private readonly CheckoutVisitorUseCase _checkoutVisitorUseCase;
    private readonly ILogger<CheckoutModel> _logger;

    public CheckoutModel(
        GetVisitorDetailUseCase getVisitorDetailUseCase,
        CheckoutVisitorUseCase checkoutVisitorUseCase,
        ILogger<CheckoutModel> logger)
    {
        _getVisitorDetailUseCase = getVisitorDetailUseCase;
        _checkoutVisitorUseCase = checkoutVisitorUseCase;
        _logger = logger;
    }

    public VisitorDto? Visitor { get; set; }

    [BindProperty]
    public Guid VisitorId { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "離場時間為必填")]
    [Display(Name = "離場時間")]
    public DateTime CheckoutTime { get; set; } = DateTime.Now;

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        VisitorId = id;
        Visitor = await _getVisitorDetailUseCase.ExecuteAsync(id);

        if (Visitor == null)
        {
            return NotFound();
        }

        CheckoutTime = DateTime.Now;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            Visitor = await _getVisitorDetailUseCase.ExecuteAsync(VisitorId);
            return Page();
        }

        try
        {
            var currentUser = User.Identity?.Name ?? "System";
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            await _checkoutVisitorUseCase.ExecuteAsync(VisitorId, CheckoutTime, currentUser, ipAddress);

            return RedirectToPage("/Visitors/Detail", new { id = VisitorId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "離場登記時發生錯誤");
            ErrorMessage = ex.Message;
            Visitor = await _getVisitorDetailUseCase.ExecuteAsync(VisitorId);
            return Page();
        }
    }
}
