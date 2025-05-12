using CineVerify.Data;
using CineVerify.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CineVerify.Pages
{
    [Authorize]
    public class MyReviewsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MyReviewsModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public class ReviewViewModel
        {
            public int Id { get; set; }
            public int MovieId { get; set; }
            public string MovieTitle { get; set; }
            public string Content { get; set; }
            public double Rating { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
            public string PosterPath { get; set; }
            public bool IsApproved { get; set; }
            public int ReleaseYear { get; set; }
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "Il contenuto della recensione è obbligatorio")]
            [StringLength(3000, MinimumLength = 10, ErrorMessage = "La recensione deve contenere tra {2} e {1} caratteri")]
            public string Content { get; set; }

            [Required(ErrorMessage = "Il voto è obbligatorio")]
            [Range(0.5, 5.0, ErrorMessage = "Il voto deve essere compreso tra {1} e {2}")]
            public double Rating { get; set; }
        }

        public List<ReviewViewModel> UserReviews { get; set; } = new List<ReviewViewModel>();

        [TempData]
        public string StatusMessage { get; set; }

        public int TotalReviews { get; set; }
        public double AverageRating { get; set; }

        public async Task<IActionResult> OnGetAsync(string sortOrder = "newest")
        {
            // Ottieni l'utente corrente
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Impossibile caricare l'utente con ID '{_userManager.GetUserId(User)}'.");
            }

            // Statistiche dell'utente
            TotalReviews = await _context.MovieReviews.CountAsync(r => r.UserId == user.Id);

            // SOLUZIONE: Calcola la media lato client per evitare problemi con SQLite
            if (TotalReviews > 0)
            {
                var ratings = await _context.MovieReviews
                    .Where(r => r.UserId == user.Id)
                    .Select(r => r.Rating)
                    .ToListAsync();

                AverageRating = (double)ratings.Average();
            }
            else
            {
                AverageRating = 0;
            }

            // Ottieni le recensioni dell'utente
            var query = _context.MovieReviews
                .Include(r => r.Movie)
                .Where(r => r.UserId == user.Id)
                .AsQueryable();

            // Ordina in base alla scelta dell'utente
            query = sortOrder switch
            {
                "oldest" => query.OrderBy(r => r.CreatedAt),
                "highest" => query.OrderByDescending(r => r.Rating),
                "lowest" => query.OrderBy(r => r.Rating),
                "movie" => query.OrderBy(r => r.Movie.Title),
                _ => query.OrderByDescending(r => r.CreatedAt)
            };

            var reviews = await query.ToListAsync();

            // Mappa i risultati al view model
            UserReviews = reviews.Select(r => new ReviewViewModel
            {
                Id = r.Id,
                MovieId = r.MovieId,
                MovieTitle = r.Movie?.Title ?? "Film sconosciuto",
                Content = r.Content,
                Rating = (double)r.Rating,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                PosterPath = r.Movie?.PosterPath ?? "",
                IsApproved = r.IsApproved
            }).ToList();

            return Page();
        }

        public async Task<IActionResult> OnGetEditAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var review = await _context.MovieReviews
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == user.Id);

            if (review == null)
            {
                return NotFound();
            }

            Input = new InputModel
            {
                Id = review.Id,
                Content = review.Content,
                Rating = (double)review.Rating
            };

            // Carica anche le recensioni dell'utente per mostrare la pagina completa
            await OnGetAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var review = await _context.MovieReviews
                .FirstOrDefaultAsync(r => r.Id == Input.Id && r.UserId == user.Id);

            if (review == null)
            {
                return NotFound();
            }

            // Aggiorna la recensione
            review.Content = Input.Content;
            review.Rating = (decimal)Input.Rating;
            review.UpdatedAt = DateTime.Now;
            review.IsApproved = false;  // La recensione deve essere riapprovata

            await _context.SaveChangesAsync();

            StatusMessage = "Recensione aggiornata con successo. Verrà esaminata per l'approvazione.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var review = await _context.MovieReviews
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == user.Id);

            if (review == null)
            {
                return NotFound();
            }

            _context.MovieReviews.Remove(review);
            await _context.SaveChangesAsync();

            StatusMessage = "Recensione eliminata con successo.";
            return RedirectToPage();
        }
    }
}