using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CineVerify.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _baseUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro";
        private readonly ILogger<GeminiService> _logger;
        private readonly Random _random = new Random();

        public GeminiService(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiService> logger)
        {
            _httpClient = httpClient;
            _apiKey = configuration["GeminiApi:ApiKey"] ?? throw new InvalidOperationException("API key for Gemini not configured");
            _logger = logger;

            _httpClient.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        public async Task<string> GenerateContentAsync(string prompt)
        {
            try
            {
                _logger.LogInformation($"Tentativo di generazione di riassunto con Gemini: {prompt.Substring(0, Math.Min(50, prompt.Length))}...");

                // Modifica del prompt per forzare risposte più lunghe
                string enhancedPrompt = $@"{prompt}

IMPORTANTE: Fornisci una risposta dettagliata e approfondita di almeno 5-6 paragrafi. Ogni paragrafo deve contenere almeno 3-4 frasi complete. 
Assicurati che ogni paragrafo sviluppi un aspetto diverso dell'argomento.
Questa è la richiesta #{DateTime.Now.Ticks % 10000} - genera una risposta completamente diversa dalle precedenti.";

                // Temperatura molto alta per massima variabilità
                var temperature = 0.9f + ((float)_random.NextDouble() * 0.09f); // Range: 0.9-0.99

                var requestPayload = new
                {
                    contents = new[]
                    {
                        new {
                            role = "user",
                            parts = new[] { new { text = enhancedPrompt } }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = temperature,
                        maxOutputTokens = 4000, // Aumentato drasticamente
                        topP = 0.95f,
                        topK = 60,
                        stopSequences = new string[] { } // Nessuna sequenza di stop
                    }
                };

                var requestContent = new StringContent(
                    JsonSerializer.Serialize(requestPayload),
                    Encoding.UTF8,
                    "application/json");

                _logger.LogDebug($"Inviando richiesta a {_baseUrl}:generateContent con temperature={temperature}");

                var response = await _httpClient.PostAsync(
                    $"{_baseUrl}:generateContent?key={_apiKey}",
                    requestContent);

                _logger.LogDebug($"Risposta ricevuta con status code: {response.StatusCode}");

                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (responseData.TryGetProperty("candidates", out var candidatesElement) &&
                        candidatesElement.GetArrayLength() > 0 &&
                        candidatesElement[0].TryGetProperty("content", out var contentElement) &&
                        contentElement.TryGetProperty("parts", out var partsElement) &&
                        partsElement.GetArrayLength() > 0 &&
                        partsElement[0].TryGetProperty("text", out var textElement))
                    {
                        string result = textElement.GetString() ?? "Riassunto non disponibile.";

                        // Controllo della lunghezza: se troppo corto, rigenera con istruzioni più esplicite
                        if (result.Split('\n').Length < 5)
                        {
                            _logger.LogWarning("Risposta troppo breve, riprovando con istruzioni più esplicite...");
                            return await ForceGenerateLongerContentAsync(prompt);
                        }

                        return result;
                    }
                    else
                    {
                        _logger.LogWarning("Risposta valida ma struttura JSON inaspettata");
                        return "Non è stato possibile elaborare il riassunto. Formato risposta inatteso.";
                    }
                }
                else
                {
                    _logger.LogWarning($"Errore API: {response.StatusCode} - {responseContent}");
                    return $"Si è verificato un errore: {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore critico durante la generazione del riassunto");
                return "Si è verificato un errore durante la generazione del riassunto. Riprova più tardi.";
            }
        }

        // Metodo specializzato per forzare contenuti più lunghi quando i tentativi normali falliscono
        private async Task<string> ForceGenerateLongerContentAsync(string originalPrompt)
        {
            // Genera un numero casuale per forzare la variabilità
            string uniqueId = Guid.NewGuid().ToString().Substring(0, 8);

            // Prompt ultra-specifico per forzare risposte lunghe
            string forcedPrompt = $@"
[ISTRUZIONI CRITICHE DI LUNGHEZZA - ID:{uniqueId}]

Originale richiesta: {originalPrompt}

REQUISITI DI LUNGHEZZA NON NEGOZIABILI:
1. La tua risposta DEVE contenere MINIMO 6 paragrafi completi.
2. Ogni paragrafo DEVE contenere almeno 4-5 frasi complete.
3. La risposta totale DEVE essere di almeno 600-800 parole.
4. Sviluppa ogni aspetto in dettaglio, con esempi specifici e analisi approfondita.
5. EVITA ASSOLUTAMENTE risposte brevi o riassuntive.

Questa è una richiesta specifica che richiede una risposta dettagliata e approfondita. 
Timestamp della richiesta: {DateTime.Now.Ticks} - genera contenuto completamente nuovo.
";

            // Parametri estremamente spinti per massimizzare la lunghezza
            var requestPayload = new
            {
                contents = new[]
                {
                    new { role = "user", parts = new[] { new { text = forcedPrompt } } }
                },
                generationConfig = new
                {
                    temperature = 0.99,
                    maxOutputTokens = 4096, // Massimo consentito
                    topP = 0.98,
                    topK = 80
                }
            };

            var requestContent = new StringContent(
                JsonSerializer.Serialize(requestPayload),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(
                $"{_baseUrl}:generateContent?key={_apiKey}",
                requestContent);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                try
                {
                    return responseData
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString() ?? "Non è stato possibile generare un contenuto sufficientemente dettagliato.";
                }
                catch
                {
                    return "Non è stato possibile generare un contenuto sufficientemente dettagliato.";
                }
            }

            return "Non è stato possibile generare un contenuto sufficientemente dettagliato.";
        }

        public async Task<string> GeneratePersonalizedRecommendationsAsync(
            string userId,
            string[] favoriteGenres,
            string[] favoriteMovies,
            int[] userRatings)
        {
            try
            {
                var genresText = string.Join(", ", favoriteGenres);
                var moviesText = string.Join(", ", favoriteMovies);
                var ratingsText = string.Join(", ", userRatings);

                // Genera un ID unico per forzare risposte diverse
                string uniqueId = DateTime.Now.Ticks.ToString().Substring(8);

                string prompt = $@"
[ISTRUZIONI LUNGHE DETTAGLIATE - ID:{uniqueId}]

Sei un critico cinematografico esperto. Genera consigli personalizzati DETTAGLIATI E APPROFONDITI per l'utente.

DATI UTENTE:
- Generi preferiti: {genresText}
- Film apprezzati: {moviesText}
- Valutazioni recenti: {ratingsText}

REQUISITI DI LUNGHEZZA:
- Scrivi almeno 5-6 paragrafi corposi
- Fornisci almeno 4-5 raccomandazioni di film
- Per ciascun film, scrivi ALMENO 4-5 frasi dettagliate
- La risposta TOTALE deve essere di almeno 600-800 parole

STRUTTURA RICHIESTA:
1. Inizia con un'introduzione personalizzata e accattivante (almeno 4 frasi)
2. Per OGNI film consigliato, includi:
   - Titolo completo e anno
   - Regista e attori principali
   - Trama generale (senza spoiler)
   - Collegamenti specifici con i gusti dell'utente
   - Motivi dettagliati per cui l'utente potrebbe apprezzarlo
3. Almeno un paragrafo di considerazioni generali sui trend cinematografici che potrebbero interessare l'utente
4. Conclusione che incoraggi l'utente a esplorare (almeno 3 frasi)

STILE:
- Usa linguaggio entusiasta, coinvolgente e professionale
- Evita ripetizioni e formule generiche
- Fornisci spunti originali e personali

Questa è la richiesta #{uniqueId} - genera una risposta completamente unica e diversa dalle precedenti.
";

                // Parametri per massima variabilità e lunghezza
                var temperature = 0.95 + (_random.NextDouble() * 0.04); // Range: 0.95-0.99

                var requestPayload = new
                {
                    contents = new[]
                    {
                        new { role = "user", parts = new[] { new { text = prompt } } }
                    },
                    generationConfig = new
                    {
                        temperature = temperature,
                        maxOutputTokens = 4000, // Molto alto per consigli lunghi
                        topP = 0.98,
                        topK = 70
                    }
                };

                var requestContent = new StringContent(
                    JsonSerializer.Serialize(requestPayload),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync(
                    $"{_baseUrl}:generateContent?key={_apiKey}",
                    requestContent);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<JsonElement>(content);

                    try
                    {
                        var generatedText = responseData
                            .GetProperty("candidates")[0]
                            .GetProperty("content")
                            .GetProperty("parts")[0]
                            .GetProperty("text")
                            .GetString();

                        // Controlla la lunghezza del risultato
                        if (generatedText != null && generatedText.Split('\n').Length < 5)
                        {
                            // Se troppo corto, riprova con un prompt più aggressivo
                            return await ForceGenerateLongerContentAsync($"Raccomandazioni personalizzate per utente con generi preferiti {genresText} e film preferiti {moviesText}");
                        }

                        return generatedText ?? "Non sono disponibili consigli personalizzati al momento.";
                    }
                    catch
                    {
                        return "Non sono disponibili consigli personalizzati al momento.";
                    }
                }
                else
                {
                    return "Non sono disponibili consigli personalizzati al momento.";
                }
            }
            catch (Exception)
            {
                return "Non sono disponibili consigli personalizzati al momento. Riprova più tardi.";
            }
        }

        public async Task<string> AnalyzeSentimentAsync(string text)
        {
            // Questo metodo non ha bisogno di modifiche per lunghezza poiché richiede solo una parola
            try
            {
                string prompt = $@"
Analizza il sentimento del seguente testo in italiano. Rispondi solo con una delle seguenti parole: 'Positivo', 'Negativo', o 'Neutro'.

Testo:
{text}

Classificazione:
";

                var requestPayload = new
                {
                    contents = new[]
                    {
                        new { role = "user", parts = new[] { new { text = prompt } } }
                    },
                    generationConfig = new
                    {
                        temperature = 0.1,
                        maxOutputTokens = 10,
                        topP = 0.95,
                        topK = 40
                    }
                };

                var requestContent = new StringContent(
                    JsonSerializer.Serialize(requestPayload),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync(
                    $"{_baseUrl}:generateContent?key={_apiKey}",
                    requestContent);

                if (response.IsSuccessStatusCode)
                {
                    var responseData = await JsonSerializer.DeserializeAsync<JsonElement>(
                        await response.Content.ReadAsStreamAsync());

                    var generatedText = responseData
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString() ?? "";

                    // Pulisci e normalizza il risultato
                    generatedText = generatedText.Trim().ToLower();

                    if (generatedText.Contains("positivo"))
                        return "Positivo";
                    else if (generatedText.Contains("negativo"))
                        return "Negativo";
                    else
                        return "Neutro";
                }
                return "Neutro";
            }
            catch
            {
                return "Neutro";
            }
        }

        public async Task<string> GenerateMovieAnalysisAsync(string title, string plot, string[] genres, string releaseYear)
        {
            try
            {
                string genresText = string.Join(", ", genres);

                // Aggiungi un ID unico per forzare variabilità
                string uniqueId = $"{DateTime.Now.Ticks}-{_random.Next(10000)}";

                string prompt = $@"
[ISTRUZIONI SPECIFICHE PER ANALISI ESTESA - ID:{uniqueId}]

Genera un'analisi APPROFONDITA, DETTAGLIATA ed ESTESA del film in italiano. 
Questa analisi DEVE essere lunga minimo 6 paragrafi, ciascuno di almeno 4-5 frasi complete.

DATI FILM:
- Titolo: {title}
- Anno: {releaseYear}
- Generi: {genresText}
- Trama: {plot}

STRUTTURA OBBLIGATORIA (segui esattamente questa struttura con lunghezze minime):
1. INTRODUZIONE (1 paragrafo, min. 5 frasi): Contesto storico e cinematografico del film, sua rilevanza, introduzione generale.
2. ANALISI TEMATICA (2 paragrafi, min. 8-10 frasi totali): Analisi approfondita dei temi principali, simbolismi, messaggi.
3. ANALISI TECNICA (1 paragrafo lungo, min. 5 frasi): Regia, fotografia, montaggio, colonna sonora, effetti visivi.
4. ANALISI ATTORIALE (1 paragrafo, min. 4 frasi): Performance degli attori principali, caratterizzazione dei personaggi.
5. IMPATTO CULTURALE (1 paragrafo, min. 4 frasi): Influenza del film sulla cultura, cinema, società.
6. CONCLUSIONE (1 paragrafo, min. 4 frasi): Valutazione complessiva, rilevanza contemporanea, eredità.

È ESSENZIALE che l'analisi:
- Sia dettagliata, profonda e articolata (almeno 700-900 parole totali)
- Utilizzi un linguaggio ricco, preciso e professionale
- Contenga osservazioni originali e specifiche, evitando genericità
- Sia scritta secondo la struttura indicata sopra
- Offra prospettive uniche e non ovvie sul film

NON USARE MAI FORMULE COME 'In conclusione' o 'In sintesi' fino al paragrafo finale.
Questa è la richiesta #{uniqueId} - genera un'analisi completamente originale e diversa rispetto a qualsiasi altra analisi precedente.
";

                // Temperatura altissima per massima variabilità
                var temperature = 0.97;

                var requestPayload = new
                {
                    contents = new[]
                    {
                        new { role = "user", parts = new[] { new { text = prompt } } }
                    },
                    generationConfig = new
                    {
                        temperature = temperature,
                        maxOutputTokens = 4096, // Massimo consentito
                        topP = 0.99,
                        topK = 100 // Massima variabilità
                    }
                };

                var requestContent = new StringContent(
                    JsonSerializer.Serialize(requestPayload),
                    Encoding.UTF8,
                    "application/json");

                _logger.LogInformation($"Generando analisi film per: {title} con ID unico: {uniqueId}");

                var response = await _httpClient.PostAsync(
                    $"{_baseUrl}:generateContent?key={_apiKey}",
                    requestContent);

                if (response.IsSuccessStatusCode)
                {
                    var responseData = await JsonSerializer.DeserializeAsync<JsonElement>(
                        await response.Content.ReadAsStreamAsync());

                    var generatedText = responseData
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();

                    // Verifica la lunghezza del testo generato
                    if (generatedText != null && (generatedText.Split('\n').Length < 6 || generatedText.Length < 500))
                    {
                        _logger.LogWarning("Analisi film troppo breve, rigenerando con istruzioni più stringenti");
                        return await ForceGenerateLongerContentAsync($"Analisi dettagliata del film {title} ({releaseYear}, {genresText}). Trama: {plot}");
                    }

                    return generatedText ?? "Non è stato possibile generare un'analisi.";
                }

                return "Non è stato possibile generare un'analisi.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella generazione dell'analisi film");
                return $"Si è verificato un errore durante la generazione dell'analisi: {ex.Message}";
            }
        }

        // Nuovo metodo ottimizzato per generare riassunti veramente estesi e dettagliati
        public async Task<string> GenerateExtendedSummaryAsync(string movieTitle, string plot, string[] genres)
        {
            try
            {
                string genresText = string.Join(", ", genres);

                // ID unico per garantire variabilità tra le richieste
                string uniqueId = $"{DateTime.Now.Ticks}-{Guid.NewGuid().ToString().Substring(0, 8)}";

                // Prompt ultra-specifico per riassunti lunghi e dettagliati
                string prompt = $@"
[ISTRUZIONI CRITICHE PER RIASSUNTO ESTESO - ID:{uniqueId}]

Genera un riassunto ESTREMAMENTE DETTAGLIATO e APPROFONDITO per il film ""{movieTitle}"" (generi: {genresText}).

Trama di base:
{plot}

REQUISITI DI LUNGHEZZA NON NEGOZIABILI:
1. MINIMO 6 paragrafi corposi (ciascuno con 5+ frasi)
2. Lunghezza totale: MINIMO 800-1000 parole
3. Ogni paragrafo deve approfondire un aspetto diverso dell'opera

STRUTTURA OBBLIGATORIA:
1. INTRODUZIONE: Contesto del film, regista, anno, posizionamento nella storia del cinema
2. SINOSSI ESTESA: Approfondimento della trama con analisi della struttura narrativa
3. ANALISI DEI PERSONAGGI: Approfondimento psicologico dei protagonisti e delle loro motivazioni
4. TEMATICHE PRINCIPALI: Analisi dettagliata dei temi e messaggi centrali del film
5. ASPETTI TECNICI E STILISTICI: Regia, fotografia, montaggio, scenografia, colonna sonora
6. IMPATTO E RILEVANZA: Accoglienza critica, importanza storica, eredità culturale
7. CONCLUSIONI: Riflessioni finali sul valore complessivo dell'opera

REQUISITI STILISTICI:
- Usa un linguaggio ricco, variato ed eloquente
- Alterna frasi lunghe e articolate con frasi più brevi per ritmo dinamico
- Includi almeno 3-4 citazioni o riferimenti specifici al film
- Evita ASSOLUTAMENTE risposte generiche o formule ripetitive
- Crea un testo che sembri scritto da un critico cinematografico esperto

Questa è la richiesta unica #{uniqueId} - genera un'analisi completamente originale e diversa da qualsiasi altra analisi precedente.
";

                // Parametri estremamente aggressivi per lunghezza e variabilità
                var requestPayload = new
                {
                    contents = new[]
                    {
                        new { role = "user", parts = new[] { new { text = prompt } } }
                    },
                    generationConfig = new
                    {
                        temperature = 0.99, // Massima variabilità
                        maxOutputTokens = 4096, // Massimo consentito
                        topP = 0.99,
                        topK = 120 // Estremamente alto per massima variabilità
                    }
                };

                var requestContent = new StringContent(
                    JsonSerializer.Serialize(requestPayload),
                    Encoding.UTF8,
                    "application/json");

                _logger.LogInformation($"Generazione riassunto ultra-esteso per: {movieTitle} con ID:{uniqueId}");

                var response = await _httpClient.PostAsync(
                    $"{_baseUrl}:generateContent?key={_apiKey}",
                    requestContent);

                if (response.IsSuccessStatusCode)
                {
                    var responseData = await JsonSerializer.DeserializeAsync<JsonElement>(
                        await response.Content.ReadAsStreamAsync());

                    var generatedText = responseData
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();

                    // Se comunque il testo è troppo corto, riprova con un altro approccio
                    if (generatedText != null && (generatedText.Split('\n').Length < 7 || generatedText.Length < 600))
                    {
                        _logger.LogWarning("Riassunto troppo breve nonostante le istruzioni, provando strategia alternativa");

                        // Dividi e conquista: genera multiple sezioni e poi combinale
                        var intro = await GenerateSectionAsync("introduzione e contesto", movieTitle, genresText);
                        var analisi = await GenerateSectionAsync("analisi approfondita", movieTitle, genresText);
                        var personaggi = await GenerateSectionAsync("analisi dei personaggi principali", movieTitle, genresText);
                        var impatto = await GenerateSectionAsync("impatto culturale e rilevanza", movieTitle, genresText);

                        return $"{intro}\n\n{analisi}\n\n{personaggi}\n\n{impatto}";
                    }

                    return generatedText ?? "Non è stato possibile generare un riassunto esteso.";
                }

                return "Non è stato possibile generare un riassunto esteso.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella generazione del riassunto esteso");
                return $"Si è verificato un errore durante la generazione del riassunto: {ex.Message}";
            }
        }

        // Metodo helper per generare singole sezioni di un'analisi
        private async Task<string> GenerateSectionAsync(string sectionType, string movieTitle, string genresText)
        {
            try
            {
                string uniqueId = Guid.NewGuid().ToString().Substring(0, 8);
                string prompt = $"Genera SOLO una {sectionType} dettagliata (minimo 2 paragrafi lunghi) per il film \"{movieTitle}\" ({genresText}). Usa ID:{uniqueId} per una risposta unica e originale.";

                var requestPayload = new
                {
                    contents = new[]
                    {
                        new { role = "user", parts = new[] { new { text = prompt } } }
                    },
                    generationConfig = new
                    {
                        temperature = 0.95,
                        maxOutputTokens = 1024,
                        topP = 0.98,
                        topK = 60
                    }
                };

                var requestContent = new StringContent(
                    JsonSerializer.Serialize(requestPayload),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync(
                    $"{_baseUrl}:generateContent?key={_apiKey}",
                    requestContent);

                if (response.IsSuccessStatusCode)
                {
                    var responseData = await JsonSerializer.DeserializeAsync<JsonElement>(
                        await response.Content.ReadAsStreamAsync());

                    return responseData
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString() ?? $"Sezione su {sectionType} non disponibile.";
                }

                return $"Sezione su {sectionType} non disponibile.";
            }
            catch
            {
                return $"Sezione su {sectionType} non disponibile.";
            }
        }
    }
}