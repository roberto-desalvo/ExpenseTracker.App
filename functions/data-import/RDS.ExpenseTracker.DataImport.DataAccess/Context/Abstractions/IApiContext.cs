namespace RDS.ExpenseTracker.DataImport.DataAccess.Context.Abstractions
{
    public interface IApiContext
    {
        Task<T> GetAsync<T>(string uri);
        Task PostAsync<TRequest>(string uri, TRequest data);
        Task<TResponse> PostAsync<TRequest, TResponse>(string uri, TRequest data);
        Task PutAsync<T>(string uri, T data);
        Task DeleteAsync(string uri);
    }
}