using System;
using System.Collections.Generic;
using System.Linq;

public interface INewsService
{
    Response AddMessage(string title, string content);
    Response ReadMessage(int id);
    Response EditMessage(int id, string newContent);
    Response DeleteMessage(int id);
}

public class Response
{
    public string Status { get; set; }
    public string Message { get; set; }

    public Response(string status, string message)
    {
        Status = status;
        Message = message;
    }
}

public class User
{
    public string Name { get; set; }
    public UserRole Role { get; set; }

    public User(string name, UserRole role)
    {
        Name = name;
        Role = role;
    }
}

public enum UserRole
{
    Guest,
    User,
    Moderator,
    Admin
}
public class Message
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }

    public Message(int id, string title, string content)
    {
        Id = id;
        Title = title;
        Content = content;
    }
}

public class NewsService : INewsService
{
    private List<Message> _messages;
    private int _nextId;

    public NewsService()
    {
        _messages = new List<Message>();
        _nextId = 1;
    }

    public Response AddMessage(string title, string content)
    {
        var message = new Message(_nextId++, title, content);
        _messages.Add(message);
        return new Response("OK", "Dodano nową wiadomość.");
    }

    public Response ReadMessage(int id)
    {
        var message = _messages.FirstOrDefault(m => m.Id == id);
        if (message != null)
        {
            return new Response("OK", $"{message.Title}: {message.Content}");
        }
        return new Response("FAIL", "Nie znaleziono wiadomości.");
    }

    public Response EditMessage(int id, string newContent)
    {
        var message = _messages.FirstOrDefault(m => m.Id == id);
        if (message == null)
        {
            return new Response("FAIL", "Nie znaleziono wiadomości.");
        }

        message.Content = newContent;
        return new Response("OK", "Zaktualizowano wiadomość.");
    }

    public Response DeleteMessage(int id)
    {
        var message = _messages.FirstOrDefault(m => m.Id == id);
        if (message == null)
        {
            return new Response("FAIL", "Nie znaleziono wiadomości.");
        }

        _messages.Remove(message);
        return new Response("OK", "Usunięto wiadomość.");
    }
}

// Proxy kontrolujące uprawnienia
public class AccessControlProxy : INewsService
{
    private readonly INewsService _service;
    private readonly User _currentUser;

    public AccessControlProxy(INewsService service, User currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    private bool CanCreate()
    {
        return _currentUser.Role != UserRole.Guest;
    }

    private bool CanModify()
    {
        return _currentUser.Role == UserRole.Moderator || _currentUser.Role == UserRole.Admin;
    }

    private bool CanRemove()
    {
        return _currentUser.Role == UserRole.Admin;
    }

    public Response AddMessage(string title, string content)
    {
        if (!CanCreate())
        {
            return new Response("DENIED", $"Użytkownik {_currentUser.Name} nie ma uprawnień do tworzenia wiadomości.");
        }
        return _service.AddMessage(title, content);
    }

    public Response ReadMessage(int id)
    {
        // Wszyscy mogą czytać
        return _service.ReadMessage(id);
    }

    public Response EditMessage(int id, string newContent)
    {
        if (!CanModify())
        {
            return new Response("DENIED", $"Użytkownik {_currentUser.Name} nie ma uprawnień do modyfikacji wiadomości.");
        }
        return _service.EditMessage(id, newContent);
    }

    public Response DeleteMessage(int id)
    {
        if (!CanRemove())
        {
            return new Response("DENIED", $"Użytkownik {_currentUser.Name} nie ma uprawnień do usuwania wiadomości.");
        }
        return _service.DeleteMessage(id);
    }
}

// Proxy z mechanizmem cache
public class CachedNewsProxy : INewsService
{
    private readonly INewsService _service;
    private readonly Dictionary<int, Response> _readCache;

    public CachedNewsProxy(INewsService service)
    {
        _service = service;
        _readCache = new Dictionary<int, Response>();
    }

    public Response AddMessage(string title, string content)
    {
        var result = _service.AddMessage(title, content);
        if (result.Status == "OK")
        {
            // Nowy element może zmienić stan - resetujemy cache
            _readCache.Clear();
        }
        return result;
    }

    public Response ReadMessage(int id)
    {
        if (_readCache.TryGetValue(id, out Response cached))
        {
            return cached;
        }

        var result = _service.ReadMessage(id);
        _readCache[id] = result;
        return result;
    }

    public Response EditMessage(int id, string newContent)
    {
        var result = _service.EditMessage(id, newContent);
        if (result.Status == "OK" && _readCache.ContainsKey(id))
        {
            // Usuwamy z cache, bo treść się zmieniła
            _readCache.Remove(id);
        }
        return result;
    }

