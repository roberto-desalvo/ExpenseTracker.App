using FluentResults;
using Microsoft.Extensions.Logging;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Enums;
using RDS.ExpenseTracker.Domain.Repositories;
using RDS.ExpenseTracker.Domain.Services;

namespace RDS.ExpenseTracker.Application.Services;

public class DemoDataService : IDemoDataService
{
    private const string CheckingAccountName = "Conto Corrente";
    private const string CreditCardAccountName = "Carta di Credito";
    private const string SavingsAccountName = "Risparmio";

    private static readonly string[] AccountNames =
    [
        CheckingAccountName,
        CreditCardAccountName,
        SavingsAccountName,
    ];

    private static readonly CategoryEnum[] RandomExpenseCategories =
    [
        CategoryEnum.FoodAndBeverage,
        CategoryEnum.Transportation,
        CategoryEnum.Entertainment,
        CategoryEnum.Clothes,
        CategoryEnum.HealthAndFitness,
    ];

    private static readonly Dictionary<CategoryEnum, string[]> ExpenseDescriptions = new()
    {
        [CategoryEnum.FoodAndBeverage] = ["Supermercato", "Ristorante", "Bar", "Pizzeria", "Consegna a domicilio"],
        [CategoryEnum.Transportation] = ["Benzina", "Abbonamento mezzi pubblici", "Parcheggio", "Manutenzione auto", "Taxi"],
        [CategoryEnum.Entertainment] = ["Cinema", "Concerto", "Abbonamento streaming", "Libreria", "Evento sportivo"],
        [CategoryEnum.Clothes] = ["Negozio di abbigliamento", "Scarpe", "Accessori"],
        [CategoryEnum.HealthAndFitness] = ["Farmacia", "Palestra", "Visita medica", "Dentista"],
        [CategoryEnum.Gifts] = ["Regalo di compleanno", "Regalo di Natale"],
    };

    private static readonly Dictionary<CategoryEnum, (decimal Min, decimal Max)> ExpenseRanges = new()
    {
        [CategoryEnum.FoodAndBeverage] = (10m, 90m),
        [CategoryEnum.Transportation] = (15m, 120m),
        [CategoryEnum.Entertainment] = (8m, 70m),
        [CategoryEnum.Clothes] = (20m, 150m),
        [CategoryEnum.HealthAndFitness] = (15m, 200m),
        [CategoryEnum.Gifts] = (20m, 150m),
    };

    private readonly IUserRepository _userRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ITransferRepository _transferRepository;
    private readonly ILogger<DemoDataService> _logger;
    private readonly Random _random = new();

