using CineVerify.Data;
using CineVerify.Models;
using CineVerify.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CineVerify.Pages.Admin
{
    [Authorize(Policy = "AdminPolicy")]
    public class EditMovieModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly GeminiService _geminiService;

        public EditMovieModel(ApplicationDbContext context, GeminiService geminiService)
        {
            _context = context;
            _geminiService = geminiService;
        }

        [BindProperty]
        public Movie Movie { get; set; } = new Movie();

        [BindProperty]
        public string GenresString { get; set; } = string.Empty;

        public int ReviewsCount { get; set; }
        public int FavoritesCount { get; set; }
        public int ViewsCount { get; set; }

        [TempData]
        public string StatusMessage { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Movie = await _context.Movies.FindAsync(id);

            if (Movie == null)
            {
                return NotFound();
            }

            // Converti i generi in stringa
            if (Movie.Genres != null)
            {
                GenresString = string.Join(", ", Movie.Genres);
            }

            // Carica le statistiche del film
            ReviewsCount = await _context.MovieReviews.CountAsync(r => r.MovieId == id);
            FavoritesCount = await _context.UserFavorites.CountAsync(f => f.MovieId == id);
            ViewsCount = await _context.MovieWatchHistory.CountAsync(h => h.MovieId == id);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Carica il film esistente
            var existingMovie = await _context.Movies.FindAsync(Movie.Id);
            if (existingMovie == null)
            {
                return NotFound();
            }

            // Converti la stringa in array di generi
            existingMovie.Genres = GenresString.Split(',')
                .Select(g => g.Trim())
                .Where(g => !string.IsNullOrEmpty(g))
                .ToArray();

            // Aggiorna le proprietà modificabili
            existingMovie.Title = Movie.Title;
            existingMovie.OriginalTitle = Movie.OriginalTitle;
            existingMovie.Description = Movie.Description;
            existingMovie.ReleaseDate = Movie.ReleaseDate;
            existingMovie.Rating = Movie.Rating;
            existingMovie.VoteCount = Movie.VoteCount;
            existingMovie.PosterPath = Movie.PosterPath;
            existingMovie.BackdropPath = Movie.BackdropPath;
            existingMovie.TrailerUrl = Movie.TrailerUrl;
            existingMovie.IsVerified = Movie.IsVerified;
            existingMovie.GeminiAnalysis = Movie.GeminiAnalysis;

            await _context.SaveChangesAsync();

            StatusMessage = $"Film \"{Movie.Title}\" aggiornato con successo.";
            return RedirectToPage("./Movies");
        }
        public async Task<IActionResult> OnPostRegenerateAnalysisAsync()
        {
            try
            {
                // Ottieni il film dal database
                var movieId = int.Parse(Request.Query["id"]);
                var movieFromDb = await _context.Movies.FindAsync(movieId);

                if (movieFromDb == null)
                {
                    return NotFound("Film non trovato nel database");
                }

                // Genera una nuova analisi tramite Gemini
                string newAnalysis = await _geminiService.GenerateMovieAnalysisAsync(
                    movieFromDb.Title,
                    movieFromDb.Description ?? "Non disponibile",
                    movieFromDb.Genres ?? new[] { "Non specificato" },
                    movieFromDb.ReleaseDate?.Year.ToString() ?? "Non specificato"
                );

                // Aggiorna il campo GeminiAnalysis nel database
                movieFromDb.GeminiAnalysis = newAnalysis;
                await _context.SaveChangesAsync();

                // Restituisci sempre JSON per chiamate AJAX
                return new JsonResult(new { success = true, analysis = newAnalysis });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }

        // Metodo aggiuntivo per tornare indietro alla pagina di dettaglio
        public IActionResult OnPostCancelAsync(int id)
        {
            return RedirectToPage("/Movies/Details", new { id });
        }
    }
}