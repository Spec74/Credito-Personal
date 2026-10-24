# Plan — Multisucursal (bóveda) y originación prendaria

**Estado:** en ejecución · iniciado 2026-10-16  
**Decisión de gerencia:** autonomía de sucursales + prendario en un solo flujo con PDFs en la misma ventana.  
**Sucursales:** Huamanga (central / principal), **Pichari**, Huanta, próximo **Andahuaylas**.

Relacionado: [SSD-04](../ssd/SSD-04-boveda.md) · [SSD-06](../ssd/SSD-06-prendario.md) · [BITACORA-DESVIACIONES](BITACORA-DESVIACIONES.md)

---

## 1. Veredicto de arquitectura

| Tema | ¿Reestructurar BD? | Qué hacer |
|------|--------------------|-----------|
| Multisucursal / bóveda | **No** | Modelo `Oficina → Boveda → Caja` ya existe. Corregir USP, proceso/UI y permisos. |
| Prendario unificado | **No** | `Credito` + `Prenda` + PDFs ya existen. Orquestar UX en una pantalla. |

---

## 2. Multisucursal — flujo de negocio acordado

```
Central (Huamanga / IndPrincipal)
    │  transferir bóveda → bóveda sucursal
    ▼
Sucursal (Pichari, Huanta, Andahuaylas…)
    │  transferir / asignar → cajeros
    ▼
Operación diaria (Caja Diario / Cobro)
    │  cierre cajas → bóveda
    ▼
Cierre bóveda → nuevo período con SaldoFinal arrastrado
    │  (opcional) devolver exceso a central
    ▼
Central ↔ Sucursal (ida y vuelta cuando lo pidan)
```

**Regla de fondeo (decisión 2026-10-16):** la transferencia inter-oficina vía `usp_TransferirBoveda` con `flagAceptar = 0` **acredita de inmediato** en origen y destino. Eso es lo que necesita gerencia para “asignar saldo a la bóveda sucursal”. `BovedaMovTemp` no se inserta en el envío; Pendientes solo anula leftovers (`flagAceptar = 2`). No inventar aceptación.

---

## 3. Backlog bóveda (orden)

| Id | Prioridad | Trabajo | Estado |
|----|-----------|---------|--------|
| B-01 | P1 | Fix destino bóveda: prod forzaba OficinaId=1; ahora `@OficinaId` + conservar MOVS_BASE (saldo inicial + movs) | **Hecho** (schema + deploy alineado a prod) |
| B-02 | P1 | Smoke cierre cajas en oficina ≠ 1 (Pichari / Huanta) | Pendiente operación |
| B-03 | P2 | UI Interoficina: texto “fondeo inmediato” + etiqueta clara | **Hecho** |
| B-04 | P2 | Pendientes = leftovers + Anular (flag 2); API bloquea flag ≠ 2 | **Hecho** |
| B-05 | P3 | Vista consolidada “Fondear sucursales” (central) + rol | Hecho + medio + tablero cash por sede |
| B-06 | P3 | Índice UNIQUE: una bóveda principal abierta por `OficinaId` | Script listo (`2026-10-24-boveda-cuadre-contrato.sql`) |
| B-07 | P4 | Capacitación ciclo autonomía Pichari → Andahuaylas | Pendiente negocio |

### Deploy B-01

Script: [`deploy/sql/2026-10-16-usp-cerrar-cajas-diarios-oficina.sql`](../../deploy/sql/2026-10-16-usp-cerrar-cajas-diarios-oficina.sql)  
Schema: [`db/schema/routines/procedures/CREDITO.usp_CerrarCajasDiarios.sql`](../../db/schema/routines/procedures/CREDITO.usp_CerrarCajasDiarios.sql)

**Criterio de aceptación B-01/B-02:** en una oficina distinta de 1, al cerrar cajas diarios con `TransBoveda=0`, aparecen asientos `TRE` en la bóveda de **esa** oficina y `TransBoveda=1` solo en diarios de esa oficina.

---

## 4. Prendario — flujo objetivo

```
Cliente (nuevo | existente)
    → Formulario único: bienes + campos de crédito
    → Simular (mismo panel)
    → Solicitar (una acción: CRE + bienes + PEN)
    → Misma ventana: PDF plan / acta / contrato
Listado: solo consulta + reimpresión + WhatsApp
```

