using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Repositories;

namespace RDS.ExpenseTracker.Infrastructure.Repositories;

public class UserRepository : RepositoryBase, IUserRepository
{
    public UserRepository(ExpenseTrackerContext context)
        : base(context)
    {
    }

    public async Task<User?> GetByAzureOid(string azureOid)
    {
        return await Context.Users.FirstOrDefaultAsync(u => u.AzureOid == azureOid);
    }

    public async Task<User?> GetByAppOid(string appOid)
    {
        return await Context.Users.FirstOrDefaultAsync(u => u.AppOid == appOid);
    }

    public async Task<User?> GetById(int id)
    {
        return await Context.Users.FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User> GetOrCreateUserAsync(string azureOid, string email)
    {
        var existing = await GetByAzureOid(azureOid);
        if (existing is not null)
        {
            return existing;
        }

        var user = new User(0, email) { AzureOid = azureOid };
        Context.Users.Add(user);

        try
        {
            await Context.SaveChangesAsync();
            return user;
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            // Race: un'altra richiesta concorrente ha creato lo stesso utente nel frattempo.
            Context.Entry(user).State = EntityState.Detached;
            return await GetByAzureOid(azureOid)
                ?? throw new InvalidOperationException("Concurrent user creation failed unexpectedly.", ex);
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        => ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627);
}
