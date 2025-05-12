using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace CineVerify.Models
{
    public class Movie
    {
        public int Id { get; set; }

        public int TmdbId { get; set; }

        public string ImdbId { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string OriginalTitle { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime? ReleaseDate { get; set; }

        public DateTime DateAdded { get; set; } = DateTime.UtcNow;

        public decimal Rating { get; set; }

        public int VoteCount { get; set; }

        public string PosterPath { get; set; } = string.Empty;

        public string BackdropPath { get; set; } = string.Empty;

        public string TrailerUrl { get; set; } = string.Empty;

        public bool IsVerified { get; set; }

        public string[] Genres { get; set; } = Array.Empty<string>();

        public string GeminiAnalysis { get; set; } = string.Empty;

        // Proprietà calcolata per l'URL completo del poster
        [NotMapped]
        public string FullPosterUrl => string.IsNullOrEmpty(PosterPath)
            ? string.Empty
            : PosterPath.StartsWith("http")
                ? PosterPath
                : $"https://image.tmdb.org/t/p/w500{PosterPath}";

        // Proprietà calcolata per l'URL completo dello sfondo
        [NotMapped]
        public string FullBackdropUrl => string.IsNullOrEmpty(BackdropPath)
            ? string.Empty
            : BackdropPath.StartsWith("http")
                ? BackdropPath
                : $"https://image.tmdb.org/t/p/original{BackdropPath}";

        // Proprietà di supporto per verificare se le immagini sono disponibili
        [NotMapped]
        public bool HasPoster => !string.IsNullOrEmpty(PosterPath);

        [NotMapped]
        public bool HasBackdrop => !string.IsNullOrEmpty(BackdropPath);

        [NotMapped]
        public bool HasTrailer => !string.IsNullOrEmpty(TrailerUrl);
    }
}