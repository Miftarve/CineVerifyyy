using CineVerify.Data;
using CineVerify.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CineVerify.Pages.Admin
{
    [Authorize(Policy = "AdminPolicy")]
    public class UsersModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UsersModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IList<UserWithRoles> UsersList { get; set; } = new List<UserWithRoles>();

        public class UserWithRoles
        {
            public ApplicationUser User { get; set; } = null!;
            public List<string> Roles { get; set; } = new List<string>();
        }

        [TempData]
        public string StatusMessage { get; set; } = string.Empty;

        public async Task OnGetAsync()
        {
            var users = await _context.Users.ToListAsync();
            UsersList = new List<UserWithRoles>();

            foreach (var user in users)
            {
                var userWithRoles = new UserWithRoles
                {
                    User = user,
                    Roles = (await _userManager.GetRolesAsync(user)).ToList()
                };

                UsersList.Add(userWithRoles);
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(string id)
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

            // Non permettere all'admin di cancellare se stesso
            if (user.Email == "admin@cineverify.com")
            {
                StatusMessage = "Errore: Non puoi eliminare l'amministratore principale.";
                return RedirectToPage();
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                StatusMessage = "Utente eliminato con successo.";
                return RedirectToPage();
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return RedirectToPage();
        }
    }
}