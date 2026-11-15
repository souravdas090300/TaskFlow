# Mutation Testing

Stryker.NET is configured via `stryker-config.json` at the repo root. It currently
mutates `TaskFlow.Application/Services/OrderSettings.cs`, which contains
`OrderPricing.Calculate` — the tax/shipping/total calculation. Mutants that
survive mean a test is missing for that calculation path.

## Run

```bash
dotnet tool install --global dotnet-stryker   # once
dotnet stryker --config-file stryker-config.json
```

Reports are written to `reports/mutation/` (HTML report: `mutation.html`).

Stryker runs the unit test project (`TaskFlow.UnitTests`) against each mutant.
Integration tests (`TaskFlow.IntegrationTests`) are not used — they need Docker
and are too slow for mutation runs.

## Interpreting results

- **Killed** — a test detected the mutation (good).
- **Survived** — no test caught it; add a test for that arithmetic path.
- **No coverage** — the mutated code isn't exercised by any test.

Thresholds in `stryker-config.json`: high >= 80%, low >= 60%. Adjust the
`mutate` glob to widen scope once the tax calculation reaches a stable score.
