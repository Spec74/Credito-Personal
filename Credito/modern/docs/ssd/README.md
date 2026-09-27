# SSD — Especificación as-built (Credito.Modern)

El jefe de informática pidió **SSD** (Spec-Driven Development / especificación del sistema)
antes de programar. La migración strangler **ya está implementada**. Este directorio no
reescribe esa historia: documenta **lo construido** con el mismo rigor que un spec-kit
previo, para auditoría, cutover y mantenimiento.

**Índice de toda la documentación del software:** [../DOCUMENTACION.md](../DOCUMENTACION.md)  
**Arquitectura:** [../ARCHITECTURE.md](../ARCHITECTURE.md)

No es un diario de fases. No duplica el inventario de endpoints de `readme.md` ni la bitácora.

## Qué hay y qué no

| Capa | ¿Completa? | Dónde vive hoy |
|------|------------|----------------|
| Código operativo (menú vivo CREDITO + SPA extra) | Casi: candidato a go-live | API .NET 10 + `Credito.Modern.Web` |
| Paridad de negocio (`usp_*`, no inventar reglas) | Sí, con desviaciones registradas | `docs/migration/BITACORA-DESVIACIONES.md` |
| Docs de ingeniería por módulo | Sí (crédito/caja/bóveda/clientes + SSD para el resto) | `docs/*_migracion.md` + `docs/ssd/` |
| SSD / Spec Kit (visión, spec, criterios, trazabilidad) | Catálogo as-built **completo** (**13/13**) | `docs/ssd/` |
| Validación FE/BE transversal | Sí | [SSD-12-validacion.md](SSD-12-validacion.md) |
| Documentación completa del software (índice + arquitectura) | Sí | [DOCUMENTACION.md](../DOCUMENTACION.md), [ARCHITECTURE.md](../ARCHITECTURE.md) |
| Cutover preprod/prod | Pendiente de **ejecución** (docs listos) | `DEPLOY-AL-SUBIR.md`, `PHASE-5-OPERATIONS-CUTOVER.md`, [SSD-00](SSD-00-cutover.md) |

## Cómo se usa (Spec Kit, sin teatro)

Cada módulo tiene **un** spec as-built. Plantilla: [SPEC-TEMPLATE.md](SPEC-TEMPLATE.md).

Secciones fijas:

1. Propósito y actores
2. Alcance (qué entra / qué no)
3. Paridad MVC (pantalla, controlador, menú)
4. Contrato de datos (`usp_*`, tablas)
5. API y SPA
6. Seguridad (JWT, roles, oficina) + validación de entrada (ver SSD-12)
7. Criterios de aceptación
8. Desviaciones → enlace a bitácora, no copiar el relato
9. Pruebas y evidencia
10. Estado de go-live

La **constitución** del proyecto (no se negocia por módulo):

- La base de datos es el contrato. El C# moderno invoca `usp_*`; no recalcula cuotas, mora ni saldos. Donde el MVC legado ya escribía SQL en el BL (venta rápida, envío de orden, parte de almacén), el moderno replica esa paridad y lo declara en el spec del módulo.
- Entradas inválidas se rechazan en Application/SPA **antes** del SP ([SSD-12](SSD-12-validacion.md)), aunque el legado no lo hiciera.
- Strangler: MVC sigue vivo hasta smoke + OK de negocio.
- Secretos fuera de Git (user-secrets / variables de entorno).
- PDFs modernos son tabulares Credix, no copia píxel a píxel de ReportViewer.

El menú vivo de **oficina 1 (CREDITO)** cubre SSD-01 … SSD-09 y SSD-00. Ventas y almacén (SSD-10, SSD-11) están implementados y quedan fuera de ese menú; no bloquean el go-live de crédito/caja. SSD-12 aplica a todos.

## Catálogo de módulos

| Id | Módulo | Código | Doc actual | Spec SSD |
|----|--------|--------|------------|----------|
| SSD-01 | Autenticación, menú, inicio | Hecho | `PASSWORD-STORAGE-ROADMAP`, `MODERN-E2E-LOGIN-MENU` | [SSD-01-auth-inicio.md](SSD-01-auth-inicio.md) |
| SSD-02 | Crédito (consulta, simulador, aprobar, tareas) | Hecho | `credito_migracion.md` | [SSD-02-credito.md](SSD-02-credito.md) |
| SSD-03 | Caja diario / chica / saldos / verificar | Hecho | `caja_*_migracion.md` | [SSD-03-caja.md](SSD-03-caja.md) |
| SSD-04 | Tesorería / bóveda | Hecho | `boveda_migracion.md` | [SSD-04-boveda.md](SSD-04-boveda.md) |
| SSD-05 | Clientes | Hecho | `clientes_migracion.md` | [SSD-05-clientes.md](SSD-05-clientes.md) |
| SSD-06 | Prendario + WhatsApp | Hecho | `PARIDAD-PRENDARIO.md` | [SSD-06-prendario.md](SSD-06-prendario.md) |
| SSD-07 | Informes y reportes | Hecho | `CATALOGO-INFORMES-COBERTURA.md` | [SSD-07-informes.md](SSD-07-informes.md) |
| SSD-08 | Admin / seguridad / oficinas | Hecho | `ui_modernizacion_modulos.md` | [SSD-08-admin.md](SSD-08-admin.md) |
| SSD-09 | Maestros | Hecho | `ui_modernizacion_modulos.md` | [SSD-09-maestros.md](SSD-09-maestros.md) |
| SSD-10 | Ventas | Hecho (SPA; **no** en menú vivo oficina 1) | `ui_modernizacion_modulos.md` | [SSD-10-ventas.md](SSD-10-ventas.md) |
| SSD-11 | Almacén | Hecho (SPA; **no** en menú vivo oficina 1) | `ui_modernizacion_modulos.md` | [SSD-11-almacen.md](SSD-11-almacen.md) |
| SSD-12 | Validación FE/BE transversal | Hecho | `Application/Validation`, `formRules.ts` | [SSD-12-validacion.md](SSD-12-validacion.md) |
| SSD-00 | Cutover y strangler | Parcial (corte no ejecutado) | `STRANGLER-MIGRATION`, `DEPLOY-AL-SUBIR`, `MIGRATION-CLOSURE` | [SSD-00-cutover.md](SSD-00-cutover.md) |

## Qué no hacer

- No generar specs vacíos “para que se vea el método”.
- No copiar el `readme.md` (lista de endpoints) a cada spec.
- No fingir fechas de especificación anteriores al código.
- No meter en SSD desviaciones financieras: siguen en la bitácora.

## Relación con OpenSpec / spec-kit

Si más adelante se instala [GitHub Spec Kit](https://github.com/github/spec-kit) o OpenSpec,
estos specs son el equivalente de `/specify` **después** del hecho. Un `plan.md` nuevo
solo se abre para **cambios** (corte RDLC, comisiones reales, WhatsApp productivo).

## Cierre documental (2026-09-27)

El paquete SSD + documentación de software se considera **terminado para auditoría/mantenimiento**.  
Lo único pendiente operativo es la **ejecución** del cutover preprod (SSD-00 §7), no redactar más specs base.
