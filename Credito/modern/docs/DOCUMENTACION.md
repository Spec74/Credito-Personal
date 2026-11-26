# Documentación de Credito.Modern

**Estado del paquete documental as-built:** **100% escrito y navegable** (2026-11-26) · rama `Eber-migracion`.  
Índice + mapa + arquitectura + guía de código + SSD 00–19 + migraciones de dominio + API overview + inventario de rutas + piloto Azure básico.

**Gobierno multi-sucursal 2026-11-26:** rol × alcance — `ENCARGADO` = gerente de sede (usuarios/cajas/medios/bóveda de su oficina); `ADMINISTRADOR` en Central = admin de red (lectura/escritura soporte); `APROBADOR` unificado (legacy 1/2). Tareas acotadas por `Credito.OficinaId` (+ `verRed` solo admin de red). Bóveda: asignar caja debita temporal si abierta (no por rol); cierre temporal no borra gerente. SQL: `deploy/sql/2026-11-26-roles-aprobador-gerente-sede.sql`, `deploy/sql/2026-11-26-medios-oficina-filtro-reportes.sql`, `deploy/sql/2026-11-26-boveda-temporal-encargado-gerente.sql`. Ver [MANUAL-USUARIO-POR-ROL.md](MANUAL-USUARIO-POR-ROL.md), [SSD-02](ssd/SSD-02-credito.md), [SSD-04](ssd/SSD-04-boveda.md), [SSD-08](ssd/SSD-08-admin.md).

**Qué no es “docs faltantes”:**  
- Casillas SSD §7 `[ ]` = **evidencia de smoke** (pista operativa). Criterios ya redactados. Ver [DEUDA-TECNICA.md](DEUDA-TECNICA.md) §1.  
- Deuda de **código** (SPA/API >1500 líneas) = backlog en [DEUDA-TECNICA.md](DEUDA-TECNICA.md) §2; no bloquea el paquete documental.
- Cutover preprod (SSD-00 §7 abierto) = ops humano; veredicto en [DEUDA-TECNICA.md](DEUDA-TECNICA.md).

**Hecho 2026-11-23:** Piloto Azure B1 + SQL Basic + API **contenedor** (Tesseract/OCR) + Vercel → `api-credito-ocr`; semilla BD; ApiPerú vía `ApiPeru__Token`. Detalle: [AZURE-PILOTO-BASIC.md](AZURE-PILOTO-BASIC.md).  
**Prep 2026-11-24:** SPA Cloudflare Pages — [CLOUDFLARE-PAGES.md](CLOUDFLARE-PAGES.md).  
Una base `CREDITO`; sucursal = `OficinaId`. Tablero Red: SSD-01.

Este es el **índice único** para informática, negocio y auditoría.

---

## 1. Empezar aquí

