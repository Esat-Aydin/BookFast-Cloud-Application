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
 - Git, Docker Desktop, .NET 8 SDK, Node.js 18+
 
 ### Steps
 1. Clone de repo
 2. Start docker-compose: `docker-compose up`
 3. Frontend: http://localhost:3000
 4. API: http://localhost:5000
 
 ## Branchingstrategie
 
 - `main`: Production-ready
 - `feature/<naam>`: Feature branches (korte duur)
 - `hotfix/<naam>`: Only for critical production bugs
 
 ## Commitconventies
 
 Gebruik Conventional Commits:

feat: add reservation endpoint fix: correct date validation docs: update branching strategy

 
 ## Pull Request Process
 
 1. Maak feature branch van `main`
 2. Commit met descriptive messages
 3. Open PR
 4. Laat CI draaien
 5. Minimaal 1 review approval
 6. Merge naar `main
