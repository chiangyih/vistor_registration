using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using VisitorReg.Application.DTOs;
using VisitorReg.Application.UseCases;

namespace VisitorReg.Web.Pages.Visitors;

public class CreateModel : PageModel
{
    private readonly CreateVisitorUseCase _createVisitorUseCase;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(
        CreateVisitorUseCase createVisitorUseCase,
        ILogger<CreateModel> logger)
    {
        _createVisitorUseCase = createVisitorUseCase;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? SuccessMessage { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "訪客姓名為必填")]
        [StringLength(80, ErrorMessage = "訪客姓名不可超過 80 字元")]
        [Display(Name = "訪客姓名")]
        public string Name { get; set; } = string.Empty;

        [StringLength(30, ErrorMessage = "證件號碼不可超過 30 字元")]
        [Display(Name = "證件號碼")]
        public string? IdNumber { get; set; }

        [StringLength(120, ErrorMessage = "公司/單位不可超過 120 字元")]
        [Display(Name = "公司/單位")]
        public string? Company { get; set; }

        [Required(ErrorMessage = "來訪目的為必填")]
        [StringLength(200, ErrorMessage = "來訪目的不可超過 200 字元")]
        [Display(Name = "來訪目的")]
        public string Purpose { get; set; } = string.Empty;

        [Required(ErrorMessage = "受訪者為必填")]
        [StringLength(80, ErrorMessage = "受訪者不可超過 80 字元")]
        [Display(Name = "受訪者")]
        public string HostName { get; set; } = string.Empty;

        [Required(ErrorMessage = "到訪時間為必填")]
        [Display(Name = "到訪時間")]
        public DateTime CheckInAt { get; set; } = DateTime.Now;

        [StringLength(30, ErrorMessage = "聯絡方式不可超過 30 字元")]
        [Display(Name = "聯絡方式")]
        public string? Phone { get; set; }

        [StringLength(400, ErrorMessage = "備註不可超過 400 字元")]
        [Display(Name = "備註")]
        public string? Note { get; set; }
    }

    public void OnGet()
    {
        // 預設到訪時間為現在
        Input.CheckInAt = DateTime.Now;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var dto = new CreateVisitorDto
            {
                Name = Input.Name,
                IdNumber = Input.IdNumber,
                Company = Input.Company,
                Purpose = Input.Purpose,
                HostName = Input.HostName,
                CheckInAt = Input.CheckInAt,
                Phone = Input.Phone,
                Note = Input.Note
            };

            var currentUser = User.Identity?.Name ?? "System";
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var result = await _createVisitorUseCase.ExecuteAsync(dto, currentUser, ipAddress);

            SuccessMessage = $"登記編號：{result.RegisterNo}";
            
            // 清空表單
            ModelState.Clear();
            Input = new InputModel { CheckInAt = DateTime.Now };

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "建立訪客登記時發生錯誤");
            ModelState.AddModelError(string.Empty, "登記失敗：" + ex.Message);
            return Page();
        }
    }
}
