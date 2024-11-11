# Dockerfile per l'applicazione Node.js dei risultati
# Questo file configura l'ambiente per l'applicazione che mostra i risultati delle votazioni

# Usa Node.js 18 slim come immagine base
# slim è una versione ridotta che contiene solo i componenti essenziali
FROM node:18-slim

# Installa le dipendenze necessarie:
# - curl: per gli healthcheck
# - tini: per una corretta gestione dei segnali e dei processi zombie
# --no-install-recommends riduce la dimensione dell'immagine
# Pulizia della cache di apt per ridurre la dimensione dell'immagine
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl tini && \
    rm -rf /var/lib/apt/lists/*

# Imposta la directory di lavoro dell'applicazione
WORKDIR /usr/local/app

# Installa nodemon globalmente
# nodemon è utile in sviluppo per il reload automatico quando i file cambiano
RUN npm install -g nodemon

# Copia i file di configurazione delle dipendenze
# Li copiamo prima del resto del codice per sfruttare la cache di Docker
COPY package*.json ./

# Installa le dipendenze:
# - npm ci è più veloce e sicuro di npm install in produzione
# - Pulisce la cache di npm
# - Sposta node_modules in una location globale per ottimizzare lo spazio
RUN npm ci && \
    npm cache clean --force && \
    mv /usr/local/app/node_modules /node_modules

# Copia il resto del codice dell'applicazione
COPY . .

# Configura la porta dell'applicazione
ENV PORT=80
EXPOSE 80

# Usa tini come entry point per una corretta gestione dei segnali
# -- separa gli argomenti di tini da quelli del comando
ENTRYPOINT ["/usr/bin/tini", "--"]
# Avvia l'applicazione Node.js
CMD ["node", "server.js"]