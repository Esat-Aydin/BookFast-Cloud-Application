# BookFast Bicep scaffold

This folder contains the phase-0 infrastructure-as-code scaffold for BookFast.

It intentionally starts with:

- a shared naming and parameter contract
- environment-specific `.bicepparam` usage
- clear module boundaries for the Azure platform that will be added in later phases

## Files

| File | Purpose |
| --- | --- |
| `main.bicep` | Root template for naming, parameters, tags, and future module composition |
| `main.dev.bicepparam` | Development environment parameter file |
| `bicepconfig.json` | Repository-specific Bicep analyzer configuration |
| `modules/README.md` | Planned module boundaries |

The full Azure resource rollout is intentionally deferred until the runtime boundaries are implemented in code.
