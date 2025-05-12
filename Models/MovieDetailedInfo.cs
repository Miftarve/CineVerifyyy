namespace CineVerify.Models
{
    public class MovieDetailedInfo
    {
        public string Title { get; set; }
        public string DetailedSummary { get; set; }
        public string[] Directors { get; set; }
        public string[] Cast { get; set; }
        public int Runtime { get; set; }
        public decimal TmdbRating { get; set; }
        public string Source { get; set; }
    }
}