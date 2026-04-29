## Create Migration
dotnet ef migrations add Initialize_Database -p RDS.ExpenseTracker.Infrastructure -s RDS.ExpenseTracker.Api -o Migrations

## Update Database
dotnet ef database update -p RDS.ExpenseTracker.Infrastructure -s RDS.ExpenseTracker.Api 

## Create Script (FROM migration TO migration)
dotnet ef migrations script FROM TO -p RDS.ExpenseTracker.Infrastructure -s RDS.ExpenseTracker.Api -o RDS.ExpenseTracker.Infrastructure\Migrations\Scripts\script.sql
