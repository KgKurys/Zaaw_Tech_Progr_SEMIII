/*
 * Zadanie: System zarządzania bazami danych z wzorcami projektowymi
 * 
 * Zaimplementowane wzorce:
 * 1. SINGLETON - ConnectionManager
 *    - Tylko jedna instancja menadżera połączeń w całym systemie
 *    - Thread-safe implementacja (double-checked locking)
 * 
 * 2. MULTITON - Database
 *    - Każda baza jest identyfikowana po nazwie
 *    - Dla tej samej nazwy zawsze zwracana jest ta sama instancja
 *    - Różne nazwy tworzą różne instancje baz danych
 * 
 * 3. OBJECT POOL - ConnectionPool
 *    - Ogranicza liczbę równoczesnych połączeń do bazy (max 3)
 *    - Po osiągnięciu limitu, połączenia są ponownie wykorzystywane cyklicznie
 *    - Czwarte żądanie połączenia zwraca pierwsze połączenie
 * 
 * Architektura:
 * - Klient używa tylko ConnectionManager (singleton)
 * - ConnectionManager zarządza wieloma bazami danych (multiton)
 * - Każda baza ma swoją pulę połączeń (object pool)
 */

using System;
using System.Linq;

interface IDatabaseConnection {
    int AddRecord(string name, int age);

    void UpdateRecord(int id, string newName, int newAge);

    void DeleteRecord(int id);

    Record? GetRecord(int id);

    void ShowAllRecords();
}

// Prosty rekord w bazie danych
class Record {
    public int Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }

    public Record(int id, string name, int age){
        Id = id;
        Name = name;
        Age = age;
    }

    public override string ToString() {
        return $"Record [ID={Id}, Name={Name}, Age={Age}]";
    }
}

// Pula połączeń (Object Pool Pattern)
class ConnectionPool {
    private readonly Database database;
    private readonly List<IDatabaseConnection> connections;
    private readonly int maxConnections;
    private int currentConnectionIndex = 0;
    private readonly object lockObject = new();

    public ConnectionPool(Database db, int maxConnections) {
        this.database = db;
        this.maxConnections = maxConnections;
        this.connections = new List<IDatabaseConnection>();
        Console.WriteLine($"[ConnectionPool] Created connection pool for {db.Name} with max {maxConnections} connections");
    }

    public IDatabaseConnection GetConnection() {
        lock (lockObject) {
            // Jeśli nie osiągnięto limitu, twórz nowe połączenie
            if (connections.Count < maxConnections) {
                var newConnection = new DatabaseConnection(database, connections.Count + 1);
                connections.Add(newConnection);
                Console.WriteLine($"[ConnectionPool] Created new connection #{connections.Count} for {database.Name}");
                return newConnection;
            }

            // W przeciwnym razie zwróć istniejące połączenie cyklicznie
            var connection = connections[currentConnectionIndex];
            Console.WriteLine($"[ConnectionPool] Reusing connection #{currentConnectionIndex + 1} for {database.Name}");
            currentConnectionIndex = (currentConnectionIndex + 1) % maxConnections;
            return connection;
        }
    }

    public int GetConnectionCount() => connections.Count;
}

// Interface menadżera połączeń
interface IConnectionManager {
    IDatabaseConnection GetConnection(string databaseName);
}

// Menadżer połączeń (Singleton Pattern)
class ConnectionManager : IConnectionManager {
    private static ConnectionManager? instance;
    private static readonly object lockObject = new();

    private ConnectionManager() {
        Console.WriteLine("[ConnectionManager] ConnectionManager instance created");
    }

    public static ConnectionManager GetInstance() {
        if (instance == null) {
            lock (lockObject) {
                if (instance == null) {
                    instance = new ConnectionManager();
                }
            }
        }
        return instance;
    }

    public IDatabaseConnection GetConnection(string databaseName) {
        Console.WriteLine($"[ConnectionManager] Requesting connection to database: {databaseName}");
        Database database = Database.GetInstance(databaseName);
        return database.GetConnection();
    }
}