| Necesitas… | Lee… |
|------------|------|
| Índice / este archivo | [DOCUMENTACION.md](DOCUMENTACION.md) |
| **Cómo está organizado y conectado el código** | [COMO-LEER-EL-CODIGO.md](COMO-LEER-EL-CODIGO.md) |
| **Mapa de carpetas (ingeniero)** | [ESTRUCTURA.md](ESTRUCTURA.md) |
| Arquitectura | [ARCHITECTURE.md](ARCHITECTURE.md) |
| **Deuda técnica restante (código / smoke)** | [DEUDA-TECNICA.md](DEUDA-TECNICA.md) |
| **Piloto Azure B1 + contenedor OCR (actual)** | [AZURE-PILOTO-BASIC.md](AZURE-PILOTO-BASIC.md) |
| **SPA en Cloudflare Pages** | [CLOUDFLARE-PAGES.md](CLOUDFLARE-PAGES.md) |
| Inventario largo de endpoints | [API-ENDPOINTS-INVENTARIO.md](API-ENDPOINTS-INVENTARIO.md) |
| Presentación gerencia (slides) | [PRESENTACION_ARQUITECTURA.html](PRESENTACION_ARQUITECTURA.html) |
| Plan multisucursal + prendario | [migration/PLAN-MULTISUCURSAL-PRENDARIO.md](migration/PLAN-MULTISUCURSAL-PRENDARIO.md) |
| Siguiente nivel UI / ventaja competitiva (Spec Kit) | [ssd/SSD-13-experiencia-operativa.md](ssd/SSD-13-experiencia-operativa.md) · [speckit/001](speckit/001-experiencia-operativa/spec.md) · [contrato C](speckit/001-experiencia-operativa/contrato-datos-c.md) · [PLA/UIF](speckit/001-experiencia-operativa/contrato-pla-uif.md) · [avisos/agenda](speckit/002-comunicados-agenda/spec.md) · [móvil](speckit/003-movil-shell/spec.md) · [SSD-15](ssd/SSD-15-movil-shell.md) · [alcance red](speckit/004-alcance-red/spec.md) · [Facturador Smart](speckit/005-facturador-smart/spec.md) · [SSD-16](ssd/SSD-16-facturador-smart.md) · [estructura](speckit/006-estructura-contemporanea/spec.md) · [SSD-17](ssd/SSD-17-estructura-contemporanea.md) · [color](ssd/SSD-19-color-scheme.md) |
| Tesis BI — preguntas al gerente | [tesis/preguntas-gerente-bi-2026.md](tesis/preguntas-gerente-bi-2026.md) |
| Glosario | [GLOSARIO.md](GLOSARIO.md) |
| Ambientes | [AMBIENTES.md](AMBIENTES.md) |
| API (visión) | [API-OVERVIEW.md](API-OVERVIEW.md) |
| Pruebas | [TEST-STRATEGY.md](TEST-STRATEGY.md) |
| **Guía go-live (jornada + batería, BD vacía)** | [GUIA-PRUEBAS-GO-LIVE.md](GUIA-PRUEBAS-GO-LIVE.md) |
| **Manual de usuario por rol (capacitación)** | [MANUAL-USUARIO-POR-ROL.md](MANUAL-USUARIO-POR-ROL.md) |
| Matriz E2E rol × módulo | [E2E-ROLE-MODULE-MATRIX.md](E2E-ROLE-MODULE-MATRIX.md) |
| Operaciones / runbooks | [OPS.md](OPS.md) |
| Endurecimiento plataforma | [PLATFORM-HARDENING.md](PLATFORM-HARDENING.md) |
| Especificaciones SSD | [ssd/README.md](ssd/README.md) |
| Correr en local | [../run-local.md](../run-local.md), [../readme.md](../readme.md) |
| Cutover strangler | [ssd/SSD-00-cutover.md](ssd/SSD-00-cutover.md), [migration/DEPLOY-AL-SUBIR.md](migration/DEPLOY-AL-SUBIR.md) |
| Desviaciones financieras | [migration/BITACORA-DESVIACIONES.md](migration/BITACORA-DESVIACIONES.md) |
| Inventario largo de endpoints | [../readme.md](../readme.md) |
| Esquema BD | [../db/schema/README.md](../db/schema/README.md) |

---

## 2. Paquete SSD (as-built 00–12 + cambios 13–18)

