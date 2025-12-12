# Wzorce Adapter i Proxy

## Część 1: Wzorzec Adapter

### 📌 Definicja
**Adapter** to strukturalny wzorzec projektowy, który pozwala obiektom z niekompatybilnymi interfejsami współpracować ze sobą. Działa jak "przejściówka" między dwoma niekompatybilnymi interfejsami.

---

### 🎯 Problem, który rozwiązuje

Wyobraź sobie, że masz:
- Stary system używający formatu XML
- Nową bibliotekę, która przyjmuje tylko JSON

**Bez Adaptera:** Musisz przepisać cały stary system lub nową bibliotekę.

**Z Adapterem:** Tworzysz "przejściówkę", która konwertuje XML na JSON.

---

### 🏗️ Struktura wzorca

```
┌─────────────┐      ┌──────────────────┐      ┌─────────────────┐
│   Klient    │─────►│     ITarget      │      │    Adaptee      │
│             │      │   (Interfejs)    │      │ (Stary system)  │
└─────────────┘      ├──────────────────┤      ├─────────────────┤
                     │ + Request()      │      │ + SpecificReq() │
                     └────────▲─────────┘      └────────▲────────┘
                              │                         │
                              │ implementuje            │ używa
                              │                         │
                     ┌────────┴─────────────────────────┴───┐
                     │              Adapter                  │
                     ├───────────────────────────────────────┤
                     │ - adaptee: Adaptee                    │
                     │ + Request() { adaptee.SpecificReq() } │
                     └───────────────────────────────────────┘
```

---

### 🔑 Kluczowe elementy

| Element | Rola | Przykład |
|---------|------|----------|
| **ITarget** | Interfejs oczekiwany przez klienta | `IModernPrinter` |
| **Adaptee** | Istniejąca klasa z niekompatybilnym interfejsem | `OldPrinter` |
| **Adapter** | Tłumaczy wywołania między interfejsami | `PrinterAdapter` |

---

### 💻 Przykład kodu

```csharp
// Interfejs oczekiwany przez klienta
public interface IModernPrinter
{
    void Print(string text);
}

// Stara klasa z innym interfejsem
public class OldPrinter
{
    public void PrintDocument(string content, int copies)
    {
        for (int i = 0; i < copies; i++)
            Console.WriteLine($"[OLD PRINTER] {content}");
    }
}

// Adapter - przejściówka
public class PrinterAdapter : IModernPrinter
{
    private OldPrinter oldPrinter;

    public PrinterAdapter(OldPrinter oldPrinter)
    {
        this.oldPrinter = oldPrinter;
    }

    public void Print(string text)
    {
        // Tłumaczymy wywołanie nowego interfejsu na stary
        oldPrinter.PrintDocument(text, 1);
    }
}

// Użycie
IModernPrinter printer = new PrinterAdapter(new OldPrinter());
printer.Print("Hello!");  // Działa mimo różnych interfejsów!
```

---

### 🔄 Przepływ działania

```
1. Klient wywołuje: printer.Print("Hello!")
          │
          ▼
2. PrinterAdapter otrzymuje wywołanie
          │
          ▼
3. Adapter tłumaczy: oldPrinter.PrintDocument("Hello!", 1)
          │
          ▼
4. OldPrinter wykonuje swoją logikę
```

---

### ✅ Zalety Adaptera

1. **Separacja kodu** - klient nie wie o starym interfejsie
2. **Wielokrotne użycie** - można używać starych klas bez modyfikacji
3. **Otwarte/Zamknięte** - nowe adaptery bez zmiany istniejącego kodu

### ❌ Wady Adaptera

1. Dodatkowa warstwa abstrakcji
2. Czasem prostsze byłoby zmodyfikowanie oryginalnej klasy

---

### 🌍 Przykłady użycia

- Integracja starego API z nowym kodem
- Biblioteki do konwersji formatów (XML → JSON)
- Sterowniki urządzeń
- Wrappery dla zewnętrznych bibliotek

---

---

## Część 2: Wzorzec Proxy

### 📌 Definicja
**Proxy** to strukturalny wzorzec projektowy, który dostarcza obiekt zastępczy (pośrednik) dla innego obiektu. Proxy kontroluje dostęp do oryginalnego obiektu, pozwalając na wykonanie akcji przed lub po przekazaniu żądania.

---

### 🎯 Problem, który rozwiązuje

Wyobraź sobie ciężki obiekt (np. duży obraz):
- Ładowanie go za każdym razem jest kosztowne
- Nie zawsze jest potrzebny od razu

**Bez Proxy:** Obiekt ładowany przy starcie - wolne!

**Z Proxy:** Obiekt ładowany dopiero gdy naprawdę potrzebny (lazy loading).

---

### 🏗️ Struktura wzorca

```
┌─────────────┐      ┌──────────────────┐
│   Klient    │─────►│    ISubject      │
│             │      │   (Interfejs)    │
└─────────────┘      ├──────────────────┤
                     │ + Request()      │
                     └────────▲─────────┘
                              │
              ┌───────────────┼───────────────┐
              │               │               │
     ┌────────┴───────┐  ┌────┴─────────┐
     │   RealSubject  │  │    Proxy     │
     ├────────────────┤  ├──────────────┤
     │ + Request()    │  │ - real: Real │
     │ (prawdziwa     │  │ + Request()  │
     │  implementacja)│  │   {kontrola} │
     └────────────────┘  └──────────────┘
```

---

### 🔑 Rodzaje Proxy

