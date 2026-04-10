# BookFast frontend

The frontend currently serves as a lightweight platform shell for local demos and repository orientation. It is intentionally smaller than the target product experience: its job is to make the integration platform visible while the backend, Azure delivery, and observability foundations mature.

## Responsibilities today

- Present the current BookFast platform surface
- Point developers to the relevant runtime, architecture, and delivery assets
- Provide a stable place to grow consumer-facing UI flows in later phases

## Scripts

```powershell
npm install
npm run dev
npm run lint
npm test -- --run
npm run build
```

## Next planned evolution

- Introduce managed API consumption once CORS and consumer contracts are formalized
- Add reservation and availability experiences on top of the hardened API
- Align the frontend with the eventual APIM-backed platform story