// Prosta baza danych z wzorcem Multiton
class Database {

    private readonly List<Record> records; // Lista przechowująca rekordy
    private int nextId = 1; // Licznik do generowania unikalnych ID
    private readonly string databaseName;
    private readonly ConnectionPool connectionPool; // Pula połączeń

    // Statyczny słownik przechowujący instancje baz danych (Multiton)
    private static readonly Dictionary<string, Database> instances = new();
    private static readonly object lockObject = new();

    private Database(string name) {
        records = new();
        databaseName = name;
        connectionPool = new ConnectionPool(this, 3); // Maksymalnie 3 połączenia
        Console.WriteLine($"[Database] Created new database instance: {databaseName}");
    }

    // Metoda do uzyskania instancji bazy danych (Multiton)
    public static Database GetInstance(string name) {
        lock (lockObject) {
            if (!instances.ContainsKey(name)) {
                instances[name] = new Database(name);
            }
            return instances[name];
        }
    }

    public string Name => databaseName;

    // Zwraca implementację interfejsu DatabaseConnection z puli
    public IDatabaseConnection GetConnection() {
        return connectionPool.GetConnection();
    }

    // Metody pomocnicze dla DatabaseConnection
    internal void AddRecordInternal(Record record) {
        records.Add(record);
    }

    internal Record? GetRecordInternal(int id) {
        return records.Where(rec => rec.Id == id).FirstOrDefault();
    }

    internal void RemoveRecordInternal(Record record) {
        records.Remove(record);
    }

    internal List<Record> GetAllRecordsInternal() {
        return records;
    }

    internal int GetNextId() {
        return nextId++;
    }
}

// Implementacja połączenia do bazy danych
class DatabaseConnection : IDatabaseConnection {
    private readonly Database db;
    private readonly int connectionId;

    public DatabaseConnection(Database database, int id) {
        db = database;
        connectionId = id;
    }

    public int ConnectionId => connectionId;

    // Dodawanie nowego rekordu
    public int AddRecord(string name, int age) {
        int id = db.GetNextId();
        Record newRecord = new(id, name, age);
        db.AddRecordInternal(newRecord);
        Console.WriteLine($"[Connection #{connectionId}] Inserted: {newRecord}");
        return id; // zwracamy id dodanego rekordu
    }

    // Pobieranie rekordu po ID
    public Record? GetRecord(int id) {
        return db.GetRecordInternal(id);
    }

    // Aktualizowanie rekordu po ID
    public void UpdateRecord(int id, string newName, int newAge) {
        Record? optionalRecord = GetRecord(id);

        if (optionalRecord != null) {
            Record record = optionalRecord;
            record.Name = newName;
            record.Age = newAge;
            Console.WriteLine($"[Connection #{connectionId}] Updated: {record}");
        } else {
            Console.WriteLine($"[Connection #{connectionId}] Record with ID {id} not found.");
        }
    }

    // Usuwanie rekordu po ID
    public void DeleteRecord(int id) {
        Record? optionalRecord = GetRecord(id);

        if (optionalRecord != null) {
            db.RemoveRecordInternal(optionalRecord);
            Console.WriteLine($"[Connection #{connectionId}] Deleted record with ID {id}");
        } else {
            Console.WriteLine($"[Connection #{connectionId}] Record with ID {id} not found.");
        }
    }

    // Wyświetlanie wszystkich rekordów
    public void ShowAllRecords() {
        var allRecords = db.GetAllRecordsInternal();
        if (allRecords.Any()) {
            Console.WriteLine($"[Connection #{connectionId}] All records in {db.Name}:");
            allRecords.ForEach(r => Console.WriteLine($"  {r}"));
        } else {
            Console.WriteLine($"[Connection #{connectionId}] No records in the database.");
        }
    }
}

