using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace RDS.ExpenseTracker.Ingestor
{
    public class OneDriveWebhook
    {
        private readonly ILogger<OneDriveWebhook> _logger;

        public OneDriveWebhook(ILogger<OneDriveWebhook> logger)
        {
            _logger = logger;
        }

        [Function("OneDriveWebhook")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            return new OkObjectResult("Welcome to Azure Functions!");
        }
    }
}
