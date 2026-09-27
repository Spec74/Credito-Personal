# Documentación de Credito.Modern

Índice maestro del software moderno (API .NET 10 + SPA React).  
Última revisión documental: **2026-09-27** · rama `Eber-migracion`.

Este documento es la **entrada única** para informática, negocio y auditoría.  
No sustituye el código ni la bitácora de desviaciones financieras.

---

## 1. Empezar aquí

| Necesitas… | Lee… |
|------------|------|
| Entender qué es el sistema y cómo está armado | [ARCHITECTURE.md](ARCHITECTURE.md) |
| Especificación por módulo (SSD / Spec-Driven) | [ssd/README.md](ssd/README.md) |
| Cómo correr en local | [../run-local.md](../run-local.md), [../readme.md](../readme.md) |
| Desplegar / cutover strangler | [ssd/SSD-00-cutover.md](ssd/SSD-00-cutover.md), [migration/DEPLOY-AL-SUBIR.md](migration/DEPLOY-AL-SUBIR.md) |
| Desviaciones de negocio aceptadas | [migration/BITACORA-DESVIACIONES.md](migration/BITACORA-DESVIACIONES.md) |
| Inventario largo de endpoints | [../readme.md](../readme.md) (sección API) |
| Esquema BD (tablas / SP) | [../db/schema/README.md](../db/schema/README.md) |

---

## 2. Paquete SSD (especificaciones as-built)

Metodología pedida por informática: **SSD** = Spec-Driven Development / especificación del sistema.  
Son specs **después** de implementar (as-built), con secciones fijas ([SPEC-TEMPLATE.md](ssd/SPEC-TEMPLATE.md)).

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
| SSD-12 | Validación FE/BE transversal | [SSD-12-validacion.md](ssd/SSD-12-validacion.md) |

Catálogo: **13/13** completo.

---

## 3. Documentación de ingeniería por dominio

Paridad UI ↔ MVC, endpoints y notas de migración:

| Dominio | Documento |
|---------|-----------|
| Crédito | [credito_migracion.md](credito_migracion.md) |
| Clientes | [clientes_migracion.md](clientes_migracion.md) |
| Caja diario | [caja_diario_migracion.md](caja_diario_migracion.md) |
| Caja saldos | [caja_saldos_migracion.md](caja_saldos_migracion.md) |
| Caja chica / verificar | [caja_chica_verificar_migracion.md](caja_chica_verificar_migracion.md) |
| Bóveda | [boveda_migracion.md](boveda_migracion.md) |
| UI módulos / menú | [ui_modernizacion_modulos.md](ui_modernizacion_modulos.md) |
| Responsive | [responsive-audit.md](responsive-audit.md) |
| Admin / maestros / ventas / almacén | Cubiertos en SSD-08…11 + `ui_modernizacion_modulos.md` (sin `*_migracion.md` aparte) |

---

## 4. Migración strangler y operaciones

| Tema | Documento |
|------|-----------|
| Visión strangler | [migration/STRANGLER-MIGRATION.md](migration/STRANGLER-MIGRATION.md) |
| Cierre candidato | [migration/MIGRATION-CLOSURE.md](migration/MIGRATION-CLOSURE.md) |
| Deploy al subir | [migration/DEPLOY-AL-SUBIR.md](migration/DEPLOY-AL-SUBIR.md) |
| Operaciones cutover | [migration/PHASE-5-OPERATIONS-CUTOVER.md](migration/PHASE-5-OPERATIONS-CUTOVER.md) |
| Proxy / auth MVC backlog | [migration/PROXY-AND-MVC-AUTH-BACKLOG.md](migration/PROXY-AND-MVC-AUTH-BACKLOG.md) |
| Login / menú E2E | [migration/MODERN-E2E-LOGIN-MENU.md](migration/MODERN-E2E-LOGIN-MENU.md) |
| Password / hash | [migration/PASSWORD-STORAGE-ROADMAP.md](migration/PASSWORD-STORAGE-ROADMAP.md) |
| Informes cobertura | [migration/CATALOGO-INFORMES-COBERTURA.md](migration/CATALOGO-INFORMES-COBERTURA.md) |
| Prendario | [migration/PARIDAD-PRENDARIO.md](migration/PARIDAD-PRENDARIO.md) |
| Bitácora desviaciones | [migration/BITACORA-DESVIACIONES.md](migration/BITACORA-DESVIACIONES.md) |

---

## 5. Manuales rápidos (uso del software)

### Roles típicos

| Rol | Pantallas principales |
|-----|------------------------|
| Gestor / cajero | Login → Inicio → Caja diario / Consulta crédito / Clientes |
| Encargado | Bóveda, Saldos, asignar caja, cierres |
| Aprobador | `/credito/aprobar` |
| Administrador | Admin usuarios/roles, maestros, oficinas, parámetros |

### Flujo diario (caja)

1. Login con oficina → menú `usp_MenuLst`.
2. Abrir **Caja diario** (sesión del día).
3. Cobrar cuotas / desembolsar APR / entrada-salida.
4. Arqueo y cierre (sin condonaciones ni pagos digitales pendientes).
5. Encargado: saldos / bóveda según operación.

Detalle de aceptación: [SSD-03](ssd/SSD-03-caja.md) §7.

### Flujo crédito

1. Simulador → generar solicitud / crédito.
2. Aprobar (1.ª / 2.ª).
3. Desembolso desde caja.
4. Gestión (cargos, observar, condonar) en ficha.

Detalle: [SSD-02](ssd/SSD-02-credito.md).

### Reglas que no se negocian

1. La **BD / `usp_*`** es el contrato de negocio; el C# no inventa cuotas ni mora.
2. **JWT** acota `oficinaId` / `usuarioId`; no se opera otra oficina.
3. **Validación** FE (`formRules`) + BE (`Application/Validation`) antes del SP ([SSD-12](ssd/SSD-12-validacion.md)).
4. PDFs modernos = Credix tabular; RDLC solo si negocio lo exige vía puente.
5. Secretos fuera de Git.

---

## 6. Estado del producto (2026-09-27)

| Capa | Estado |
|------|--------|
| Código operativo (menú vivo CREDITO) | Candidato a go-live |
| SSD catálogo | Completo (13/13) |
| Validación FE/BE transversal | Hecho (SSD-12) |
| Cutover preprod firmado | Pendiente de ejecución (SSD-00) |
| Retiro MVC | No firmado |

---

## 7. Mantenimiento de esta documentación

- Cambio de módulo → actualizar el SSD correspondiente + bitácora si hay desviación financiera.
- Cambio transversal (auth, proxy, validación) → SSD-00 / SSD-01 / SSD-12 + `ARCHITECTURE.md`.
- No duplicar listas de endpoints en cada SSD (viven en `readme.md`).
- No inventar fechas de spec anteriores al código.
