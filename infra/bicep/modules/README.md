# Planned Bicep modules

The BookFast Azure platform will be decomposed into the following module areas:

| Module area | Planned responsibility |
| --- | --- |
| `platform` | Shared platform resources such as Log Analytics, Application Insights, and naming/tagging conventions |
| `api` | API hosting resources and APIM integration |
| `functions` | Function App hosting and app settings |
| `messaging` | Service Bus namespaces, queues, and topics |
| `security` | Key Vault, identities, and access wiring |
| `observability` | Diagnostics settings, alerts, and monitoring configuration |

These modules are intentionally documented before they are implemented so the future rollout follows a stable structure.
