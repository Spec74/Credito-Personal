# Spec Kit — cambios (Credito.Modern)

Equivalente a [GitHub Spec Kit](https://github.com/github/spec-kit) **sin instalar el CLI** todavía. Artefactos en Markdown para que informática y el agente implementen en el mismo orden:

`constitution` → `specify` → `clarify` → `plan` → `checklist` → `tasks` → `analyze` → `implement` → `converge`

| Artefacto | Ruta |
|-----------|------|
| Constitución UX (nueva; no pisa la de `usp_*`) | [CONSTITUTION-UX.md](CONSTITUTION-UX.md) |
| Feature 001 — sistema Credix (todas las pantallas) | [001-experiencia-operativa/spec.md](001-experiencia-operativa/spec.md) · [modulos.md](001-experiencia-operativa/modulos.md) · [contrato C](001-experiencia-operativa/contrato-datos-c.md) · [PLA/UIF](001-experiencia-operativa/contrato-pla-uif.md) |
| Feature 002 — avisos y agenda | [002-comunicados-agenda/spec.md](002-comunicados-agenda/spec.md) · [research](002-comunicados-agenda/research.md) · [contrato](002-comunicados-agenda/contrato.md) |
| Feature 003 — shell móvil | [003-movil-shell/spec.md](003-movil-shell/spec.md) · [research](003-movil-shell/research.md) · [plan](003-movil-shell/plan.md) · [auditoría módulos](003-movil-shell/audit-modulos.md) |
| Feature 004 — alcance sucursal vs Central | [004-alcance-red/spec.md](004-alcance-red/spec.md) · [research](004-alcance-red/research.md) · [plan](004-alcance-red/plan.md) |
| Feature 005 — lecciones Facturador Smart | [005-facturador-smart/spec.md](005-facturador-smart/spec.md) · [auditoría](005-facturador-smart/audit-modulos.md) · [research](005-facturador-smart/research.md) · [SSD-16](../ssd/SSD-16-facturador-smart.md) |
| Feature 006 — estructura contemporánea (HTML + chrome) | [006-estructura-contemporanea/spec.md](006-estructura-contemporanea/spec.md) · [SSD-17](../ssd/SSD-17-estructura-contemporanea.md) |
| Cómo aprendes el loop | [COMO-APRENDER.md](COMO-APRENDER.md) |
| SSD catálogo | [../ssd/SSD-13-experiencia-operativa.md](../ssd/SSD-13-experiencia-operativa.md) · [SSD-14](../ssd/SSD-14-comunicados-agenda.md) · [SSD-15](../ssd/SSD-15-movil-shell.md) · [SSD-16](../ssd/SSD-16-facturador-smart.md) · [SSD-19 color](../ssd/SSD-19-color-scheme.md) |

As-built (no es change): [../ssd/README.md](../ssd/README.md).

Si más adelante se corre `specify init` en el repo, estos archivos se mapean a `.specify/` / `specs/001-…` sin reescribir el contenido.
