using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using RDS.ExpenseTracker.DataImport.Business.Pipelines.Factories.Abstractions;
using RDS.ExpenseTracker.DataImport.Business.Services.Abstractions;

namespace RDS.ExpenseTracker.DataImport.IsolatedFunctions;

public class MainFunction
{
    private readonly IPipelineFactory _pipelineFactory;
    private readonly ITransactionService _transactionService;
    private readonly ILogger<MainFunction> _logger;
    public MainFunction(IPipelineFactory pipelineFactory, ITransactionService transactionService, ILogger<MainFunction> logger)
    {
        _pipelineFactory = pipelineFactory ?? throw new ArgumentNullException(nameof(pipelineFactory));
        _transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [Function("ImportNew")]
    public async Task<IActionResult> RunImportNew(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req
        )
    {
        var fileContent = await req.ReadFormAsync();
        var file = fileContent.Files["fileContent"];

        if (file == null)
        {
            var errorMessage = "No file found in the request.";
            _logger.LogWarning(errorMessage);
            return new BadRequestObjectResult(errorMessage);
        }

        var pipeline = _pipelineFactory.CreateExcelDataExtractionPipeline(false);

        try
        {
            var transactions = await pipeline.ProcessAsync(file);
            if (transactions.Count == 0)
            {
                var message = "No new transactions found for adding";
                _logger.LogInformation(message);
                return new ObjectResult(message);
            }

            _logger.LogInformation("New transactions extracted from the file: {TransactionCount}", transactions.Count);

            await _transactionService.AddRangeAsync(transactions);
            return new OkObjectResult($"Imported {transactions.Count} transactions");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during file import and data reset");
            return new ObjectResult($"An error occurred during file import and data reset:\n{ex}, {ex.Message}")
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }


    [Function("ImportAll")]
    public async Task<IActionResult> RunImportAll(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req
            )
    {
        var fileContent = await req.ReadFormAsync();
        var file = fileContent.Files["fileContent"];

        if (file == null)
        {
            var errorMessage = "No file found in the request.";
            _logger.LogWarning(errorMessage);
            return new BadRequestObjectResult(errorMessage);
        }

        var pipeline = _pipelineFactory.CreateExcelDataExtractionPipeline(true);

        try
        {
            var transactions = await pipeline.ProcessAsync(file);
            if (transactions.Count == 0)
            {
                var message = "No new transactions found for adding";
                _logger.LogInformation(message);
                return new ObjectResult(message);
            }

            _logger.LogInformation("New transactions extracted from the file: {TransactionCount}", transactions.Count);

            await _transactionService.ResetAllTransactions(transactions);
            return new OkObjectResult($"Imported {transactions.Count} transactions");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during file import and data reset");
            return new ObjectResult($"An error occurred during file import and data reset:\n{ex}, {ex.Message}")
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}
