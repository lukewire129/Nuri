# Nuri Documentation

The English documents in this directory are the shared implementation reference for maintainers, contributors, and coding agents. Start with the document map below and read only the areas relevant to the change.

## Required Reading by Change

| Change area | Read first |
|---|---|
| Runtime, hooks, keys, reconciliation, invalidation, lifecycle | [Runtime Architecture](architecture/RUNTIME_ARCHITECTURE.md), [Runtime Identity](architecture/RUNTIME_IDENTITY.md), [Lifecycle](architecture/LIFECYCLE.md) |
| Renderer materialization or parity | [Renderer Contracts](renderers/README.md), then the relevant architecture sections and source |
| Duxel performance | [Duxel Performance Analysis](performance/DUXEL_PERFORMANCE_ANALYSIS.md) |
| YAML visual styles, theme tokens, external overrides | [YAML Styles](guides/YAML_STYLES.md) |
| Formatter | [Formatting](guides/FORMATTING.md) |

`operations/SESSION_HANDOFF.md` is an optional recovery note. It is not a design authority and is not required reading for normal work.

## Directory Roles

- `architecture/`: durable system contracts, runtime identity, reconciliation, and lifecycle rules.
- `renderers/`: shared renderer boundaries, ownership, and parity expectations.
- `performance/`: measurement procedures and interpretation guidance.
- `guides/`: focused tool and usage guidance.
- `operations/`: short-lived recovery and maintenance notes.
- `ko/`: Korean translations of durable English references, with the same directory structure.

## Documentation Rules

- Update documentation when behavior, contracts, supported capabilities, commands, package versions, or measured baselines change.
- Before finishing a meaningful change, review related documents for stale or missing statements instead of updating only the first obvious file.
- Keep one authoritative location for each durable fact and link to it rather than copying long status lists.
- Update each durable English reference and its Korean translation in the same change. Keep code symbols, paths, commands, API names, package versions, and measured numbers identical.
- Operational notes do not require a complete Korean mirror. Move any durable decision from an operational note into paired reference documents.
- Split a document only after it contains clearly independent responsibilities or has become difficult to review as one unit.
