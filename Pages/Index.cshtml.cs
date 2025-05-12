using CineVerify.Data;
using CineVerify.Models;
using CineVerify.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CineVerify.Pages
{
   // [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly GeminiService _geminiService;
        private readonly MovieApiService _movieApiService;

        public IndexModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            GeminiService geminiService,
            MovieApiService movieApiService)
        {
            _context = context;
            _userManager = userManager;
            _geminiService = geminiService;
            _movieApiService = movieApiService;
        }

        public List<Movie> LatestMovies { get; set; } = new List<Movie>();
        public List<Movie> TopRatedMovies { get; set; } = new List<Movie>();
        public List<Movie> RecommendedMovies { get; set; } = new List<Movie>();

        // Dizionario per tenere traccia dei film che non sono ancora nel database
        public Dictionary<int, int> TmdbMovieIds { get; set; } = new Dictionary<int, int>();

        public string RecommendationText { get; set; } = string.Empty;
        public bool HasRecommendations { get; set; } = false;
        public string ErrorMessage { get; set; } = string.Empty;

        public async Task OnGetAsync()
        {
            try
            {
                // Ottieni i film popolari da TMDB API prima di tutto
                var tmdbMovies = new List<Movie>();
                try
                {
                    tmdbMovies = await _movieApiService.GetPopularMoviesAsync(1);

                    // Importa automaticamente alcuni film TMDB se il database è quasi vuoto
                    var dbMovieCount = await _context.Movies.CountAsync();
                    if (dbMovieCount < 10)
                    {
                        await _movieApiService.ImportPopularMoviesToDbAsync(10);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Errore durante il recupero dei film TMDB: {ex.Message}");
                    ErrorMessage = "Impossibile recuperare alcuni film da TMDB API";
                }

                // Ottieni i film dal database
                var dbMovies = await _context.Movies.ToListAsync();

                // Combina i film TMDB con quelli del database per i film più recenti
                // Prima salva i TMDB ID dei film che non sono ancora nel database
                foreach (var tmdbMovie in tmdbMovies)
                {
                    if (!dbMovies.Any(m => m.TmdbId == tmdbMovie.TmdbId))
                    {
                        TmdbMovieIds[tmdbMovie.TmdbId] = tmdbMovie.TmdbId;
                    }
                }

                // Film più recenti - combina database e TMDB
                var allRecentMovies = new List<Movie>();
                allRecentMovies.AddRange(dbMovies);

                // Aggiungi solo film TMDB che non sono già nel database
                foreach (var tmdbMovie in tmdbMovies)
                {
                    if (!dbMovies.Any(m => m.TmdbId == tmdbMovie.TmdbId))
                    {
                        allRecentMovies.Add(tmdbMovie);
                    }
                }

                LatestMovies = allRecentMovies
                    .OrderByDescending(m => m.ReleaseDate)
                    .Take(8)
                    .ToList();

                // Film con valutazione più alta
                TopRatedMovies = allRecentMovies
                    .Where(m => m.VoteCount > 5)
                    .OrderByDescending(m => m.Rating)
                    .Take(4)
                    .ToList();

                // Per le raccomandazioni personalizzate
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null)
                {
                    // Ottieni film che l'utente potrebbe apprezzare basandosi sui suoi preferiti e valutazioni
                    var favoriteMovies = await _context.UserFavorites
                        .Where(f => f.UserId == currentUser.Id)
                        .Include(f => f.Movie)
                        .Select(f => f.Movie)
                        .ToListAsync();

                    var userRatings = await _context.MovieUserRatings
                        .Where(r => r.UserId == currentUser.Id && r.Rating >= 7) // Solo rating positivi
                        .Include(r => r.Movie)
                        .Select(r => r.Movie)
                        .ToListAsync();

                    // Raccogli i generi preferiti dell'utente
                    var favoriteGenres = new HashSet<string>();

                    foreach (var movie in favoriteMovies.Concat(userRatings))
                    {
                        if (movie.Genres != null && movie.Genres.Length > 0)
                        {
                            foreach (var genre in movie.Genres)
                            {
                                favoriteGenres.Add(genre);
                            }
                        }
                    }

                    // Se abbiamo alcuni generi preferiti, cerca film simili
                    if (favoriteGenres.Any())
                    {
                        // Film che l'utente non ha ancora valutato o aggiunto ai preferiti
                        var ratedMovieIds = userRatings.Select(m => m.Id).ToHashSet();
                        var favoriteMovieIds = favoriteMovies.Select(m => m.Id).ToHashSet();
                        var excludedMovieIds = ratedMovieIds.Union(favoriteMovieIds).ToHashSet();

                        // Trova film con generi simili (inclusi quelli TMDB)
                        RecommendedMovies = new List<Movie>();

                        foreach (var movie in allRecentMovies)
                        {
                            // Per i film del database, controllo ID
                            if (movie.Id > 0 && excludedMovieIds.Contains(movie.Id))
                                continue;

                            if (movie.Genres != null && movie.Genres.Length > 0)
                            {
                                if (movie.Genres.Any(g => favoriteGenres.Contains(g)))
                                {
                                    RecommendedMovies.Add(movie);
                                    if (RecommendedMovies.Count >= 4)
                                        break;
                                }
                            }
                        }
                    }
                }

                // Se non abbiamo trovato raccomandazioni, mostra film casuali
                if (RecommendedMovies.Count == 0)
                {
                    var random = new Random();
                    RecommendedMovies = allRecentMovies
                        .OrderBy(x => random.Next())
                        .Take(4)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore generale nella pagina Index: {ex.Message}");
                ErrorMessage = "Si è verificato un errore nel caricamento dei film";
            }
        }

        public async Task<IActionResult> OnPostImportAndViewAsync(int tmdbId)
        {
            try
            {
                // Importa il film nel database
                await _movieApiService.ImportMovieToDbAsync(tmdbId);

                // Trova il film appena importato
                var movie = await _context.Movies.FirstOrDefaultAsync(m => m.TmdbId == tmdbId);

                if (movie != null)
                {
                    // Reindirizza alla pagina dettagli
                    return RedirectToPage("/Movies/Details", new { id = movie.Id });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore durante l'importazione e visualizzazione del film: {ex.Message}");
            }

            // In caso di errore, torna alla home page
            return RedirectToPage("/Index");


        }
    }
}