| Typ | Zastosowanie | Przykład |
|-----|--------------|----------|
| **Virtual Proxy** | Lazy loading - opóźnione tworzenie | Ładowanie obrazu na żądanie |
| **Protection Proxy** | Kontrola dostępu | Sprawdzenie uprawnień użytkownika |
| **Remote Proxy** | Reprezentuje obiekt zdalny | Proxy dla web service |
| **Logging Proxy** | Logowanie operacji | Zapisywanie historii wywołań |
| **Caching Proxy** | Cache wyników | Zapamiętywanie odpowiedzi |

---

### 💻 Przykład: Virtual Proxy (Lazy Loading)

```csharp
// Wspólny interfejs
public interface IImage
{
    void Display();
}

// Prawdziwy obiekt - ciężki do załadowania
public class RealImage : IImage
{
    private string filename;

    public RealImage(string filename)
    {
        this.filename = filename;
        LoadFromDisk();  // Kosztowna operacja!
    }

    private void LoadFromDisk()
    {
        Console.WriteLine($"Ładowanie obrazu: {filename}...");
        Thread.Sleep(2000);  // Symulacja wolnego ładowania
    }

    public void Display()
    {
        Console.WriteLine($"Wyświetlam: {filename}");
    }
}

// Proxy - leniwe ładowanie
public class ImageProxy : IImage
{
    private string filename;
    private RealImage realImage;  // null na początku!

    public ImageProxy(string filename)
    {
        this.filename = filename;
        // Nie ładujemy obrazu od razu!
    }

    public void Display()
    {
        // Ładujemy dopiero gdy potrzebny
        if (realImage == null)
        {
            realImage = new RealImage(filename);
        }
        realImage.Display();
    }
}

// Użycie
IImage image = new ImageProxy("photo.jpg");  // Szybkie!
// ... dużo kodu ...
image.Display();  // Dopiero teraz ładuje obraz
```

---

### 💻 Przykład: Protection Proxy (Kontrola dostępu)

```csharp
public interface IDocument
{
    void Read();
    void Write(string content);
}

public class RealDocument : IDocument
{
    public void Read() => Console.WriteLine("Czytam dokument");
    public void Write(string content) => Console.WriteLine($"Zapisuję: {content}");
}

public class ProtectedDocumentProxy : IDocument
{
    private RealDocument document;
    private string userRole;

    public ProtectedDocumentProxy(string userRole)
    {
        this.userRole = userRole;
        this.document = new RealDocument();
    }

    public void Read()
    {
        document.Read();  // Każdy może czytać
    }

    public void Write(string content)
    {
        if (userRole == "Admin")
        {
            document.Write(content);
        }
        else
        {
            Console.WriteLine("BRAK UPRAWNIEŃ do zapisu!");
        }
    }
}

// Użycie
IDocument doc = new ProtectedDocumentProxy("Guest");
doc.Read();           // OK
doc.Write("test");    // BRAK UPRAWNIEŃ!
```

---

### 🔄 Przepływ działania (Virtual Proxy)

```
1. Klient tworzy: new ImageProxy("photo.jpg")
          │
          ▼
2. Proxy zapamiętuje nazwę pliku (realImage = null)
          │
          ▼
3. ... czas mija, inne operacje ...
          │
          ▼
4. Klient wywołuje: image.Display()
          │
          ▼
5. Proxy sprawdza: realImage == null? TAK
          │
          ▼
6. Proxy tworzy: realImage = new RealImage("photo.jpg")
          │
          ▼
7. RealImage ładuje się z dysku
          │
          ▼
8. Proxy deleguje: realImage.Display()
```

---

### ✅ Zalety Proxy

1. **Kontrola dostępu** - możesz dodać sprawdzanie uprawnień
2. **Lazy loading** - oszczędność zasobów
3. **Logowanie** - możesz śledzić wywołania
4. **Cache** - możesz buforować wyniki
5. **Przezroczystość** - klient nie wie, że używa proxy

### ❌ Wady Proxy

1. Dodatkowa warstwa - może spowolnić
2. Skomplikowana struktura kodu
3. Opóźniona odpowiedź przy pierwszym użyciu (lazy loading)

---

### 🌍 Przykłady użycia

- **ORM** (Entity Framework) - lazy loading encji
- **Serwery proxy** - kontrola ruchu sieciowego
- **Wirtualne maszyny** - symulacja sprzętu
- **Smart pointers** w C++

---

---

## 🔍 Porównanie: Adapter vs Proxy

| Cecha | Adapter | Proxy |
|-------|---------|-------|
| **Cel** | Zmiana interfejsu | Kontrola dostępu |
| **Interfejs** | Różne interfejsy (source → target) | Ten sam interfejs |
| **Relacja** | Adapter ≠ Adaptee | Proxy = RealSubject (ten sam interfejs) |
| **Kiedy używać** | Integracja niekompatybilnych klas | Lazy loading, kontrola, cache |

### Wizualnie:

```
ADAPTER:
┌────────────┐     ┌───────────┐
│ INewFormat │ ←── │  Adapter  │ ←── OldFormat (inny interfejs!)
└────────────┘     └───────────┘

PROXY:
┌────────────┐     ┌───────────┐     ┌─────────────┐
│ ISubject   │ ←── │   Proxy   │ ──► │ RealSubject │
└────────────┘     └───────────┘     └─────────────┘
                    (ten sam interfejs!)
```

---

## 📝 Podsumowanie

> **Adapter = "Zmień interfejs, żeby pasował"**
> 
> **Proxy = "Kontroluj dostęp do obiektu"**

- **Adapter** - używasz gdy masz niekompatybilne interfejsy
- **Proxy** - używasz gdy chcesz dodać warstwę kontroli (lazy loading, uprawnienia, cache, logowanie)
