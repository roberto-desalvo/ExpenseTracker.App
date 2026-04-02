using Microsoft.Extensions.Logging;
using RDS.ExpenseTracker.DataImport.Business.Pipelines.Abstractions;
using RDS.ExpenseTracker.DataImport.Business.Pipelines.Steps.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDS.ExpenseTracker.DataImport.Business.Pipelines
{
    public class Pipeline<TIn> : Pipeline<TIn, TIn>
    {
        public Pipeline(ILogger<Pipeline<TIn>> logger) : base(logger, x => Task.FromResult(x))
        {
        }
    }

    public class Pipeline<TIn, TOut> : IPipeline<TIn, TOut>
    {
        private readonly Func<TIn, Task<TOut>> _pipelineFunc;
        protected readonly ILogger<Pipeline<TIn>> _logger;

        public Pipeline(ILogger<Pipeline<TIn>>  logger, Func<TIn, Task<TOut>> pipelineFunc)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _pipelineFunc = pipelineFunc ?? throw new ArgumentNullException(nameof(pipelineFunc));
        }

        public Task<TOut> ProcessAsync(TIn input)
        {
            try
            {
                return _pipelineFunc(input);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the pipeline.");
                throw;
            }
        }

        public Pipeline<TIn, TNextOut> AddStep<TNextOut>(IPipelineStep<TOut, TNextOut> nextStep)
        {
            return new Pipeline<TIn, TNextOut>(_logger, async input =>
            {
                var intermediate = await _pipelineFunc(input);
                return await nextStep.ProcessAsync(intermediate);
            });
        }
    }

}