| Id | Prioridad | Trabajo | Estado |
|----|-----------|---------|--------|
| P-01 | P1 | Documentar flujo objetivo (este plan + SSD-06) | **Hecho** |
| P-02 | P1 | Campos de crédito en `/prendario/nuevo` + pasar a simulador | **Hecho** (monto, interés, cuotas, modalidad, trámite, 1.er pago, obs., central → query) |
| P-03 | P2 | Unificar simular + generar + PDFs en una sola pantalla | Pendiente |
| P-04 | P2 | Listado sin originación forzada (solo consulta/reimpresión) | Pendiente |
| P-05 | P3 | Endpoint compuesto opcional `originar-prendario` | Opcional |

---

## 5. Oficinas de referencia

| Oficina | Rol esperado |
|---------|----------------|
| Huamanga | Central / principal (`IndPrincipal`) — fondea |
| Pichari | Sucursal operativa — autonomía diaria |
| Huanta | Sucursal operativa |
| Andahuaylas | Próxima apertura — misma plantilla |

Alta de oficina nueva: fila `MAESTRO.Oficina` + bóveda abierta + cajas + usuarios `UsuarioOficina`. **No** requiere tablas nuevas.

---

## 5.1 Checklist de alta de sucursal (Pichari / Andahuaylas)

Orden profesional para que la sucursal opere con autonomía:

| # | Paso | Dónde | Estado / nota |
|---|------|-------|----------------|
| 1 | Crear oficina (no principal) + ubicación | `/mantenimiento/oficinas` | Debe crear también `CREDITO.Boveda` abierta (IndCierre=0, IndTemporal=0). **Bug 2026-10-17:** el moderno insertaba `TESORERIA.Boveda` (esquema inexistente) → “No se pudo completar la operación”. Corregido a `CREDITO.Boveda`. |
| 2 | Verificar bóveda de la oficina | `/tesoreria/boveda` (logueado en esa oficina) o SQL | `SELECT * FROM CREDITO.Boveda WHERE OficinaId=@id AND IndCierre=0` |
| 3 | Asignar usuarios a la oficina **y roles en esa sede** | Usuarios → Oficinas + Roles | `UsuarioOficina` + `UsuarioRol` **por oficina**. ADMIN en Huamanga ≠ ADMIN en Pichari; hay que marcar roles en cada sede. |
| 4 | Crear cajas de la sucursal | Maestro de cajas | Al menos una caja operativa por cajero / turno |
| 5 | Fondear desde central | Bóveda Huamanga → **Fondear / interoficina** | Transferencia inmediata a bóveda Pichari |
| 6 | Asignar / transferir a cajeros | Saldos / bóveda→caja | Ciclo diario autónomo |
| 7 | Smoke cierre | Cerrar cajas + bóveda en Pichari | Tras deploy `usp_CerrarCajasDiarios` (destino `@OficinaId`) |
| 8 | Menú / roles | Menú por oficina | Confirmar que aparecen Caja Diario, Cobro, Bóveda |

**No basta con crear la oficina:** sin usuarios, cajas y fondeo, la sucursal no opera.

### Bug corregido (alta oficina)

- **Síntoma:** modal Nueva oficina → “No se pudo completar la operación.”
- **Causa:** `OficinaWriteService` hacía `INSERT INTO TESORERIA.Boveda` (no existe en BD CREDITO).
- **Fix:** `INSERT INTO CREDITO.Boveda` (+ `IndTemporal=0`), paridad `OficinaBL.CrearOficina`.
- **Extra:** `UsuarioAsignadoId` 0 → default 3; al marcar Principal se limpia el flag en las demás.

---

## 6. Mensaje a gerencia (una frase)

> La base ya es multisucursal; corregimos el cierre de cajas para que Pichari y Andahuaylas cuadren, el central fondea con transferencia inmediata a cada bóveda, y el prendario se unifica en un solo llenado con documentos al instante sin reestructurar la BD.

UX de siguiente nivel, home por rol, wizard de sede y ciclo prendario completo: [SSD-13](../ssd/SSD-13-experiencia-operativa.md).
