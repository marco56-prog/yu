# Discovery Document

## 1. Project Types

*   **Primary Language:** C#
*   **Framework:** .NET 8
*   **UI:** WPF (Windows Presentation Foundation)
*   **Database:** SQL Server LocalDB
*   **ORM:** Entity Framework Core
*   **Architecture:** MVVM (Model-View-ViewModel)

## 2. Build and Run Commands

### Local Development

*   **Restore dependencies:** `dotnet restore AccountingSystem.sln`
*   **Build the project:** `dotnet build AccountingSystem.sln --configuration Release`
*   **Run the application:** `dotnet run --project AccountingSystem.WPF`
*   **Run tests:** `dotnet test AccountingSystem.Tests/AccountingSystem.Tests.csproj`

### Database

*   **Update database schema:** `cd AccountingSystem.Data && dotnet ef database update`

## 3. CI/CD

The repository contains a `.github/` directory, which suggests that GitHub Actions is used for CI/CD. The specific workflows need to be examined to understand the CI/CD pipeline.

## 4. Required Secrets/Configs

*   The `appsettings.json` file in `AccountingSystem.WPF` contains the database connection string and other application settings. No explicit secrets are mentioned in the `README.md`, but it's possible that a production environment would require a more secure way to store the database connection string.

## 5. Missing Pieces

*   The `AccountingSystem.Tests` project is not included in the `AccountingSystem.sln` solution file. This means it won't be built or tested by default when running solution-level commands.
*   The `README.md` mentions `README_ADVANCED.md` and other documentation files. These should be reviewed for more in-depth information.
*   The `CHANGELOG.md` should be reviewed to understand the project's history.
*   The `.github/` directory should be explored to understand the CI/CD setup.
*   The `AGENTS.md` file is missing, so I will rely on the provided instructions and my own expertise.