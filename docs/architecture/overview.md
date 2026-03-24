 # BookFast - Architectuuroverzicht
 
 ## Componenten
 
 ### Frontend (React)
 - Single Page Application
 - Components voor ruimtelisting, reservering, overzicht
 
 ### Backend (ASP.NET Core)
 - REST API endpoints
 - Logging en health checks
 
 ### Database (Azure SQL)
 - Schema voor users, rooms, reservations
 
 ### Deployment
 - Docker containers
 - Azure Container Registry
 - Azure App Service
 
 ## Dataflow
 

Browser → React App → API (.NET) → Azure SQL

 
 ## Omgevingen
 
 - **dev**: Continuous deployment van main
 - **test**: Manual deployment, QA
 - **prod**: Gated approvals, production
 
 ## Security
 
 - Azure Key Vault voor secrets
 - Azure AD voor authentication
 - Encryption at rest & in transit
