using CineVerify.Data;
using CineVerify.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CineVerify.Repositories
{
    public class MovieAnalysisRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MovieAnalysisRepository> _logger;

        public MovieAnalysisRepository(
            ApplicationDbContext context,
            ILogger<MovieAnalysisRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<MovieAnalysis> SaveMovieAnalysisAsync(int movieId, string userId, string pdfPath, string analysisContent)
        {
            try
            {
                var analysis = new MovieAnalysis
                {
                    MovieId = movieId,
                    UserId = userId,
                    AnalysisContent = analysisContent,
                    PdfPath = pdfPath,
                    CreatedAt = DateTime.UtcNow
                };

                _context.MovieAnalyses.Add(analysis);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Analisi salvata con successo per il film ID: {movieId}, PDF: {pdfPath}");

                return analysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Errore nel salvataggio dell'analisi per il film {movieId}");
                throw;
            }
        }

        public async Task<List<MovieAnalysis>> GetAnalysesByMovieIdAsync(int movieId)
        {
            return await _context.MovieAnalyses
                .Where(a => a.MovieId == movieId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<MovieAnalysis> GetAnalysisByIdAsync(int analysisId)
        {
            return await _context.MovieAnalyses
                .FirstOrDefaultAsync(a => a.Id == analysisId);
        }
    }
}