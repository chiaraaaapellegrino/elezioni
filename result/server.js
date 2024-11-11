// Importazione delle dipendenze necessarie
var express = require('express'),        // Framework web
    async = require('async'),            // Per gestione operazioni asincrone
    { Pool } = require('pg'),            // Client PostgreSQL
    cookieParser = require('cookie-parser'), // Per gestione cookies
    app = express(),                     // Istanza Express
    server = require('http').Server(app), // Server HTTP
    io = require('socket.io')(server);   // WebSocket per aggiornamenti real-time

// Configurazione della porta (da variabile d'ambiente o default 4000)
var port = process.env.PORT || 4000;

// Configurazione WebSocket
io.on('connection', function (socket) {
    // Invia messaggio di benvenuto quando un client si connette
    socket.emit('message', { text : 'Welcome!' });

    // Gestisce la sottoscrizione ai canali
    socket.on('subscribe', function (data) {
        socket.join(data.channel);
    });
});

// Configurazione del pool di connessioni PostgreSQL
var pool = new Pool({
    connectionString: 'postgres://postgres:postgres@db/postgres'
});

// Tenta di connettersi al database con retry
// Riprova fino a 1000 volte con intervallo di 1 secondo
async.retry(
    {times: 1000, interval: 1000},
    function(callback) {
        pool.connect(function(err, client, done) {
            if (err) {
                console.error("Waiting for db");
            }
            callback(err, client);
        });
    },
    function(err, client) {
        if (err) {
            return console.error("Giving up");
        }
        console.log("Connected to db");
        getVotes(client);
    }
);

// Funzione per ottenere i voti dal database
function getVotes(client) {
    // Query per contare i voti raggruppati per opzione
    client.query('SELECT vote, COUNT(id) AS count FROM votes GROUP BY vote', [], function(err, result) {
        if (err) {
            console.error("Error performing query: " + err);
        } else {
            // Elabora i risultati e li invia ai client connessi
            var votes = collectVotesFromResult(result);
            io.sockets.emit("scores", JSON.stringify(votes));
        }

        // Polling: ripete la query ogni secondo
        setTimeout(function() {getVotes(client) }, 1000);
    });
}

// Funzione per elaborare i risultati della query
function collectVotesFromResult(result) {
    // Inizializza oggetto con conteggio voti
    var votes = {a: 0, b: 0};

    // Aggiorna i conteggi con i risultati del database
    result.rows.forEach(function (row) {
        votes[row.vote] = parseInt(row.count);
    });

    return votes;
}

// Configurazione middleware Express
app.use(cookieParser());                    // Per gestire i cookie
app.use(express.urlencoded());              // Per parsing dati form
app.use(express.static(__dirname + '/views')); // Serve file statici

// Route principale che serve la pagina HTML
app.get('/', function (req, res) {
    res.sendFile(path.resolve(__dirname + '/views/index.html'));
});

// Avvia il server sulla porta configurata
server.listen(port, function () {
    var port = server.address().port;
    console.log('App running on port ' + port);
});