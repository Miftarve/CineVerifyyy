using CineVerify.Data;
using CineVerify.Models;
using CineVerify.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CineVerify.Pages.Movies
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly MovieApiService _movieApiService;
        private readonly GeminiService _geminiService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(
            ApplicationDbContext context,
            MovieApiService movieApiService,
            GeminiService geminiService,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<DetailsModel> logger)
        {
            _context = context;
            _movieApiService = movieApiService;
            _geminiService = geminiService;
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        public Movie Movie { get; set; }
        public List<MovieReview> Reviews { get; set; } = new List<MovieReview>();
        public bool IsAuthenticated { get; set; }
        public bool IsFavorite { get; set; }
        public decimal UserRatingValue { get; set; }
        public string FormattedTrailerUrl { get; set; }

        private void FixImagePaths(Movie movie)
        {
            // Base URL per le immagini di TMDB
            const string imageBaseUrl = "https://image.tmdb.org/t/p/";

            // Correggi il percorso del poster
            if (!string.IsNullOrEmpty(movie.PosterPath) && !movie.PosterPath.StartsWith("http"))
            {
                movie.PosterPath = $"{imageBaseUrl}w500{movie.PosterPath}";
            }

            // Correggi il percorso dell'immagine di sfondo
            if (!string.IsNullOrEmpty(movie.BackdropPath) && !movie.BackdropPath.StartsWith("http"))
            {
                movie.BackdropPath = $"{imageBaseUrl}original{movie.BackdropPath}";
            }
        }

        private string FormatTrailerUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return string.Empty;

            try
            {
                // Estrai ID video YouTube da vari formati di URL
                string videoId = string.Empty;

                if (url.Contains("youtu.be/"))
                {
                    // Formato breve: https://youtu.be/VIDEO_ID
                    var parts = url.Split(new[] { "youtu.be/" }, StringSplitOptions.None);
                    if (parts.Length > 1)
                    {
                        videoId = parts[1].Split('?')[0];
                    }
                }
                else if (url.Contains("youtube.com/watch"))
                {
                    // Formato normale: https://www.youtube.com/watch?v=VIDEO_ID
                    if (url.Contains("v="))
                    {
                        var vParam = url.Split(new[] { "v=" }, StringSplitOptions.None)[1];
                        videoId = vParam.Split('&')[0];
                    }
                }
                else if (url.Contains("youtube.com/embed/"))
                {
                    // Formato di incorporamento: https://www.youtube.com/embed/VIDEO_ID
                    var parts = url.Split(new[] { "youtube.com/embed/" }, StringSplitOptions.None);
                    if (parts.Length > 1)
                    {
                        videoId = parts[1].Split('?')[0];
                    }
                }

                if (!string.IsNullOrEmpty(videoId))
                {
                    return $"https://www.youtube.com/embed/{videoId}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Errore nel formato dell'URL del trailer: {url}");
            }

            // Restituisci l'URL originale se non riesci a formattarlo
            return url;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            IsAuthenticated = User.Identity?.IsAuthenticated ?? false;

            Movie = await _context.Movies.FindAsync(id);

            if (Movie == null)
            {
                _logger.LogWarning($"Film con ID {id} non trovato");
                return Page();
            }

            // Controlla e aggiorna il trailer se necessario
            if (string.IsNullOrEmpty(Movie.TrailerUrl) && Movie.TmdbId > 0)
            {
                try
                {
                    _logger.LogInformation($"Recupero trailer per il film ID {id} (TMDB ID: {Movie.TmdbId})");
                    var trailerUrl = await _movieApiService.GetMovieTrailerAsync(Movie.TmdbId);

                    if (!string.IsNullOrEmpty(trailerUrl))
                    {
                        Movie.TrailerUrl = trailerUrl;
                        _context.Movies.Update(Movie);
                        await _context.SaveChangesAsync();
                        _logger.LogInformation($"Trailer aggiornato per il film ID {id}: {trailerUrl}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Errore nel recuperare il trailer per il film {Movie.Title} (ID: {Movie.Id})");
                }
            }

            // Formatta l'URL del trailer per l'incorporamento in iframe
            if (!string.IsNullOrEmpty(Movie.TrailerUrl))
            {
                FormattedTrailerUrl = FormatTrailerUrl(Movie.TrailerUrl);
                _logger.LogInformation($"URL trailer formattato: {FormattedTrailerUrl}");
            }

            // Carica le recensioni
            Reviews = await _context.MovieReviews
                .Where(r => r.MovieId == id)
                .OrderByDescending(r => r.DateCreated)
                .ToListAsync();

            if (IsAuthenticated)
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                // Verifica se il film è nei preferiti dell'utente
                IsFavorite = await _context.UserFavorites
                    .AnyAsync(f => f.MovieId == id && f.UserId == userId);

                // Ottieni il rating dell'utente dalle recensioni (non da UserRatings)
                var userReview = await _context.MovieReviews
                    .FirstOrDefaultAsync(r => r.MovieId == id && r.UserId == userId);

                UserRatingValue = userReview?.Rating ?? 0;
            }

            return Page();
        }

        // Tutti i metodi POST richiedono autenticazione
        public async Task<IActionResult> OnPostAddToFavoritesAsync(int movieId)
        {
            // Verifica autenticazione
            if (!User.Identity.IsAuthenticated)
            {
                return Challenge();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            // Resto del codice rimane invariato...
            var movie = await _context.Movies.FindAsync(movieId);
            if (movie == null)
            {
                TempData["ErrorMessage"] = "Il film richiesto non è stato trovato.";
                return RedirectToPage("/Index");
            }

            var newFavorite = new UserFavorite
            {
                UserId = currentUser.Id,
                MovieId = movieId,
                DateAdded = DateTime.UtcNow
            };

            _context.UserFavorites.Add(newFavorite);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Details", new { id = movieId });
        }

        public async Task<IActionResult> OnPostRemoveFromFavoritesAsync(int movieId)
        {
            // Verifica autenticazione
            if (!User.Identity.IsAuthenticated)
            {
                return Challenge();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            // Resto del codice rimane invariato...
            var movie = await _context.Movies.FindAsync(movieId);
            if (movie == null)
            {
                TempData["ErrorMessage"] = "Il film richiesto non è stato trovato.";
                return RedirectToPage("/Index");
            }

            var favorite = await _context.UserFavorites
                .FirstOrDefaultAsync(f => f.MovieId == movieId && f.UserId == currentUser.Id);

            if (favorite != null)
            {
                _context.UserFavorites.Remove(favorite);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Details", new { id = movieId });
        }

        public async Task<IActionResult> OnPostAddReviewAsync(int movieId, string title, string content, decimal rating)
        {
            // Verifica autenticazione
            if (!User.Identity.IsAuthenticated)
            {
                return Challenge();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            // Resto del codice rimane invariato...
            var existingReview = await _context.MovieReviews
                .FirstOrDefaultAsync(r => r.MovieId == movieId && r.UserId == currentUser.Id);

            if (existingReview != null)
            {
                existingReview.Title = title;
                existingReview.Content = content;
                existingReview.Rating = rating;
                existingReview.DateModified = DateTime.UtcNow;
            }
            else
            {
                var review = new MovieReview
                {
                    UserId = currentUser.Id,
                    UserName = currentUser.UserName,
                    MovieId = movieId,
                    Title = title,
                    Content = content,
                    Rating = rating,
                    DateCreated = DateTime.UtcNow,
                    IsApproved = true
                };

                _context.MovieReviews.Add(review);
            }

            await _context.SaveChangesAsync();

            // Analisi del sentiment con Gemini in background
            _ = Task.Run(async () =>
            {
                try
                {
                    var review = await _context.MovieReviews
                        .FirstOrDefaultAsync(r => r.MovieId == movieId && r.UserId == currentUser.Id);

                    if (review != null)
                    {
                        review.SentimentAnalysis = await _geminiService.AnalyzeSentimentAsync(content);
                        await _context.SaveChangesAsync();
                    }
                }
                catch
                {
                    // Ignora errori nell'analisi
                }
            });

            return RedirectToPage("./Details", new { id = movieId });
        }

        // Gli altri metodi POST rimangono protetti nello stesso modo...
        public async Task<IActionResult> OnPostEditReviewAsync(int reviewId, int movieId, string content, decimal rating)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Challenge();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            // Resto del codice rimane invariato...
            var review = await _context.MovieReviews
                .FirstOrDefaultAsync(r => r.Id == reviewId && r.UserId == currentUser.Id);

            if (review == null)
            {
                return NotFound();
            }

            review.Content = content;
            review.Rating = rating;
            review.DateModified = DateTime.UtcNow;
            review.SentimentAnalysis = null;

            await _context.SaveChangesAsync();

            _ = Task.Run(async () =>
            {
                try
                {
                    review.SentimentAnalysis = await _geminiService.AnalyzeSentimentAsync(content);
                    await _context.SaveChangesAsync();
                }
                catch
                {
                    // Ignora errori nell'analisi
                }
            });

            return RedirectToPage("./Details", new { id = movieId });
        }

        public async Task<IActionResult> OnPostDeleteReviewAsync(int reviewId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Challenge();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            // Resto del codice rimane invariato...
            var review = await _context.MovieReviews
                .FirstOrDefaultAsync(r => r.Id == reviewId && (r.UserId == currentUser.Id || User.IsInRole("Admin")));

            if (review == null)
            {
                return NotFound();
            }

            var movieId = review.MovieId;
            _context.MovieReviews.Remove(review);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Details", new { id = movieId });
        }

        public async Task<IActionResult> OnPostMarkAsWatchedAsync(int movieId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Challenge();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            // Resto del codice rimane invariato...
            var movie = await _context.Movies.FindAsync(movieId);
            if (movie == null)
            {
                return NotFound();
            }

            var watchRecord = new MovieWatchHistory
            {
                UserId = currentUser.Id,
                MovieId = movieId,
                ViewedAt = DateTime.UtcNow,
                Completed = true
            };

            _context.MovieWatchHistory.Add(watchRecord);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Details", new { id = movieId });
        }

        public async Task<IActionResult> OnPostRateAsync(int movieId, decimal rating)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Challenge();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            var movie = await _context.Movies.FindAsync(movieId);
            if (movie == null)
            {
                return NotFound();
            }

            // Nel sistema attuale, il rating è parte delle recensioni
            var existingReview = await _context.MovieReviews
                .FirstOrDefaultAsync(r => r.MovieId == movieId && r.UserId == currentUser.Id);

            if (existingReview != null)
            {
                // Aggiorna solo il rating nella recensione esistente
                existingReview.Rating = rating;
                existingReview.DateModified = DateTime.UtcNow;
                _context.MovieReviews.Update(existingReview);
            }
            else
            {
                // Crea una nuova recensione minima con solo il rating
                var review = new MovieReview
                {
                    UserId = currentUser.Id,
                    UserName = currentUser.UserName,
                    MovieId = movieId,
                    Title = "Valutazione",
                    Content = "Valutazione senza recensione",
                    Rating = rating,
                    DateCreated = DateTime.UtcNow,
                    IsApproved = true
                };
                _context.MovieReviews.Add(review);
            }

            await _context.SaveChangesAsync();

            return RedirectToPage("./Details", new { id = movieId });
        }

        public async Task<IActionResult> OnPostGenerateAnalysisAsync(int movieId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Challenge();
            }

            if (!User.IsInRole("Admin") && !User.IsInRole("Critic"))
            {
                return Forbid();
            }

            // Resto del codice rimane invariato...
            var movie = await _context.Movies.FindAsync(movieId);
            if (movie == null)
            {
                return NotFound();
            }

            try
            {
                movie.GeminiAnalysis = await _geminiService.GenerateMovieAnalysisAsync(
                    movie.Title,
                    movie.Description,
                    movie.Genres,
                    movie.ReleaseDate?.Year.ToString() ?? "N/A"
                );

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Errore nella generazione dell'analisi: {ex.Message}";
            }

            return RedirectToPage("./Details", new { id = movieId });
        }
    }
}