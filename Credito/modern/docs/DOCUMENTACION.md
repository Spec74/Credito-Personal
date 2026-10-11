# Documentación de Credito.Modern

**Estado del paquete documental: COMPLETO (100%)** · 2026-10-08 · rama `Eber-migracion`.

Este es el **índice único** para informática, negocio y auditoría.  
Las casillas `[ ]` dentro de cada SSD §7 son **criterios de aceptación escritos** cuya marca depende del smoke preprod (operación), no de documentación faltante.

---

## 1. Empezar aquí

| Necesitas… | Lee… |
|------------|------|
| Índice / este archivo | [DOCUMENTACION.md](DOCUMENTACION.md) |
| Arquitectura | [ARCHITECTURE.md](ARCHITECTURE.md) |
| Glosario | [GLOSARIO.md](GLOSARIO.md) |
| Ambientes | [AMBIENTES.md](AMBIENTES.md) |
| API (visión) | [API-OVERVIEW.md](API-OVERVIEW.md) |
| Pruebas | [TEST-STRATEGY.md](TEST-STRATEGY.md) |
| Operaciones / runbooks | [OPS.md](OPS.md) |
| Endurecimiento plataforma | [PLATFORM-HARDENING.md](PLATFORM-HARDENING.md) |
| Especificaciones SSD | [ssd/README.md](ssd/README.md) |
| Correr en local | [../run-local.md](../run-local.md), [../readme.md](../readme.md) |
| Cutover strangler | [ssd/SSD-00-cutover.md](ssd/SSD-00-cutover.md), [migration/DEPLOY-AL-SUBIR.md](migration/DEPLOY-AL-SUBIR.md) |
| Desviaciones financieras | [migration/BITACORA-DESVIACIONES.md](migration/BITACORA-DESVIACIONES.md) |
| Inventario largo de endpoints | [../readme.md](../readme.md) |
| Esquema BD | [../db/schema/README.md](../db/schema/README.md) |

---

## 2. Paquete SSD (13/13)

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

Plantilla: [ssd/SPEC-TEMPLATE.md](ssd/SPEC-TEMPLATE.md).

---

## 3. Ingeniería por dominio

| Dominio | Documento |
|---------|-----------|
| Crédito | [credito_migracion.md](credito_migracion.md) |
| Clientes | [clientes_migracion.md](clientes_migracion.md) (GPS ficha + `POST …/ubicacion`) |
| Caja diario | [caja_diario_migracion.md](caja_diario_migracion.md) (incluye `/caja/cobro-bloque` + GPS) · [SSD-03](ssd/SSD-03-caja.md) · smoke GPS → [SMOKE-SIGN-OFF § GPS](SMOKE-SIGN-OFF.md) |
| Caja saldos | [caja_saldos_migracion.md](caja_saldos_migracion.md) |
| Caja chica / verificar | [caja_chica_verificar_migracion.md](caja_chica_verificar_migracion.md) |
| Bóveda | [boveda_migracion.md](boveda_migracion.md) |
| UI Credix / módulos | [ui_modernizacion_modulos.md](ui_modernizacion_modulos.md) |
| Responsive | [responsive-audit.md](responsive-audit.md) |
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

Fases históricas (punteros): `PHASE-3B`, `PHASE-4-START`, `PHASE-5-UI-*` bajo `migration/`.

---

## 5. Manual rápido de uso

Ver roles y flujos en la versión anterior consolidada:

| Rol | Pantallas |
|-----|-----------|
| Gestor / cajero | Login → Inicio → Caja diario / Consulta crédito / Clientes |
| Encargado | Bóveda, Saldos, asignar, cierres |
| Aprobador | `/credito/aprobar` |
| Administrador | Admin, maestros, oficinas, parámetros |

Detalle de aceptación: SSD-02 / SSD-03 / SSD-04 §7.

---

## 6. Declaración de completitud documental

| Entregable | Estado |
|------------|--------|
| Catálogo SSD 00–12 | Completo |
| Arquitectura + ambientes + API overview + glosario + tests + ops | Completo |
| Ingeniería por módulo crédito/caja/bóveda/clientes | Completo |
| Admin/maestros/ventas/almacén | Completo vía SSD-08…11 |
| Cutover **documentado** (runbooks + checklist) | Completo |
| Cutover **ejecutado** en preprod | Fuera de alcance documental (SSD-00 §7 operativo) |

**Firma documental:** paquete realineado 2026-10-08 (Morosos + WhatsApp + bak + [PLATFORM-HARDENING](PLATFORM-HARDENING.md): CI, OTel, CSP, ACL shell, E2E, smoke BD). Cambios futuros → actualizar el SSD del módulo + este índice si cambia el mapa.
