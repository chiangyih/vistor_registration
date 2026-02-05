using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace VisitorReg.Web.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<RegisterModel> _logger;

        public RegisterModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ILogger<RegisterModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = default!;

        public string? ReturnUrl { get; set; }

        public IList<AuthenticationScheme>? ExternalLogins { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "請輸入電子郵件")]
            [EmailAddress(ErrorMessage = "請輸入有效的電子郵件地址")]
            [Display(Name = "電子郵件")]
            public string Email { get; set; } = default!;

            [Required(ErrorMessage = "請輸入密碼")]
            [StringLength(100, ErrorMessage = "{0} 必須至少 {2} 個字元，最多 {1} 個字元。", MinimumLength = 8)]
            [DataType(DataType.Password)]
            [Display(Name = "密碼")]
            public string Password { get; set; } = default!;

            [DataType(DataType.Password)]
            [Display(Name = "確認密碼")]
            [Compare("Password", ErrorMessage = "密碼和確認密碼不相符。")]
            public string ConfirmPassword { get; set; } = default!;
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                var user = new IdentityUser { UserName = Input.Email, Email = Input.Email };
                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("使用者建立了新帳號並設定了密碼。");

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect(returnUrl);
                }

                foreach (var error in result.Errors)
                {
                    // 將錯誤訊息翻譯為中文
                    var errorMessage = error.Code switch
                    {
                        "DuplicateUserName" => "此電子郵件已被使用。",
                        "DuplicateEmail" => "此電子郵件已被使用。",
                        "InvalidEmail" => "電子郵件格式不正確。",
                        "PasswordTooShort" => "密碼太短。",
                        "PasswordRequiresNonAlphanumeric" => "密碼必須包含至少一個特殊字元。",
                        "PasswordRequiresDigit" => "密碼必須包含至少一個數字 ('0'-'9')。",
                        "PasswordRequiresLower" => "密碼必須包含至少一個小寫字母 ('a'-'z')。",
                        "PasswordRequiresUpper" => "密碼必須包含至少一個大寫字母 ('A'-'Z')。",
                        _ => error.Description
                    };
                    ModelState.AddModelError(string.Empty, errorMessage);
                }
            }

            // 如果執行到這裡，表示發生錯誤，重新顯示表單
            return Page();
        }
    }
}
