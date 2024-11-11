# Dockerfile per l'applicazione di voto in Python
# Questo file usa il pattern multi-stage build per ottimizzare l'immagine finale

# --- STAGE 1: Immagine Base ---
# Usa Python 3.11 slim come immagine base (versione ridotta per dimensioni minori)
FROM python:3.11-slim AS base

# Installa curl per gli healthcheck
# --no-install-recommends riduce la dimensione dell'immagine
# Rimuove la cache di apt per ridurre ulteriormente la dimensione
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/*

# Imposta la directory di lavoro dell'applicazione
WORKDIR /usr/local/app

# Copia e installa le dipendenze Python
# Copiando solo requirements.txt prima, sfruttiamo la cache di Docker
COPY requirements.txt ./requirements.txt
# --no-cache-dir riduce la dimensione dell'immagine finale
RUN pip install --no-cache-dir -r requirements.txt

# --- STAGE 2: Ambiente di Sviluppo ---
# Crea uno stage specifico per lo sviluppo
FROM base AS dev
# Installa watchdog per il reload automatico in sviluppo
RUN pip install watchdog
# Imposta l'ambiente Flask in modalità development
ENV FLASK_ENV=development
# Avvia l'app in modalità debug
CMD ["python", "app.py"]

# --- STAGE 3: Immagine Produzione ---
# Crea l'immagine finale ottimizzata per la produzione
FROM base AS final

# Copia tutto il codice dell'applicazione nella working directory
COPY . .

# Dichiara che il container esporrà la porta 80
EXPOSE 80

# Comando per avviare l'applicazione in produzione usando gunicorn
# gunicorn è un server WSGI production-ready per Python
CMD ["gunicorn", 
     "app:app",                # Nome dell'applicazione da eseguire
     "-b", "0.0.0.0:80",      # Bind all'indirizzo e porta
     "--log-file", "-",       # Log su stdout
     "--access-logfile", "-", # Access log su stdout
     "--workers", "4",        # Numero di worker processes
     "--keep-alive", "0"      # Disabilita keepalive
    ]