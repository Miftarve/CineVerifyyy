using CineVerify.Data;
using CineVerify.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Linq;
using System.Text.Json;

namespace CineVerify.Services
{
    public class MovieApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly string _baseUrl = "https://api.themoviedb.org/3";
        private readonly string _imageBaseUrl = "https://image.tmdb.org/t/p/";

        public MovieApiService(HttpClient httpClient, IConfiguration configuration, ApplicationDbContext context)
        {
            _httpClient = httpClient;
            _context = context;
            _configuration = configuration;

            // Verifica entrambi i percorsi di configurazione possibili
            _apiKey = configuration["TMDB:ApiKey"] ??
                      configuration["MovieApi:TmdbApiKey"] ??
                      throw new InvalidOperationException("API key for TMDB not found in configuration");

            Console.WriteLine($"TMDB API Key configurata correttamente: {!string.IsNullOrEmpty(_apiKey)}");
        }

        public async Task<List<Movie>> SearchMoviesAsync(string query, int page = 1)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<TmdbSearchResponse>(
                    $"{_baseUrl}/search/movie?api_key={_apiKey}&query={Uri.EscapeDataString(query)}&page={page}&language=it-IT");

                if (response?.Results == null)
                    return new List<Movie>();

                return response.Results.Select(m => ConvertToMovie(m)).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore durante la ricerca dei film: {ex.Message}");
                return new List<Movie>();
            }
        }

        public async Task<List<Movie>> GetPopularMoviesAsync(int page = 1)
        {
            try
            {
                Console.WriteLine($"Chiamata API: {_baseUrl}/movie/popular?api_key=***&page={page}&language=it-IT");
                var response = await _httpClient.GetFromJsonAsync<TmdbMovieResponse>(
                    $"{_baseUrl}/movie/popular?api_key={_apiKey}&page={page}&language=it-IT");

                if (response?.Results == null)
                {
                    Console.WriteLine("Risposta API: Nessun risultato trovato");
                    return new List<Movie>();
                }

                Console.WriteLine($"Risposta API: {response.Results.Count} film trovati");
                return response.Results.Select(m => ConvertToMovie(m)).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore durante il recupero dei film popolari: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                return new List<Movie>();
            }
        }

        // Metodo per ottenere i trailer dei film - CORRETTO (rimosso duplicato)
        public async Task<string> GetMovieTrailerAsync(int tmdbId)
        {
            try
            {
                Console.WriteLine($"Recupero trailer per il film TMDB ID: {tmdbId}");

                // Chiamata all'API per ottenere i video del film
                var response = await _httpClient.GetAsync($"{_baseUrl}/movie/{tmdbId}/videos?api_key={_apiKey}");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Errore nella chiamata API per i video del film {tmdbId}: {response.StatusCode}");
                    return string.Empty;
                }

                var content = await response.Content.ReadAsStringAsync();
                var videosData = JsonSerializer.Deserialize<TmdbVideoResponse>(content);

                if (videosData?.Results == null || !videosData.Results.Any())
                {
                    Console.WriteLine($"Nessun video trovato per il film {tmdbId}");
                    return string.Empty;
                }

                // Cerca prima il trailer ufficiale in italiano
                var trailer = videosData.Results.FirstOrDefault(v =>
                    v.Site?.ToLower() == "youtube" &&
                    v.Type?.ToLower() == "trailer" &&
                    v.Name?.ToLower().Contains("italiano") == true);

                // Se non trova quello italiano, cerca qualsiasi trailer ufficiale
                if (trailer == null)
                {
                    trailer = videosData.Results.FirstOrDefault(v =>
                        v.Site?.ToLower() == "youtube" &&
                        v.Type?.ToLower() == "trailer");
                }

                // Se ancora non trova, accetta qualsiasi video etichettato come trailer nel nome
                if (trailer == null)
                {
                    trailer = videosData.Results.FirstOrDefault(v =>
                        v.Site?.ToLower() == "youtube" &&
                        v.Name?.ToLower().Contains("trailer") == true);
                }

                // Se ancora non trova, accetta qualsiasi teaser
                if (trailer == null)
                {
                    trailer = videosData.Results.FirstOrDefault(v =>
                        v.Site?.ToLower() == "youtube" &&
                        v.Type?.ToLower() == "teaser");
                }

                // Se ancora non trova, prendi il primo video disponibile su YouTube
                if (trailer == null)
                {
                    trailer = videosData.Results.FirstOrDefault(v => v.Site?.ToLower() == "youtube");
                }

                if (trailer != null && !string.IsNullOrEmpty(trailer.Key))
                {
                    Console.WriteLine($"Trailer trovato per il film {tmdbId}: {trailer.Name}");
                    return $"https://www.youtube.com/watch?v={trailer.Key}";
                }

                Console.WriteLine($"Nessun trailer YouTube trovato per il film {tmdbId}");
                return string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore durante il recupero del trailer: {ex.Message}");
                return string.Empty;
            }
        }

        public async Task<List<Movie>> GetTopRatedMoviesAsync(int page = 1)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<TmdbMovieResponse>(
                    $"{_baseUrl}/movie/top_rated?api_key={_apiKey}&page={page}&language=it-IT");

                if (response?.Results == null)
                    return new List<Movie>();

                return response.Results.Select(m => ConvertToMovie(m)).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore durante il recupero dei film top rated: {ex.Message}");
                return new List<Movie>();
            }
        }

        // NUOVO METODO: Ottieni film per genere
        public async Task<List<Movie>> GetMoviesByGenreAsync(int genreId, int page = 1)
        {
            try
            {
                Console.WriteLine($"Chiamata API: {_baseUrl}/discover/movie?api_key=***&with_genres={genreId}&page={page}&language=it-IT");
                var response = await _httpClient.GetFromJsonAsync<TmdbMovieResponse>(
                    $"{_baseUrl}/discover/movie?api_key={_apiKey}&with_genres={genreId}&page={page}&language=it-IT");

                if (response?.Results == null)
                {
                    Console.WriteLine("Risposta API: Nessun risultato trovato per il genere");
                    return new List<Movie>();
                }

                Console.WriteLine($"Risposta API: {response.Results.Count} film trovati per il genere {genreId}");
                return response.Results.Select(m => ConvertToMovie(m)).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore durante il recupero dei film per genere: {ex.Message}");
                return new List<Movie>();
            }
        }

        // NUOVO METODO: Ottieni la lista di tutti i generi
        public async Task<List<Genre>> GetGenresAsync()
        {
            try
            {
                Console.WriteLine($"Chiamata API: {_baseUrl}/genre/movie/list?api_key=***&language=it-IT");
                var response = await _httpClient.GetFromJsonAsync<GenreListResponse>(
                    $"{_baseUrl}/genre/movie/list?api_key={_apiKey}&language=it-IT");

                if (response?.Genres == null)
                {
                    Console.WriteLine("Risposta API: Nessun genere trovato");
                    return new List<Genre>();
                }

                Console.WriteLine($"Risposta API: {response.Genres.Count} generi trovati");
                return response.Genres;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore durante il recupero dei generi: {ex.Message}");
                return new List<Genre>();
            }
        }

        public async Task<Movie?> GetMovieDetailsAsync(int tmdbId)
        {
            try
            {
                var tmdbMovie = await _httpClient.GetFromJsonAsync<TmdbMovieDetail>(
                    $"{_baseUrl}/movie/{tmdbId}?api_key={_apiKey}&language=it-IT&append_to_response=videos,credits");

                if (tmdbMovie == null)
                    return null;

                return ConvertToMovieWithDetails(tmdbMovie);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore durante il recupero dei dettagli del film: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> ImportMovieToDbAsync(int tmdbId)
        {
            try
            {
                // Verifica se il film esiste già nel database
                var existingMovie = await _context.Movies.FirstOrDefaultAsync(m => m.TmdbId == tmdbId);
                if (existingMovie != null)
                {
                    return true; // Il film esiste già
                }

                Console.WriteLine($"Importazione film con TMDB ID: {tmdbId}");

                // Ottieni i dettagli del film da TMDB
                var movie = await GetMovieDetailsAsync(tmdbId);
                if (movie == null)
                {
                    Console.WriteLine("Impossibile recuperare i dettagli del film");
                    return false;
                }

                Console.WriteLine($"Film trovato: {movie.Title}");

                // Salva il film nel database
                _context.Movies.Add(movie);
                await _context.SaveChangesAsync();

                Console.WriteLine("Film importato con successo");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore durante l'importazione del film: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Movie>> ImportPopularMoviesToDbAsync(int count = 20)
        {
            List<Movie> importedMovies = new List<Movie>();

            try
            {
                // Calcola quante pagine dobbiamo recuperare (20 film per pagina)
                int pages = (count + 19) / 20;

                Console.WriteLine($"Tentativo di importazione di {count} film popolari...");

                for (int i = 1; i <= pages; i++)
                {
                    var movies = await GetPopularMoviesAsync(i);
                    Console.WriteLine($"Pagina {i}: Recuperati {movies.Count} film");

                    foreach (var movie in movies)
                    {
                        // Verifica se il film esiste già
                        var existingMovie = await _context.Movies.FirstOrDefaultAsync(m =>
                            m.TmdbId == movie.TmdbId || m.Title == movie.Title);

                        if (existingMovie == null)
                        {
                            Console.WriteLine($"Importazione del film: {movie.Title} (ID TMDB: {movie.TmdbId})");
                            _context.Movies.Add(movie);
                            importedMovies.Add(movie);

                            // Se abbiamo raggiunto il numero richiesto di film, usciamo
                            if (importedMovies.Count >= count)
                                break;
                        }
                        else
                        {
                            Console.WriteLine($"Film già esistente: {movie.Title}");
                        }
                    }

                    if (importedMovies.Count >= count)
                        break;
                }

                await _context.SaveChangesAsync();
                Console.WriteLine($"Importati con successo {importedMovies.Count} film");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore durante l'importazione dei film: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
            }

            return importedMovies;
        }

        // NUOVO METODO: Importa multiple pagine di film popolari
        public async Task<List<Movie>> ImportMultiplePagesOfMoviesAsync(int pages = 5)
        {
            List<Movie> importedMovies = new List<Movie>();

            try
            {
                Console.WriteLine($"Importazione di {pages} pagine di film popolari...");

                // Ogni pagina contiene 20 film
                for (int i = 1; i <= pages; i++)
                {
                    Console.WriteLine($"Importazione pagina {i} di {pages}...");
                    var pageMovies = await GetPopularMoviesAsync(i);

                    if (pageMovies.Count == 0)
                        break;

                    foreach (var movie in pageMovies)
                    {
                        // Verifica se il film esiste già
                        var existingMovie = await _context.Movies.FirstOrDefaultAsync(m =>
                            m.TmdbId == movie.TmdbId || m.Title == movie.Title);

                        if (existingMovie == null)
                        {
                            _context.Movies.Add(movie);
                            importedMovies.Add(movie);
                        }
                    }

                    // Salva ogni pagina per evitare timeout su operazioni troppo lunghe
                    if (importedMovies.Count > 0)
                    {
                        await _context.SaveChangesAsync();
                        Console.WriteLine($"Salvati {importedMovies.Count} film finora");
                    }
                }

                Console.WriteLine($"Importazione completata. Totale film importati: {importedMovies.Count}");
                return importedMovies;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore durante l'importazione multipla: {ex.Message}");
                return importedMovies;
            }
        }

        // NUOVO METODO: Importa film per genere
        public async Task<List<Movie>> ImportMoviesByGenreAsync(int genreId, int pages = 3)
        {
            List<Movie> importedMovies = new List<Movie>();

            try
            {
                Console.WriteLine($"Importazione film del genere {genreId}...");

                for (int i = 1; i <= pages; i++)
                {
                    var movies = await GetMoviesByGenreAsync(genreId, i);

                    foreach (var movie in movies)
                    {
                        // Verifica se il film esiste già
                        var existingMovie = await _context.Movies.FirstOrDefaultAsync(m =>
                            m.TmdbId == movie.TmdbId || m.Title == movie.Title);

                        if (existingMovie == null)
                        {
                            _context.Movies.Add(movie);
                            importedMovies.Add(movie);
                        }
                    }

                    // Salva ogni pagina
                    if (importedMovies.Count > 0)
                    {
                        await _context.SaveChangesAsync();
                    }
                }

                return importedMovies;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore durante l'importazione di film per genere: {ex.Message}");
                return importedMovies;
            }
        }

        // NUOVO METODO: Aggiorna trailer per film esistente
        public async Task<bool> UpdateMovieTrailerAsync(int movieId)
        {
            try
            {
                var movie = await _context.Movies.FindAsync(movieId);
                if (movie == null || movie.TmdbId <= 0)
                {
                    return false;
                }

                // Se il film ha già un trailer valido, non fare nulla
                if (!string.IsNullOrEmpty(movie.TrailerUrl))
                {
                    return true;
                }

                var trailerUrl = await GetMovieTrailerAsync(movie.TmdbId);
                if (!string.IsNullOrEmpty(trailerUrl))
                {
                    movie.TrailerUrl = trailerUrl;
                    _context.Movies.Update(movie);
                    await _context.SaveChangesAsync();
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nell'aggiornamento del trailer: {ex.Message}");
                return false;
            }
        }

        // Aggiunto il metodo ConvertToMovie che mancava
        private Movie ConvertToMovie(TmdbMovie tmdbMovie)
        {
            var posterPath = !string.IsNullOrEmpty(tmdbMovie.PosterPath) ?
                $"{_imageBaseUrl}w500{tmdbMovie.PosterPath}" : null;

            var backdropPath = !string.IsNullOrEmpty(tmdbMovie.BackdropPath) ?
                $"{_imageBaseUrl}original{tmdbMovie.BackdropPath}" : null;

            return new Movie
            {
                Title = !string.IsNullOrEmpty(tmdbMovie.Title) ? tmdbMovie.Title : tmdbMovie.OriginalTitle,
                OriginalTitle = tmdbMovie.OriginalTitle,
                Description = tmdbMovie.Overview,
                ReleaseDate = DateOnly.TryParse(tmdbMovie.ReleaseDate, out var date) ? date.ToDateTime(TimeOnly.MinValue) : null,
                Rating = (decimal)(tmdbMovie.VoteAverage ?? 0),
                VoteCount = tmdbMovie.VoteCount ?? 0,
                PosterPath = posterPath,  // URL completa dell'immagine
                BackdropPath = backdropPath,  // URL completa dell'immagine
                TmdbId = tmdbMovie.Id,
                Genres = tmdbMovie.GenreIds?.Select(id => GetGenreName(id)).Where(g => g != null).ToArray() ?? Array.Empty<string>(),
                DateAdded = DateTime.UtcNow,
                IsVerified = true
            };
        }

        private Movie ConvertToMovieWithDetails(TmdbMovieDetail tmdbMovie)
        {
            // FIX: Aggiunta delle URL complete per le immagini
            var posterPath = !string.IsNullOrEmpty(tmdbMovie.PosterPath) ?
                $"{_imageBaseUrl}w500{tmdbMovie.PosterPath}" : null;

            var backdropPath = !string.IsNullOrEmpty(tmdbMovie.BackdropPath) ?
                $"{_imageBaseUrl}original{tmdbMovie.BackdropPath}" : null;

            string trailerUrl = null;

            // Cerca il trailer ufficiale nei video
            if (tmdbMovie.Videos?.Results != null)
            {
                var trailer = tmdbMovie.Videos.Results
                    .FirstOrDefault(v => v.Site?.ToLower() == "youtube" &&
                                       (v.Type?.ToLower() == "trailer" || v.Name?.ToLower().Contains("trailer") == true));

                if (trailer != null && !string.IsNullOrEmpty(trailer.Key))
                {
                    trailerUrl = $"https://www.youtube.com/watch?v={trailer.Key}";
                }
            }

            return new Movie
            {
                Title = !string.IsNullOrEmpty(tmdbMovie.Title) ? tmdbMovie.Title : tmdbMovie.OriginalTitle,
                OriginalTitle = tmdbMovie.OriginalTitle,
                Description = tmdbMovie.Overview,
                ReleaseDate = DateOnly.TryParse(tmdbMovie.ReleaseDate, out var date) ? date.ToDateTime(TimeOnly.MinValue) : null,
                Rating = (decimal)(tmdbMovie.VoteAverage ?? 0),
                VoteCount = tmdbMovie.VoteCount ?? 0,
                PosterPath = posterPath,  // URL completa dell'immagine
                BackdropPath = backdropPath,  // URL completa dell'immagine
                TmdbId = tmdbMovie.Id,
                ImdbId = tmdbMovie.ImdbId,
                Genres = tmdbMovie.Genres?.Select(g => g.Name).ToArray() ?? Array.Empty<string>(),
                TrailerUrl = trailerUrl,
                DateAdded = DateTime.UtcNow,
                IsVerified = true
            };
        }

        private string? GetGenreName(int genreId)
        {
            // Mappatura degli ID dei generi di TMDB ai nomi
            var genreMap = new Dictionary<int, string>
            {
                {28, "Azione"},
                {12, "Avventura"},
                {16, "Animazione"},
                {35, "Commedia"},
                {80, "Crime"},
                {99, "Documentario"},
                {18, "Dramma"},
                {10751, "Famiglia"},
                {14, "Fantasy"},
                {36, "Storia"},
                {27, "Horror"},
                {10402, "Musica"},
                {9648, "Mistero"},
                {10749, "Romance"},
                {878, "Fantascienza"},
                {10770, "Film TV"},
                {53, "Thriller"},
                {10752, "Guerra"},
                {37, "Western"}
            };

            return genreMap.TryGetValue(genreId, out var name) ? name : null;
        }
    }

    // Classi per deserializzare le risposte di TMDB
    public class TmdbSearchResponse
    {
        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("results")]
        public List<TmdbMovie>? Results { get; set; }

        [JsonPropertyName("total_pages")]
        public int TotalPages { get; set; }

        [JsonPropertyName("total_results")]
        public int TotalResults { get; set; }
    }

    public class TmdbMovieResponse
    {
        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("results")]
        public List<TmdbMovie>? Results { get; set; }

        [JsonPropertyName("total_pages")]
        public int TotalPages { get; set; }

        [JsonPropertyName("total_results")]
        public int TotalResults { get; set; }
    }

    public class TmdbMovie
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("original_title")]
        public string OriginalTitle { get; set; } = string.Empty;

        [JsonPropertyName("overview")]
        public string Overview { get; set; } = string.Empty;

        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; set; }

        [JsonPropertyName("backdrop_path")]
        public string? BackdropPath { get; set; }

        [JsonPropertyName("release_date")]
        public string? ReleaseDate { get; set; }

        [JsonPropertyName("vote_average")]
        public double? VoteAverage { get; set; }

        [JsonPropertyName("vote_count")]
        public int? VoteCount { get; set; }

        [JsonPropertyName("genre_ids")]
        public List<int>? GenreIds { get; set; }
    }

    public class TmdbMovieDetail : TmdbMovie
    {
        [JsonPropertyName("imdb_id")]
        public string? ImdbId { get; set; }

        [JsonPropertyName("genres")]
        public List<TmdbGenre>? Genres { get; set; }

        [JsonPropertyName("videos")]
        public TmdbVideoResponse? Videos { get; set; }

        [JsonPropertyName("credits")]
        public TmdbCreditsResponse? Credits { get; set; }
    }

    public class TmdbGenre
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class TmdbVideoResponse
    {
        [JsonPropertyName("results")]
        public List<TmdbVideo>? Results { get; set; }
    }

    public class TmdbVideo
    {
        [JsonPropertyName("key")]
        public string? Key { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("site")]
        public string? Site { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }
    }

    public class TmdbCreditsResponse
    {
        [JsonPropertyName("cast")]
        public List<TmdbCast>? Cast { get; set; }
    }

    public class TmdbCast
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("character")]
        public string? Character { get; set; }

        [JsonPropertyName("profile_path")]
        public string? ProfilePath { get; set; }
    }

    // NUOVA CLASSE: Per la lista di generi
    public class GenreListResponse
    {
        [JsonPropertyName("genres")]
        public List<Genre> Genres { get; set; } = new List<Genre>();
    }

    // NUOVA CLASSE: Per rappresentare un genere
    public class Genre
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
}