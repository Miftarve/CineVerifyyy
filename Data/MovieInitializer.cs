using CineVerify.Models;
using CineVerify.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace CineVerify.Data
{
    public static class MovieInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider, ILogger logger)
        {
            // Controlla il database
            var context = serviceProvider.GetService<ApplicationDbContext>();
            if (context == null)
            {
                logger.LogError("ApplicationDbContext non disponibile per l'inizializzazione");
                return;
            }

            // Conta i film esistenti
            var movieCount = await context.Movies.CountAsync();
            logger.LogInformation($"Database contiene {movieCount} film");

            // Se abbiamo meno di 50 film, importa più film
            if (movieCount < 50)
            {
                var movieApiService = serviceProvider.GetService<MovieApiService>();
                if (movieApiService == null)
                {
                    logger.LogError("MovieApiService non disponibile per l'importazione di film");
                    return;
                }

                logger.LogInformation("Inizializzazione database con più film da TMDB...");

                try
                {
                    // Importa film popolari (3 pagine = 60 film)
                    var importedPopular = await movieApiService.ImportMultiplePagesOfMoviesAsync(3);
                    logger.LogInformation($"Importati {importedPopular.Count} film popolari");

                    // Importa alcuni film per genere (i più comuni)
                    var genreIds = new[] { 28, 12, 35, 18, 27, 10749, 878 }; // Action, Adventure, Comedy, Drama, Horror, Romance, Sci-Fi
                    foreach (var genreId in genreIds)
                    {
                        var importedGenre = await movieApiService.ImportMoviesByGenreAsync(genreId, 1);
                        logger.LogInformation($"Importati {importedGenre.Count} film per il genere {genreId}");
                    }

                    logger.LogInformation("Importazione film completata con successo");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Errore durante l'importazione di film da TMDB");
                }
            }
        }
    }
}