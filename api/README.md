# Expense Tracker
This is a personal project which aim is to provide a program able to handle incomes and expenses. 
This data is managed in a database created with EF Core with code first approach, and is exposed through an ASP.NET Core api.

## Project Structure
This solution is organized into different sub projects, each one covering a own logical and functional area.

### RDS.ExpenseTracker.Domain
This project contains the core classes of the domain, in this case the Transaction, Account and Category.

### RDS.ExpenseTracker.DataAccess
This project contains the classes responsible for the access to the database. Repository pattern is used to ensure the data access is isolated from the other layers of the application, 
and to enhance the control of exposed features of EF Core to the business layer. 

### RDS.ExpenseTracker.Business
This project is responsible for the orchestration of the business operations of the application. 
In particular it contains some abstractions that connects to other parts of the application (for example the data access and the data import layer) and uses them to interconnect these components.

### RDS.ExpenseTracker.Api
This project is an ASP.NET project responsible for exposing the application data and functionalities through a web api.

### RDS.ExpenseTracker.Tests
This project contains all the unit tests of the application
