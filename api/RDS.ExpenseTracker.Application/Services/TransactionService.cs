using AutoMapper;
using FluentResults;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Dtos;
using RDS.ExpenseTracker.Domain.Dtos.Requests;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Enums;
using RDS.ExpenseTracker.Domain.Repositories;
using RDS.ExpenseTracker.Domain.Services;
using System.Globalization;

namespace RDS.ExpenseTracker.Application.Services;

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _repository;
    private readonly IAccountRepository _accountRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IMapper _mapper;

    public TransactionService(
        ITransactionRepository repository,
        IAccountRepository accountRepository,
        ICategoryRepository categoryRepository,
        IMapper mapper)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<Result<TransactionQueryResult>> GetPagedTransactions(TransactionQueryRequest request)
    {
        var (items, totalCount, totalIncomes, totalOutcomes, totalNet) = await _repository.GetPagedTransactions(request);
        return Result.Ok(new TransactionQueryResult
        {
            Items = _mapper.Map<IEnumerable<TransactionDto>>(items),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalIncomes = totalIncomes,
            TotalOutcomes = totalOutcomes,
            TotalNet = totalNet
        });
    }

    public async Task<Result<IEnumerable<TransactionMonthOptionDto>>> GetAvailableMonthOptions()
    {
        var cultureInfo = new CultureInfo("it-IT");
        var monthOptions = (await _repository.GetAvailableMonthRanges())
            .Select(range => new TransactionMonthOptionDto
            {
                StartDate = range.StartDate,
                EndDate = range.EndDate,
                Description = cultureInfo.TextInfo.ToTitleCase(
                    range.StartDate.ToString("MMMM yyyy", cultureInfo))
            });

        return Result.Ok(monthOptions);
    }

    public async Task<Result<TransactionDto?>> GetTransaction(int id)
    {
        if (id <= 0)
            return Result.Fail(DomainErrors.InvalidId("transaction", id));

        var transaction = await _repository.GetTransaction(id);
        if (transaction is null)
            return Result.Fail(DomainErrors.NotFound("Transaction", id));

        return Result.Ok(_mapper.Map<TransactionDto?>(transaction));
    }

    public async Task<Result<TransactionDto?>> GetLatestTransaction()
    {
        var transaction = await _repository.GetLatestTransaction();
        if (transaction is null || transaction.Id <= 0)
            return Result.Fail(DomainErrors.NotFound("Latest transaction"));

        return Result.Ok(_mapper.Map<TransactionDto?>(transaction));
    }

    public async Task<Result> AddTransactions(IEnumerable<TransactionDto> dtos)
    {
        if (dtos is null || !dtos.Any())
            return Result.Fail(DomainErrors.Required("transactions"));

        var entities = _mapper.Map<IEnumerable<Transaction>>(dtos);
        await _repository.AddTransactions(entities);
        await _repository.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result<int>> AddTransaction(TransactionDto dto)
    {
        if (dto.AccountId <= 0)
            return Result.Fail(DomainErrors.InvalidId("account", dto.AccountId));

        var account = await _accountRepository.GetAccount(dto.AccountId);
        if (account is null)
            return Result.Fail(DomainErrors.NotFound("Account", dto.AccountId));

        var entity = _mapper.Map<Transaction>(dto);
        await _repository.AddTransactions([entity]);
        await _repository.SaveChangesAsync();
        return Result.Ok(entity.Id);
    }

    public async Task<Result> UpdateTransaction(TransactionDto dto)
    {
        if (dto.Id <= 0)
            return Result.Fail(DomainErrors.InvalidId("transaction", dto.Id));

        var existing = await _repository.GetTransaction(dto.Id);
        if (existing is null)
            return Result.Fail(DomainErrors.NotFound("Transaction", dto.Id));

        var entity = _mapper.Map<Transaction>(dto);
        await _repository.UpdateTransaction(entity);
        await _repository.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result> DeleteTransaction(int id)
    {
        if (id <= 0)
            return Result.Fail(DomainErrors.InvalidId("transaction", id));

        var existing = await _repository.GetTransaction(id);
        if (existing is null)
            return Result.Fail(DomainErrors.NotFound("Transaction", id));

        await _repository.DeleteTransaction(id);
        await _repository.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result> DeleteAllTransactions()
    {
        await _repository.DeleteAllTransactions();
        await _repository.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result<TimeSeriesListDto>> GetTimeSeries(TimeSeriesRequestDto request)
    {
        if (request == null)
            return Result.Fail(DomainErrors.Required("request"));

        if (request.StartDate > request.EndDate)
            return Result.Fail(DomainErrors.BadRequest("EndDate must be greater than StartDate"));

        var rawGranularity = request.Granularity;
        var granularity = Enum.IsDefined(typeof(TimeGranularityEnum), rawGranularity)
            ? (TimeGranularityEnum)rawGranularity
            : TimeGranularityEnum.Daily;

        var filtered = (await _repository.GetTimeSeriesTransactions(request)).ToList();

        if (!filtered.Any())
            return Result.Ok(new TimeSeriesListDto
            {
                Granularity = granularity.ToString(),
                Series = []
            });

        var series = new List<TimeSeriesDto>();

        if (request.IdAccounts.Any())
        {
            foreach (var accountId in request.IdAccounts)
            {
                var points = filtered
                    .Where(t => t.AccountId == accountId)
                    .GroupBy(t => GetPeriodKey(t.Date, granularity))
                    .OrderBy(g => g.Key)
                    .Select(g => new TimeSeriesPointDto
                    {
                        Period = g.Key,
                        Amount = g.Sum(x => x.Amount),
                        Earned = g.Where(x => x.Amount > 0).Sum(x => x.Amount),
                        Spent = Math.Abs(g.Where(x => x.Amount < 0).Sum(x => x.Amount))
                    })
                    .ToList();

                series.Add(new TimeSeriesDto
                {
                    Dimensions = [new TimeSeriesDimensionDto { Key = "AccountId", Value = accountId.ToString() }],
                    Values = points
                });
            }
        }
        else if (request.IdCategories.Any())
        {
            foreach (var categoryId in request.IdCategories)
            {
                var points = filtered
                    .Where(t => t.CategoryId == categoryId)
                    .GroupBy(t => GetPeriodKey(t.Date, granularity))
                    .OrderBy(g => g.Key)
                    .Select(g => new TimeSeriesPointDto
                    {
                        Period = g.Key,
                        Amount = g.Sum(x => x.Amount),
                        Earned = g.Where(x => x.Amount > 0).Sum(x => x.Amount),
                        Spent = Math.Abs(g.Where(x => x.Amount < 0).Sum(x => x.Amount))
                    })
                    .ToList();

                series.Add(new TimeSeriesDto
                {
                    Dimensions = [new TimeSeriesDimensionDto { Key = "CategoryId", Value = categoryId.ToString() }],
                    Values = points
                });
            }
        }
        else
        {
            var points = filtered
                .GroupBy(t => GetPeriodKey(t.Date, granularity))
                .OrderBy(g => g.Key)
                .Select(g => new TimeSeriesPointDto
                {
                    Period = g.Key,
                    Amount = g.Sum(x => x.Amount),
                    Earned = g.Where(x => x.Amount > 0).Sum(x => x.Amount),
                    Spent = Math.Abs(g.Where(x => x.Amount < 0).Sum(x => x.Amount))
                })
                .ToList();

            series.Add(new TimeSeriesDto
            {
                Dimensions = [],
                Values = points
            });
        }

        return Result.Ok(new TimeSeriesListDto
        {
            Granularity = granularity.ToString(),
            Series = series
        });
    }

    public async Task<Result<TimeSeriesListDto>> GetStock(TimeSeriesRequestDto request)
    {
        if (request == null)
            return Result.Fail(DomainErrors.Required("request"));

        if (request.StartDate > request.EndDate)
            return Result.Fail(DomainErrors.BadRequest("EndDate must be greater than StartDate"));

        var rawGranularity = request.Granularity;
        var granularity = Enum.IsDefined(typeof(TimeGranularityEnum), rawGranularity)
            ? (TimeGranularityEnum)rawGranularity
            : TimeGranularityEnum.Daily;

        // La giacenza di un conto deve riflettere ogni movimento di denaro reale, trasferimenti
        // compresi: un giroconto sposta denaro dentro/fuori dal singolo conto anche se si annulla
        // a livello di patrimonio totale. Escluderlo (come fa invece GetTimeSeries per i flussi
        // entrate/uscite) renderebbe il saldo calcolato sistematicamente errato per i conti con
        // trasferimenti, quindi qui il flag ExcludeTransfers del chiamante viene ignorato.
        var balanceRequest = new TimeSeriesRequestDto
        {
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IdAccounts = request.IdAccounts,
            IdCategories = request.IdCategories,
            Granularity = request.Granularity,
            ExcludeTransfers = false
        };

        var filtered = (await _repository.GetTimeSeriesTransactionsUntilDate(balanceRequest)).ToList();

        var series = new List<TimeSeriesDto>();

        if (request.IdAccounts.Any())
        {
            foreach (var accountId in request.IdAccounts)
            {
                var accountTransactions = filtered
                    .Where(t => t.AccountId == accountId)
                    .Select(t => (t.Date, t.Amount))
                    .ToList();

                series.Add(new TimeSeriesDto
                {
                    Dimensions = [new TimeSeriesDimensionDto { Key = "AccountId", Value = accountId.ToString() }],
                    Values = BuildAveragePoints(accountTransactions, request.StartDate, request.EndDate, granularity)
                });
            }
        }
        else if (request.IdCategories.Any())
        {
            foreach (var categoryId in request.IdCategories)
            {
                var categoryTransactions = filtered
                    .Where(t => t.CategoryId == categoryId)
                    .Select(t => (t.Date, t.Amount))
                    .ToList();

                series.Add(new TimeSeriesDto
                {
                    Dimensions = [new TimeSeriesDimensionDto { Key = "CategoryId", Value = categoryId.ToString() }],
                    Values = BuildAveragePoints(categoryTransactions, request.StartDate, request.EndDate, granularity)
                });
            }
        }
        else
        {
            var totalTransactions = filtered
                .Select(t => (t.Date, t.Amount))
                .ToList();

            series.Add(new TimeSeriesDto
            {
                Dimensions = [],
                Values = BuildAveragePoints(totalTransactions, request.StartDate, request.EndDate, granularity)
            });
        }

        return Result.Ok(new TimeSeriesListDto
        {
            Granularity = granularity.ToString(),
            Series = series
        });
    }

    public async Task<Result<LandingDashboardDto>> GetLanding(bool excludeTransfers = true)
    {
        var asOf = DateTime.Now;
        var monthStart = new DateTime(asOf.Year, asOf.Month, 1);

        var accounts = (await _accountRepository.GetAccounts()).ToList();
        var balances = (await _repository.GetAccountBalances(asOf))
            .ToDictionary(item => item.AccountId, item => item.Balance);
        var accountTotals = (await _repository.GetAccountMonthTotals(monthStart, asOf, excludeTransfers))
            .ToDictionary(item => item.AccountId);

        var accountItems = accounts
            .Select(account =>
            {
                var hasItem = accountTotals.TryGetValue(account.Id, out var item);
                var spent = hasItem ? item.Spent : 0m;
                var earned = hasItem ? item.Earned : 0m;

                return new LandingAccountBalanceDto
                {
                    AccountId = account.Id,
                    Name = account.Name,
                    CurrentBalance = balances.GetValueOrDefault(account.Id, 0m),
                    SpentMonth = spent,
                    EarnedMonth = earned,
                    NetMonth = earned - spent
                };
            })
            .OrderBy(item => item.Name)
            .ToList();

        var categories = (await _categoryRepository.GetCategories()).ToList();
        var categoryTotals = (await _repository.GetCategoryMonthTotals(monthStart, asOf, excludeTransfers))
            .ToDictionary(item => item.CategoryId);

        var categoryItems = categories
            .Select(category =>
            {
                var hasItem = categoryTotals.TryGetValue(category.Id, out var item);
                var spent = hasItem ? item.Spent : 0m;
                var earned = hasItem ? item.Earned : 0m;

                return new LandingCategorySummaryDto
                {
                    CategoryId = category.Id,
                    Name = category.Name,
                    SpentMonth = spent,
                    EarnedMonth = earned,
                    NetMonth = earned - spent
                };
            })
            .OrderBy(item => item.Name)
            .ToList();

        var monthTotals = await _repository.GetMonthTotals(monthStart, asOf, excludeTransfers);

        var netWorthSeries = await BuildNetWorthSeries(asOf);

        return Result.Ok(new LandingDashboardDto
        {
            AsOf = asOf,
            MonthStart = monthStart,
            Accounts = accountItems,
            Categories = categoryItems,
            Totals = new LandingTotalsDto
            {
                CurrentBalanceTotal = accountItems.Sum(item => item.CurrentBalance),
                SpentMonth = monthTotals.Spent,
                EarnedMonth = monthTotals.Earned,
                NetMonth = monthTotals.Earned - monthTotals.Spent
            },
            NetWorthSeries = netWorthSeries
        });
    }

    private async Task<TimeSeriesListDto> BuildNetWorthSeries(DateTime asOf)
    {
        var startMonth = new DateTime(asOf.Year, asOf.Month, 1).AddMonths(-11);

        var request = new TimeSeriesRequestDto
        {
            StartDate = startMonth,
            EndDate = asOf,
            Granularity = (int)TimeGranularityEnum.Monthly,
            IdAccounts = [],
            IdCategories = [],
            ExcludeTransfers = false
        };

        var stockResult = await GetStock(request);
        return stockResult.IsSuccess
            ? stockResult.Value
            : new TimeSeriesListDto
            {
                Granularity = TimeGranularityEnum.Monthly.ToString(),
                Series = []
            };
    }

    private static List<TimeSeriesPointDto> BuildAveragePoints(
        IEnumerable<(DateTime Date, decimal Amount)> orderedTransactions,
        DateTime startDate,
        DateTime endDate,
        TimeGranularityEnum granularity)
    {
        var startPeriodKey = GetPeriodKey(startDate, granularity);

        var runningTotal = 0m;
        var carryBalance = 0m;
        var samplesByPeriod = new Dictionary<string, List<decimal>>();
        var lastBalanceByPeriod = new Dictionary<string, decimal>();

        foreach (var (date, amount) in orderedTransactions)
        {
            runningTotal += amount;
            var periodKey = GetPeriodKey(date, granularity);

            if (string.Compare(periodKey, startPeriodKey, StringComparison.Ordinal) < 0)
            {
                carryBalance = runningTotal;
                continue;
            }

            if (!samplesByPeriod.TryGetValue(periodKey, out var samples))
            {
                samples = [];
                samplesByPeriod[periodKey] = samples;
            }

            samples.Add(runningTotal);
            lastBalanceByPeriod[periodKey] = runningTotal;
        }

        var points = new List<TimeSeriesPointDto>();

        foreach (var periodKey in EnumeratePeriodKeys(startDate, endDate, granularity))
        {
            if (samplesByPeriod.TryGetValue(periodKey, out var samples))
            {
                points.Add(new TimeSeriesPointDto { Period = periodKey, Amount = samples.Average() });
                carryBalance = lastBalanceByPeriod[periodKey];
            }
            else
            {
                points.Add(new TimeSeriesPointDto { Period = periodKey, Amount = carryBalance });
            }
        }

        return points;
    }

    private static IEnumerable<string> EnumeratePeriodKeys(DateTime startDate, DateTime endDate, TimeGranularityEnum granularity)
    {
        string? lastKey = null;

        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            var key = GetPeriodKey(date, granularity);
            if (key != lastKey)
            {
                yield return key;
                lastKey = key;
            }
        }
    }

    private static string GetPeriodKey(DateTime date, TimeGranularityEnum granularity)
    {
        return granularity switch
        {
            TimeGranularityEnum.Daily => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            TimeGranularityEnum.Weekly => $"{ISOWeek.GetYear(date)}-W{ISOWeek.GetWeekOfYear(date):00}",
            TimeGranularityEnum.Monthly => date.ToString("yyyy-MM", CultureInfo.InvariantCulture),
            TimeGranularityEnum.Yearly => date.Year.ToString(CultureInfo.InvariantCulture),
            _ => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
        };
    }
}
