# Wzorzec Observer (Obserwator)

## 📌 Definicja
**Observer** to behawioralny wzorzec projektowy, który definiuje relację jeden-do-wielu między obiektami. Gdy jeden obiekt (Subject) zmienia swój stan, wszystkie zależne od niego obiekty (Observers) są automatycznie powiadamiane.

---

## 🎯 Problem, który rozwiązuje
Wyobraź sobie, że masz konto bankowe i kilka modułów banku, które muszą reagować na zmiany salda:
- Moduł kredytowy chce wiedzieć, kiedy saldo spada
- Moduł lokatowy chce wiedzieć o dużych wpłatach
- Moduł kart chce liczyć wypłaty

**Bez wzorca Observer:** Każdy moduł musiałby ciągle sprawdzać stan konta (polling) - nieefektywne!

**Z wzorcem Observer:** Konto samo powiadamia zainteresowane moduły, gdy coś się zmieni.

---

## 🏗️ Struktura wzorca

```
┌─────────────────┐         ┌──────────────────┐
│     Subject     │         │   IObserver      │
│  (Obserwowany)  │◄────────│   (Interfejs)    │
├─────────────────┤         ├──────────────────┤
│ - observers[]   │         │ + Update()       │
│ + Attach()      │         └────────▲─────────┘
│ + Detach()      │                  │
│ + Notify()      │                  │ implementuje
└─────────────────┘                  │
                           ┌─────────┴─────────┐
                           │ ConcreteObserver  │
                           ├───────────────────┤
                           │ + Update()        │
                           │ - własne dane     │
                           └───────────────────┘
```

---

## 🔑 Kluczowe elementy

| Element | Rola | W naszym przykładzie |
|---------|------|---------------------|
| **Subject** | Obiekt obserwowany, przechowuje listę obserwatorów | `BankAccount` |
| **IObserver** | Interfejs z metodą `Update()` | `IAccountObserver` |
| **ConcreteObserver** | Konkretny obserwator reagujący na zmiany | `CreditObserver`, `DepositObserver`, `CardObserver` |
| **Attach()** | Dodaje obserwatora do listy | `account.Attach(observer)` |
| **Detach()** | Usuwa obserwatora z listy | `account.Detach(observer)` |
| **Notify()** | Powiadamia wszystkich obserwatorów | Wywoływane po `Deposit()` i `Withdraw()` |

---

## 💻 Jak to działa w kodzie

### 1. Interfejs obserwatora
```csharp
public interface IAccountObserver
{
    void Update(string accountHolder, OperationType operation, 
                decimal amount, decimal newBalance);
}
```

### 2. Subject (BankAccount) - obiekt obserwowany
```csharp
public class BankAccount
{
    private List<IAccountObserver> observers = new List<IAccountObserver>();

    public void Attach(IAccountObserver observer) => observers.Add(observer);
    public void Detach(IAccountObserver observer) => observers.Remove(observer);

    private void Notify(OperationType operation, decimal amount)
    {
        foreach (var observer in observers)
            observer.Update(AccountHolder, operation, amount, balance);
    }

    public void Withdraw(decimal amount)
    {
        balance -= amount;
        Notify(OperationType.Withdraw, amount);  // Powiadom wszystkich!
    }
}
```

### 3. Konkretny obserwator
```csharp
public class CreditObserver : IAccountObserver
{
    private decimal threshold;

    public void Update(string accountHolder, OperationType op, 
                       decimal amount, decimal balance)
    {
        if (balance < threshold)
            Console.WriteLine($"[KREDYT] {accountHolder}, proponujemy kredyt!");
    }
}
```

### 4. Użycie
```csharp
var account = new BankAccount("Jan Kowalski", 2000);
account.Attach(new CreditObserver(500));
account.Attach(new DepositObserver(1000));

account.Withdraw(1800);  // Automatycznie powiadomi obserwatorów!
```

---

## 🔄 Przepływ działania

```
1. Klient wywołuje: account.Withdraw(1800)
          │
          ▼
2. BankAccount zmienia stan: balance = 200
          │
          ▼
3. BankAccount wywołuje: Notify()
          │
          ▼
4. Notify() iteruje po observers[]
          │
          ├──► CreditObserver.Update() → saldo < 500? TAK → wyświetl ofertę
          │
          ├──► DepositObserver.Update() → to wypłata → nic nie rób
          │
          └──► CardObserver.Update() → licznik++ → sprawdź próg
```

---

## ✅ Zalety

1. **Luźne powiązanie** - Subject nie zna szczegółów obserwatorów
2. **Otwarte/Zamknięte** - można dodać nowych obserwatorów bez zmiany Subject
3. **Dynamiczne subskrypcje** - obserwatorzy mogą być dodawani/usuwani w runtime

## ❌ Wady

1. Obserwatorzy powiadamiani w losowej kolejności
2. Trudne debugowanie przy wielu obserwatorach
3. Potencjalne wycieki pamięci (jeśli nie usuniemy obserwatorów)

---

## 🌍 Przykłady użycia w praktyce

- **System zdarzeń w C#** - `event` i `delegate`
- **GUI** - przycisk powiadamia o kliknięciu
- **MVC** - Model powiadamia View o zmianach
- **Newsletter** - subskrybenci otrzymują powiadomienia

---

## 📝 Podsumowanie

> **Observer = "Powiadom mnie, gdy się zmienisz"**

Subject nie wie *co* zrobią obserwatorzy - tylko ich informuje. Każdy obserwator sam decyduje, jak zareagować na zmianę.
