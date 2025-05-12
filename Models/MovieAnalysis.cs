using System;
using System.ComponentModel.DataAnnotations;

namespace CineVerify.Models
{
    public class MovieAnalysis
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MovieId { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public string AnalysisContent { get; set; }

        [Required]
        public string PdfPath { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}