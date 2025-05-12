using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using CineVerify.Models;

namespace CineVerify.Pages
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string ReturnUrl { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "L'indirizzo email è obbligatorio")]
            [EmailAddress(ErrorMessage = "L'indirizzo email non è valido")]
            public string Email { get; set; }

            [Required(ErrorMessage = "La password è obbligatoria")]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Ricordami")]
            public bool RememberMe { get; set; }
        }

        public IActionResult OnGet(string returnUrl = null)
        {
            // Log per debug per identificare il problema
            _logger.LogInformation($"[DEBUG] Login page requested with returnUrl: {returnUrl}");

            // Se l'utente è già autenticato, reindirizzalo alla home o al returnUrl
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return LocalRedirect(returnUrl ?? Url.Content("~/"));
            }

            // Assicuriamoci che il returnUrl sia impostato correttamente
            ReturnUrl = returnUrl ?? Url.Content("~/");
            _logger.LogInformation($"[DEBUG] ReturnUrl impostato a: {ReturnUrl}");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Log per debug
            _logger.LogInformation($"[DEBUG] OnPost eseguito con ReturnUrl: {ReturnUrl}");

            // Se il ReturnUrl è vuoto, impostiamo la home come predefinita
            ReturnUrl = string.IsNullOrEmpty(ReturnUrl) ? Url.Content("~/") : ReturnUrl;

            _logger.LogInformation($"[DEBUG] ReturnUrl finale: {ReturnUrl}");

            if (ModelState.IsValid)
            {
                _logger.LogInformation($"[DEBUG] Model valido, tentativo login per {Input.Email}");

                // Tentativo di login
                var result = await _signInManager.PasswordSignInAsync(
                    Input.Email,
                    Input.Password,
                    Input.RememberMe,
                    lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    _logger.LogInformation($"[DEBUG] Login riuscito, reindirizzo a: {ReturnUrl}");
                    return LocalRedirect(ReturnUrl); // Usa LocalRedirect per gestire correttamente gli URL
                }
                if (result.RequiresTwoFactor)
                {
                    _logger.LogInformation("[DEBUG] Richiesta autenticazione a due fattori");
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning($"[DEBUG] Account bloccato per {Input.Email}");
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    _logger.LogWarning($"[DEBUG] Tentativo di login fallito per {Input.Email}");
                    ModelState.AddModelError(string.Empty, "Username o password non validi.");
                    return Page();
                }
            }

            // Se arriviamo qui, qualcosa è fallito, visualizziamo nuovamente il form
            _logger.LogInformation("[DEBUG] Model non valido, ritorno alla pagina di login");
            return Page();
        }
    }
}