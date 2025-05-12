using System;

namespace CineVerify.Models
{
    public class MovieDetails
    {
        public int Id { get; set; }
        public int TmdbId { get; set; }
        public string Title { get; set; }
        public string OriginalTitle { get; set; }
        public string Description { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public decimal Rating { get; set; }
        public int VoteCount { get; set; }
        public string PosterPath { get; set; }
        public string BackdropPath { get; set; }
        public string[] Genres { get; set; }
        public int Runtime { get; set; }
        public string Director { get; set; }
        public string[] Cast { get; set; }
        public string TrailerKey { get; set; }
        public bool IsVerified { get; set; }
    }
}