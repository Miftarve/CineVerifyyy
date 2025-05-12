using System;

namespace CineVerify.Models.TMDB
{
    public class TmdbMovieDetails
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Overview { get; set; }
        public decimal VoteAverage { get; set; }
        public decimal Popularity { get; set; }
        public int Runtime { get; set; }
        public string[] Directors { get; set; } = Array.Empty<string>();
        public string[] Cast { get; set; } = Array.Empty<string>();
    }
}