using CineVerify.Data;
using CineVerify.Models;
using CineVerify.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CineVerify.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ImportMoviesModel : PageModel
    {
        private readonly MovieApiService _movieApiService;
        private readonly ApplicationDbContext _context;

        public ImportMoviesModel(MovieApiService movieApiService, ApplicationDbContext context)
        {
            _movieApiService = movieApiService;
            _context = context;
        }

        [BindProperty]
        public string SearchQuery { get; set; } = string.Empty;

        [BindProperty]
        public int Count { get; set; } = 50;

        public List<Movie> SearchResults { get; set; } = new List<Movie>();
        public List<Movie> ImportedMovies { get; set; } = new List<Movie>();
        public List<Genre> Genres { get; set; } = new List<Genre>();
        public int TotalMovies { get; set; }
        public int TotalGenres { get; set; }
        public int TmdbMovies { get; set; }

        [TempData]
        public string SuccessMessage { get; set; } = string.Empty;

        [TempData]
        public string ErrorMessage { get; set; } = string.Empty;

        public async Task OnGetAsync()
        {
            await LoadStatistics();
        }

        private async Task LoadStatistics()
        {
            // Carica i generi da TMDB
            try
            {
                Genres = await _movieApiService.GetGenresAsync();
                TotalGenres = Genres.Count;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Errore nel caricamento dei generi: {ex.Message}";
                Genres = new List<Genre>();
                TotalGenres = 0;
            }

            // Statistiche del database
            try
            {
                TotalMovies = await _context.Movies.CountAsync();
                TmdbMovies = await _context.Movies.CountAsync(m => m.TmdbId > 0);

                // Se non ci sono generi da TMDB, carica i generi dal database
                if (Genres.Count == 0)
                {
                    var dbGenres = await _context.Movies
                        .Where(m => m.Genres != null && m.Genres.Length > 0)
                        .SelectMany(m => m.Genres)
                        .Distinct()
                        .ToListAsync();

                    TotalGenres = dbGenres.Count;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Errore nel caricamento delle statistiche: {ex.Message}";
            }
        }

        public async Task<IActionResult> OnPostSearchAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery))
            {
                ErrorMessage = "Inserisci un termine di ricerca";
                return RedirectToPage();
            }

            try
            {
                SearchResults = await _movieApiService.SearchMoviesAsync(SearchQuery);
                await LoadStatistics();
                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Errore durante la ricerca: {ex.Message}";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostImportPopularAsync(int pages = 3)
        {
            if (pages <= 0 || pages > 20)
            {
                ErrorMessage = "Il numero di pagine deve essere tra 1 e 20";
                return RedirectToPage();
            }

            try
            {
                var importedMovies = await _movieApiService.ImportMultiplePagesOfMoviesAsync(pages);
                ImportedMovies = importedMovies;
                SuccessMessage = $"Importati con successo {importedMovies.Count} film popolari";
                await LoadStatistics();
                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Errore durante l'importazione: {ex.Message}";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostImportByGenreAsync(int genreId, int pages = 3)
        {
            if (genreId <= 0)
            {
                ErrorMessage = "Seleziona un genere valido";
                return RedirectToPage();
            }

            if (pages <= 0 || pages > 20)
            {
                ErrorMessage = "Il numero di pagine deve essere tra 1 e 20";
                return RedirectToPage();
            }

            try
            {
                var importedMovies = await _movieApiService.ImportMoviesByGenreAsync(genreId, pages);
                ImportedMovies = importedMovies;

                // Trova il nome del genere
                Genres = await _movieApiService.GetGenresAsync();
                var genreName = Genres.FirstOrDefault(g => g.Id == genreId)?.Name ?? "sconosciuto";

                SuccessMessage = $"Importati con successo {importedMovies.Count} film del genere {genreName}";
                await LoadStatistics();
                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Errore durante l'importazione per genere: {ex.Message}";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostImportMovieAsync(int tmdbId)
        {
            if (tmdbId <= 0)
            {
                ErrorMessage = "ID film non valido";
                return RedirectToPage();
            }

            try
            {
                var success = await _movieApiService.ImportMovieToDbAsync(tmdbId);
                if (success)
                {
                    // Trova il film appena importato per mostrarlo
                    var movie = await _context.Movies.FirstOrDefaultAsync(m => m.TmdbId == tmdbId);
                    if (movie != null)
                    {
                        ImportedMovies = new List<Movie> { movie };
                    }

                    SuccessMessage = "Film importato con successo";
                }
                else
                {
                    ErrorMessage = "Errore durante l'importazione del film";
                }

                await LoadStatistics();
                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Errore durante l'importazione del film: {ex.Message}";
                return RedirectToPage();
            }
        }
    }
}