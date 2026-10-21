# Spec Kit — cambios (Credito.Modern)

Equivalente a [GitHub Spec Kit](https://github.com/github/spec-kit) **sin instalar el CLI** todavía. Artefactos en Markdown para que informática y el agente implementen en el mismo orden:

`constitution` → `specify` → `clarify` → `plan` → `checklist` → `tasks` → `analyze` → `implement` → `converge`

| Artefacto | Ruta |
|-----------|------|
| Constitución UX (nueva; no pisa la de `usp_*`) | [CONSTITUTION-UX.md](CONSTITUTION-UX.md) |
| Feature 001 — sistema Credix (todas las pantallas) | [001-experiencia-operativa/spec.md](001-experiencia-operativa/spec.md) · [modulos.md](001-experiencia-operativa/modulos.md) · [contrato C](001-experiencia-operativa/contrato-datos-c.md) · [PLA/UIF](001-experiencia-operativa/contrato-pla-uif.md) |
| Feature 002 — avisos y agenda | [002-comunicados-agenda/spec.md](002-comunicados-agenda/spec.md) · [research](002-comunicados-agenda/research.md) · [contrato](002-comunicados-agenda/contrato.md) |
| SSD catálogo | [../ssd/SSD-13-experiencia-operativa.md](../ssd/SSD-13-experiencia-operativa.md) · [SSD-14](../ssd/SSD-14-comunicados-agenda.md) |

As-built (no es change): [../ssd/README.md](../ssd/README.md).

Si más adelante se corre `specify init` en el repo, estos archivos se mapean a `.specify/` / `specs/001-…` sin reescribir el contenido.
