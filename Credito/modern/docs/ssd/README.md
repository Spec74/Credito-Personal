# SSD — Especificación as-built (Credito.Modern)

El jefe de informática pidió **SSD** (Spec-Driven Development / especificación del sistema)
antes de programar. La migración strangler **ya está implementada**. Este directorio no
reescribe esa historia: documenta **lo construido** con el mismo rigor que un spec-kit
previo, para auditoría, cutover y mantenimiento.

No es un diario de fases. No duplica `readme.md` ni la bitácora.

## Qué hay y qué no

| Capa | ¿Completa? | Dónde vive hoy |
|------|------------|----------------|
| Código operativo (menú vivo CREDITO + SPA extra) | Casi: candidato a go-live | API .NET 10 + `Credito.Modern.Web` |
| Paridad de negocio (`usp_*`, no inventar reglas) | Sí, con desviaciones registradas | `docs/migration/BITACORA-DESVIACIONES.md` |
| Docs de ingeniería por módulo | Parcial | `docs/*_migracion.md` (crédito, clientes, caja, bóveda); el resto se destila en `docs/ssd/` |
| SSD / Spec Kit (visión, spec, criterios, trazabilidad) | Catálogo as-built **completo** (12/12) | `docs/ssd/` |
| Cutover preprod/prod | Pendiente de ejecución | `DEPLOY-AL-SUBIR.md`, `PHASE-5-OPERATIONS-CUTOVER.md`, [SSD-00](SSD-00-cutover.md) |

## Cómo se usa (Spec Kit, sin teatro)

Cada módulo tiene **un** `spec.md` as-built. Plantilla: [SPEC-TEMPLATE.md](SPEC-TEMPLATE.md).

Secciones fijas:

1. Propósito y actores
2. Alcance (qué entra / qué no)
3. Paridad MVC (pantalla, controlador, menú)
4. Contrato de datos (`usp_*`, tablas)
5. API y SPA
6. Seguridad (JWT, roles, oficina)
7. Criterios de aceptación
8. Desviaciones → enlace a bitácora, no copiar el relato
9. Pruebas y evidencia
10. Estado de go-live

La **constitución** del proyecto (no se negocia por módulo):

- La base de datos es el contrato. El C# moderno invoca `usp_*`; no recalcula cuotas, mora ni saldos. Donde el MVC legado ya escribía SQL en el BL (venta rápida, envío de orden, parte de almacén), el moderno replica esa paridad y lo declara en el spec del módulo.
- Strangler: MVC sigue vivo hasta smoke + OK de negocio.
- Secretos fuera de Git (user-secrets / variables de entorno).
- PDFs modernos son tabulares Credix, no copia píxel a píxel de ReportViewer.

El menú vivo de **oficina 1 (CREDITO)** cubre SSD-01 … SSD-09 y SSD-00. Ventas y almacén (SSD-10, SSD-11) están implementados y quedan fuera de ese menú; no bloquean el go-live de crédito/caja.

## Catálogo de módulos

Orden de redacción (primero lo que el menú vivo usa todos los días):

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
| SSD-00 | Cutover y strangler | Parcial (corte no ejecutado) | `STRANGLER-MIGRATION`, `DEPLOY-AL-SUBIR`, `MIGRATION-CLOSURE` | [SSD-00-cutover.md](SSD-00-cutover.md) |

## Qué no hacer

- No generar specs vacíos “para que se vea el método”.
- No copiar el `readme.md` (lista de endpoints) a cada spec.
- No fingir fechas de especificación anteriores al código.
- No meter en SSD desviaciones financieras: siguen en la bitácora.

## Relación con OpenSpec / spec-kit

Si más adelante se instala [GitHub Spec Kit](https://github.com/github/spec-kit) o OpenSpec,
estos `spec.md` son el equivalente de `/specify` **después** del hecho. Un `plan.md` nuevo
solo se abre para **cambios** (corte RDLC, comisiones reales, WhatsApp productivo).
