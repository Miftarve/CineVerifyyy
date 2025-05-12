function generateSummary() {
    if (aiSummarySection) {
        aiSummarySection.style.display = 'block';
        aiSummarySection.classList.add('ai-summary-slide-in');

        if (summaryContent) {
            summaryContent.querySelector('.ai-summary-loading').style.display = 'block';
            summaryText.style.display = 'none';

            // Nascondi i controlli PDF se già mostrati
            const pdfControls = summaryContent.querySelector('.pdf-controls');
            if (pdfControls) {
                pdfControls.style.display = 'none';
            }
        }

        aiSummarySection.scrollIntoView({ behavior: 'smooth' });

        fetch(`/api/Summary/Generate?movieId=${movieId}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            }
        })
            .then(response => {
                if (!response.ok) {
                    throw new Error(response.status);
                }
                return response.json();
            })
            .then(data => {
                if (summaryContent) {
                    summaryContent.querySelector('.ai-summary-loading').style.display = 'none';
                }

                if (summaryText) {
                    if (data && data.summary) {
                        // Salva i dati completi per la generazione del PDF
                        summaryData = data;

                        // Formatta il riassunto per visualizzarlo meglio
                        let formattedSummary = data.summary.replace(/\n/g, '<br>');

                        // Crea un contenuto HTML strutturato
                        let html = `
                        <div class="detailed-summary">
                            ${formattedSummary}
                        </div>
                    `;

                        // Se ci sono dati aggiuntivi, mostrali
                        if (data.directors && data.directors.length > 0) {
                            html += `
                            <div class="summary-metadata">
                                <div class="metadata-item">
                                    <span class="metadata-label">Regia:</span>
                                    <span class="metadata-value">${data.directors.join(', ')}</span>
                                </div>
                        `;
                        }

                        if (data.cast && data.cast.length > 0) {
                            html += `
                            <div class="metadata-item">
                                <span class="metadata-label">Cast principale:</span>
                                <span class="metadata-value">${data.cast.join(', ')}</span>
                            </div>
                        `;
                        }

                        if (data.runtime) {
                            html += `
                            <div class="metadata-item">
                                <span class="metadata-label">Durata:</span>
                                <span class="metadata-value">${data.runtime} minuti</span>
                            </div>
                        `;
                        }

                        // Se ci sono metadati, chiudi il div
                        if (data.directors && data.directors.length > 0) {
                            html += `</div>`;
                        }

                        // Aggiungi controlli per il PDF
                        html += `
                        <div class="pdf-controls">
                            <button id="generatePdfBtn" class="btn btn-primary">
                                <i class="bi bi-file-pdf"></i> Scarica analisi in PDF
                            </button>
                            <span id="pdfGenerationStatus" style="display:none;"></span>
                        </div>
                    `;

                        summaryText.innerHTML = html;

                        // Aggiungi event listener per il download
                        const generatePdfBtn = document.getElementById('generatePdfBtn');
                        if (generatePdfBtn) {
                            generatePdfBtn.addEventListener('click', generateAndDownloadPdf);
                        }
                    } else {
                        summaryText.innerHTML = "<p>Non è stato possibile generare un riassunto per questo film.</p>";
                    }
                    summaryText.style.display = 'block';
                    summaryText.classList.add('ai-summary-fade-in');
                }
            })
            .catch(error => {
                console.error("Errore nella generazione del riassunto:", error);

                if (summaryContent) {
                    summaryContent.querySelector('.ai-summary-loading').style.display = 'none';
                }

                if (summaryText) {
                    summaryText.innerHTML = `<div class="ai-error">
                    <i class="bi bi-exclamation-triangle"></i> 
                    Si è verificato un errore durante la generazione dell'analisi. 
                    Per favore riprova più tardi.
                </div>`;
                    summaryText.style.display = 'block';
                }
            });
    }
}

function generateAndDownloadPdf() {
    if (!summaryData) {
        alert('Errore: nessun dato disponibile per generare il PDF');
        return;
    }

    const pdfGenerationStatus = document.getElementById('pdfGenerationStatus');
    const generatePdfBtn = document.getElementById('generatePdfBtn');

    if (pdfGenerationStatus && generatePdfBtn) {
        generatePdfBtn.disabled = true;
        pdfGenerationStatus.textContent = 'Generazione PDF in corso...';
        pdfGenerationStatus.style.display = 'inline-block';
        pdfGenerationStatus.className = 'text-info';
    }

    fetch('/api/Summary/GenerateAndDownloadPdf', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({
            movieId: summaryData.movieId,
            summary: summaryData.summary,
            directors: summaryData.directors,
            cast: summaryData.cast,
            runtime: summaryData.runtime,
            rating: summaryData.rating,
            source: summaryData.source
        })
    })
        .then(response => {
            if (!response.ok) {
                throw new Error('Errore nella generazione del PDF');
            }
            return response.json();
        })
        .then(data => {
            if (data.success && data.downloadUrl) {
                if (pdfGenerationStatus) {
                    pdfGenerationStatus.textContent = 'PDF generato con successo!';
                    pdfGenerationStatus.className = 'text-success';
                }

                // Crea un link per il download del file
                const downloadLink = document.createElement('a');
                downloadLink.href = data.downloadUrl;
                downloadLink.className = 'btn btn-success mt-2';
                downloadLink.innerHTML = '<i class="bi bi-download"></i> Scarica PDF';
                downloadLink.download = data.pdfFileName;

                // Aggiungi il link dopo il pulsante di generazione
                const pdfControls = document.querySelector('.pdf-controls');
                if (pdfControls) {
                    pdfControls.appendChild(downloadLink);
                }

                // Riabilita il pulsante
                if (generatePdfBtn) {
                    generatePdfBtn.disabled = false;
                    generatePdfBtn.textContent = 'Genera nuovo PDF';
                }
            } else {
                throw new Error('Risposta non valida dal server');
            }
        })
        .catch(error => {
            console.error('Errore:', error);
            if (pdfGenerationStatus) {
                pdfGenerationStatus.textContent = 'Errore nella generazione del PDF.';
                pdfGenerationStatus.className = 'text-danger';
            }

            if (generatePdfBtn) {
                generatePdfBtn.disabled = false;
            }
        });
}

// Variabile per memorizzare i dati del riassunto
let summaryData = null;

// Recupera l'ID del film dalla pagina
const movieId = document.getElementById('movieIdInput')?.value;

// Elementi DOM
const aiSummarySection = document.getElementById('aiSummarySection');
const summaryContent = document.getElementById('summaryContent');
const summaryText = document.getElementById('summaryText');
const generateSummaryBtn = document.getElementById('generateSummaryBtn');

// Aggiungi l'event listener al pulsante
if (generateSummaryBtn) {
    generateSummaryBtn.addEventListener('click', generateSummary);
}

// Controlla se ci sono analisi precedenti
function checkPreviousAnalyses() {
    if (!movieId) return;

    fetch(`/api/Summary/UserAnalyses?movieId=${movieId}`)
        .then(response => response.json())
        .then(data => {
            if (data.analyses && data.analyses.length > 0) {
                const previousAnalysesSection = document.getElementById('previousAnalysesSection');
                if (previousAnalysesSection) {
                    let html = '<h4>Analisi precedenti</h4><ul class="list-group">';

                    data.analyses.forEach(analysis => {
                        const date = new Date(analysis.createdAt).toLocaleString();
                        html += `
                            <li class="list-group-item d-flex justify-content-between align-items-center">
                                Analisi del ${date}
                                <a href="/api/Summary/DownloadPdf?fileName=${analysis.pdfPath}" 
                                   class="btn btn-sm btn-outline-primary" download>
                                   <i class="bi bi-download"></i> Scarica PDF
                                </a>
                            </li>
                        `;
                    });

                    html += '</ul>';
                    previousAnalysesSection.innerHTML = html;
                    previousAnalysesSection.style.display = 'block';
                }
            }
        })
        .catch(error => console.error('Errore nel recupero delle analisi precedenti:', error));
}

// Esegui la verifica delle analisi precedenti quando la pagina è caricata
document.addEventListener('DOMContentLoaded', checkPreviousAnalyses);