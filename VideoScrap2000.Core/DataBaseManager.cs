using Microsoft.Data.Sqlite;

using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace MovieScrap2000
{
  public class DatabaseManager
  {
    private readonly string _databasePath;
    private readonly string _connectionString;
    private static readonly SemaphoreSlim AsyncDbLock = new SemaphoreSlim(1, 1);
    private const int SchemaVersion = 1; // Für zukünftige Datenbank-Upgrades

    // Konstruktor: Definiert den Pfad und den Connection String
    public DatabaseManager(string dbFileName = "MovieScrap2000_Cache.sqlite")
    {
      // Speichere die Datenbankdatei im Anwendungsordner
      _databasePath = Path.Combine(Utils.GetOptionsPath(), dbFileName);

      // Connection String für SQLite
      _connectionString = new SqliteConnectionStringBuilder { DataSource = _databasePath, ForeignKeys = true }.ToString();
    }

    // Öffentliche Methode zum Initialisieren der Datenbank
    public async Task InitializeDatabaseAsync()
    {
      string creationSql = GetCreationSQL();
      await ExecuteAsync(async connection =>
      {
        using (var command = connection.CreateCommand())
        {
          command.CommandText = creationSql;
          DatabaseManager.TraceSql(command, true);
          await command.ExecuteNonQueryAsync();
        }
      });
    }

    /// <summary>
    /// Führt eine DB-Operation ohne Rückgabe aus (z.B. INSERT, UPDATE, DELETE).
    /// </summary>
    /// <param name="action">Der Code, der mit der geöffneten Verbindung ausgeführt werden soll.</param>
    /// <returns>Task</returns>
    public async Task ExecuteAsync(Func<SqliteConnection, Task> action)
    {
      await AsyncDbLock.WaitAsync(); // Wartet asynchron auf das Lock

      try
      {
        using (var connection = new SqliteConnection(_connectionString))
        {
          await connection.OpenAsync();
          await action(connection);
        }
      }
      finally
      {
        AsyncDbLock.Release(); // Gibt das Lock frei
      }
    }

    /// <summary>
    /// Führt eine komplexe DB-Logik mit Rückgabe aus. Verwendung: Transaktionen oder komplexes 
    /// Lesen/Schreiben, das ein Ergebnis (z.B. eine Tupel oder ein Detailobjekt) liefert.
    /// </summary>
    /// <typeparam name="T">Der Typ</typeparam>
    /// <param name="executionLogic">Der Code, der mit der geöffneten Verbindung ausgeführt werden soll.</param>
    /// <returns>Task<typeparamref name="T"/></returns>
    public async Task<T> ExecuteAsync<T>(Func<SqliteConnection, Task<T>> executionLogic)
    {
      await AsyncDbLock.WaitAsync(); // Erwerbe asynchron das Lock

      try
      {
        using (var connection = new SqliteConnection(_connectionString))
        {
          await connection.OpenAsync();

          // Führe die gesamte komplexe Abruflotik aus (beide Abfragen)
          return await executionLogic(connection);
        }
      }
      finally
      {
        AsyncDbLock.Release(); // Gib das Lock frei
      }
    }

    /// <summary>
    /// Spezialisiert auf Operationen, die einen einzelnen Wert zurückgeben (Skalar). Abfragen wie COUNT, MAX, 
    /// oder das Abrufen einer einzelnen ID.
    /// </summary>
    /// <typeparam name="T">Der Typ</typeparam>
    /// <param name="query">Der Code, der mit der geöffneten Verbindung ausgeführt werden soll.</param>
    /// <returns>Task<typeparamref name="T"/></returns>
    public async Task<T> ExecuteScalarAsync<T>(Func<SqliteConnection, Task<T>> query)
    {
      await AsyncDbLock.WaitAsync(); // Warte auf das asynchrone Lock

      try
      {
        using (var connection = new SqliteConnection(_connectionString))
        {
          await connection.OpenAsync();

          // Führe die externe Logik aus (ExecuteScalarAsync) und warte darauf
          return await query(connection);
        }
      }
      finally
      {
        AsyncDbLock.Release(); // Gib das Lock frei
      }
    }

    /// <summary>
    /// Spezialisiert auf Operationen, die mehrere Datensätze zurückgeben. Abfragen, die Listen von Objekten 
    /// (List<Actor>, List<MovieViewModel>) füllen.
    /// </summary>
    /// <typeparam name="T">Der Typ</typeparam>
    /// <param name="queryAction">Der Code, der mit der geöffneten Verbindung ausgeführt werden soll.</param>
    /// <returns>Task<typeparamref name="T"/></returns>
    public async Task<T> ExecuteReaderAsync<T>(Func<SqliteConnection, Task<T>> queryAction)
    {
      await AsyncDbLock.WaitAsync();

      try
      {
        using (var connection = new SqliteConnection(_connectionString))
        {
          await connection.OpenAsync();

          // Führt die Logik aus, die den Reader erstellt und verarbeitet
          return await queryAction(connection);
        }
      }
      finally
      {
        AsyncDbLock.Release();
      }
    }

    /// <summary>
    /// Liefert den vollständigen SQL-String zur Erstellung aller Tabellen.
    /// </summary>
    private string GetCreationSQL()
    {
      return @"PRAGMA foreign_keys = ON;

-- I. Stammdaten-Tabellen (Lookup Tables)
CREATE TABLE IF NOT EXISTS Actor (
    Id INTEGER PRIMARY KEY,
    Name TEXT NOT NULL UNIQUE,
    TMDbId INTEGER,
    Thumb BLOB
);

CREATE TABLE IF NOT EXISTS Director (
    Id INTEGER PRIMARY KEY,
    Name TEXT NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS Studio (
    Id INTEGER PRIMARY KEY,
    Name TEXT NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS Genre (
    Id INTEGER PRIMARY KEY,
    Name TEXT NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS Country (
    Id INTEGER PRIMARY KEY,
    Name TEXT NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS Credit (
    Id INTEGER PRIMARY KEY,
    Name TEXT NOT NULL UNIQUE
);

-- II. Haupttabellen (Inhaltstypen)
CREATE TABLE IF NOT EXISTS Movie (
    Id INTEGER PRIMARY KEY,
    ImdbId TEXT UNIQUE,
    TMDbId INTEGER,
    Title TEXT NOT NULL,
    OriginalTitle TEXT,
    CollectionId INTEGER,
    CollectionName TEXT,
    Year TEXT,
    ReleaseDate TEXT,
    Mpaa TEXT,
    Plot TEXT,
    Outline TEXT,
    Tagline TEXT,
    Runtime INTEGER,
    Rating REAL,
    Votes INTEGER,
    FolderPath TEXT NOT NULL UNIQUE,
    VideoFilePath TEXT,
    ScrapStatus INTEGER NOT NULL DEFAULT 1,
    NfoModified INTEGER,
    DateAdded TEXT NOT NULL,
    DateModified TEXT NOT NULL,
    JsonMetadata TEXT
);

CREATE TABLE IF NOT EXISTS TvShow (
    Id INTEGER PRIMARY KEY,
    Title TEXT NOT NULL,
    Premiered TEXT,
    Status TEXT,
    Plot TEXT,
    FolderPath TEXT NOT NULL UNIQUE,
    NfoLastModified INTEGER,
    DateAdded TEXT NOT NULL,
    DateModified TEXT NOT NULL,
    TMDbId INTEGER,
    JsonMetadata TEXT
);

CREATE TABLE IF NOT EXISTS Episode (
    Id INTEGER PRIMARY KEY,
    TvShowId INTEGER NOT NULL,
    SeasonNumber INTEGER NOT NULL,
    EpisodeNumber INTEGER NOT NULL,
    Title TEXT NOT NULL,
    Aired TEXT,
    Plot TEXT,
    NfoPath TEXT NOT NULL UNIQUE,
    JsonMetadata TEXT,
    NfoLastModified INTEGER,
    DateAdded TEXT NOT NULL,
    DateModified TEXT NOT NULL,
    FOREIGN KEY (TvShowId) REFERENCES TvShow(Id) ON DELETE CASCADE
);

-- III. Verbindungstabellen (N:M Junction Tables)
CREATE TABLE IF NOT EXISTS MovieActor (
    MovieId INTEGER NOT NULL,
    ActorId INTEGER NOT NULL,
    Role TEXT,
    SortOrder INTEGER,
    PRIMARY KEY (MovieId, ActorId),
    FOREIGN KEY (MovieId) REFERENCES Movie(Id) ON DELETE CASCADE,
    FOREIGN KEY (ActorId) REFERENCES Actor(Id)
);

CREATE TABLE IF NOT EXISTS MovieDirector (
    MovieId INTEGER NOT NULL,
    DirectorId INTEGER NOT NULL,
    PRIMARY KEY (MovieId, DirectorId),
    FOREIGN KEY (MovieId) REFERENCES Movie(Id) ON DELETE CASCADE,
    FOREIGN KEY (DirectorId) REFERENCES Director(Id)
);

CREATE TABLE IF NOT EXISTS MovieStudio (
    MovieId INTEGER NOT NULL,
    StudioId INTEGER NOT NULL,
    PRIMARY KEY (MovieId, StudioId),
    FOREIGN KEY (MovieId) REFERENCES Movie(Id) ON DELETE CASCADE,
    FOREIGN KEY (StudioId) REFERENCES Studio(Id)
);

CREATE TABLE IF NOT EXISTS MovieGenre (
    MovieId INTEGER NOT NULL,
    GenreId INTEGER NOT NULL,
    PRIMARY KEY (MovieId, GenreId),
    FOREIGN KEY (MovieId) REFERENCES Movie(Id) ON DELETE CASCADE,
    FOREIGN KEY (GenreId) REFERENCES Genre(Id)
);

CREATE TABLE IF NOT EXISTS MovieCountry (
    MovieId INTEGER NOT NULL,
    CountryId INTEGER NOT NULL,
    PRIMARY KEY (MovieId, CountryId),
    FOREIGN KEY (MovieId) REFERENCES Movie(Id) ON DELETE CASCADE,
    FOREIGN KEY (CountryId) REFERENCES Country(Id)
);

CREATE TABLE IF NOT EXISTS MovieCredit (
    MovieId INTEGER NOT NULL,
    CreditId INTEGER NOT NULL,
    PRIMARY KEY (MovieId, CreditId),
    FOREIGN KEY (MovieId) REFERENCES Movie(Id) ON DELETE CASCADE,
    FOREIGN KEY (CreditId) REFERENCES Credit(Id)
);

-- V. Caching-Tabelle
CREATE TABLE IF NOT EXISTS ImageCache (
    Id INTEGER PRIMARY KEY,
    MovieId INTEGER,
    TvShowId INTEGER,
    EpisodeId INTEGER,
    Type TEXT NOT NULL,
    Data BLOB NOT NULL,
    FOREIGN KEY (MovieId) REFERENCES Movie(Id) ON DELETE CASCADE,
    FOREIGN KEY (TvShowId) REFERENCES TvShow(Id) ON DELETE CASCADE,
    FOREIGN KEY (EpisodeId) REFERENCES Episode(Id) ON DELETE CASCADE,
    CHECK (
        (MovieId IS NULL) +
        (TvShowId IS NULL) +
        (EpisodeId IS NULL) = 2
    )
);";
    }

    public static void TraceSql(SqliteCommand command, bool interpolate = false)
    {
      interpolate = false;
      var parameterList = new List<string>();
      var traceSql = command.CommandText;

      foreach (SqliteParameter p in command.Parameters)
      {
        string valueString;

        // Wertbehandlung für NULL und DBNull
        if (p.Value == DBNull.Value || p.Value == null)
        {
          valueString = "NULL";
        }
        // Behandelt Strings und DateTime (mit Anführungszeichen)
        else if (p.Value is string s)
        {
          // Doppeltes Anführungszeichen, um SQL-Syntax zu erhalten
          valueString = $"'{s.Replace("'", "''")}'";
        }
        else if (p.Value is DateTime dt)
        {
          // SQLite-freundliches Format für Datum/Zeit
          valueString = $"'{dt:yyyy-MM-dd HH:mm:ss}'";
        }
        // Behandelt Byte-Arrays (BLOBs)
        else if (p.Value is byte[] bytes)
        {
          valueString = $"BLOB[{bytes.Length} bytes]";
        }
        else
        {
          // Numerische oder andere skalare Werte
          valueString = p.Value.ToString() ?? "NULL";
        }

        parameterList.Add($"{p.ParameterName}={valueString}");

        // Führt die Ersetzung im SQL-String durch (nur für den Trace!)
        if (interpolate)
        {
          // Ersetzt NUR, wenn der Platzhalter als ganzes Wort vorkommt, um '@TMDbId' nicht in '@TM' zu zerlegen.
          traceSql = traceSql.Replace(p.ParameterName, valueString);
        }
      }

      if (interpolate)
      {
        Trace.WriteLine($"{traceSql}");
      }
      else
      {
        Trace.WriteLine($"SQL: {command.CommandText} | Params: {string.Join(", ", parameterList)}");
      }
    }
  }
}