| Id | Módulo | Spec |
|----|--------|------|
| SSD-00 | Cutover y strangler | [SSD-00-cutover.md](ssd/SSD-00-cutover.md) |
| SSD-01 | Auth, menú, inicio | [SSD-01-auth-inicio.md](ssd/SSD-01-auth-inicio.md) |
| SSD-02 | Crédito | [SSD-02-credito.md](ssd/SSD-02-credito.md) |
| SSD-03 | Caja | [SSD-03-caja.md](ssd/SSD-03-caja.md) |
| SSD-04 | Bóveda / tesorería | [SSD-04-boveda.md](ssd/SSD-04-boveda.md) |
| SSD-05 | Clientes | [SSD-05-clientes.md](ssd/SSD-05-clientes.md) |
| SSD-06 | Prendario | [SSD-06-prendario.md](ssd/SSD-06-prendario.md) |
| SSD-07 | Informes | [SSD-07-informes.md](ssd/SSD-07-informes.md) |
| SSD-08 | Admin | [SSD-08-admin.md](ssd/SSD-08-admin.md) |
| SSD-09 | Maestros | [SSD-09-maestros.md](ssd/SSD-09-maestros.md) |
| SSD-10 | Ventas | [SSD-10-ventas.md](ssd/SSD-10-ventas.md) |
| SSD-11 | Almacén | [SSD-11-almacen.md](ssd/SSD-11-almacen.md) |
| SSD-12 | Validación FE/BE | [SSD-12-validacion.md](ssd/SSD-12-validacion.md) |
| SSD-13 | Experiencia operativa (Spec Kit) | [SSD-13-experiencia-operativa.md](ssd/SSD-13-experiencia-operativa.md) |
| SSD-14 | Comunicados y agenda | [SSD-14-comunicados-agenda.md](ssd/SSD-14-comunicados-agenda.md) |
| SSD-15 | Shell móvil (todas las pantallas) | [SSD-15-movil-shell.md](ssd/SSD-15-movil-shell.md) · [cómo aprender Spec Kit](speckit/COMO-APRENDER.md) |
| SSD-16 | Lecciones Facturador Smart (sin clonar ERP) | [SSD-16-facturador-smart.md](ssd/SSD-16-facturador-smart.md) · [005](speckit/005-facturador-smart/spec.md) · [auditoría](speckit/005-facturador-smart/audit-modulos.md) |
| SSD-17 | Estructura contemporánea (documento + chrome) | [SSD-17-estructura-contemporanea.md](ssd/SSD-17-estructura-contemporanea.md) · [006](speckit/006-estructura-contemporanea/spec.md) |
| SSD-18 | Flujos de cada módulo + alcance red | [SSD-18-flujos-modulos.md](ssd/SSD-18-flujos-modulos.md) |
| SSD-19 | Esquema de color (claro / oscuro, WCAG 2.2 AA) | [SSD-19-color-scheme.md](ssd/SSD-19-color-scheme.md) |

Plantilla: [ssd/SPEC-TEMPLATE.md](ssd/SPEC-TEMPLATE.md).

---

## 3. Ingeniería por dominio

| Dominio | Documento |
|---------|-----------|
| Crédito | [credito_migracion.md](credito_migracion.md) |
| **Dashboard / inicio** | [dashboard_migracion.md](dashboard_migracion.md) · [SSD-01](ssd/SSD-01-auth-inicio.md) · [COMO-LEER-EL-CODIGO.md](COMO-LEER-EL-CODIGO.md) |
| Clientes | [clientes_migracion.md](clientes_migracion.md) (GPS ficha + `POST …/ubicacion`) |
| Caja diario | [caja_diario_migracion.md](caja_diario_migracion.md) (incluye `/caja/cobro` cola + GPS) · [SSD-03](ssd/SSD-03-caja.md) · smoke → [SMOKE-SIGN-OFF § GPS/Cobro](SMOKE-SIGN-OFF.md) |
| Caja saldos | [caja_saldos_migracion.md](caja_saldos_migracion.md) |
| Caja chica / verificar | [caja_chica_verificar_migracion.md](caja_chica_verificar_migracion.md) |
| Bóveda | [boveda_migracion.md](boveda_migracion.md) |
| **Tickets / comprobantes térmicos** | [tickets_comprobantes.md](tickets_comprobantes.md) (caja · bóveda · caja chica) |
| UI Credix / módulos | [ui_modernizacion_modulos.md](ui_modernizacion_modulos.md) |
| Responsive | [responsive-audit.md](responsive-audit.md) · [SSD-15](ssd/SSD-15-movil-shell.md) |
| Color claro / oscuro | [SSD-19-color-scheme.md](ssd/SSD-19-color-scheme.md) |
| Admin / maestros / ventas / almacén | SSD-08…11 (canónicos) + `ui_modernizacion_modulos.md` |

---

## 4. Migración strangler

