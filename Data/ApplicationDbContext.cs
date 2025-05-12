using CineVerify.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Linq;
using System.Text.Json;

namespace CineVerify.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Movie> Movies { get; set; }
        public DbSet<MovieReview> MovieReviews { get; set; }
        public DbSet<MovieUserRating> MovieUserRatings { get; set; }
        public DbSet<UserFavorite> UserFavorites { get; set; }
        public DbSet<MovieWatchHistory> MovieWatchHistory { get; set; } // Aggiunta questa riga

        public DbSet<MovieAnalysis> MovieAnalyses { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configurazione per array di stringhe in SQLite
            modelBuilder.Entity<Movie>()
                .Property(m => m.Genres)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<string[]>(v, (JsonSerializerOptions)null) ?? Array.Empty<string>(),
                    new ValueComparer<string[]>(
                        (c1, c2) => c1.SequenceEqual(c2),
                        c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => c.ToArray())
                );

            // Rating come REAL per SQLite
            modelBuilder.Entity<Movie>()
                .Property(m => m.Rating)
                .HasColumnType("REAL");

            modelBuilder.Entity<MovieReview>()
                .Property(r => r.Rating)
                .HasColumnType("REAL");

            modelBuilder.Entity<MovieUserRating>()
                .Property(r => r.Rating)
                .HasColumnType("REAL");
            modelBuilder.Entity<MovieAnalysis>()
    .HasIndex(m => m.MovieId);

            modelBuilder.Entity<MovieAnalysis>()
                .HasIndex(m => m.UserId);
            // Chiavi composite
            modelBuilder.Entity<UserFavorite>()
                .HasKey(uf => new { uf.UserId, uf.MovieId });

            modelBuilder.Entity<MovieUserRating>()
                .HasKey(mur => new { mur.UserId, mur.MovieId });

            // Relazioni tra entità
            modelBuilder.Entity<MovieReview>()
                .HasOne(r => r.Movie)
                .WithMany()
                .HasForeignKey(r => r.MovieId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserFavorite>()
                .HasOne(uf => uf.Movie)
                .WithMany()
                .HasForeignKey(uf => uf.MovieId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MovieUserRating>()
                .HasOne(mur => mur.Movie)
                .WithMany()
                .HasForeignKey(mur => mur.MovieId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MovieWatchHistory>()
        .HasOne(mwh => mwh.Movie)
        .WithMany()
        .HasForeignKey(mwh => mwh.MovieId)
        .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MovieWatchHistory>()
                .HasOne(mwh => mwh.User)
                .WithMany()
                .HasForeignKey(mwh => mwh.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}