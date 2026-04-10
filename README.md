# BookFast - Room Reservation System
 
Een oefenproject voor AZ-400 DevOps Engineer Expert certification.
 
## Tech Stack
 - Frontend: React
 - Backend: ASP.NET Core (.NET 8)
 - Database: Azure SQL
 - CI/CD: Azure DevOps Pipelines
 - Infrastructure: Bicep
 
## Lokaal starten

### Prerequisites
- Git, Docker Desktop, .NET 10 SDK, Node.js 22+
 
### Steps
1. Clone de repo
2. Start docker-compose: `docker-compose up`
3. Frontend: http://localhost:3000
4. API: http://localhost:5000

## Git hooks

Activeer na het clonen eenmalig de repo hooks:

`pwsh -ExecutionPolicy Bypass -File .\scripts\setup-git-hooks.ps1`

De pre-commit hook draait dezelfde .NET format-check als CI voor `src/api/BookFast.API/BookFast.API.csproj` en blokkeert de commit als formatting ontbreekt.

Los formatting lokaal op met:

`Push-Location src\api; dotnet tool restore; dotnet tool run dotnet-format -- BookFast.API\BookFast.API.csproj; Pop-Location`
 
## Branchingstrategie

- `main`: Production-ready
- `develop`: Integration branch en primaire CI branch
- `feature/<naam>`: Feature branches vanaf `develop`
- `hotfix/<naam>`: Only for critical production bugs
 
## Commitconventies
 
Gebruik Conventional Commits:

feat: add reservation endpoint fix: correct date validation docs: update branching strategy

 
## Pull Request Process
 
1. Maak feature branch van `develop`
2. Commit met descriptive messages
3. Open PR
4. Laat CI draaien
5. Minimaal 1 review approval
6. Merge naar `develop`
