# Spec — Alcance sucursal vs Central (red)

**Feature:** `004-alcance-red`  
**SSD:** [SSD-07](../../ssd/SSD-07-informes.md) · [SSD-03](../../ssd/SSD-03-caja.md) · [SSD-01](../../ssd/SSD-01-auth-inicio.md) · [SSD-08](../../ssd/SSD-08-admin.md)  
**Tipo:** cambio de producto · Spec Kit `/speckit.specify`  
**Fecha:** 2026-10-24  
**Constitución:** principio 10 (misma plantilla; Central ve consolidado; sucursal ve lo suyo) + JWT = oficina

## Problema

Usuarios, cierre gerencial y morosos ya respetan sede vs Central. El resto no estaba cerrado:

1. Un ADMIN / APROBADOR / REPORTEPARCIAL **en sucursal** podía pedir otra `oficinaId` (o ninguna = toda la red) en cobranza, cobro diario y morosidad gestor.
2. Maestro de cajas listaba y editaba cajas de **todas** las oficinas.
3. El tablero gerencial (`/inicio`) solo veía la oficina del login: Central tenía que cerrar sesión para ver Pichari.

## Visión

La misma regla en toda lectura de red: **sucursal = JWT; Central = red o una sede**. Sin USP nuevo. Escrituras operativas (caja diario, crédito) siguen exigiendo JWT = oficina (entrar a la sede para operar).

## User stories

### US-1 — Sucursal no ve la cartera ajena (P0)

Como encargado en Pichari, aunque tenga rol de reporte, no consulto Huamanga ni “todas las oficinas”.

**Aceptación**

- `CobroDiarioReportAccess` fuerza `vendix:oficina_id` si `IndPrincipal` es falso.
- Cobranza: el combo Oficina solo aparece en Central.

### US-2 — Maestro de cajas por sede (P0)

Como admin de sucursal, solo listo, creo y activo cajas de mi oficina.

**Aceptación**

- `GET /cajas/gestion`, gestores y combo filtran por JWT salvo Central (`sedeId` o toda la red).
- `POST /cajas/guardar` y activar rechazan otra `OficinaId` en sucursal.

### US-3 — Central ve el tablero de otra sede (P1)

Como gerencia en Huamanga, elijo sede en `/inicio` sin volver a entrar.

**Aceptación**

- `GET /dashboard/admin/*?sedeId=` solo honra el query si la sesión es Central.
- Sucursal ignora `sedeId` y usa JWT.

## Fuera de alcance

- Inventar `usp_*`. Offline / SQL por sucursal.
- Relajar `ValidateJwtOficina` en escrituras de crédito/caja diario.
- Selector de sede en los ~28 informes que ya mandan JWT oculto (Central entra a la sede o usa cierre/morosos). Se documenta, no se reescriben 40 endpoints.
- Clientes: Persona es de red (DNI único); el crédito sigue acotado a oficina.
- Roles / marcas / artículos: catálogo de empresa.

## Relación

Complementa el alcance 2026-10-24 de usuarios / cierre / morosos. No sustituye RoleHome.
