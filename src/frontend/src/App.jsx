import './App.css'

const currentCapabilities = [
  {
    title: 'REST /api/v1',
    summary: 'Minimal API endpoints for rooms and reservations with consistent ProblemDetails responses.'
  },
  {
    title: 'GraphQL /graphql',
    summary: 'Consumer-driven read surface with paging and cost guardrails powered by Hot Chocolate.'
  },
  {
    title: 'Operational baseline',
    summary: 'Health checks, correlation middleware, structured request logging, and exception handling.'
  }
]

const targetCapabilities = [
  {
    title: 'Azure API Management',
    summary: 'Products, subscriptions, policies, revisions, and consumer lifecycle management.'
  },
  {
    title: 'Azure Functions + Service Bus',
    summary: 'Asynchronous integration processing, retries, dead-letter handling, and downstream workflows.'
  },
  {
    title: 'Azure Monitor platform',
    summary: 'Application Insights, Log Analytics, dashboards, alerts, and incident-driven runbooks.'
  }
]

const repositoryAssets = [
  {
    path: 'docs/architecture/overview.md',
    description: 'Current runtime view and target Azure platform direction.'
  },
  {
    path: 'docs/architecture/bounded-contexts.md',
    description: 'Responsibility boundaries for reservation, query, integration, and operations concerns.'
  },
  {
    path: 'infra/bicep/main.bicep',
    description: 'Bicep naming, parameter, and module scaffold for the future Azure rollout.'
  },
  {
    path: 'pipelines/azure-devops/ci.yml',
    description: 'Azure DevOps validation pipeline scaffold aligned with the current repository layout.'
  }
]

function App() {
  return (
    <main className="platform-shell">
      <section className="platform-shell__hero">
        <p className="platform-shell__eyebrow">Portfolio for Azure Integration Engineering</p>
        <h1 className="platform-shell__title">BookFast integration platform</h1>
        <p className="platform-shell__summary">
          BookFast is evolving from a room reservation demo into a compact Azure integration
          platform with managed APIs, event-driven processing, and enterprise operational
          guardrails.
        </p>
      </section>

      <section className="platform-shell__section">
        <div className="platform-shell__section-header">
          <h2 className="platform-shell__section-title">Current implementation surface</h2>
          <p className="platform-shell__section-text">
            These capabilities exist in the repository today and are available for local
            validation.
          </p>
        </div>

        <div className="platform-shell__grid">
          {currentCapabilities.map((capability) => (
            <article className="platform-card" key={capability.title}>
              <h3 className="platform-card__title">{capability.title}</h3>
              <p className="platform-card__summary">{capability.summary}</p>
            </article>
          ))}
        </div>
      </section>

      <section className="platform-shell__section">
        <div className="platform-shell__section-header">
          <h2 className="platform-shell__section-title">Planned Azure capabilities</h2>
          <p className="platform-shell__section-text">
            These are the next platform layers that the repository is now structured to receive.
          </p>
        </div>

        <div className="platform-shell__grid">
          {targetCapabilities.map((capability) => (
            <article className="platform-card" key={capability.title}>
              <h3 className="platform-card__title">{capability.title}</h3>
              <p className="platform-card__summary">{capability.summary}</p>
            </article>
          ))}
        </div>
      </section>

      <section className="platform-shell__section">
        <div className="platform-shell__section-header">
          <h2 className="platform-shell__section-title">Repository assets</h2>
          <p className="platform-shell__section-text">
            The platform shell surfaces the architectural and delivery assets that now anchor the
            next implementation phases.
          </p>
        </div>

        <ul className="platform-list">
          {repositoryAssets.map((asset) => (
            <li className="platform-list__item" key={asset.path}>
              <code className="platform-list__path">{asset.path}</code>
              <p className="platform-list__description">{asset.description}</p>
            </li>
          ))}
        </ul>
      </section>
    </main>
  )
}

export default App
