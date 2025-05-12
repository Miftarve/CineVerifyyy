using CineVerify.Data;
using CineVerify.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CineVerify.Pages.Admin
{
    [Authorize(Policy = "AdminPolicy")]
    public class MoviesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public MoviesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Movie> Movies { get; set; } = new List<Movie>();

        [BindProperty(SupportsGet = true)]
        public string SearchTitle { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string VerificationStatus { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string OrderBy { get; set; } = "date_desc";

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        public int TotalPages { get; set; }

        public int TotalMovies { get; set; }

        [TempData]
        public string StatusMessage { get; set; } = string.Empty;

        public async Task OnGetAsync()
        {
            const int pageSize = 20;

            // Costruisci la query di base
            var query = _context.Movies.AsQueryable();

            // Filtra per titolo
            if (!string.IsNullOrEmpty(SearchTitle))
            {
                query = query.Where(m => m.Title.Contains(SearchTitle) || m.OriginalTitle.Contains(SearchTitle));
            }

            // Filtra per stato di verifica
            if (VerificationStatus == "verified")
            {
                query = query.Where(m => m.IsVerified);
            }
            else if (VerificationStatus == "unverified")
            {
                query = query.Where(m => !m.IsVerified);
            }

            // Ordinamento
            query = OrderBy switch
            {
                "date_asc" => query.OrderBy(m => m.ReleaseDate),
                "title_asc" => query.OrderBy(m => m.Title),
                "title_desc" => query.OrderByDescending(m => m.Title),
                "rating_desc" => query.OrderByDescending(m => m.Rating),
                "rating_asc" => query.OrderBy(m => m.Rating),
                _ => query.OrderByDescending(m => m.ReleaseDate)
            };

            // Ottieni il conteggio totale per la paginazione
            TotalMovies = await query.CountAsync();
            TotalPages = (TotalMovies + pageSize - 1) / pageSize;

            // Assicurati che la pagina corrente sia valida
            if (CurrentPage < 1)
            {
                CurrentPage = 1;
            }
            else if (CurrentPage > TotalPages && TotalPages > 0)
            {
                CurrentPage = TotalPages;
            }

            // Esegui la query con paginazione
            Movies = await query
                .Skip((CurrentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostToggleVerificationAsync(int movieId)
        {
            var movie = await _context.Movies.FindAsync(movieId);
            if (movie == null)
            {
                return NotFound();
            }

            // Cambia lo stato di verifica
            movie.IsVerified = !movie.IsVerified;

            await _context.SaveChangesAsync();

            StatusMessage = movie.IsVerified
                ? $"Film \"{movie.Title}\" verificato con successo."
                : $"Film \"{movie.Title}\" contrassegnato come non verificato.";

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteMovieAsync(int movieId)
        {
            var movie = await _context.Movies.FindAsync(movieId);
            if (movie == null)
            {
                return NotFound();
            }

            // Ottieni il titolo prima di eliminare
            var title = movie.Title;

            // Elimina tutte le recensioni associate
            var reviews = await _context.MovieReviews.Where(r => r.MovieId == movieId).ToListAsync();
            _context.MovieReviews.RemoveRange(reviews);

            // Elimina tutti i rating associati
            var ratings = await _context.MovieUserRatings.Where(r => r.MovieId == movieId).ToListAsync();
            _context.MovieUserRatings.RemoveRange(ratings);

            // Elimina tutti i preferiti associati
            var favorites = await _context.UserFavorites.Where(f => f.MovieId == movieId).ToListAsync();
            _context.UserFavorites.RemoveRange(favorites);

            // Elimina la cronologia di visualizzazione
            var history = await _context.MovieWatchHistory.Where(h => h.MovieId == movieId).ToListAsync();
            _context.MovieWatchHistory.RemoveRange(history);

            // Elimina il film
            _context.Movies.Remove(movie);

            await _context.SaveChangesAsync();

            StatusMessage = $"Film \"{title}\" eliminato con successo.";

            return RedirectToPage();
        }
    }
}