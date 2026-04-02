using FluentAssertions;
using RDS.ExpenseTracker.DataImport.Business.Pipelines.Steps.TransactionEnrichment;
using RDS.ExpenseTracker.DataImport.Business.Pipelines.Utilities;
using RDS.ExpenseTracker.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDS.ExpenseTracker.DataImport.Business.Tests.Pipelines.Steps
{
    public class AssignDateStepTests
    {
        [Fact]
        public async Task CheckAndAssignDate_WhenTransactionDateIsNull_ShouldAssignDefaultDate()
        {
            var transaction = new Transaction { Date = null };
            var defaultDate = new DateTime(2021, 1, 1);
            var sut = new AssignDateStep(defaultDate);

            transaction = await sut.ProcessAsync(transaction);

            transaction.Date.Should().Be(defaultDate);
        }

        [Fact]
        public async Task CheckAndAssignDate_WhenTransactionDateIsNotNull_ShouldNotAssignDefaultDate()
        {
            var originalDate = new DateTime(2021, 2, 2);
            var transaction = new Transaction { Date = originalDate };
            var defaultDate = new DateTime(2021, 1, 1);
            var sut = new AssignDateStep(defaultDate);

            transaction = await sut.ProcessAsync(transaction);

            transaction.Date.Should().NotBe(defaultDate);
        }

        [Fact]
        public async Task CheckAndAssignDate_WhenTransactionDateIsNotNull_ShouldAssignDefaultDateYear()
        {
            var originalDate = new DateTime(2021, 2, 2);
            var transaction = new Transaction { Date = originalDate };
            var defaultDate = new DateTime(2022, 1, 1);
            var sut = new AssignDateStep(defaultDate);

            transaction = await sut.ProcessAsync(transaction);

            transaction.Date.Value.Year.Should().Be(defaultDate.Year);
        }
    }
}
