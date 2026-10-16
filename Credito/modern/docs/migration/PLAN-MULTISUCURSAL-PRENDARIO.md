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

**Regla de fondeo (decisión 2026-10-16):** la transferencia inter-oficina vía `usp_TransferirBoveda` con `flagAceptar = 0` **acredita de inmediato** en origen y destino. Eso es lo que necesita gerencia para “asignar saldo a la bóveda sucursal”. La pestaña “Pendientes / aceptación” queda documentada como **incompleta** (el USP no implementa `flagAceptar = 1` ni inserta `BovedaMovTemp` en el envío); no bloquear el fondeo por ese flujo hasta una fase 2.

---

## 3. Backlog bóveda (orden)

| Id | Prioridad | Trabajo | Estado |
|----|-----------|---------|--------|
| B-01 | P1 | Fix destino bóveda: prod forzaba OficinaId=1; ahora `@OficinaId` + conservar MOVS_BASE (saldo inicial + movs) | **Hecho** (schema + deploy alineado a prod) |
| B-02 | P1 | Smoke cierre cajas en oficina ≠ 1 (Pichari / Huanta) | Pendiente operación |
| B-03 | P2 | UI Interoficina: texto “fondeo inmediato” + etiqueta clara | **Hecho** |
| B-04 | P2 | Documentar / ocultar o advertir pestaña Pendientes hasta completar USP | **Hecho** (aviso) |
| B-05 | P3 | Vista consolidada “Fondear sucursales” (central) + rol | Pendiente |
| B-06 | P3 | Índice UNIQUE: una bóveda principal abierta por `OficinaId` | Pendiente |
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
| P-02 | P1 | Campos de crédito en `/prendario/nuevo` + pasar a simulador | **Hecho** (monto/interés → query) |
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

## 6. Mensaje a gerencia (una frase)

> La base ya es multisucursal; corregimos el cierre de cajas para que Pichari y Andahuaylas cuadren, el central fondea con transferencia inmediata a cada bóveda, y el prendario se unifica en un solo llenado con documentos al instante sin reestructurar la BD.
