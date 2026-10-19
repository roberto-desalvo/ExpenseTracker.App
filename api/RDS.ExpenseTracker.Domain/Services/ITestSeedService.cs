using FluentResults;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Dtos;

namespace RDS.ExpenseTracker.Domain.Services;

public interface ITestSeedService : IService
{
    // Rigenera da zero conti e movimenti fittizi dell'utente di test (creandolo se non esiste).
    // Pensato per essere chiamato in modo schedulato per riportare l'ambiente a dati puliti.
    Task<Result<TestSeedResultDto>> ResetTestUserDataAsync();
}
