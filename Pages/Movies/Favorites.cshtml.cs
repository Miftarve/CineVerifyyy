using CineVerify.Data;
using CineVerify.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CineVerify.Pages.Movies
{
    [Authorize]
    public class FavoritesModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public FavoritesModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<UserFavorite> Favorites { get; set; } = new List<UserFavorite>();

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        public int TotalPages { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            const int pageSize = 20;

            // Ottieni il conteggio totale per la paginazione
            var totalFavorites = await _context.UserFavorites
                .Where(f => f.UserId == currentUser.Id)
                .CountAsync();

            TotalPages = (totalFavorites + pageSize - 1) / pageSize;

            // Assicurati che la pagina corrente sia valida
            if (CurrentPage < 1)
            {
                CurrentPage = 1;
            }
            else if (CurrentPage > TotalPages && TotalPages > 0)
            {
                CurrentPage = TotalPages;
            }

            // Carica i preferiti con paginazione
            Favorites = await _context.UserFavorites
                .Include(f => f.Movie)
                .Where(f => f.UserId == currentUser.Id)
                .OrderByDescending(f => f.DateAdded)
                .Skip((CurrentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostRemoveFavoriteAsync(int favoriteId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            // Trova il preferito
            var favorite = await _context.UserFavorites
                .FirstOrDefaultAsync(f => f.Id == favoriteId && f.UserId == currentUser.Id);

            if (favorite == null)
            {
                return NotFound();
            }

            // Rimuovi il preferito
            _context.UserFavorites.Remove(favorite);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}