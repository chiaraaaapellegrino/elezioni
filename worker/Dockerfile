# Dockerfile multi-stage per l'applicazione .NET Worker
# Usa build multi-platform per supportare diverse architetture (AMD64, ARM64, etc.)

# --- STAGE 1: Build Stage ---
# Usa l'immagine SDK di .NET 7.0 per compilare l'applicazione
# --platform=${BUILDPLATFORM} assicura che l'immagine di build sia compatibile con la macchina host
FROM --platform=${BUILDPLATFORM} mcr.microsoft.com/dotnet/sdk:7.0 AS build

# Definisce gli argomenti per il cross-platform building
ARG TARGETPLATFORM  # Piattaforma target (es. linux/amd64, linux/arm64)
ARG TARGETARCH     # Architettura target (es. amd64, arm64)
ARG BUILDPLATFORM  # Piattaforma di build

# Log informativo per debug del processo di build
RUN echo "I am running on $BUILDPLATFORM, building for $TARGETPLATFORM"

# Imposta la directory di lavoro per il build
WORKDIR /source

# Copia solo il file di progetto e ripristina le dipendenze
# Questo step viene cachato se il .csproj non cambia
COPY *.csproj .
RUN dotnet restore -a $TARGETARCH

# Copia il resto del codice sorgente
COPY . .

# Compila l'applicazione
# -c release: build in modalità release
# -o /app: output nella directory /app
# -a $TARGETARCH: specifica l'architettura target
# --self-contained false: usa il runtime installato nel container
# --no-restore: salta il restore perché già fatto
RUN dotnet publish -c release -o /app -a $TARGETARCH --self-contained false --no-restore

# --- STAGE 2: Runtime Stage ---
# Usa l'immagine runtime di .NET 7.0 (più leggera dell'SDK)
FROM mcr.microsoft.com/dotnet/runtime:7.0

# Imposta la directory di lavoro
WORKDIR /app

# Copia solo i file pubblicati dal build stage
COPY --from=build /app .

# Definisce il comando di avvio dell'applicazione
ENTRYPOINT ["dotnet", "Worker.dll"]