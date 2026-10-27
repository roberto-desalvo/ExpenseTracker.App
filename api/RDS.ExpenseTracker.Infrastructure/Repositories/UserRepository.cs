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

    public async Task<User?> GetByEmail(string email)
    {
        // L'email non è univoca (lo stesso indirizzo può avere più utenti): si privilegia
        // l'utente con AppOid valorizzato, poi il più vecchio.
        return await Context.Users
            .Where(u => u.Email == email)
            .OrderByDescending(u => u.AppOid != null)
            .ThenBy(u => u.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<User> GetOrCreateUserAsync(string azureOid, string email)
    {
        var existing = await GetByAzureOid(azureOid);
        if (existing is not null)
        {
            return existing;
        }

        // Ogni nuovo utente parte dalle categorie di default, salvate insieme all'utente.
        var user = new User(0, email) { AzureOid = azureOid, Categories = [.. Category.CreateDefaultCategories(0)] };
        Context.Users.Add(user);

        try
        {
            await Context.SaveChangesAsync();
            return user;
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            // Race: un'altra richiesta concorrente ha creato lo stesso utente nel frattempo.
            foreach (var category in user.Categories)
                Context.Entry(category).State = EntityState.Detached;
            Context.Entry(user).State = EntityState.Detached;
            return await GetByAzureOid(azureOid)
                ?? throw new InvalidOperationException("Concurrent user creation failed unexpectedly.", ex);
        }
    }

    public async Task AddUser(User user)
    {
        await Context.Users.AddAsync(user);
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        => ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627);
}
