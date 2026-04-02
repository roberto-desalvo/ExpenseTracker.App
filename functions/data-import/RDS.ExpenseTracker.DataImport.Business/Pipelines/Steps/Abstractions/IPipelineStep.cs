using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDS.ExpenseTracker.DataImport.Business.Pipelines.Steps.Abstractions
{
    public interface IPipelineStep<T> : IPipelineStep<T, T>
    {
    }

    public interface IPipelineStep<TIn, TOut>
    {
        Task<TOut> ProcessAsync(TIn input);
    }
}
