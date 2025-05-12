using System;

namespace CineVerify.Models
{
    public class UserFavorite
    {
        public int Id { get; set; }

        public int MovieId { get; set; }
        public virtual Movie Movie { get; set; }

        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        public DateTime DateAdded { get; set; } = DateTime.UtcNow;
    }
}