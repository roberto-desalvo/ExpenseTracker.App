using ExcelDataReader;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RDS.ExpenseTracker.DataImport.Business.Pipelines.Steps.Abstractions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDS.ExpenseTracker.DataImport.Business.Pipelines.Steps.ExcelDataExtraction
{
    public class ReadExcelFileStep : IPipelineStep<IFormFile, DataSet>
    {
        public Task<DataSet> ProcessAsync(IFormFile file)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using var stream = file.OpenReadStream();
            using var reader = ExcelReaderFactory.CreateReader(stream);
            return Task.FromResult(reader.AsDataSet());
        }
    }
}
