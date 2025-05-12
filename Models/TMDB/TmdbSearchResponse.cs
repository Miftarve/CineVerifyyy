using System.Collections.Generic;

namespace CineVerify.Models.TMDB
{
    public class TmdbSearchResponse
    {
        public List<TmdbSearchResult> Results { get; set; } = new List<TmdbSearchResult>();
    }
}