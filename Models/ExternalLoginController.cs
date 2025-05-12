using CineVerify.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CineVerify.Controllers
{
    [Route("[controller]")]
    public class ExternalLoginController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ExternalLoginController> _logger;

        public ExternalLoginController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<ExternalLoginController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index(string provider, string returnUrl = null)
        {
            // Registra informazioni di debug
            _logger.LogInformation($"Richiesta login esterno per provider: {provider}, returnUrl: {returnUrl}");

            // Verifica input
            if (string.IsNullOrEmpty(provider))
            {
                return BadRequest("Provider non specificato");
            }

            // Normalizza returnUrl
            returnUrl = returnUrl ?? Url.Content("~/");

            // Proprietà per memorizzare il returnUrl
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider,
                Url.Action(nameof(Callback), new { returnUrl }));

            // Inizia la sfida di autenticazione
            return Challenge(properties, provider);
        }

        [HttpGet("Callback")]
        public async Task<IActionResult> Callback(string returnUrl = null, string remoteError = null)
        {
            // Normalizza returnUrl
            returnUrl = returnUrl ?? Url.Content("~/");

            // Gestisci gli errori
            if (remoteError != null)
            {
                _logger.LogError($"Errore dal provider esterno: {remoteError}");
                return RedirectToPage("/Login", new { ReturnUrl = returnUrl });
            }

            // Ottieni le informazioni dell'utente esterno
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                _logger.LogWarning("Impossibile caricare le informazioni di login esterno");
                return RedirectToPage("/Login", new { ReturnUrl = returnUrl });
            }

            // Tenta di accedere con l'account esterno
            var result = await _signInManager.ExternalLoginSignInAsync(
                info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

            if (result.Succeeded)
            {
                _logger.LogInformation("Utente autenticato con {LoginProvider}", info.LoginProvider);
                return LocalRedirect(returnUrl);
            }

            // Se l'utente non esiste ancora, crealo
            if (result.IsNotAllowed || result.RequiresTwoFactor)
            {
                return RedirectToPage("/Login");
            }

            // Se arriviamo qui, l'utente non esiste, quindi lo creiamo
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (email == null)
            {
                _logger.LogWarning("Impossibile estrarre l'email dai claims");
                return RedirectToPage("/Login");
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                Nome = info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? "Utente",
                Cognome = info.Principal.FindFirstValue(ClaimTypes.Surname) ?? "Google",
                DataRegistrazione = DateTime.UtcNow,
                EmailConfirmed = true,
                ProfilePictureUrl = "/images/default-avatar.png" // Usa l'avatar predefinito
            };

            // Crea l'utente
            var createResult = await _userManager.CreateAsync(user);
            if (createResult.Succeeded)
            {
                // Aggiungi il login esterno all'utente
                createResult = await _userManager.AddLoginAsync(user, info);
                if (createResult.Succeeded)
                {
                    _logger.LogInformation("Utente creato con provider {LoginProvider}", info.LoginProvider);

                    // Aggiungi al ruolo User
                    await _userManager.AddToRoleAsync(user, "User");

                    // Accedi con l'utente
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect(returnUrl);
                }
            }

            // Se siamo arrivati qui, qualcosa è andato storto
            _logger.LogError("Errore nella creazione dell'utente esterno: {Errors}",
                string.Join(", ", createResult.Errors.Select(e => e.Description)));

            return RedirectToPage("/Login");
        }
    }
}