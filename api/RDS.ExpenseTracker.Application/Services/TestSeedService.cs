using FluentResults;
using Microsoft.Extensions.Logging;
using RDS.ExpenseTracker.Application.Utilities;
using RDS.ExpenseTracker.Domain.Dtos;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Enums;
using RDS.ExpenseTracker.Domain.Repositories;
using RDS.ExpenseTracker.Domain.Services;

namespace RDS.ExpenseTracker.Application.Services;

/// <summary>
/// Genera dati fittizi ma verosimili per l'utente di test: 5 conti e circa 5 anni di movimenti
/// (stipendio, affitto, bollette, spese con carta, contanti, risparmio, PAC) con trasferimenti collegati.
/// Ad ogni chiamata i dati esistenti dell'utente (categorie comprese) vengono eliminati e rigenerati da zero.
/// </summary>
public class TestSeedService : ITestSeedService
{
    public const string TestUserEmail = "test@dev.local";
    public const int YearsOfHistory = 5;

    public const string CheckingAccountName = "Conto Corrente";
    public const string CreditCardAccountName = "Carta di Credito";
    public const string SavingsAccountName = "Conto Deposito";
    public const string BrokerAccountName = "Conto Titoli";
    public const string CashAccountName = "Contanti";

    private readonly IUserRepository _userRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ITransferRepository _transferRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ILogger<TestSeedService> _logger;

