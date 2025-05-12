using CineVerify.Data;
using CineVerify.Models;
using CineVerify.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CineVerify.Pages.Movies
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public PaginatedList<Movie> Movies { get; set; }

        [BindProperty(SupportsGet = true)]
        public string CurrentSort { get; set; }

        [BindProperty(SupportsGet = true)]
        public string TitleSort { get; set; }

        [BindProperty(SupportsGet = true)]
        public string DateSort { get; set; }

        [BindProperty(SupportsGet = true)]
        public string RatingSort { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SearchQuery { get; set; }

        [BindProperty(SupportsGet = true)]
        public string GenreFilter { get; set; }

        public List<string> AvailableGenres { get; set; } = new List<string>();

        public bool IsAuthenticated { get; set; }

        public async Task OnGetAsync(string sortOrder, string searchString, string genreFilter, int? pageIndex)
        {
            IsAuthenticated = User.Identity?.IsAuthenticated ?? false;

            CurrentSort = sortOrder;
            TitleSort = string.IsNullOrEmpty(sortOrder) ? "title_desc" : "";
            DateSort = sortOrder == "date" ? "date_desc" : "date";
            RatingSort = sortOrder == "rating" ? "rating_desc" : "rating";

            // Se abbiamo una nuova stringa di ricerca o filtro, resetta a pagina 1
            if (searchString != null)
            {
                pageIndex = 1;
                SearchQuery = searchString;
            }
            else if (genreFilter != null)
            {
                pageIndex = 1;
                GenreFilter = genreFilter;
            }

            // Carica tutti i generi disponibili - CORREZIONE
            // Prima carica tutti i film in memoria
            var allMovies = await _context.Movies
                .AsNoTracking()
                .ToListAsync();

            // Poi estrai i generi dai film in memoria
            AvailableGenres = allMovies
                .Where(m => m.Genres != null && m.Genres.Length > 0)
                .SelectMany(m => m.Genres)
                .Distinct()
                .OrderBy(g => g)
                .ToList();

            // Preparazione della query per i film
            IQueryable<Movie> moviesQuery = _context.Movies;

            // Filtraggio per ricerca
            if (!string.IsNullOrEmpty(SearchQuery))
            {
                moviesQuery = moviesQuery.Where(m =>
                    m.Title.Contains(SearchQuery) ||
                    (m.OriginalTitle != null && m.OriginalTitle.Contains(SearchQuery)) ||
                    (m.Description != null && m.Description.Contains(SearchQuery)));
            }

            // Filtraggio per genere
            if (!string.IsNullOrEmpty(GenreFilter))
            {
                // Carica i film con questo genere in memoria e filtra
                var moviesWithGenre = await moviesQuery.AsNoTracking().ToListAsync();
                var filteredIds = moviesWithGenre
                    .Where(m => m.Genres != null && m.Genres.Contains(GenreFilter))
                    .Select(m => m.Id)
                    .ToList();

                // Usa l'elenco di ID per filtrare la query originale
                moviesQuery = moviesQuery.Where(m => filteredIds.Contains(m.Id));
            }

            // Ordinamento
            moviesQuery = sortOrder switch
            {
                "title_desc" => moviesQuery.OrderByDescending(m => m.Title),
                "date" => moviesQuery.OrderBy(m => m.ReleaseDate),
                "date_desc" => moviesQuery.OrderByDescending(m => m.ReleaseDate),
                "rating" => moviesQuery.OrderBy(m => m.Rating),
                "rating_desc" => moviesQuery.OrderByDescending(m => m.Rating),
                _ => moviesQuery.OrderBy(m => m.Title),
            };

            int pageSize = 12;
            Movies = await PaginatedList<Movie>.CreateAsync(
                moviesQuery.AsNoTracking(), pageIndex ?? 1, pageSize);

            // Fix per le immagini
            foreach (var movie in Movies)
            {
                if (!string.IsNullOrEmpty(movie.PosterPath) && !movie.PosterPath.StartsWith("http"))
                {
                    movie.PosterPath = $"https://image.tmdb.org/t/p/w500{movie.PosterPath}";
                }
            }
        }
    }

    // Se la classe PaginatedList non è già definita, definiscila qui
    public class PaginatedList<T> : List<T>
    {
        public int PageIndex { get; private set; }
        public int TotalPages { get; private set; }

        public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
        {
            PageIndex = pageIndex;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);
            this.AddRange(items);
        }

        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;

        public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageIndex, int pageSize)
        {
            var count = await source.CountAsync();
            var items = await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PaginatedList<T>(items, count, pageIndex, pageSize);
        }
    }
}