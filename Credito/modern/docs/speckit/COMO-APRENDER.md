# Cómo aprendes Spec Kit + SSD en este repo

No hace falta instalar el CLI de [GitHub Spec Kit](https://github.com/github/spec-kit) todavía. Aquí el método vive en Markdown. El CLI, el día que se corra `specify init`, solo **mueve** estos archivos a `.specify/` / `specs/001-…`.

## Las dos carpetas (no las mezcles)

| Carpeta | Qué es | Cuándo la tocas |
|---------|--------|-----------------|
| `docs/ssd/SSD-00` … `SSD-12` | **As-built:** lo que *ya existe* (auth, crédito, caja…). Plantilla `[SPEC-TEMPLATE.md](../ssd/SPEC-TEMPLATE.md)` | Cuando cambias un módulo ya vivo: actualizas el SSD, no inventas otro número |
| `docs/speckit/` + `SSD-13`… | **Change:** un cambio de producto *nuevo* | Cuando el trabajo no cabe en un SSD de módulo (experiencia, avisos, móvil) |

SSD = especificación del sistema (visión, alcance, contrato `usp_*`, criterios).  
Spec Kit = el **orden** para un cambio: no se programa a ciegas.

## El loop (de memoria)

```
constitution → specify → clarify → plan → checklist → tasks → analyze → implement → converge
```

| Paso | Archivo en este repo | Pregunta que responde |
|------|----------------------|------------------------|
| constitution | `CONSTITUTION-UX.md` (+ constitución de negocio en `ssd/README.md`) | ¿Qué **nunca** se negocia? (`usp_*`, tokens, español, oficina = JWT) |
| specify | `00N-…/spec.md` | ¿Qué problema y para quién? User stories + aceptación |
| clarify | (notas en research / el propio spec) | ¿Qué quedó ambiguo? |
| plan | `plan.md` | ¿Qué archivos y qué **no** se toca? |
| checklist | `checklist.md` | ¿Cómo se prueba en 375 px / preprod? |
| tasks | `tasks.md` | Lista corta, en orden |
| analyze | `research.md` + `audit-*.md` | Evidencia, alternativas descartadas |
| implement | código | Solo lo del plan |
| converge | `SSD-NN.md` + bitácora | As-built: qué hay, qué no hay |

Ejemplo trabajado: **003-movil-shell** (este cambio). Spec → research → audit de módulos → plan (un CSS de shell + parches donde el CSS lazy gana) → tasks → `credix-mobile-shell.css` → SSD-15.

## Cómo corres *tú* el siguiente cambio

1. **¿Es módulo o cambio?** Si es “caja debe mostrar X del USP”, edita `SSD-03`. Si es “todas las pantallas deben…”, abre `004-…` en speckit y un SSD-16.
2. Copia la carpeta `003-movil-shell` como plantilla. No copies SSD-01 (ese es as-built).
3. Escribe **primero** `spec.md` (problema + 2–4 user stories). Si no puedes decir la aceptación en una frase, no implementes.
4. `research.md`: una tabla vacío / causa / qué no es. Descarta 1–2 alternativas.
5. `plan.md`: lista de archivos. Si la lista tiene 40 TSX, el plan está mal (falta un patrón).
6. `checklist.md`: rutas reales (`/caja/diario`), no “probar todo”.
7. Recién entonces código. Al terminar: SSD as-built + una entrada en `BITACORA-DESVIACIONES.md` si te apartas del legado o del spec.
8. Constitución: **no se reescribe** por un ticket. Si un principio estorba, se discute; no se “parchea el principio”.

## Qué no es Spec Kit

- Un diario de commits.
- Copiar el `readme.md` de endpoints a cada spec.
- Un SSD vacío “para que se vea el método”.
- Pedir al agente “implementa” sin spec: eso es lo que este loop evita.

## Práctica (30 minutos)

Coge un hueco chico (ej. “el breadcrumb de informes se corta mal en 360 px”). Escribe tú el `spec.md` de 20 líneas **antes** de abrir el CSS. Compáralo con `003/spec.md`. Si tu spec no tiene aceptación medible, reescribe.

Fuente oficial del CLI (cuando quieras instalarlo): [github.com/github/spec-kit](https://github.com/github/spec-kit) — comandos `/speckit.specify`, `/speckit.plan`, `/speckit.tasks`, `/speckit.implement`. Aquí son los mismos nombres de archivo.
