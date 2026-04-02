using RDS.ExpenseTracker.DataImport.Business.Pipelines.Steps.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDS.ExpenseTracker.DataImport.Business.Pipelines.Abstractions
{
    public interface IPipeline<TIn, TOut>
    {
        Pipeline<TIn, TNextOut> AddStep<TNextOut>(IPipelineStep<TOut, TNextOut> nextStep);
        Task<TOut> ProcessAsync(TIn input);
    }
}
