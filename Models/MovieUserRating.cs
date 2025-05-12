using System;

namespace CineVerify.Models
{
    public class MovieUserRating
    {
        public int Id { get; set; }

        public int MovieId { get; set; }
        public virtual Movie Movie { get; set; }

        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        public decimal Rating { get; set; }

        public DateTime DateRated { get; set; } = DateTime.UtcNow;
    }
}