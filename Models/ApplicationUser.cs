using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace CineVerify.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Nome { get; set; } = string.Empty;

        public string Cognome { get; set; } = string.Empty;

        public DateTime DataRegistrazione { get; set; } = DateTime.UtcNow;

        // Con valore predefinito per evitare l'errore NOT NULL
        public string ProfilePictureUrl { get; set; } = "/images/default-avatar.png";
        public bool IsCritic { get; set; } = false;

        // Proprietà di compatibilità per codice inglese
        public string FirstName { get => Nome; set => Nome = value; }
        public string LastName { get => Cognome; set => Cognome = value; }
        public DateTime JoinDate { get => DataRegistrazione; set => DataRegistrazione = value; }

        // Relazioni
        public virtual ICollection<MovieReview> Reviews { get; set; }
        public virtual ICollection<MovieUserRating> Ratings { get; set; }
        public virtual ICollection<UserFavorite> Favorites { get; set; }
        public virtual ICollection<MovieWatchHistory> WatchHistory { get; set; }
    }
}