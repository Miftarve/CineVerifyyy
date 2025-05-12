using CineVerify.Data;
using CineVerify.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CineVerify.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public int TotalMovies { get; set; }
        public int TotalUsers { get; set; }
        public int TotalReviews { get; set; }
        public int TotalGenres { get; set; }

        public DashboardModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // Carica le statistiche di base
            TotalMovies = await _context.Movies.CountAsync();
            TotalUsers = await _userManager.Users.CountAsync();
            TotalReviews = await _context.MovieReviews.CountAsync();

            // CORREZIONE: Prima carica i film nel client, poi elabora i generi
            var allMovies = await _context.Movies
                .AsNoTracking()  // Per migliorare le prestazioni
                .ToListAsync();

            // Elabora la lista dei generi nel client
            var uniqueGenres = allMovies
                .Where(m => m.Genres != null && m.Genres.Length > 0)
                .SelectMany(m => m.Genres)
                .Distinct()
                .ToList();

            TotalGenres = uniqueGenres.Count;

            return Page();
        }

        public IActionResult OnPostClearCache()
        {
            // In un'applicazione reale, qui dovresti implementare la logica per svuotare la cache
            // Questa è solo una simulazione
            TempData["SuccessMessage"] = "Cache svuotata con successo!";
            return RedirectToPage();
        }
    }
}