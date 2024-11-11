// Importazione delle librerie necessarie
using System;
using System.Data.Common;              // Per operazioni database generiche
using System.Linq;                     // Per operazioni LINQ
using System.Net;                      // Per operazioni di rete
using System.Net.Sockets;             // Per gestione socket
using System.Threading;                // Per operazioni asincrone
using Newtonsoft.Json;                // Per serializzazione JSON
using Npgsql;                         // Client PostgreSQL
using StackExchange.Redis;            // Client Redis

namespace Worker
{
    public class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                // Inizializzazione connessioni database
                var pgsql = OpenDbConnection("Server=db;Username=postgres;Password=postgres;");
                var redisConn = OpenRedisConnection("redis");
                var redis = redisConn.GetDatabase();

                // Comando keep-alive per PostgreSQL
                // Workaround per mancanza di implementazione keep-alive in Npgsql
                var keepAliveCommand = pgsql.CreateCommand();
                keepAliveCommand.CommandText = "SELECT 1";

                // Definizione struttura del voto
                var definition = new { vote = "", voter_id = "" };

                // Loop principale dell'applicazione
                while (true)
                {
                    // Pausa per evitare sovraccarico CPU
                    Thread.Sleep(100);

                    // Gestione riconnessione Redis
                    if (redisConn == null || !redisConn.IsConnected) {
                        Console.WriteLine("Reconnecting Redis");
                        redisConn = OpenRedisConnection("redis");
                        redis = redisConn.GetDatabase();
                    }

                    // Preleva un voto dalla coda Redis
                    string json = redis.ListLeftPopAsync("votes").Result;
                    if (json != null)
                    {
                        // Deserializza il voto
                        var vote = JsonConvert.DeserializeAnonymousType(json, definition);
                        Console.WriteLine($"Processing vote for '{vote.vote}' by '{vote.voter_id}'");

                        // Gestione riconnessione PostgreSQL
                        if (!pgsql.State.Equals(System.Data.ConnectionState.Open))
                        {
                            Console.WriteLine("Reconnecting DB");
                            pgsql = OpenDbConnection("Server=db;Username=postgres;Password=postgres;");
                        }
                        else
                        {
                            // Aggiorna il voto nel database
                            UpdateVote(pgsql, vote.voter_id, vote.vote);
                        }
                    }
                    else
                    {
                        // Esegue keep-alive se non ci sono voti da processare
                        keepAliveCommand.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                return 1;
            }
        }

        // Funzione per aprire e inizializzare la connessione PostgreSQL
        private static NpgsqlConnection OpenDbConnection(string connectionString)
        {
            NpgsqlConnection connection;

            // Tentativo di connessione con retry
            while (true)
            {
                try
                {
                    connection = new NpgsqlConnection(connectionString);
                    connection.Open();
                    break;
                }
                catch (SocketException)
                {
                    Console.Error.WriteLine("Waiting for db");
                    Thread.Sleep(1000);
                }
                catch (DbException)
                {
                    Console.Error.WriteLine("Waiting for db");
                    Thread.Sleep(1000);
                }
            }

            Console.Error.WriteLine("Connected to db");

            // Creazione della tabella votes se non esiste
            var command = connection.CreateCommand();
            command.CommandText = @"CREATE TABLE IF NOT EXISTS votes (
                                        id VARCHAR(255) NOT NULL UNIQUE,
                                        vote VARCHAR(255) NOT NULL
                                    )";
            command.ExecuteNonQuery();

            return connection;
        }

        // Funzione per aprire la connessione Redis
        private static ConnectionMultiplexer OpenRedisConnection(string hostname)
        {
            // Risolve l'IP per evitare problemi noti con StackExchange.Redis
            var ipAddress = GetIp(hostname);
            Console.WriteLine($"Found redis at {ipAddress}");

            // Tentativo di connessione con retry
            while (true)
            {
                try
                {
                    Console.Error.WriteLine("Connecting to redis");
                    return ConnectionMultiplexer.Connect(ipAddress);
                }
                catch (RedisConnectionException)
                {
                    Console.Error.WriteLine("Waiting for redis");
                    Thread.Sleep(1000);
                }
            }
        }

        // Funzione per risolvere l'hostname in IP
        private static string GetIp(string hostname)
            => Dns.GetHostEntryAsync(hostname)
                .Result
                .AddressList
                .First(a => a.AddressFamily == AddressFamily.InterNetwork)
                .ToString();

        // Funzione per aggiornare o inserire un voto
        private static void UpdateVote(NpgsqlConnection connection, string voterId, string vote)
        {
            var command = connection.CreateCommand();
            try
            {
                // Tenta prima l'inserimento
                command.CommandText = "INSERT INTO votes (id, vote) VALUES (@id, @vote)";
                command.Parameters.AddWithValue("@id", voterId);
                command.Parameters.AddWithValue("@vote", vote);
                command.ExecuteNonQuery();
            }
            catch (DbException)
            {
                // Se fallisce (votante esistente), aggiorna il voto
                command.CommandText = "UPDATE votes SET vote = @vote WHERE id = @id";
                command.ExecuteNonQuery();
            }
            finally
            {
                command.Dispose();
            }
        }
    }
}