# SSD-17 — Estructura contemporánea (documento + chrome)

**Estado:** change · 2026-10-26  
**Código:** `index.html`, `AppShell`, `CredixSessionControl`, `CredixContextBar`  
**Spec Kit:** [006-estructura-contemporanea](../speckit/006-estructura-contemporanea/spec.md)  
**No entra negocio:** ningún `usp_*`.

## 1. Propósito y actores

Quien opera en celular o PC debe leer **sede y caja** en el chrome, no un renglón MVC truncado. El lector/teclado reconoce landmarks.

## 2. Alcance

**Entra:** host HTML, landmarks, control de sesión, chip de caja.  
**No entra:** 95 TSX, PWA, i18n, rediseño de marca, reglas de crédito/caja.

## 3. Paridad MVC

El legado tenía un toolbar de usuario/oficina. El moderno **no** lo copia pixel a pixel: sesión = control + panel. Desviación de producto (bitácora).

## 4. Contrato de datos

Ninguno nuevo. Oficina y usuario del JWT; caja de `fetchCajaDiarioSesion` (ya existe).

## 5. API y SPA

Sin API nueva. Superficie: `AppShell` (todas las rutas autenticadas).

## 6. Seguridad

Sin cambio de JWT ni menú. El panel de sesión no relaja CSP.

## 7. Criterios de aceptación

- [x] Código: landmarks + sesión control + caja corta en móvil.
- [x] Código: `viewport-fit=cover` + `color-scheme: light`.
- [x] Smoke 375 px: chrome en `/inicio` (oficina + Sin caja; panel con nombres). `/ventas/ordenes` pendiente con usuario que tenga menú.

## 8. Desviaciones

- **2026-10-26 — chrome no es el toolbar MVC:** [BITACORA](../migration/BITACORA-DESVIACIONES.md).

## 9. Pruebas

`tsc -b`. Checklist: [006/checklist.md](../speckit/006-estructura-contemporanea/checklist.md).

## 10. Go-live

Listo en Development al deployar `Eber-migracion`. No bloquea cutover SSD-00.
