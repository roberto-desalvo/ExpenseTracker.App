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
                        Amount = g.Sum(x => x.Amount)
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
                        Amount = g.Sum(x => x.Amount)
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
                    Amount = g.Sum(x => x.Amount)
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

        var startPeriodKey = GetPeriodKey(request.StartDate, granularity);

        var filtered = (await _repository.GetTimeSeriesTransactionsUntilDate(request)).ToList();

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
                        Amount = g.Sum(x => x.Amount)
                    })
                    .ToList();

                var incrementalPoints = BuildIncrementalPoints(points, startPeriodKey);

                series.Add(new TimeSeriesDto
                {
                    Dimensions = [new TimeSeriesDimensionDto { Key = "AccountId", Value = accountId.ToString() }],
                    Values = incrementalPoints
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
                        Amount = g.Sum(x => x.Amount)
                    })
                    .ToList();

                var incrementalPoints = BuildIncrementalPoints(points, startPeriodKey);

                series.Add(new TimeSeriesDto
                {
                    Dimensions = [new TimeSeriesDimensionDto { Key = "CategoryId", Value = categoryId.ToString() }],
                    Values = incrementalPoints
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
                    Amount = g.Sum(x => x.Amount)
                })
                .ToList();

            var incrementalPoints = BuildIncrementalPoints(points, startPeriodKey);

            series.Add(new TimeSeriesDto
            {
                Dimensions = [],
                Values = incrementalPoints
            });
        }

        return Result.Ok(new TimeSeriesListDto
        {
            Granularity = granularity.ToString(),
            Series = series
        });
    }

    public async Task<Result<LandingDashboardDto>> GetLanding()
    {
        var asOf = DateTime.Now;
        var monthStart = new DateTime(asOf.Year, asOf.Month, 1);

        var accounts = (await _accountRepository.GetAccounts()).ToList();
        var balances = (await _repository.GetAccountBalances(asOf))
            .ToDictionary(item => item.AccountId, item => item.Balance);

        var accountItems = accounts
            .Select(account => new LandingAccountBalanceDto
            {
                AccountId = account.Id,
                Name = account.Name,
                CurrentBalance = balances.GetValueOrDefault(account.Id, 0m)
            })
            .OrderBy(item => item.Name)
            .ToList();

        var categories = (await _categoryRepository.GetCategories()).ToList();
        var categoryTotals = (await _repository.GetCategoryMonthTotals(monthStart, asOf))
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

        var monthTotals = await _repository.GetMonthTotals(monthStart, asOf);

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
        var baselineDate = startMonth.AddTicks(-1);
        var baselineBalance = (await _repository.GetAccountBalances(baselineDate)).Sum(item => item.Balance);

        var request = new TimeSeriesRequestDto
        {
            StartDate = startMonth,
            EndDate = asOf,
            Granularity = (int)TimeGranularityEnum.Monthly,
            IdAccounts = [],
            IdCategories = []
        };

        var stockResult = await GetStock(request);
        var stockSeries = stockResult.IsSuccess
            ? stockResult.Value
            : new TimeSeriesListDto
            {
                Granularity = TimeGranularityEnum.Monthly.ToString(),
                Series = []
            };

        var rawPoints = stockSeries.Series.FirstOrDefault()?.Values ?? [];
        var pointByPeriod = rawPoints
            .OrderBy(point => point.Period)
            .ToDictionary(point => point.Period, point => point.Amount);

        var normalized = new List<TimeSeriesPointDto>();
        var runningAmount = baselineBalance;

        for (var month = startMonth; month <= asOf; month = month.AddMonths(1))
        {
            var periodKey = month.ToString("yyyy-MM", CultureInfo.InvariantCulture);
            if (pointByPeriod.TryGetValue(periodKey, out var amount))
            {
                runningAmount = amount;
            }

            normalized.Add(new TimeSeriesPointDto
            {
                Period = periodKey,
                Amount = runningAmount
            });
        }

        return new TimeSeriesListDto
        {
            Granularity = TimeGranularityEnum.Monthly.ToString(),
            Series =
            [
                new TimeSeriesDto
                {
                    Dimensions = [],
                    Values = normalized
                }
            ]
        };
    }

    private static List<TimeSeriesPointDto> BuildIncrementalPoints(IEnumerable<TimeSeriesPointDto> points, string startPeriodKey)
    {
        var runningTotal = 0m;
        var incrementalPoints = new List<TimeSeriesPointDto>();

        foreach (var point in points)
        {
            runningTotal += point.Amount;

            if (string.Compare(point.Period, startPeriodKey, StringComparison.Ordinal) >= 0)
            {
                incrementalPoints.Add(new TimeSeriesPointDto
                {
                    Period = point.Period,
                    Amount = runningTotal
                });
            }
        }

        return incrementalPoints;
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
