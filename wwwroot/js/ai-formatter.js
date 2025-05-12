/**
 * Formatta intelligentemente il testo generato dall'AI
 */
function enhanceAiText(text) {
    if (!text) return "";

    // Dividi il testo in sezioni/paragrafi
    let paragraphs = text.split(/\n\n+/);
    let formattedHtml = '';

    // Processa ogni paragrafo
    paragraphs.forEach((paragraph, index) => {
        paragraph = paragraph.trim();

        // Salta paragrafi vuoti
        if (!paragraph) return;

        // Rilevamento dei titoli delle sezioni (tutto maiuscolo o che termina con :)
        if ((paragraph.toUpperCase() === paragraph && paragraph.length < 80) ||
            /^[A-Z\s\d:"']+:$/.test(paragraph) ||
            paragraph.startsWith('#')) {

            // Formatta come titolo di sezione
            let title = paragraph.replace(/^#+\s*/, '').replace(/:$/, '').trim();
            formattedHtml += `<h3>${title}</h3>`;
        }
        // Se è un elenco puntato (inizia con - o *)
        else if (paragraph.includes('\n- ') || paragraph.includes('\n* ')) {
            let lines = paragraph.split('\n');
            let introText = lines[0];
            let listItems = lines.slice(1).filter(line => line.startsWith('- ') || line.startsWith('* '));

            // Aggiungi testo introduttivo se presente
            if (introText.trim() && !introText.startsWith('- ') && !introText.startsWith('* ')) {
                formattedHtml += `<p>${enhanceParagraphText(introText)}</p>`;
            }

            // Inizia lista
            formattedHtml += '<ul style="padding-left: 20px; margin-bottom: 20px;">';

            // Aggiungi elementi lista
            listItems.forEach(item => {
                let itemText = item.replace(/^[*-]\s+/, '').trim();
                if (itemText) {
                    formattedHtml += `<li style="margin-bottom: 8px;">${enhanceParagraphText(itemText)}</li>`;
                }
            });

            // Chiudi lista
            formattedHtml += '</ul>';
        }
        // Potrebbe essere una citazione
        else if (paragraph.startsWith('>')) {
            formattedHtml += `<blockquote style="border-left: 4px solid #8E54E9; padding-left: 15px; font-style: italic; margin: 20px 0; color: #c2c2f0;">
                ${enhanceParagraphText(paragraph.substring(1).trim())}
            </blockquote>`;
        }
        // Paragrafo normale
        else {
            formattedHtml += `<p>${enhanceParagraphText(paragraph)}</p>`;
        }
    });

    return formattedHtml;
}

/**
 * Formatta il testo all'interno di un paragrafo
 */
function enhanceParagraphText(text) {
    return text
        // Enfatizza titoli film tra virgolette
        .replace(/"([^"]+)"/g, '<span style="color: #c2c2f0;">"$1"</span>')
        // Converti ** in grassetto
        .replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>')
        // Converti * in corsivo
        .replace(/\*([^*]+)\*/g, '<em>$1</em>');
}

/**
 * Funzione aggiornata per generare il riassunto
 */
function generateSummary() {
    if (aiSummarySection) {
        aiSummarySection.style.display = 'block';
        aiSummarySection.classList.add('ai-summary-slide-in');

        if (summaryContent) {
            summaryContent.querySelector('.ai-summary-loading').style.display = 'flex';
            summaryText.style.display = 'none';

            // Nascondi le azioni
            const summaryActions = document.getElementById('summaryActions');
            if (summaryActions) summaryActions.style.display = 'none';
        }

        // Smooth scroll to the section
        aiSummarySection.scrollIntoView({ behavior: 'smooth', block: 'start' });

        fetch(`/api/Summary/Generate?movieId=@Model.Movie.Id`, {
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
                        // Salva i dati per il PDF
                        window.summaryData = data;

                        // Genera HTML migliorato per il testo
                        let enhancedHtml = `<div class="detailed-summary">${enhanceAiText(data.summary)}</div>`;

                        // Aggiungi metadati se disponibili
                        let metadataHtml = '';
                        if (data.directors && data.directors.length > 0 ||
                            data.cast && data.cast.length > 0 ||
                            data.runtime) {

                            metadataHtml = `<div class="summary-metadata">`;

                            if (data.directors && data.directors.length > 0) {
                                metadataHtml += `
                                <div class="metadata-item">
                                    <span class="metadata-label">Regia:</span>
                                    <span class="metadata-value">${data.directors.join(', ')}</span>
                                </div>
                            `;
                            }

                            if (data.cast && data.cast.length > 0) {
                                metadataHtml += `
                                <div class="metadata-item">
                                    <span class="metadata-label">Cast principale:</span>
                                    <span class="metadata-value">${data.cast.join(', ')}</span>
                                </div>
                            `;
                            }

                            if (data.runtime) {
                                metadataHtml += `
                                <div class="metadata-item">
                                    <span class="metadata-label">Durata:</span>
                                    <span class="metadata-value">${data.runtime} minuti</span>
                                </div>
                            `;
                            }

                            metadataHtml += `</div>`;
                        }

                        // Completa il contenuto HTML e impostalo
                        summaryText.innerHTML = enhancedHtml + metadataHtml;

                        // Mostra le azioni
                        const summaryActions = document.getElementById('summaryActions');
                        if (summaryActions) {
                            summaryActions.style.display = 'flex';

                            // Aggiungi gestori eventi ai pulsanti
                            const downloadPdfBtn = document.getElementById('downloadPdfBtn');
                            const shareAnalysisBtn = document.getElementById('shareAnalysisBtn');

                            if (downloadPdfBtn) {
                                downloadPdfBtn.addEventListener('click', function () {
                                    // Implementa la funzione di download PDF
                                    alert('Funzionalità di download PDF in implementazione');
                                });
                            }

                            if (shareAnalysisBtn) {
                                shareAnalysisBtn.addEventListener('click', function () {
                                    // Implementa la condivisione
                                    if (navigator.share) {
                                        navigator.share({
                                            title: 'Analisi AI di ' + '@Model.Movie.Title',
                                            text: 'Guarda questa interessante analisi AI di ' + '@Model.Movie.Title',
                                            url: window.location.href,
                                        })
                                            .catch((error) => console.log('Errore nella condivisione', error));
                                    } else {
                                        alert('La condivisione non è supportata dal tuo browser');
                                    }
                                });
                            }
                        }
                    } else {
                        summaryText.innerHTML = `<div class="ai-error">
                        <i class="bi bi-exclamation-triangle"></i>
                        <div>
                            <p style="margin-bottom: 8px; font-weight: bold;">Impossibile generare l'analisi</p>
                            <p style="margin: 0;">Non è stato possibile produrre un'analisi per questo film. Riprova più tardi.</p>
                        </div>
                    </div>`;
                    }

                    summaryText.style.display = 'block';
                    summaryText.classList.add('ai-summary-fade-in');
                }
            })
            .catch(error => {
                console.error("Errore nella generazione dell'analisi:", error);

                if (summaryContent) {
                    summaryContent.querySelector('.ai-summary-loading').style.display = 'none';
                }

                if (summaryText) {
                    summaryText.innerHTML = `<div class="ai-error">
                    <i class="bi bi-exclamation-triangle"></i>
                    <div>
                        <p style="margin-bottom: 8px; font-weight: bold;">Si è verificato un errore</p>
                        <p style="margin: 0;">Non è stato possibile completare l'analisi. Per favore riprova più tardi.</p>
                    </div>
                </div>`;
                    summaryText.style.display = 'block';
                }
            });
    }
}