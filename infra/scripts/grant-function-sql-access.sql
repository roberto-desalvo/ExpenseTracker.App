-- Grants the Function App managed identity and the GitHub deploy identity access to the ExpenseTracker database.
-- Run ONCE after the first infrastructure deploy, connected to the database (not master) as the
-- Entra ID SQL administrator (e.g. Azure Data Studio / SSMS / sqlcmd -G).
-- Replace <function-app-name> with the `functionAppName` output of the deployment.

CREATE USER [<function-app-name>] FROM EXTERNAL PROVIDER;

ALTER ROLE db_datareader ADD MEMBER [<function-app-name>];
ALTER ROLE db_datawriter ADD MEMBER [<function-app-name>];

-- EF Core migrations are applied by .github/workflows/deploy-database.yml with the GitHub deploy identity
-- (app registration created by infra/bootstrap, display name 'ExpenseTracker GitHub deploy'),
-- so the app identity does not need db_ddladmin.

CREATE USER [ExpenseTracker GitHub deploy] FROM EXTERNAL PROVIDER;

ALTER ROLE db_ddladmin ADD MEMBER [ExpenseTracker GitHub deploy];
ALTER ROLE db_datareader ADD MEMBER [ExpenseTracker GitHub deploy];
ALTER ROLE db_datawriter ADD MEMBER [ExpenseTracker GitHub deploy];
