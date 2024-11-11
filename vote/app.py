# Applicazione Flask per il sistema di votazione
# Importazione delle librerie necessarie
from flask import Flask, render_template, request, make_response, g  # Framework web
from redis import Redis  # Database per memorizzare i voti
import os        # Per variabili d'ambiente
import socket    # Per ottenere l'hostname
import random    # Per generare ID votanti
import json      # Per serializzazione dati
import logging   # Per logging applicativo

# Configurazione delle opzioni di voto dalle variabili d'ambiente
# Se non specificate, usa valori di default
option_a = os.getenv('OPTION_A', "Cats")  # Prima opzione
option_b = os.getenv('OPTION_B', "Dogs")  # Seconda opzione
hostname = socket.gethostname()            # Nome del container

# Inizializzazione dell'applicazione Flask
app = Flask(__name__)

# Configurazione del logging
# Usa il logger di gunicorn per uniformità nei log
gunicorn_error_logger = logging.getLogger('gunicorn.error')
app.logger.handlers.extend(gunicorn_error_logger.handlers)
app.logger.setLevel(logging.INFO)

def get_redis():
    """
    Funzione per ottenere una connessione Redis
    Usa Flask.g per memorizzare la connessione durante la richiesta
    """
    if not hasattr(g, 'redis'):
        # Crea una nuova connessione Redis se non esiste
        g.redis = Redis(host="redis", db=0, socket_timeout=5)
    return g.redis

@app.route("/", methods=['POST','GET'])
def hello():
    """
    Route principale dell'applicazione
    Gestisce sia GET (visualizzazione) che POST (votazione)
    """
    # Ottiene o crea un ID univoco per il votante
    voter_id = request.cookies.get('voter_id')
    if not voter_id:
        voter_id = hex(random.getrandbits(64))[2:-1]

    vote = None

    # Gestione del voto (richiesta POST)
    if request.method == 'POST':
        redis = get_redis()
        vote = request.form['vote']
        app.logger.info('Received vote for %s', vote)
        # Salva il voto in Redis come JSON
        data = json.dumps({'voter_id': voter_id, 'vote': vote})
        redis.rpush('votes', data)

    # Renderizza il template con i dati necessari
    resp = make_response(render_template(
        'index.html',
        option_a=option_a,
        option_b=option_b,
        hostname=hostname,
        vote=vote,
    ))
    # Imposta il cookie con l'ID del votante
    resp.set_cookie('voter_id', voter_id)
    return resp

# Avvio dell'applicazione in modalità debug se eseguita direttamente
if __name__ == "__main__":
    app.run(
        host='0.0.0.0',    # Accetta connessioni da qualsiasi indirizzo
        port=80,           # Porta 80 (HTTP)
        debug=True,        # Modalità debug attiva
        threaded=True      # Supporto multi-thread
    )