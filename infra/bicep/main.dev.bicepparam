using './main.bicep'

param appName = 'bookfast'
param environmentName = 'dev'
param tags = {
  application: 'bookfast'
  environment: 'dev'
  owner: 'portfolio'
  workload: 'integration-platform'
}
