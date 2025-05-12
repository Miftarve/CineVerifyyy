using CineVerify.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace CineVerify.Pages.Admin
{
    [Authorize(Policy = "AdminPolicy")]
    public class EditModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public EditModel(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            // Inizializza RolesList nel costruttore per evitare l'errore
            RolesList = new SelectList(Enumerable.Empty<string>());
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        // Cambiato da non-nullable a nullable
        public SelectList RolesList { get; set; }

        [TempData]
        public string StatusMessage { get; set; } = string.Empty;

        public class InputModel
        {
            [Required(ErrorMessage = "L'ID utente è obbligatorio")]
            public string Id { get; set; } = string.Empty;

            [Required(ErrorMessage = "Il nome è obbligatorio")]
            [Display(Name = "Nome")]
            public string Nome { get; set; } = string.Empty;

            [Required(ErrorMessage = "Il cognome è obbligatorio")]
            [Display(Name = "Cognome")]
            public string Cognome { get; set; } = string.Empty;

            [Required(ErrorMessage = "L'email è obbligatoria")]
            [EmailAddress(ErrorMessage = "Formato email non valido")]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Display(Name = "Ruolo")]
            public string Role { get; set; } = string.Empty;

            [Display(Name = "Account confermato")]
            public bool EmailConfirmed { get; set; }

            [StringLength(100, ErrorMessage = "La {0} deve essere lunga almeno {2} e massimo {1} caratteri.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Nuova password (lasciare vuoto per mantenere quella attuale)")]
            public string? NewPassword { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Popola i ruoli disponibili
            var roles = _roleManager.Roles.ToList();
            RolesList = new SelectList(roles, "Name", "Name");

            // Ottieni il ruolo attuale dell'utente (prendiamo il primo se ne ha più di uno)
            var userRoles = await _userManager.GetRolesAsync(user);

            Input = new InputModel
            {
                Id = user.Id,
                Nome = user.FirstName,  // Modificato da Nome a FirstName
                Cognome = user.LastName, // Modificato da Cognome a LastName
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                Role = userRoles.FirstOrDefault() ?? ""
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // Ripopola i ruoli disponibili in caso di errore
                var roles = _roleManager.Roles.ToList();
                RolesList = new SelectList(roles, "Name", "Name");
                return Page();
            }

            var user = await _userManager.FindByIdAsync(Input.Id);
            if (user == null)
            {
                return NotFound($"Impossibile caricare l'utente con ID '{Input.Id}'.");
            }

            // Non permettere di cambiare email all'admin principale
            if (user.Email == "admin@cineverify.com" && Input.Email != "admin@cineverify.com")
            {
                ModelState.AddModelError(string.Empty, "Non puoi modificare l'email dell'amministratore principale.");
                var roles = _roleManager.Roles.ToList();
                RolesList = new SelectList(roles, "Name", "Name");
                return Page();
            }

            // Aggiorna i campi
            var emailChanged = user.Email != Input.Email;

            user.FirstName = Input.Nome;  // Modificato da Nome a FirstName
            user.LastName = Input.Cognome; // Modificato da Cognome a LastName
            user.Email = Input.Email;
            user.UserName = Input.Email; // Anche lo username è l'email
            user.EmailConfirmed = Input.EmailConfirmed;

            // Aggiorna l'utente
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                var roles = _roleManager.Roles.ToList();
                RolesList = new SelectList(roles, "Name", "Name");
                return Page();
            }

            // Gestisci la password se inserita
            if (!string.IsNullOrEmpty(Input.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await _userManager.ResetPasswordAsync(user, token, Input.NewPassword);
                if (!passwordResult.Succeeded)
                {
                    foreach (var error in passwordResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    var roles = _roleManager.Roles.ToList();
                    RolesList = new SelectList(roles, "Name", "Name");
                    return Page();
                }
            }

            // Gestisci i ruoli
            var currentRoles = await _userManager.GetRolesAsync(user);

            // Non permettere di cambiare ruolo all'admin principale
            if (user.Email == "admin@cineverify.com" && !currentRoles.Contains(Input.Role))
            {
                ModelState.AddModelError(string.Empty, "Non puoi modificare il ruolo dell'amministratore principale.");
                var roles = _roleManager.Roles.ToList();
                RolesList = new SelectList(roles, "Name", "Name");
                return Page();
            }

            // Prima rimuove tutti i ruoli attuali
            if (currentRoles.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            }

            // Poi aggiungi il nuovo ruolo selezionato
            if (!string.IsNullOrEmpty(Input.Role))
            {
                await _userManager.AddToRoleAsync(user, Input.Role);
            }

            StatusMessage = "Utente aggiornato con successo.";
            return RedirectToPage("./Users");
        }
    }
}
