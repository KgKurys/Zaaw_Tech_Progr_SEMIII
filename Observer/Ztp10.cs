using System;
using System.Collections.Generic;

// Typ operacji
public enum OperationType { Deposit, Withdraw }

// Interfejs obserwatora
public interface IAccountObserver
{
    void Update(string accountHolder, OperationType operation, decimal amount, decimal newBalance);
}

// Konto bankowe - obiekt obserwowany (Subject)
public class BankAccount
{
    public string AccountHolder { get; }
    private decimal balance;
    private List<IAccountObserver> observers = new List<IAccountObserver>();

    public BankAccount(string accountHolder, decimal initialBalance)
    {
        AccountHolder = accountHolder;
        balance = initialBalance;
    }

    public decimal Balance => balance;

    public void Attach(IAccountObserver observer) => observers.Add(observer);
    public void Detach(IAccountObserver observer) => observers.Remove(observer);

    private void Notify(OperationType operation, decimal amount)
    {
        foreach (var observer in observers)
            observer.Update(AccountHolder, operation, amount, balance);
    }

    public void Deposit(decimal amount)
    {
        if (amount <= 0) throw new ArgumentException("Kwota musi być większa od zera.");
        balance += amount;
        Console.WriteLine($"Wpłata: {amount:C}. Saldo: {balance:C}");
        Notify(OperationType.Deposit, amount);
    }

    public void Withdraw(decimal amount)
    {
        if (amount <= 0) throw new ArgumentException("Kwota musi być większa od zera.");
        if (amount > balance) throw new InvalidOperationException("Brak środków.");
        balance -= amount;
        Console.WriteLine($"Wypłata: {amount:C}. Saldo: {balance:C}");
        Notify(OperationType.Withdraw, amount);
    }
}

// Moduł kredytowy - oferta gdy saldo spada poniżej progu
public class CreditObserver : IAccountObserver
{
    private decimal threshold;

    public CreditObserver(decimal threshold) => this.threshold = threshold;

    public void Update(string accountHolder, OperationType op, decimal amount, decimal balance)
    {
        if (balance < threshold)
            Console.WriteLine($"  >> [KREDYT] {accountHolder}, saldo poniżej {threshold:C}. Proponujemy kredyt!");
    }
}

// Moduł lokatowy - oferta przy dużej wpłacie
public class DepositObserver : IAccountObserver
{
    private decimal minDeposit;

    public DepositObserver(decimal minDeposit) => this.minDeposit = minDeposit;

    public void Update(string accountHolder, OperationType op, decimal amount, decimal balance)
    {
        if (op == OperationType.Deposit && amount >= minDeposit)
            Console.WriteLine($"  >> [LOKATA] {accountHolder}, wpłata {amount:C}. Proponujemy lokatę 5%!");
    }
}

// Moduł karty kredytowej - oferta po kilku wypłatach
public class CardObserver : IAccountObserver
{
    private int withdrawCount = 0;
    private int threshold;

    public CardObserver(int threshold) => this.threshold = threshold;

    public void Update(string accountHolder, OperationType op, decimal amount, decimal balance)
    {
        if (op == OperationType.Withdraw)
        {
            withdrawCount++;
            if (withdrawCount == threshold)
                Console.WriteLine($"  >> [KARTA] {accountHolder}, {withdrawCount} wypłat. Proponujemy kartę kredytową!");
        }
    }
}

// Program testowy
public class Program
{
    public static void Main()
    {
        var account = new BankAccount("Jan Kowalski", 2000);

        // Rejestracja obserwatorów
        account.Attach(new CreditObserver(500));      // kredyt gdy saldo < 500
        account.Attach(new DepositObserver(1000));    // lokata przy wpłacie >= 1000
        account.Attach(new CardObserver(3));          // karta po 3 wypłatach

        Console.WriteLine("--- Test obserwatorów ---\n");

        account.Deposit(1500);    // duża wpłata -> oferta lokaty
        account.Withdraw(500);    // 1. wypłata
        account.Withdraw(800);    // 2. wypłata
        account.Withdraw(1000);   // 3. wypłata -> oferta karty
        account.Withdraw(900);    // saldo < 500 -> oferta kredytu

        Console.WriteLine($"\nKońcowe saldo: {account.Balance:C}");
    }
}

