using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CineVerify.Models
{
    public class MovieReview
    {
        public int Id { get; set; }

        // Modifica: Rendi UserId nullable
        public string? UserId { get; set; }

        [Required]
        public string UserName { get; set; } = string.Empty;

        public int MovieId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        [Column(TypeName = "decimal(3,1)")]
        public decimal Rating { get; set; }

        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        public DateTime? DateModified { get; set; }

        public string? SentimentAnalysis { get; set; }

        public bool IsApproved { get; set; } = true;

        // Relazione con ApplicationUser
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        // Relazione con Movie
        [ForeignKey("MovieId")]
        public virtual Movie Movie { get; set; }


        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public double RatinRg { get; internal set; }
    }
}