    public DemoDataService(
        IUserRepository userRepository,
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository,
        ITransferRepository transferRepository,
        ILogger<DemoDataService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _transferRepository = transferRepository ?? throw new ArgumentNullException(nameof(transferRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> GenerateDemoDataAsync(int userId)
    {
        var user = await _userRepository.GetById(userId);
        if (user is null || !user.IsDemo)
            return Result.Fail(DomainErrors.Forbidden("Solo l'account demo può generare dati casuali."));

        try
        {
            await ResetExistingDataAsync(userId);

            var accounts = AccountNames.Select(name => new Account(0, name, userId)).ToList();
            await _accountRepository.AddAccounts(accounts);
            await _accountRepository.SaveChangesAsync();

            var checkingAccount = accounts.Single(a => a.Name == CheckingAccountName);
            var creditCardAccount = accounts.Single(a => a.Name == CreditCardAccountName);
            var savingsAccount = accounts.Single(a => a.Name == SavingsAccountName);

            var transactions = new List<Transaction>();
            var transfers = new List<Transfer>();
            var transferTransactions = new List<Transaction>();

            GenerateMonthlyData(checkingAccount, creditCardAccount, savingsAccount, transactions, transfers, transferTransactions);

            if (transfers.Count > 0)
            {
                foreach (var transfer in transfers)
                    await _transferRepository.AddTransfer(transfer);

                await _transactionRepository.AddTransactions(transferTransactions);
            }

            if (transactions.Count > 0)
                await _transactionRepository.AddTransactions(transactions);

            await _accountRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Generated demo data for user {UserId}: {AccountCount} accounts, {TransactionCount} transactions, {TransferCount} transfers",
                userId, accounts.Count, transactions.Count, transfers.Count);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating demo data for user {UserId}", userId);
            return Result.Fail($"Demo data generation failed: {ex.Message}");
        }
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

    private void GenerateMonthlyData(
        Account checkingAccount,
        Account creditCardAccount,
        Account savingsAccount,
        List<Transaction> transactions,
        List<Transfer> transfers,
        List<Transaction> transferTransactions)
    {
        var today = DateTime.UtcNow.Date;
        var monthsCount = _random.Next(6, 13);

        for (var offset = monthsCount - 1; offset >= 0; offset--)
        {
            var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-offset);
            var monthEnd = offset == 0 ? today : monthStart.AddMonths(1).AddDays(-1);

            DateTime RandomDateInMonth() => monthStart.AddDays(_random.Next(0, (monthEnd - monthStart).Days + 1));

            transactions.Add(new Transaction
            {
                AccountId = checkingAccount.Id,
                CategoryId = (int)CategoryEnum.WorkIncomes,
                Amount = RandomAmount(1800m, 3000m),
                Description = "Stipendio",
                Date = new DateTime(monthStart.Year, monthStart.Month, Math.Min(27, DateTime.DaysInMonth(monthStart.Year, monthStart.Month)), 0, 0, 0, DateTimeKind.Utc),
                CreatedOn = DateTime.UtcNow,
            });

            transactions.Add(new Transaction
            {
                AccountId = checkingAccount.Id,
                CategoryId = (int)CategoryEnum.Housing,
                Amount = -RandomAmount(700m, 1200m),
                Description = "Affitto",
                Date = new DateTime(monthStart.Year, monthStart.Month, 3, 0, 0, 0, DateTimeKind.Utc),
                CreatedOn = DateTime.UtcNow,
            });

            var expenseCount = _random.Next(10, 26);
            for (var i = 0; i < expenseCount; i++)
            {
                var category = RandomExpenseCategories[_random.Next(RandomExpenseCategories.Length)];
                var (min, max) = ExpenseRanges[category];
                var descriptions = ExpenseDescriptions[category];
                var account = _random.NextDouble() < 0.6 ? checkingAccount : creditCardAccount;

                transactions.Add(new Transaction
                {
                    AccountId = account.Id,
                    CategoryId = (int)category,
                    Amount = -RandomAmount(min, max),
                    Description = descriptions[_random.Next(descriptions.Length)],
                    Date = RandomDateInMonth(),
                    CreatedOn = DateTime.UtcNow,
                });
            }

            if (_random.NextDouble() < 0.35)
            {
                var descriptions = ExpenseDescriptions[CategoryEnum.Gifts];
                var (min, max) = ExpenseRanges[CategoryEnum.Gifts];

                transactions.Add(new Transaction
                {
                    AccountId = checkingAccount.Id,
                    CategoryId = (int)CategoryEnum.Gifts,
                    Amount = -RandomAmount(min, max),
                    Description = descriptions[_random.Next(descriptions.Length)],
                    Date = RandomDateInMonth(),
                    CreatedOn = DateTime.UtcNow,
                });
            }

            var transferAmount = RandomAmount(100m, 400m);
            var transferDate = new DateTime(monthStart.Year, monthStart.Month, Math.Min(28, DateTime.DaysInMonth(monthStart.Year, monthStart.Month)), 0, 0, 0, DateTimeKind.Utc);
            var transfer = new Transfer { CreatedOn = DateTime.UtcNow };
            transfers.Add(transfer);

            transferTransactions.Add(new Transaction
            {
                AccountId = checkingAccount.Id,
                CategoryId = (int)CategoryEnum.MoneyTransfers,
                Amount = -transferAmount,
                Description = "Trasferimento verso Risparmio",
                Date = transferDate,
                CreatedOn = DateTime.UtcNow,
                TransferNavigation = transfer,
            });

            transferTransactions.Add(new Transaction
            {
                AccountId = savingsAccount.Id,
                CategoryId = (int)CategoryEnum.MoneyTransfers,
                Amount = transferAmount,
                Description = "Trasferimento da Conto Corrente",
                Date = transferDate,
                CreatedOn = DateTime.UtcNow,
                TransferNavigation = transfer,
            });
        }
    }

    private decimal RandomAmount(decimal min, decimal max)
        => Math.Round(min + (decimal)_random.NextDouble() * (max - min), 2);
}