public class Ztp01 {
    public static void Main(string[] args) {
        Console.WriteLine("=== Test wzorców projektowych: Singleton, Multiton, Object Pool ===\n");

        // Uzyskanie instancji Singletona - menedżera połączeń
        ConnectionManager connectionManager = ConnectionManager.GetInstance();
        
        // Sprawdzenie, że to singleton (próba pobrania drugiej instancji)
        ConnectionManager connectionManager2 = ConnectionManager.GetInstance();
        Console.WriteLine($"\nConnectionManager is singleton: {ReferenceEquals(connectionManager, connectionManager2)}\n");

        Console.WriteLine("=== Test 1: Połączenia z bazą DB1 ===");
        // Uzyskanie połączenia do DB1
        IDatabaseConnection connection1 = connectionManager.GetConnection("DB1");
        connection1.AddRecord("Karol", 23);
        connection1.ShowAllRecords();

        Console.WriteLine("\n=== Test 2: Drugie połączenie do DB1 (ta sama baza) ===");
        IDatabaseConnection connection2 = connectionManager.GetConnection("DB1");
        connection2.AddRecord("Anna", 28);
        connection2.ShowAllRecords(); // Powinno pokazać oba rekordy (ta sama baza!)

        Console.WriteLine("\n=== Test 3: Trzecie połączenie do DB1 ===");
        IDatabaseConnection connection3 = connectionManager.GetConnection("DB1");
        connection3.AddRecord("Marek", 35);
        connection3.ShowAllRecords();

        Console.WriteLine("\n=== Test 4: Czwarte połączenie do DB1 (ponowne użycie connection #1) ===");
        IDatabaseConnection connection4 = connectionManager.GetConnection("DB1");
        connection4.AddRecord("Zofia", 42);
        
        // Sprawdzenie czy connection4 to ten sam obiekt co connection1
        Console.WriteLine($"\nConnection 4 is same as Connection 1: {ReferenceEquals(connection1, connection4)}");
        Console.WriteLine($"Connection 4 is same as Connection 2: {ReferenceEquals(connection2, connection4)}");
        
        connection4.ShowAllRecords();

        Console.WriteLine("\n=== Test 5: Druga baza danych DB2 ===");
        IDatabaseConnection connectionDB2_1 = connectionManager.GetConnection("DB2");
        connectionDB2_1.AddRecord("Piotr", 30);
        connectionDB2_1.ShowAllRecords();

        Console.WriteLine("\n=== Test 6: Drugie połączenie do DB2 ===");
        IDatabaseConnection connectionDB2_2 = connectionManager.GetConnection("DB2");
        connectionDB2_2.AddRecord("Ewa", 27);
        connectionDB2_2.ShowAllRecords(); // Ta sama baza DB2

        Console.WriteLine("\n=== Test 7: Ponowne połączenie z DB1 - pokazuje wszystkie wcześniejsze rekordy ===");
        IDatabaseConnection connection5 = connectionManager.GetConnection("DB1");
        connection5.ShowAllRecords();
        Console.WriteLine($"Connection 5 is same as Connection 2: {ReferenceEquals(connection2, connection5)}");

        Console.WriteLine("\n=== Test 8: Trzecia baza DB3 ===");
        IDatabaseConnection connectionDB3 = connectionManager.GetConnection("DB3");
        connectionDB3.AddRecord("Jan", 45);
        connectionDB3.ShowAllRecords();

        Console.WriteLine("\n=== Weryfikacja Multiton Pattern dla Database ===");
        // Ponowne pobranie bazy DB1 - powinno zwrócić tę samą instancję
        var db1_again = Database.GetInstance("DB1");
        var db1_original = Database.GetInstance("DB1");
        Console.WriteLine($"Database DB1 is multiton (same instance): {ReferenceEquals(db1_again, db1_original)}");

        Console.WriteLine("\n=== Podsumowanie testów ===");
        Console.WriteLine("✓ ConnectionManager jest Singletonem");
        Console.WriteLine("✓ Database jest Multitonem (jedna instancja per nazwa)");
        Console.WriteLine("✓ Pula połączeń ogranicza liczbę połączeń do 3 per baza");
        Console.WriteLine("✓ Czwarte połączenie ponownie używa pierwszego połączenia");
        Console.WriteLine("✓ Wszystkie połączenia do tej samej bazy współdzielą dane");
    }
}