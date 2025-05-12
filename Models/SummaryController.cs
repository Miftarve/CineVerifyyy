using CineVerify.Data;
using CineVerify.Models;
using CineVerify.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace CineVerify.Modules
{
    [Route("api/[controller]")]
    [ApiController]
    public class SummaryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly AdvancedMovieInfoService _movieInfoService;
        private readonly ILogger<SummaryController> _logger;

        public SummaryController(
            ApplicationDbContext context,
            AdvancedMovieInfoService movieInfoService,
            ILogger<SummaryController> logger)
        {
            _context = context;
            _movieInfoService = movieInfoService;
            _logger = logger;
        }

        [HttpPost("Generate")]
        [Authorize]
        public async Task<IActionResult> GenerateSummary([FromQuery] int movieId)
        {
            try
            {
                _logger.LogInformation($"Richiesta di analisi dettagliata per il film ID: {movieId}");

                // Recupera i dettagli del film
                var movie = await _context.Movies.FindAsync(movieId);
                if (movie == null)
                {
                    _logger.LogWarning($"Film non trovato: {movieId}");
                    return NotFound(new { error = "Film non trovato" });
                }

                _logger.LogInformation($"Recupero informazioni dettagliate per: {movie.Title} (ID: {movieId})");

                // Ottieni l'analisi dettagliata utilizzando il nuovo servizio
                var detailedInfo = await _movieInfoService.GetDetailedMovieInfoAsync(movie);

                // Formatta il riassunto per renderlo più leggibile
                string formattedSummary = detailedInfo.DetailedSummary.Trim().Replace("\n\n", "\n");

                _logger.LogInformation($"Analisi dettagliata generata con successo per il film {movieId}");

                // Aggiungi metadati aggiuntivi alla risposta
                return Ok(new
                {
                    summary = formattedSummary,
                    directors = detailedInfo.Directors,
                    cast = detailedInfo.Cast,
                    runtime = detailedInfo.Runtime,
                    rating = detailedInfo.TmdbRating,
                    source = detailedInfo.Source
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Errore nella generazione dell'analisi dettagliata per il film {movieId}");
                return StatusCode(500, new
                {
                    error = "Errore nella generazione dell'analisi dettagliata",
                    summary = "Non è stato possibile produrre un'analisi dettagliata per questo film. Il servizio potrebbe essere temporaneamente non disponibile."
                });
            }
        }
    }
}