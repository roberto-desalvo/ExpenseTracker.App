using FluentAssertions;
using RDS.ExpenseTracker.DataImport.Business.Helpers;
using RDS.ExpenseTracker.DataImport.Business.Pipelines.Utilities;
using RDS.ExpenseTracker.Domain.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDS.ExpenseTracker.DataImport.Importer.Tests.Pipelines.Utilities
{
    public class ExcelDataRowHelperTests
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
                TransferAccountToIndex = 9
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
        public void GetDataRowModel_WhenCalled_ShouldReturnCustomExcelDataRowModel()
        {
            // Arrange
            var dataTable = GetDataTableWithColumns();
            var dataRow = GetDataRowWithStandardData(dataTable);
            dataRow["Outflow"] = "100";
            dataRow["Inflow"] = "0";
            dataRow["TransferAmount"] = "50";
            dataRow["TransferAccountFrom"] = "Account1";
            dataRow["TransferAccountTo"] = "Account2";
            dataTable.Rows.Add(dataRow);
            var config = GetConfiguration();

            // Act
            var model = ExcelDataRowHelper.GetDataRowModel(dataRow, config);

            // Assert
            model.TransactionDate.Should().Be(new DateTime(2021, 1, 1));
            model.TransactionDescription.Should().Be("Test Transaction");
            model.TransactionAmount.Should().Be(-100); 
            model.TransactionAccountName.Should().Be("Account1");
            model.TransferDate.Should().Be(new DateTime(2021, 1, 1));
            model.TransferDescription.Should().Be("Transfer");
            model.TransferAmount.Should().Be(50);
            model.TransferAccountFrom.Should().Be("Account1");
            model.TransferAccountTo.Should().Be("Account2");
        }

        [Fact]
        public void ExtractStandardTransaction_WhenCalled_ShouldReturnTransaction()
        {
            // Arrange
            var model = new ExcelDataRowModel
            {
                TransactionDate = new DateTime(2021, 1, 1),
                TransactionDescription = "Test Transaction",
                TransactionAmount = -100,
                TransactionAccountName = "Account1"
            };

            // Act
            var transaction = ExcelDataRowHelper.ExtractStandardTransaction(model);

            // Assert
            transaction.Amount.Should().Be(-100);
            transaction.Date.Should().Be(new DateTime(2021, 1, 1));
            transaction.Description.Should().Be("Test Transaction");
            transaction.FinancialAccountName.Should().Be("Account1");
            transaction.IsTransfer.Should().BeFalse();
        }

        [Fact]
        public void ExtractOutgoingTransfer_WhenCalled_ShouldReturnTransaction()
        {
            // Arrange
            var model = new ExcelDataRowModel
            {
                TransferDate = new DateTime(2021, 1, 1),
                TransferDescription = "Transfer",
                TransferAmount = 50
            };

            // Act
            var transaction = ExcelDataRowHelper.ExtractOutgoingTransfer(model);

            // Assert
            transaction.Amount.Should().Be(-50);
            transaction.Date.Should().Be(new DateTime(2021, 1, 1));
            transaction.Description.Should().Be("Transfer");
            transaction.FinancialAccountName.Should().Be(model.TransferAccountFrom);
            transaction.IsTransfer.Should().BeTrue();
        }

        [Fact]
        public void ExtractIngoingTransfer_WhenCalled_ShouldReturnTransaction()
        {
            // Arrange
            var model = new ExcelDataRowModel
            {
                TransferDate = new DateTime(2021, 1, 1),
                TransferDescription = "Transfer to Account2",
                TransferAmount = 50
            };

            // Act
            var transaction = ExcelDataRowHelper.ExtractIngoingTransfer(model);

            // Assert
            transaction.Amount.Should().Be(50);
            transaction.Date.Should().Be(new DateTime(2021, 1, 1));
            transaction.Description.Should().Be("Transfer to Account2");
            transaction.FinancialAccountName.Should().Be(model.TransferAccountTo);
            transaction.IsTransfer.Should().BeTrue();
        }
    }
}
