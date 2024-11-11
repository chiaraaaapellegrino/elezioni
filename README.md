# Applicazione di Esempio per Votazioni

Un'applicazione distribuita semplice che viene eseguita attraverso multipli container Docker.

## Introduzione a Docker
Docker è una piattaforma che permette di eseguire applicazioni in ambienti isolati chiamati "container". I container sono leggeri, veloci da avviare e garantiscono che l'applicazione funzioni allo stesso modo su qualsiasi sistema.

### Concetti Chiave:
- **Container**: Ambiente isolato che contiene tutto il necessario per eseguire un'applicazione
- **Immagine Docker**: Template da cui vengono creati i container
- **Docker Compose**: Strumento per gestire applicazioni multi-container
- **Volume**: Spazio di archiviazione persistente per i dati dei container

## Per Iniziare

1. Scarica [Docker Desktop](https://www.docker.com/products/docker-desktop) per Mac o Windows. 
   - Docker Desktop include già Docker Compose
   - Per gli utenti Windows, assicuratevi di avere WSL2 installato
   - Per gli utenti Mac, sia Intel che Apple Silicon sono supportati

2. Per Linux, installate l'ultima versione di [Docker Compose](https://docs.docker.com/compose/install/)
   - Seguite la guida ufficiale per la vostra distribuzione
   - Verificate l'installazione con `docker compose version`

## Tecnologie Utilizzate
Questa applicazione dimostrativa utilizza diverse tecnologie per mostrare l'interoperabilità di Docker:

- **Frontend (Python)**: Applicazione web per la votazione
- **Redis**: Database in-memory per la gestione temporanea dei voti
- **Worker (.NET)**: Elabora i voti
- **PostgreSQL**: Database per l'archiviazione permanente
- **Results (Node.js)**: Applicazione web per visualizzare i risultati

## Esecuzione dell'Applicazione

Nel directory del progetto, eseguite:

```shell
docker compose up
```

Questo comando:
1. Scarica tutte le immagini necessarie
2. Crea i container
3. Avvia l'applicazione

Potete accedere a:
- Applicazione di voto: [http://localhost:8080](http://localhost:8080)
- Risultati: [http://localhost:8081](http://localhost:8081)

Per fermare l'applicazione, premete `Ctrl+C` o eseguite:
```shell
docker compose down
```

## Architettura

![Architecture diagram](architecture.excalidraw.png)

L'applicazione è composta da:

1. **Frontend (Python)**
   - Interfaccia web per votare tra due opzioni
   - Porta 8080

2. **Redis**
   - Database temporaneo per i nuovi voti
   - Comunica internamente con worker e frontend

3. **Worker (.NET)**
   - Elabora i voti da Redis
   - Li salva nel database PostgreSQL

4. **Database (PostgreSQL)**
   - Archiviazione permanente dei voti
   - Utilizza un volume Docker per persistenza

5. **Results (Node.js)**
   - Visualizza i risultati in tempo reale
   - Porta 8081

## Note Importanti

- L'applicazione accetta un solo voto per browser
- I voti successivi dallo stesso client vengono ignorati
- Questa non è un'applicazione perfettamente progettata, ma serve come esempio didattico
- Mostra l'integrazione di diversi linguaggi e tecnologie in un ambiente Docker

## Comandi Docker Utili per Principianti

```shell
# Visualizza i container in esecuzione
docker ps

# Visualizza i log di un container
docker logs [nome-container]

# Ferma e rimuove tutti i container
docker compose down

# Ricostruisce le immagini senza cache
docker compose build --no-cache

# Visualizza le risorse utilizzate
docker stats
```

## Risoluzione Problemi Comuni

1. **Porte occupate**: Se le porte 8080 o 8081 sono già in uso, modificate il file docker-compose.yml
2. **Problemi di memoria**: Assicuratevi di avere allocato abbastanza memoria in Docker Desktop
3. **Database non raggiungibile**: Verificate che il volume PostgreSQL sia stato creato correttamente