    public Response DeleteMessage(int id)
    {
        var result = _service.DeleteMessage(id);
        if (result.Status == "OK" && _readCache.ContainsKey(id))
        {
            // Usuwamy z cache skasowany element
            _readCache.Remove(id);
        }
        return result;
    }
}

public class Program
{
    public static void Main()
    {
        // Podstawowy serwis
        INewsService baseService = new NewsService();

        // Różne role użytkowników
        User guestAccount = new User("Gość Jan", UserRole.Guest);
        User normalAccount = new User("Kowalski", UserRole.User);
        User modAccount = new User("ModMaria", UserRole.Moderator);
        User adminAccount = new User("AdminPiotr", UserRole.Admin);

        Console.WriteLine("Test 1: Admin tworzy wiadomości");
        
        INewsService adminService = new CachedNewsProxy(new AccessControlProxy(baseService, adminAccount));
        
        var r1 = adminService.AddMessage("Ogłoszenie", "Nowa funkcjonalność w systemie.");
        Console.WriteLine($"[{adminAccount.Name}] {r1.Status}: {r1.Message}");
        
        var r2 = adminService.AddMessage("Informacja", "Planowana przerwa techniczna.");
        Console.WriteLine($"[{adminAccount.Name}] {r2.Status}: {r2.Message}");

        Console.WriteLine("\nTest 2: Gość próbuje tworzyć");
        
        INewsService guestService = new CachedNewsProxy(new AccessControlProxy(baseService, guestAccount));
        
        var r3 = guestService.AddMessage("Spam", "Niepowołana wiadomość.");
        Console.WriteLine($"[{guestAccount.Name}] {r3.Status}: {r3.Message}");

        Console.WriteLine("\nTest 3: Gość czyta (cache test)");
        
        var r4 = guestService.ReadMessage(1);
        Console.WriteLine($"[{guestAccount.Name}] Pierwsze wywołanie -> {r4.Status}: {r4.Message}");
        
        var r5 = guestService.ReadMessage(1);
        Console.WriteLine($"[{guestAccount.Name}] Drugie wywołanie (cache) -> {r5.Status}: {r5.Message}");

        Console.WriteLine("\nTest 4: Zwykły user dodaje wiadomość");
        
        INewsService userService = new CachedNewsProxy(new AccessControlProxy(baseService, normalAccount));
        
        var r6 = userService.AddMessage("Post użytkownika", "Moja wiadomość na forum.");
        Console.WriteLine($"[{normalAccount.Name}] {r6.Status}: {r6.Message}");

        Console.WriteLine("\nTest 5: Zwykły user próbuje edytować");
        
        var r7 = userService.EditMessage(1, "Próba zmiany treści.");
        Console.WriteLine($"[{normalAccount.Name}] {r7.Status}: {r7.Message}");

        Console.WriteLine("\nTest 6: Moderator edytuje wiadomość");
        
        INewsService modService = new CachedNewsProxy(new AccessControlProxy(baseService, modAccount));
        
        var r8 = modService.EditMessage(1, "Treść poprawiona przez moderatora.");
        Console.WriteLine($"[{modAccount.Name}] {r8.Status}: {r8.Message}");
        
        var r9 = modService.ReadMessage(1);
        Console.WriteLine($"[{modAccount.Name}] Odczyt po edycji -> {r9.Status}: {r9.Message}");

        Console.WriteLine("\nTest 7: Moderator próbuje usunąć");
        
        var r10 = modService.DeleteMessage(2);
        Console.WriteLine($"[{modAccount.Name}] {r10.Status}: {r10.Message}");

        Console.WriteLine("\nTest 8: Admin usuwa wiadomość");
        
        var r11 = adminService.DeleteMessage(2);
        Console.WriteLine($"[{adminAccount.Name}] {r11.Status}: {r11.Message}");
        
        var r12 = adminService.ReadMessage(2);
        Console.WriteLine($"[{adminAccount.Name}] Odczyt usuniętej -> {r12.Status}: {r12.Message}");

        Console.WriteLine("\nTest 9: Cache dla nieistniejącej wiadomości");
        
        var r13 = guestService.ReadMessage(777);
        Console.WriteLine($"[{guestAccount.Name}] Pierwsze -> {r13.Status}: {r13.Message}");
        
        var r14 = guestService.ReadMessage(777);
        Console.WriteLine($"[{guestAccount.Name}] Drugie (cache) -> {r14.Status}: {r14.Message}");
    }
}
