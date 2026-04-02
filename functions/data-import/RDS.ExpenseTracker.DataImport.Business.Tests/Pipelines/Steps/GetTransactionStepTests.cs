using FluentAssertions;
using Moq;
using RDS.ExpenseTracker.DataImport.Business.Helpers;
using RDS.ExpenseTracker.DataImport.Business.Pipelines.Steps.ExcelDataExtraction;
using RDS.ExpenseTracker.DataImport.Business.Pipelines.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDS.ExpenseTracker.DataImport.Importer.Tests.Pipelines.Steps
{
    public class GetTransactionStepTests
    {
        private static ExpenseTrackerExcelOptions GetConfiguration()
        {
            return new ExpenseTrackerExcelOptions
            {
                SheetsToIgnore = new List<string> { "ignore" },
                TransactionDateIndex = 0,
                TransactionDescriptionIndex = 1,
                TransactionOutflowIndex = 2,
                TransactionInflowIndex = 3,
                TransactionAccountNameIndex = 4,
                TransferDateIndex = 5,
                TransferDescriptionIndex = 6,
                TransferAmountIndex = 7,
                TransferAccountFromIndex = 8,
                TransferAccountToIndex = 9,
            };
        }

        private static DataTable GetDataTableWithColumns()
        {
            return GetDataTableWithColumns(string.Empty);
        }

        private static DataTable GetDataTableWithColumns(string name)
        {
            var dataTable = new DataTable(name);
            dataTable.Columns.Add("Date");
            dataTable.Columns.Add("Description");
            dataTable.Columns.Add("Outflow");
            dataTable.Columns.Add("Inflow");
            dataTable.Columns.Add("AccountName");
            dataTable.Columns.Add("TransferDate");
            dataTable.Columns.Add("TransferDescription");
            dataTable.Columns.Add("TransferAmount");
            dataTable.Columns.Add("TransferAccountFrom");
            dataTable.Columns.Add("TransferAccountTo");
            return dataTable;
        }

        private static DataRow GetDataRowWithStandardData(DataTable dataTable)
        {
            var dataRow = dataTable.NewRow();
            dataRow["Date"] = "2021-01-01";
            dataRow["Description"] = "Test Transaction";
            dataRow["AccountName"] = "Account1";
            dataRow["TransferDate"] = "2021-01-01";
            dataRow["TransferDescription"] = "Transfer";
            return dataRow;
        }

        [Fact]
        public void GetTransactionsFromRow_WhenTransactionAmountIsZero_ShouldNotReturnTransaction()
        {
            // Arrange
            var dataTable = GetDataTableWithColumns();
            var dataRow = GetDataRowWithStandardData(dataTable);
            dataRow["Outflow"] = "0";
            dataRow["Inflow"] = "0";
            dataRow["TransferAmount"] = "0";
            dataTable.Rows.Add(dataRow);
            var config = GetConfiguration();
            var sut = new GetTransactionsStep(config);

            // Act
            var transactions = sut.GetTransactionsFromRow(dataRow);

            // Assert
            transactions.Should().BeEmpty();
        }

        [Fact]
        public void GetTransactionsFromRow_WhenTransactionAmountIsNonZero_ShouldReturnTransaction()
        {
            // Arrange
            var dataTable = GetDataTableWithColumns();
            var dataRow = GetDataRowWithStandardData(dataTable);
            dataRow["Outflow"] = "100";
            dataRow["Inflow"] = "0";
            dataRow["TransferAmount"] = "50";
            dataTable.Rows.Add(dataRow);
            var config = GetConfiguration();
            var sut = new GetTransactionsStep(config);

            // Act
            var transactions = sut.GetTransactionsFromRow(dataRow).ToList();

            // Assert
            transactions.Should().HaveCount(3);
            transactions[0].Amount.Should().Be(-100);
            transactions[0].Description.Should().Be("Test Transaction");
            transactions[0].FinancialAccountName.Should().Be("Account1");
            transactions[0].IsTransfer.Should().BeFalse();
        }

        [Fact]
        public void GetTransactionsFromRow_WhenTransferAmountIsNonZero_ShouldReturnTransferTransactions()
        {
            // Arrange
            var dataTable = GetDataTableWithColumns();
            var dataRow = GetDataRowWithStandardData(dataTable);
            dataRow["Outflow"] = "0";
            dataRow["Inflow"] = "0";
            dataRow["TransferAmount"] = "50";
            dataRow["TransferAccountFrom"] = "Account1";
            dataRow["TransferAccountTo"] = "Account2";
            dataRow["TransferDescription"] = "Transfer to Account2";
            dataTable.Rows.Add(dataRow);
            var config = GetConfiguration();
            var sut = new GetTransactionsStep(config);

            // Act
            var transactions = sut.GetTransactionsFromRow(dataRow).ToList();

            // Assert
            transactions.Should().HaveCount(2);
            transactions[0].Amount.Should().Be(-50);
            transactions[0].Description.Should().Be("Transfer to Account2");
            transactions[0].FinancialAccountName.Should().Be("Account1");
            transactions[0].IsTransfer.Should().BeTrue();

            transactions[1].Amount.Should().Be(50);
            transactions[1].Description.Should().Be("Transfer to Account2");
            transactions[1].FinancialAccountName.Should().Be("Account2");
            transactions[1].IsTransfer.Should().BeTrue();
        }

        [Fact]
        public void GetTransactions_WhenCalled_ShouldReturnTransactions()
        {
            // Arrange
            var dataTable = GetDataTableWithColumns();
            var dataRow = GetDataRowWithStandardData(dataTable);
            dataRow["Outflow"] = "100";
            dataRow["Inflow"] = "0";
            dataRow["TransferAmount"] = "50";
            dataTable.Rows.Add(dataRow);
            var config = GetConfiguration();
            var sut = new GetTransactionsStep(config);

            // Act
            var transactions = sut.GetTransactions(dataTable);

            // Assert
            transactions.Should().HaveCount(3);
        }

        
    }
}
