using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace VisitorReg.Web.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(SignInManager<IdentityUser> signInManager, ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = default!;

        public IList<AuthenticationScheme> ExternalLogins { get; set; } = default!;

        public string? ReturnUrl { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "請輸入電子郵件")]
            [EmailAddress(ErrorMessage = "請輸入有效的電子郵件地址")]
            [Display(Name = "電子郵件")]
            public string Email { get; set; } = default!;

            [Required(ErrorMessage = "請輸入密碼")]
            [DataType(DataType.Password)]
            [Display(Name = "密碼")]
            public string Password { get; set; } = default!;

            [Display(Name = "記住我")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // 清除現有的外部 cookie，以確保乾淨的登入流程
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                // 這不會計入帳戶鎖定失敗次數
                // 若要在特定次數的密碼失敗後觸發帳戶鎖定，請設定 lockoutOnFailure: true
                var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);
                
                if (result.Succeeded)
                {
                    _logger.LogInformation("使用者已登入。");
                    return LocalRedirect(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("使用者帳戶已被鎖定。");
                    ModelState.AddModelError(string.Empty, "您的帳戶已被鎖定。請稍後再試。");
                    return Page();
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "登入嘗試無效。請檢查您的電子郵件和密碼。");
                    return Page();
                }
            }

            // 如果執行到這裡，表示發生錯誤，重新顯示表單
            return Page();
        }
    }
}
