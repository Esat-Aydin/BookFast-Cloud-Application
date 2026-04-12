# BookFast Terraform scaffold

This folder contains the active Terraform-based infrastructure scaffold for BookFast.

The current scope intentionally focuses on:

- a shared naming and tag contract
- environment-specific `.tfvars` files
- an optional resource group resource for future rollout automation
- clear module boundaries for the Azure platform that will be added in later phases

## Files

| File | Purpose |
| --- | --- |
| `versions.tf` | Terraform and AzureRM provider requirements |
| `variables.tf` | Shared input contract for environment name, location, tags, and naming |
| `main.tf` | Naming locals and optional resource group management |
| `outputs.tf` | Planned resource names and scaffold metadata |
| `environments/*.tfvars` | Environment-specific Terraform variable files |

The full Azure resource rollout remains intentionally deferred until the later platform phases are implemented in code.
