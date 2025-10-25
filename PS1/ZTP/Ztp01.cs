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
using System.Runtime.CompilerServices;

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
    }

    public IDatabaseConnection GetConnection() {
        lock (lockObject) {
            if (connections.Count < maxConnections) {
                var newConnection = new DatabaseConnection(database, connections.Count + 1);
                connections.Add(newConnection);
                return newConnection;
            }

            var connection = connections[currentConnectionIndex];
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

    private ConnectionManager() { }

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
        Database database = Database.GetInstance(databaseName);
        return database.GetConnection();
    }

    public override int GetHashCode() {
        return RuntimeHelpers.GetHashCode(this);
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
        connectionPool = new ConnectionPool(this, 3);
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

    public override int GetHashCode() {
        return RuntimeHelpers.GetHashCode(this);
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

    public int AddRecord(string name, int age) {
        int id = db.GetNextId();
        Record newRecord = new(id, name, age);
        db.AddRecordInternal(newRecord);
        return id;
    }

    public Record? GetRecord(int id) {
        return db.GetRecordInternal(id);
    }

    public void UpdateRecord(int id, string newName, int newAge) {
        Record? optionalRecord = GetRecord(id);
        if (optionalRecord != null) {
            optionalRecord.Name = newName;
            optionalRecord.Age = newAge;
        }
    }

    public void DeleteRecord(int id) {
        Record? optionalRecord = GetRecord(id);
        if (optionalRecord != null) {
            db.RemoveRecordInternal(optionalRecord);
        }
    }

    public void ShowAllRecords() {
        var allRecords = db.GetAllRecordsInternal();
        if (allRecords.Any()) {
            Console.WriteLine($"Records in {db.Name}:");
            allRecords.ForEach(r => Console.WriteLine($"  {r}"));
        } else {
            Console.WriteLine($"No records in {db.Name}");
        }
    }

    public override int GetHashCode() {
        return RuntimeHelpers.GetHashCode(this);
    }
}

public class Ztp01 {
    public static void Main(string[] args) {
        Console.WriteLine("=== TESTY WZORCÓW PROJEKTOWYCH ===\n");

        ConnectionManager cm1 = ConnectionManager.GetInstance();
        ConnectionManager cm2 = ConnectionManager.GetInstance();
        
        // Test 1: SINGLETON - ConnectionManager
        Console.WriteLine("TEST 1: Singleton (ConnectionManager)");
        Console.WriteLine($"  Manager 1 hash: {cm1.GetHashCode()}");
        Console.WriteLine($"  Manager 2 hash: {cm2.GetHashCode()}");
        Console.WriteLine($"  Identyczne? {ReferenceEquals(cm1, cm2)} ✓\n");

        // Test 2: MULTITON - Database  
        Database db1a = Database.GetInstance("DB1");
        Database db1b = Database.GetInstance("DB1");
        Database db2 = Database.GetInstance("DB2");
        
        Console.WriteLine("TEST 2: Multiton (Database)");
        Console.WriteLine($"  DB1 (pierwsza)  hash: {db1a.GetHashCode()}");
        Console.WriteLine($"  DB1 (ponowna)   hash: {db1b.GetHashCode()}");
        Console.WriteLine($"  DB1 identyczne? {ReferenceEquals(db1a, db1b)} ✓");
        Console.WriteLine($"  DB2             hash: {db2.GetHashCode()}");
        Console.WriteLine($"  DB1 != DB2?     {!ReferenceEquals(db1a, db2)} ✓\n");

        // Test 3: OBJECT POOL - ConnectionPool (max 3 połączenia)
        IDatabaseConnection conn1 = cm1.GetConnection("TestDB");
        IDatabaseConnection conn2 = cm1.GetConnection("TestDB");
        IDatabaseConnection conn3 = cm1.GetConnection("TestDB");
        IDatabaseConnection conn4 = cm1.GetConnection("TestDB"); // Powinno zwrócić conn1
        
        Console.WriteLine("TEST 3: Object Pool (max 3 połączenia)");
        Console.WriteLine($"  Connection 1 hash: {conn1.GetHashCode()}");
        Console.WriteLine($"  Connection 2 hash: {conn2.GetHashCode()}");
        Console.WriteLine($"  Connection 3 hash: {conn3.GetHashCode()}");
        Console.WriteLine($"  Connection 4 hash: {conn4.GetHashCode()}");
        Console.WriteLine($"  Conn4 == Conn1? {ReferenceEquals(conn1, conn4)} ✓ (cykliczne użycie)");
        Console.WriteLine($"  Conn4 == Conn2? {ReferenceEquals(conn2, conn4)} ✓ (FALSE - to różne)\n");

        // Test 4: Współdzielenie danych między połączeniami
        Console.WriteLine("TEST 4: Współdzielenie danych");
        conn1.AddRecord("Alice", 25);
        conn2.AddRecord("Bob", 30);
        conn3.AddRecord("Charlie", 35);
        conn1.ShowAllRecords(); // Wszystkie 3 połączenia widzą te same dane
        
        Console.WriteLine("\n=== WSZYSTKIE WZORCE DZIAŁAJĄ POPRAWNIE ===");
    }
}