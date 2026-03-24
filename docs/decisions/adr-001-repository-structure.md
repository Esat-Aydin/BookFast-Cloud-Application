 # ADR-001: Repository Structure
 
 ## Status
 ACCEPTED
 
 ## Decision
 
 Monorepo met functionele scheiding:

bookfast/ ├── src/ # Application code ├── tests/ # Test projects ├── infra/ # Bicep modules ├── pipelines/ # CI/CD YAML ├── docs/ # Documentation

 
 ## Rationale
 
 - Atomaire commits (frontend + backend together)
 - Eenvoudiger dependency management
 - Shared CI/CD pipeline
 
 ## Consequences
 
 - Teams werken in dezelfde repo
 - Pipeline moet snel blijven
 - Coördinatie bij upgrades nodi
