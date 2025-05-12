using CineVerify.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CineVerify.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(ApplicationDbContext context)
        {
            // Assicurati che il database esista
            await context.Database.EnsureCreatedAsync();

            // Controlla se ci sono già dei film
            if (await context.Movies.AnyAsync())
            {
                return;   // Il DB è già popolato
            }

            var movies = new Movie[]
            {
                new Movie
                {
                    Title = "Inception",
                    OriginalTitle = "Inception",
                    Description = "Un ladro che ruba segreti aziendali attraverso l'uso della tecnologia di condivisione dei sogni...",
                    ReleaseDate = new DateTime(2010, 7, 16),
                    Rating = 8.8m,
                    VoteCount = 32876,
                    PosterPath = "/9gk7adHYeDvHkCSEqAvQNLV5Uge.jpg",
                    BackdropPath = "/s3TBrRGB1iav7gFOCNx3H31MoES.jpg",
                    Genres = new[] { "Azione", "Fantascienza", "Thriller" },
                    IsVerified = true
                },
                new Movie
                {
                    Title = "The Shawshank Redemption",
                    OriginalTitle = "The Shawshank Redemption",
                    Description = "Condannato ingiustamente per omicidio, Andy Dufresne lotta per la libertà dal carcere...",
                    ReleaseDate = new DateTime(1994, 9, 23),
                    Rating = 9.3m,
                    VoteCount = 24596,
                    PosterPath = "/q6y0Go1tsGEsmtFryDOJo3dEmqu.jpg",
                    BackdropPath = "/avedvodAZUcwqevBfm8p4G2NziQ.jpg",
                    Genres = new[] { "Dramma", "Crime" },
                    IsVerified = true
                },
                new Movie
                {
                    Title = "Il Padrino",
                    OriginalTitle = "The Godfather",
                    Description = "Don Vito Corleone, capo di una famiglia mafiosa di New York, cerca di trasferire il suo impero al figlio...",
                    ReleaseDate = new DateTime(1972, 3, 14),
                    Rating = 9.2m,
                    VoteCount = 18934,
                    PosterPath = "/3bhkrj58Vtu7enYsRolD1fZdja1.jpg",
                    BackdropPath = "/rSPw7tgCH9c6NqICZef4kZjFOQ5.jpg",
                    Genres = new[] { "Dramma", "Crime" },
                    IsVerified = true
                },
                new Movie
                {
                    Title = "Pulp Fiction",
                    OriginalTitle = "Pulp Fiction",
                    Description = "Le vite di due sicari, un pugile, la moglie di un gangster e una coppia di banditi si intrecciano...",
                    ReleaseDate = new DateTime(1994, 10, 14),
                    Rating = 8.9m,
                    VoteCount = 25687,
                    PosterPath = "/plnlrtBUULT0rh3Xsjmpubiso3L.jpg",
                    BackdropPath = "/suaEOtk1N1sgg2MTM7oZd2cfVp3.jpg",
                    Genres = new[] { "Crime", "Thriller" },
                    IsVerified = true
                },
                new Movie
                {
                    Title = "Fight Club",
                    OriginalTitle = "Fight Club",
                    Description = "Un impiegato insoddisfatto e un venditore di sapone misterioso fondano un club di lotta clandestino...",
                    ReleaseDate = new DateTime(1999, 10, 15),
                    Rating = 8.8m,
                    VoteCount = 26389,
                    PosterPath = "/pB8BM7pdSp6B6Ih7QZ4DrQ3PmJK.jpg",
                    BackdropPath = "/hZkgoQYus5vegHoetLkCJzb17zJ.jpg",
                    Genres = new[] { "Dramma" },
                    IsVerified = true
                },
                new Movie
                {
                    Title = "Interstellar",
                    OriginalTitle = "Interstellar",
                    Description = "Un team di esploratori viaggia attraverso un wormhole nello spazio nel tentativo di garantire la sopravvivenza dell'umanità.",
                    ReleaseDate = new DateTime(2014, 11, 7),
                    Rating = 8.6m,
                    VoteCount = 29845,
                    PosterPath = "/gEU2QniE6E77NI6lCU6MxlNBvIx.jpg",
                    BackdropPath = "/xJHokMbljvjADYdit5fK5VQsXEG.jpg",
                    Genres = new[] { "Avventura", "Dramma", "Fantascienza" },
                    IsVerified = true
                },
                new Movie
                {
                    Title = "Dune",
                    OriginalTitle = "Dune",
                    Description = "Paul Atreides, un giovane brillante e talentuoso, deve viaggiare verso il pianeta più pericoloso dell'universo per garantire il futuro della sua famiglia e del suo popolo.",
                    ReleaseDate = new DateTime(2021, 10, 22),
                    Rating = 8.0m,
                    VoteCount = 15642,
                    PosterPath = "/d5NXSklXo0qyIYkgV94XAgMIckC.jpg",
                    BackdropPath = "/jYEW5xZkZk2WTrdbMGAPFuBqbDc.jpg",
                    Genres = new[] { "Fantascienza", "Avventura" },
                    IsVerified = true
                },
                new Movie
                {
                    Title = "Oppenheimer",
                    OriginalTitle = "Oppenheimer",
                    Description = "La storia di J. Robert Oppenheimer, il fisico teorico che ha guidato il Progetto Manhattan per sviluppare la bomba atomica durante la Seconda Guerra Mondiale.",
                    ReleaseDate = new DateTime(2023, 7, 21),
                    Rating = 8.4m,
                    VoteCount = 12354,
                    PosterPath = "/8Gxv8gSFCU0XGDykEGv7zR1n2ua.jpg",
                    BackdropPath = "/rLb2cwF3Pazuxaj0sRXQ037tGI1.jpg",
                    Genres = new[] { "Dramma", "Storia", "Thriller" },
                    IsVerified = true
                }
            };

            await context.Movies.AddRangeAsync(movies);
            await context.SaveChangesAsync();
        }
    }
}