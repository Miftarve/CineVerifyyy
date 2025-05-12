using System;

namespace CineVerify.Models
{
    public class MovieConsolidatedData
    {
        public string Title { get; set; }
        public string OriginalTitle { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public string[] Genres { get; set; }
        public string Description { get; set; }
        public string TmdbOverview { get; set; }
        public decimal TmdbPopularity { get; set; }
        public decimal TmdbRating { get; set; }
        public int TmdbId { get; set; }
        public string[] Directors { get; set; }
        public string[] Cast { get; set; }
        public int Runtime { get; set; }
    }
}