| Tema | Documento |
|------|-----------|
| Visión | [migration/STRANGLER-MIGRATION.md](migration/STRANGLER-MIGRATION.md) |
| Cierre candidato | [migration/MIGRATION-CLOSURE.md](migration/MIGRATION-CLOSURE.md) |
| Deploy | [migration/DEPLOY-AL-SUBIR.md](migration/DEPLOY-AL-SUBIR.md) |
| Ops cutover | [migration/PHASE-5-OPERATIONS-CUTOVER.md](migration/PHASE-5-OPERATIONS-CUTOVER.md) |
| Proxy / MVC auth | [migration/PROXY-AND-MVC-AUTH-BACKLOG.md](migration/PROXY-AND-MVC-AUTH-BACKLOG.md) |
| Login E2E | [migration/MODERN-E2E-LOGIN-MENU.md](migration/MODERN-E2E-LOGIN-MENU.md) |
| Password | [migration/PASSWORD-STORAGE-ROADMAP.md](migration/PASSWORD-STORAGE-ROADMAP.md) |
| Informes cobertura | [migration/CATALOGO-INFORMES-COBERTURA.md](migration/CATALOGO-INFORMES-COBERTURA.md) |
| Morosos empresariales | [migration/PARIDAD-MOROSIDAD.md](migration/PARIDAD-MOROSIDAD.md) |
| Prendario | [migration/PARIDAD-PRENDARIO.md](migration/PARIDAD-PRENDARIO.md) |
| Bitácora | [migration/BITACORA-DESVIACIONES.md](migration/BITACORA-DESVIACIONES.md) |

Fases UI/proxy históricas: retiradas 2026-10-27 (el mapa vivo es [ESTRUCTURA.md](ESTRUCTURA.md) + [ARCHITECTURE.md](ARCHITECTURE.md) + [DEPLOY-AL-SUBIR.md](migration/DEPLOY-AL-SUBIR.md)). Cutover operativo: [PHASE-5-OPERATIONS-CUTOVER.md](migration/PHASE-5-OPERATIONS-CUTOVER.md).

---

## 5. Manual de usuario por rol

Capacitación operativa completa (flujos, pantallas, matriz ¿quién hace qué?, FAQ):

→ **[MANUAL-USUARIO-POR-ROL.md](MANUAL-USUARIO-POR-ROL.md)**

| Perfil | Enfoque |
|--------|---------|
| Gestor / Analista | Clientes, solicitud, simulador, prendario, consulta, mora |
| Cajero | Caja diario, cobro, tickets |
| Aprobador | Bandeja `/credito/aprobar` |
| Encargado | Saldos, asignar caja, verificar pagos, fondeo |
| Administrador | Usuarios, roles, bóveda, condonaciones, red |
| Lectura / reportes | Informes sin mutar |

ACL automatizado: [E2E-ROLE-MODULE-MATRIX.md](E2E-ROLE-MODULE-MATRIX.md). Criterios SSD: SSD-02 / SSD-03 / SSD-04 §7.

---

## 6. Declaración de completitud documental

| Entregable | Estado |
|------------|--------|
| Catálogo SSD 00–19 | Completo |
| Arquitectura + ambientes + API overview + glosario + tests + ops | Completo |
| Ingeniería por módulo crédito/caja/bóveda/clientes | Completo |
| Tickets térmicos (caja · bóveda · caja chica) | Completo → [tickets_comprobantes.md](tickets_comprobantes.md) |
| Responsive + color oscuro (avisos, saldos, cards) | Completo → SSD-15/19 + [responsive-audit.md](responsive-audit.md) |
| Admin/maestros/ventas/almacén | Completo vía SSD-08…11 |
| Cutover **documentado** (runbooks + checklist) | Completo |
| Cutover **ejecutado** en preprod | Fuera de alcance documental (SSD-00 §7 operativo) |

**Firma documental:** paquete realineado 2026-11-09 — tickets as-built ([tickets_comprobantes.md](tickets_comprobantes.md)), responsive/dark (SSD-15/19 + avisos/saldos), Azure batch tesorería ([deploy/sql/README-2026-11-08-AZURE.md](../deploy/sql/README-2026-11-08-AZURE.md)). Base previa 2026-10-08 (Morosos + WhatsApp + [PLATFORM-HARDENING](PLATFORM-HARDENING.md)). Cambios futuros → actualizar el SSD del módulo + este índice si cambia el mapa.
