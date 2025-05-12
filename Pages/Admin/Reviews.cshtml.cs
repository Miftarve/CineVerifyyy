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
    [Authorize(Policy = "AdminPolicy")]
    public class ReviewsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReviewsModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public class ReviewViewModel
        {
            public int Id { get; set; }
            public int MovieId { get; set; }
            public string MovieTitle { get; set; }
            public string UserId { get; set; }
            public string UserName { get; set; }
            public string UserEmail { get; set; }
            public string Content { get; set; }
            public double Rating { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
            public string PosterPath { get; set; }
            public bool IsApproved { get; set; }

            public int ReleaseYear { get; set; }
        }

        public List<ReviewViewModel> Reviews { get; set; } = new List<ReviewViewModel>();

        [TempData]
        public string StatusMessage { get; set; }

        public int TotalReviews { get; set; }
        public int TotalMoviesReviewed { get; set; }
        public double AverageRating { get; set; }

        public async Task<IActionResult> OnGetAsync(string sortOrder = "newest", string searchString = "", int pageIndex = 1)
        {
            // Statistiche generali
            TotalReviews = await _context.MovieReviews.CountAsync();
            TotalMoviesReviewed = await _context.MovieReviews.Select(r => r.MovieId).Distinct().CountAsync();

            // Soluzione per SQLite: calcolo dell'average sul lato client per evitare problemi con tipi decimal
            if (TotalReviews > 0)
            {
                var ratings = await _context.MovieReviews.Select(r => r.Rating).ToListAsync();
                AverageRating = (double)ratings.Average();
            }
            else
            {
                AverageRating = 0;
            }

            // Ottieni tutte le recensioni con dati correlati
            var query = _context.MovieReviews
                .Include(r => r.Movie)
                .Include(r => r.User)
                .AsQueryable();

            // Filtra per termine di ricerca
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(r =>
                    r.Content.Contains(searchString) ||
                    r.Movie.Title.Contains(searchString) ||
                    r.User.UserName.Contains(searchString) ||
                    r.User.Email.Contains(searchString));
            }

            // Ordina in base alla scelta dell'utente
            query = sortOrder switch
            {
                "oldest" => query.OrderBy(r => r.CreatedAt),
                "highest" => query.OrderByDescending(r => r.Rating),
                "lowest" => query.OrderBy(r => r.Rating),
                "movie" => query.OrderBy(r => r.Movie.Title),
                "user" => query.OrderBy(r => r.User.UserName),
                _ => query.OrderByDescending(r => r.CreatedAt)
            };

            // Paginazione (10 recensioni per pagina)
            int pageSize = 10;
            var reviews = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Mappa i risultati al view model
            Reviews = reviews.Select(r => new ReviewViewModel
            {
                Id = r.Id,
                MovieId = r.MovieId,
                MovieTitle = r.Movie?.Title ?? "Film sconosciuto",
                UserId = r.UserId,
                UserName = r.User?.UserName ?? "Utente sconosciuto",
                UserEmail = r.User?.Email ?? "Email sconosciuta",
                Content = r.Content,
                Rating = (double)r.Rating,// Corretto l'errore di battitura RatinRg
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                PosterPath = r.Movie?.PosterPath ?? "",
                IsApproved = r.IsApproved
            }).ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var review = await _context.MovieReviews.FindAsync(id);

            if (review == null)
            {
                return NotFound();
            }

            _context.MovieReviews.Remove(review);
            await _context.SaveChangesAsync();

            StatusMessage = "Recensione eliminata con successo.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostApproveAsync(int id)
        {
            var review = await _context.MovieReviews.FindAsync(id);

            if (review == null)
            {
                return NotFound();
            }

            review.IsApproved = true;
            review.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            StatusMessage = "Recensione approvata con successo.";
            return RedirectToPage();
        }
    }
}