    public TestSeedService(
        IUserRepository userRepository,
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository,
        ITransferRepository transferRepository,
        ICategoryRepository categoryRepository,
        ILogger<TestSeedService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _transferRepository = transferRepository ?? throw new ArgumentNullException(nameof(transferRepository));
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<TestSeedResultDto>> ResetTestUserDataAsync()
    {
        try
        {
            var (user, userCreated) = await GetOrCreateTestUserAsync();
            await ResetExistingDataAsync(user.Id);
            var categories = await ResetCategoriesAsync(user.Id);

            var accounts = new SeedAccounts(
                new Account(0, CheckingAccountName, user.Id),
                new Account(0, CreditCardAccountName, user.Id),
                new Account(0, SavingsAccountName, user.Id),
                new Account(0, BrokerAccountName, user.Id),
                new Account(0, CashAccountName, user.Id));

            await _accountRepository.AddAccounts(accounts.All);
            await _accountRepository.SaveChangesAsync();

            var toDate = DateTime.UtcNow.Date;
            var fromDate = new DateTime(toDate.Year - YearsOfHistory, toDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var generator = new SeedGenerator(accounts, categories, new Random(), fromDate, toDate);
            generator.Generate();

            foreach (var transfer in generator.Transfers)
                await _transferRepository.AddTransfer(transfer);

            await _transactionRepository.AddTransactions(generator.Transactions);
            await _transactionRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Seeded test user {UserId} ({Email}, created={UserCreated}): {AccountCount} accounts, {TransactionCount} transactions, {TransferCount} transfers from {FromDate:yyyy-MM-dd} to {ToDate:yyyy-MM-dd}",
                user.Id, user.Email, userCreated, accounts.All.Length, generator.Transactions.Count, generator.Transfers.Count, fromDate, toDate);

            return Result.Ok(new TestSeedResultDto
            {
                UserId = user.Id,
                Email = user.Email,
                UserCreated = userCreated,
                AccountCount = accounts.All.Length,
                TransactionCount = generator.Transactions.Count,
                TransferCount = generator.Transfers.Count,
                FromDate = fromDate,
                ToDate = toDate,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding test data for {Email}", TestUserEmail);
            return Result.Fail($"Test seed failed: {ex.Message}");
        }
    }

    private async Task<(User User, bool Created)> GetOrCreateTestUserAsync()
    {
        var existing = await _userRepository.GetByEmail(TestUserEmail);
        if (existing is not null)
            return (existing, false);

        var user = new User(0, TestUserEmail);
        await _userRepository.AddUser(user);
        await _userRepository.SaveChangesAsync();
        return (user, true);
    }

    private async Task ResetExistingDataAsync(int userId)
    {
        var existingAccounts = (await _accountRepository.GetAccounts(userId)).ToList();
        if (existingAccounts.Count == 0)
            return;

        var accountIds = existingAccounts.Select(a => a.Id).ToList();

        var transferIds = (await _transactionRepository.DeleteTransactionsByAccountIds(accountIds)).ToList();
        if (transferIds.Count > 0)
            await _transferRepository.DeleteTransfers(transferIds);

        await _accountRepository.DeleteAccounts(accountIds);
        await _accountRepository.SaveChangesAsync();
    }

    // Due salvataggi distinti: l'indice univoco (UserId, Kind) non deve vedere vecchie e nuove categorie insieme.
    private async Task<UserCategories> ResetCategoriesAsync(int userId)
    {
        await _categoryRepository.DeleteCategories(userId);
        await _categoryRepository.SaveChangesAsync();

        var categories = Category.CreateDefaultCategories(userId).ToList();
        await _categoryRepository.AddCategories(categories);
        await _categoryRepository.SaveChangesAsync();

        return new UserCategories(categories);
    }

    private sealed record SeedAccounts(Account Checking, Account CreditCard, Account Savings, Account Broker, Account Cash)
    {
        public Account[] All => [Checking, CreditCard, Savings, Broker, Cash];
    }

    /// <summary>
    /// Costruisce i movimenti mese per mese tenendo traccia dei saldi, così che i flussi
    /// dipendenti (saldo carta, giroconti verso/da risparmio, spese in contanti) restino coerenti.
    /// </summary>
    private sealed class SeedGenerator(SeedAccounts accounts, UserCategories categories, Random random, DateTime fromDate, DateTime toDate)
    {
        private const decimal BaseNetSalary = 2500m;
        private const decimal BaseRent = 780m;
        // Saldo del conto corrente dopo lo stipendio: deve coprire affitto, bollette e addebito carta
        // del mese successivo, che arrivano prima del prossimo accredito.
        private const decimal CheckingTargetBalance = 4000m;
        private const decimal CheckingMinimumBalance = 3000m;

        private static readonly string[] GroceryDescriptions =
            ["Spesa supermercato", "Spesa discount", "Spesa ipermercato", "Macelleria", "Ortofrutta"];

        private static readonly string[] RestaurantDescriptions =
            ["Ristorante", "Pizzeria", "Trattoria", "Ristorante sushi", "Aperitivo", "Hamburgeria"];

        private static readonly string[] ClothesDescriptions =
            ["Negozio di abbigliamento", "Calzature", "Abbigliamento sportivo", "Accessori", "Acquisto abbigliamento online"];

        private static readonly string[] LeisureDescriptions =
            ["Cinema", "Libreria", "Videogioco", "Museo", "Bowling"];

        private static readonly (CategoryEnum Category, string Description, decimal Min, decimal Max)[] CashExpenses =
        [
            (CategoryEnum.FoodAndBeverage, "Caffè al bar", 1.20m, 3.50m),
            (CategoryEnum.FoodAndBeverage, "Colazione al bar", 2.50m, 6.50m),
            (CategoryEnum.FoodAndBeverage, "Panificio", 2.50m, 9m),
            (CategoryEnum.FoodAndBeverage, "Mercato rionale", 8m, 28m),
            (CategoryEnum.Entertainment, "Edicola", 1.50m, 6m),
            (CategoryEnum.HealthAndFitness, "Barbiere", 15m, 22m),
        ];

        private readonly Dictionary<Account, decimal> _balances = [];
        private readonly DateTime _createdOn = DateTime.UtcNow;
        private decimal _investedCapital;

        public List<Transaction> Transactions { get; } = [];
        public List<Transfer> Transfers { get; } = [];

        public void Generate()
        {
            Income(accounts.Checking, CategoryEnum.Default, 4000m, "Saldo iniziale", fromDate);
            Income(accounts.Savings, CategoryEnum.Default, 6000m, "Saldo iniziale", fromDate);
            Income(accounts.Cash, CategoryEnum.Default, 60m, "Saldo iniziale", fromDate);

            for (var monthStart = fromDate; monthStart <= toDate; monthStart = monthStart.AddMonths(1))
            {
                var yearIndex = monthStart.Year - fromDate.Year;
                var inflation = (decimal)Math.Pow(1.02, yearIndex);

                // Il saldo della carta del mese precedente va pagato prima delle nuove spese.
                PayCreditCard(monthStart);
                AddIncomes(monthStart, yearIndex);
                AddHousing(monthStart, yearIndex, inflation);
                AddTransportation(monthStart, inflation);
                AddFoodAndHealth(monthStart, yearIndex, inflation);
                AddLeisure(monthStart, yearIndex, inflation);
                AddHolidaysAndGifts(monthStart, inflation);
                AddCash(monthStart);
                AddInvestments(monthStart, yearIndex);
                AddSavings(monthStart);
            }
        }

        private void PayCreditCard(DateTime monthStart)
        {
            var due = -Balance(accounts.CreditCard);
            Transfer(accounts.Checking, accounts.CreditCard, due, Day(monthStart, 15),
                "Addebito estratto conto Carta di Credito", "Saldo estratto conto");
        }

        private void AddIncomes(DateTime monthStart, int yearIndex)
        {
            var salary = Math.Round(BaseNetSalary * (decimal)Math.Pow(1.03, yearIndex), 0);
            var payday = PreviousWorkingDay(Day(monthStart, 27));

            Income(accounts.Checking, CategoryEnum.WorkIncomes, salary + Amount(-40m, 80m), "Accredito stipendio", payday);

            switch (monthStart.Month)
            {
                case 3:
                    Income(accounts.Checking, CategoryEnum.WorkIncomes, Math.Round(Amount(600m, 1500m), 0), "Premio di risultato", payday);
                    break;
                case 7:
                    Income(accounts.Checking, CategoryEnum.Default, Amount(80m, 450m), "Rimborso IRPEF 730", payday);
                    break;
                case 12:
                    Income(accounts.Checking, CategoryEnum.WorkIncomes, salary, "Tredicesima mensilità", PreviousWorkingDay(Day(monthStart, 15)));
                    break;
            }

            if (Chance(0.05))
                Income(accounts.CreditCard, CategoryEnum.Default, Amount(20m, 150m), "Rimborso acquisto online", RandomDay(monthStart));
        }

        private void AddHousing(DateTime monthStart, int yearIndex, decimal inflation)
        {
            var month = monthStart.Month;
            var rent = Math.Round(BaseRent * (decimal)Math.Pow(1.015, yearIndex), 0);

            Expense(accounts.Checking, CategoryEnum.Housing, rent, "Affitto appartamento", Day(monthStart, 1));
            Expense(accounts.Checking, CategoryEnum.Housing, 27.90m, "Abbonamento fibra internet", Day(monthStart, 8));
            Expense(accounts.Checking, CategoryEnum.Housing, yearIndex >= 3 ? 11.99m : 9.99m, "Canone telefonia mobile", Day(monthStart, 12));

            if (month % 2 == 1)
                Expense(accounts.Checking, CategoryEnum.Housing, Amount(55m, 95m, inflation), "Bolletta energia elettrica", Day(monthStart, 18));

            if (month % 2 == 0)
            {
                var isWinter = month is 12 or 2;
                var gas = isWinter ? Amount(140m, 240m, inflation) : Amount(25m, 60m, inflation);
                Expense(accounts.Checking, CategoryEnum.Housing, gas, "Bolletta gas", Day(monthStart, 22));
            }

            if (month % 3 == 0)
                Expense(accounts.Checking, CategoryEnum.Housing, Amount(38m, 70m, inflation), "Bolletta acqua", Day(monthStart, 12));

            if (month is 4 or 10)
                Expense(accounts.Checking, CategoryEnum.Housing, Amount(110m, 150m, inflation), "TARI - tassa rifiuti", Day(monthStart, 16));

            if (Chance(0.06))
                Expense(accounts.CreditCard, CategoryEnum.Housing, Amount(40m, 250m, inflation),
                    Pick(["Ferramenta", "Articoli per la casa", "Intervento idraulico"]), RandomDay(monthStart));
        }

        private void AddTransportation(DateTime monthStart, decimal inflation)
        {
            var month = monthStart.Month;

            for (var i = random.Next(2, 4); i > 0; i--)
                Expense(accounts.CreditCard, CategoryEnum.Transportation, Amount(45m, 70m, inflation), "Rifornimento carburante", RandomDay(monthStart));

            for (var i = random.Next(0, 3); i > 0; i--)
                Expense(accounts.CreditCard, CategoryEnum.Transportation, Amount(1.50m, 8m), "Parcheggio", RandomDay(monthStart));

            if (Chance(0.5))
                Expense(accounts.CreditCard, CategoryEnum.Transportation, Amount(3m, 28m), "Pedaggio autostradale", RandomDay(monthStart));

            if (Chance(0.1))
                Expense(accounts.CreditCard, CategoryEnum.Transportation, Amount(12m, 45m, inflation), Pick(["Biglietto treno", "Taxi"]), RandomDay(monthStart));

            if (month == 5)
                Expense(accounts.Checking, CategoryEnum.Transportation, Math.Round(Amount(430m, 560m, inflation), 0), "Assicurazione RC auto", Day(monthStart, 10));

            if (month == 9)
                Expense(accounts.Checking, CategoryEnum.Transportation, 186.40m, "Bollo auto", Day(monthStart, 20));

            if (month == 11)
                Expense(accounts.CreditCard, CategoryEnum.Transportation, Amount(180m, 340m, inflation), "Tagliando auto", RandomDay(monthStart));

            if (month is 4 or 11)
                Expense(accounts.CreditCard, CategoryEnum.Transportation, Amount(45m, 70m), "Cambio pneumatici stagionale", RandomDay(monthStart));
        }

        private void AddFoodAndHealth(DateTime monthStart, int yearIndex, decimal inflation)
        {
            var month = monthStart.Month;

            for (var i = random.Next(4, 7); i > 0; i--)
                Expense(accounts.CreditCard, CategoryEnum.FoodAndBeverage, Amount(25m, 110m, inflation), Pick(GroceryDescriptions), RandomDay(monthStart));

            for (var i = random.Next(1, 4); i > 0; i--)
                Expense(accounts.Checking, CategoryEnum.FoodAndBeverage, Amount(15m, 70m, inflation), "Spesa supermercato (bancomat)", RandomDay(monthStart));

            for (var i = random.Next(1, 5); i > 0; i--)
                Expense(accounts.CreditCard, CategoryEnum.FoodAndBeverage, Amount(25m, 85m, inflation), Pick(RestaurantDescriptions), RandomDay(monthStart));

            if (Chance(0.5))
                Expense(accounts.CreditCard, CategoryEnum.FoodAndBeverage, Amount(15m, 35m, inflation), "Consegna cibo a domicilio", RandomDay(monthStart));

            // Palestra sospesa ad agosto.
            if (month != 8)
                Expense(accounts.CreditCard, CategoryEnum.HealthAndFitness, yearIndex >= 2 ? 55m : 49m, "Abbonamento palestra", Day(monthStart, 3));

            for (var i = random.Next(0, 3); i > 0; i--)
                Expense(accounts.CreditCard, CategoryEnum.HealthAndFitness, Amount(6m, 45m, inflation), "Farmacia", RandomDay(monthStart));

            if (month is 2 or 9)
                Expense(accounts.CreditCard, CategoryEnum.HealthAndFitness, Amount(60m, 130m, inflation), "Dentista - igiene dentale", RandomDay(monthStart));

            if (Chance(0.08))
                Expense(accounts.CreditCard, CategoryEnum.HealthAndFitness, Amount(80m, 160m, inflation), "Visita medica specialistica", RandomDay(monthStart));
        }

        private void AddLeisure(DateTime monthStart, int yearIndex, decimal inflation)
        {
            var month = monthStart.Month;

            Expense(accounts.CreditCard, CategoryEnum.Entertainment, yearIndex >= 2 ? 13.99m : 12.99m, "Abbonamento streaming video", Day(monthStart, 5));
            Expense(accounts.CreditCard, CategoryEnum.Entertainment, yearIndex >= 3 ? 11.99m : 10.99m, "Abbonamento streaming musicale", Day(monthStart, 18));

            for (var i = random.Next(0, 3); i > 0; i--)
                Expense(accounts.CreditCard, CategoryEnum.Entertainment, Amount(8m, 30m, inflation), Pick(LeisureDescriptions), RandomDay(monthStart));

            if (Chance(0.08))
                Expense(accounts.CreditCard, CategoryEnum.Entertainment, Amount(40m, 95m, inflation), "Biglietto concerto", RandomDay(monthStart));

            // Saldi invernali ed estivi.
            var clothes = month is 1 or 7 ? random.Next(2, 5) : random.Next(0, 2);
            for (var i = clothes; i > 0; i--)
                Expense(accounts.CreditCard, CategoryEnum.Clothes, Amount(20m, 140m, inflation), Pick(ClothesDescriptions), RandomDay(monthStart));
        }

        private void AddHolidaysAndGifts(DateTime monthStart, decimal inflation)
        {
            switch (monthStart.Month)
            {
                case 2 or 6 or 10:
                    Expense(accounts.CreditCard, CategoryEnum.Gifts, Amount(30m, 80m), "Regalo di compleanno", RandomDay(monthStart));
                    break;
                case 4 when Chance(0.5):
                    Expense(accounts.CreditCard, CategoryEnum.Entertainment, Amount(150m, 350m, inflation), "B&B weekend di Pasqua", RandomDay(monthStart));
                    break;
                case 5:
                    Expense(accounts.CreditCard, CategoryEnum.Transportation, Amount(150m, 380m, inflation), "Biglietti aerei vacanze estive", RandomDay(monthStart));
                    break;
                case 8:
                    Expense(accounts.CreditCard, CategoryEnum.Entertainment, Amount(550m, 1200m, inflation), "Hotel vacanze estive", Day(monthStart, random.Next(5, 12)));
                    for (var i = random.Next(3, 7); i > 0; i--)
                        Expense(accounts.CreditCard, CategoryEnum.FoodAndBeverage, Amount(25m, 90m, inflation), "Ristorante in vacanza", RandomDay(monthStart, 5, 25));
                    break;
                case 12:
                    for (var i = random.Next(3, 7); i > 0; i--)
                        Expense(accounts.CreditCard, CategoryEnum.Gifts, Amount(20m, 120m), "Regalo di Natale", RandomDay(monthStart, 5, 24));
                    break;
            }

            if (monthStart.Month is 5 or 6 or 9 && Chance(0.15))
                Expense(accounts.Checking, CategoryEnum.Gifts, random.Next(3, 7) * 50m, "Regalo di nozze", RandomDay(monthStart));
        }

        private void AddCash(DateTime monthStart)
        {
            // Si preleva solo quando il contante è quasi finito; le spese sono sempre successive
            // al prelievo e non superano il contante disponibile, così il saldo non va mai sotto zero.
            var firstSpendDay = 1;
            if (Balance(accounts.Cash) < 60m)
            {
                var withdrawalDate = RandomDay(monthStart, 1, 10);
                Transfer(accounts.Checking, accounts.Cash, random.Next(10, 21) * 10m, withdrawalDate,
                    "Prelievo bancomat", "Prelievo contanti da Conto Corrente");
                firstSpendDay = withdrawalDate.Day;
            }

            var floor = Amount(5m, 30m);
            for (var i = random.Next(5, 11); i > 0 && Balance(accounts.Cash) > floor + 1m; i--)
            {
                var (category, description, min, max) = CashExpenses[random.Next(CashExpenses.Length)];
                var amount = Math.Min(Amount(min, max), Balance(accounts.Cash) - floor);
                if (amount < 1m)
                    break;

                Expense(accounts.Cash, category, Math.Round(amount, 2), description, RandomDay(monthStart, firstSpendDay));
            }
        }

        private void AddInvestments(DateTime monthStart, int yearIndex)
        {
            var month = monthStart.Month;
            var pacDate = PreviousWorkingDay(Day(monthStart, 10));
            var pac = yearIndex >= 2 ? 300m : 200m;

            if (month == 1 && Balance(accounts.Broker) >= 50m)
            {
                var reinvest = Math.Floor(Balance(accounts.Broker));
                if (Expense(accounts.Broker, CategoryEnum.SavingsAndInvestments, reinvest, "Reinvestimento dividendi", pacDate))
                    _investedCapital += reinvest;
            }

            Transfer(accounts.Checking, accounts.Broker, pac, pacDate, "Bonifico PAC verso Conto Titoli", "Versamento da Conto Corrente");
            if (Expense(accounts.Broker, CategoryEnum.SavingsAndInvestments, pac, "Acquisto ETF azionario globale (PAC)", pacDate))
                _investedCapital += pac;

            if (month % 3 == 0 && _investedCapital > 0m)
            {
                var dividend = Math.Round(_investedCapital * (decimal)(0.0025 + random.NextDouble() * 0.002), 2);
                Income(accounts.Broker, CategoryEnum.SavingsAndInvestments, dividend, "Dividendo ETF (netto)", PreviousWorkingDay(Day(monthStart, 20)));
            }
        }

        private void AddSavings(DateTime monthStart)
        {
            var savingsBalance = Balance(accounts.Savings);
            if (monthStart.Month % 3 == 0 && savingsBalance > 0m)
            {
                // Tasso lordo 2,5% annuo, al netto della ritenuta del 26%, liquidato trimestralmente.
                var interest = Math.Round(savingsBalance * 0.025m * 0.74m / 4m, 2);
                Income(accounts.Savings, CategoryEnum.SavingsAndInvestments, interest, "Interessi netti maturati", Day(monthStart, 31));
            }

            var date = PreviousWorkingDay(Day(monthStart, 28));
            var checkingBalance = Balance(accounts.Checking);

            if (checkingBalance < CheckingMinimumBalance)
            {
                var needed = Math.Ceiling((CheckingTargetBalance - checkingBalance) / 100m) * 100m;
                var available = Math.Floor(Balance(accounts.Savings) / 100m) * 100m;
                Transfer(accounts.Savings, accounts.Checking, Math.Min(needed, available), date,
                    "Giroconto verso Conto Corrente", "Giroconto da Conto Deposito");
            }
            else if (checkingBalance > CheckingTargetBalance + 100m)
            {
                var amount = Math.Floor((checkingBalance - CheckingTargetBalance) * 0.7m / 50m) * 50m;
                Transfer(accounts.Checking, accounts.Savings, amount, date,
                    "Giroconto verso Conto Deposito", "Giroconto da Conto Corrente");
            }
        }

        private bool Income(Account account, CategoryEnum category, decimal amount, string description, DateTime date)
            => AddMovement(account, category, amount, description, date, null);

        private bool Expense(Account account, CategoryEnum category, decimal amount, string description, DateTime date)
            => AddMovement(account, category, -amount, description, date, null);

        private void Transfer(Account from, Account to, decimal amount, DateTime date, string fromDescription, string toDescription)
        {
            if (amount <= 0m || !IsInRange(date))
                return;

            var transfer = new Transfer { CreatedOn = _createdOn };
            Transfers.Add(transfer);

            AddMovement(from, CategoryEnum.MoneyTransfers, -amount, fromDescription, date, transfer);
            AddMovement(to, CategoryEnum.MoneyTransfers, amount, toDescription, date, transfer);
        }

        // I movimenti fuori dal periodo (es. date future nel mese corrente) vengono scartati.
        private bool AddMovement(Account account, CategoryEnum category, decimal amount, string description, DateTime date, Transfer? transfer)
        {
            if (amount == 0m || !IsInRange(date))
                return false;

            Transactions.Add(new Transaction
            {
                AccountId = account.Id,
                CategoryId = categories.IdOf(category),
                Amount = Math.Round(amount, 2),
                Description = description,
                Date = date,
                CreatedOn = _createdOn,
                TransferNavigation = transfer,
            });

            _balances[account] = Balance(account) + Math.Round(amount, 2);
            return true;
        }

        private bool IsInRange(DateTime date) => date >= fromDate && date <= toDate;

        private decimal Balance(Account account) => _balances.GetValueOrDefault(account);

        private static DateTime Day(DateTime monthStart, int day)
            => monthStart.AddDays(Math.Min(day, DateTime.DaysInMonth(monthStart.Year, monthStart.Month)) - 1);

        // Nel mese corrente il giorno casuale non supera la data odierna.
        private DateTime RandomDay(DateTime monthStart, int minDay = 1, int maxDay = 31)
        {
            var lastDay = Math.Min(maxDay, DateTime.DaysInMonth(monthStart.Year, monthStart.Month));
            if (monthStart.Year == toDate.Year && monthStart.Month == toDate.Month)
                lastDay = Math.Min(lastDay, toDate.Day);

            return lastDay < minDay
                ? Day(monthStart, minDay)
                : Day(monthStart, random.Next(minDay, lastDay + 1));
        }

        private static DateTime PreviousWorkingDay(DateTime date) => date.DayOfWeek switch
        {
            DayOfWeek.Saturday => date.AddDays(-1),
            DayOfWeek.Sunday => date.AddDays(-2),
            _ => date,
        };

        private decimal Amount(decimal min, decimal max, decimal factor = 1m)
            => Math.Round((min + (decimal)random.NextDouble() * (max - min)) * factor, 2);

        private bool Chance(double probability) => random.NextDouble() < probability;

        private T Pick<T>(T[] items) => items[random.Next(items.Length)];
    }
}
