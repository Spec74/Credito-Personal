# Guía de pruebas go-live — Credix moderno

**Base:** Azure `CREDITO` de **prueba** (operación vaciada 2/11/2026).  
**App:** Vercel / API de prueba. **No** producción.  
**Quién:** un usuario real de cada rol en la misma oficina, luego repetir corto en otra sede.

Marca `[x]` al pasar. Un **bloqueante** en rojo de la lista no se salta: sin eso no hay go-live.

---

## Cómo usar este documento

- Envíalo como archivo, ábrelo en GitHub, o pégalo en Google Docs / Word.
- El canvas de Cursor **no tiene URL pública**; este Markdown sí se comparte.
- SQL de esquema (no vacía datos): `Credito/modern/deploy/sql/2026-11-02-prod-deltas.sql`
- SQL de vaciar (solo prueba, ya corrido): `Credito/modern/deploy/sql/2026-11-02-vaciar-registros-azure-prueba.sql`

---

## Día 0 — obligatorio antes de originar

Tras el vaciado: no hay clientes ni créditos; **sí** hay usuarios, menú, oficinas, productos, cajas y almacenes. Bóveda en 0. `MAESTRO.Acceso` vacío.

- [ ] **D0-01 Esquema.** F5 `2026-11-02-prod-deltas.sql` en esta Azure. Result set final: todo en `1`, sobre todo `SimuladorSaltarDomingo` y `CreditoInsSaltarDomingo`.
- [ ] **D0-02 Login / Acceso.** Si dice que el cliente no está autorizado: en App Service de prueba `Auth__RequerirClienteAcceso=false`, o `INSERT` de la IP en `MAESTRO.Acceso`.
- [ ] **D0-03 Bóveda.** ENCARGADO → ingreso de efectivo (SaldoInicial = 0). Sin esto no se asigna caja.
- [ ] **D0-04 Caja.** ENCARGADO asigna caja al CAJERO. El cajero abre Diario y ve sesión.

Humo SQL (opcional, mismo SSMS):

```sql
EXEC CREDITO.usp_SimuladorCredito 1000,'D',1,5,'20261101',0,1; -- domingo → lunes
EXEC CREDITO.usp_SimuladorCredito 1000,'S',1,5,'20261101',0,1;
EXEC CREDITO.usp_SimuladorCredito 1000,'Q',1,5,'20261101',0,1;
EXEC CREDITO.usp_SimuladorCredito 1000,'M',1,5,'20261101',0,1;
EXEC CREDITO.usp_SimuladorCredito 1000,'M',1,5,'20261101',0,0; -- prendario: domingo se queda
```

---

## Orden de la jornada (un día por sede)

- [ ] 1. Día 0 (paquete + login + bóveda + asignar caja)
- [ ] 2. ANALISTA: 2 clientes nuevos (DNI distintos, ubigeo + tope)
- [ ] 3. ANALISTA: crédito **D, S, Q y M** con 1.er pago **domingo → lunes**
- [ ] 4. ANALISTA: 1 prendario (domingo **no** se salta) + 1 extra para remate
- [ ] 5. APROBADOR: aprobar los personales + prendarios; rechazar 1 PEN de prueba
- [ ] 6. CAJERO: desembolsar; cobrar 1 cuota; recibo
- [ ] 7. GESTOR: cobro en bloque Cartera → Enviados
- [ ] 8. ANALISTA: prendario cobrar interés / renovar; el otro rematar a stock
- [ ] 9. ADMIN: kardex de esa serie + venta rápida; **no** existe Entrada compra
- [ ] 10. ENCARGADO: arqueo, cerrar caja, cierre diarios a bóveda de **esa** sede
- [ ] 11. ADMIN: comunicado + 3 informes (caja, cartera, créditos) PDF/CSV
- [ ] 12. ADMIN: usuario con rol distinto en 2 sedes; REPORTEPARCIAL solo lee
- [ ] 13. Sucursal B: cliente → un crédito → desembolso, sin cruzar datos con A

---

## Quién prueba qué

| Rol | Qué cubre |
|-----|-----------|
| ANALISTA | Clientes, 4 personales D/S/Q/M, simulador, prendario, remate |
| CAJERO | Diario: cobranza, desembolso, ingreso/egreso, arqueo |
| GESTOR | Cobro en bloque e informes de su cartera |
| ENCARGADO | Bóveda, asignar caja, verificar pagos, cierre |
| APROBADOR | Bandeja, rechazar, condonación, cierre gerencial |
| ADMIN de sede | Usuarios/roles de **esa** oficina, comunicados, informes, almacén |
| REPORTEPARCIAL | Solo PDF. No asigna caja ni origina |
| Central | Red y fondeo. La sucursal no ve otras oficinas |

Caja chica y Entrada de almacén **no** están en menú (gerencia). No es un fallo.

---

## Guion por módulo

### Acceso e inicio

1. Login usuario + clave + oficina. Sesión de esa sede. Oscuro y claro usables. Teléfono **954 892 020**. Sin texto de entorno. Sin scroll de página en escritorio.
2. Usuario con 2+ oficinas: combo solo las suyas. Clave mala: no entra.
3. Menú = `RolMenu` de esa oficina. ANALISTA no ve Admin. CAJERO no ve Aprobar. GESTOR no ve Bóveda.
4. `/inicio`: con BD vacía, ceros / vacío, **no error**. Tras originar, los números coinciden.

### Clientes

Lista vacía. ANALISTA → Nuevo.

1. DNI 8 dígitos, nombres, sexo, nacimiento, celular.
2. Ubicación: ubigeo (sigue el catálogo) + dirección / GPS.
3. Calificación y tope. Guardar.
4. Segundo cliente distinto. Rebuscar el primer DNI: abre ficha, no duplica.
5. Editar. Cliente bloqueado no origina. Aval/cónyuge si aplica.

### Crédito personal (D / S / Q / M)

1. Nuevo crédito → cliente. Producto **no** prendario.
2. Primer pago: un **domingo**. Alta en **D, S, Q y M**: esa cuota debe ir al **lunes**.
3. Plan = SQL. Vencimiento = última cuota. Tope/bloqueo impiden seguir.
4. Simulador suelto: cotizar sin grabar; PDF; si pasas a solicitud, mismas fechas.
5. APROBADOR: bandeja de **esa** oficina. Aprobar 4. Rechazar un 5.º. ANALISTA no aprueba.
6. Consulta por DNI y nro: plan, gestión (anular PEN sobrante), movimientos tras caja.

### Caja y bóveda

1. CAJERO Diario → Desembolsos: salen los aprobados. Desembolsar. Segunda vez no.
2. Cobranzas: una cuota, recibo. Ingreso/egreso de prueba. Arqueo = billetes + movimientos. Cerrar.
3. GESTOR → Cobro: cartera de sus DES, tipar, Enviados, no duplicar.
4. ENCARGADO: verificar transferencia. Saldos. Cerrar diarios: el dinero va a la bóveda de **esa** oficina, no a la 1.

### Prendario y almacén

1. Nuevo prendario: bienes, tasación, mensual. Primer pago +1 mes. Si es domingo, **se queda** (corrido).
2. Aprobar y desembolsar.
3. Gestión: ficha (obs/foto), cobrar interés, renovar. Liberar = cancelación + bien DEVUELTO.
4. Otro crédito: cola remate (`FechaRemate` &lt; hoy) → Rematar. Kardex = ingreso. Venta rápida de esa serie. **No** hay Entrada compra.

### Admin, comunicados, informes, 2.ª sede

1. Comunicado nuevo (tablas vacías) → se ve en Inicio de la sede. Adjunto. Marcar leído.
2. Usuario con rol distinto por sede. REPORTEPARCIAL solo PDF.
3. Informes del menú: uno de caja, uno de cartera, uno de créditos. Vacío no es error; tras el ciclo deben salir esos créditos. Sucursal **sin** combo “todas”.
4. PLA/UIF: `PlaParametro` se conservó. Tras un desembolso grande, candidatos RO.
5. Oficina B: cliente + un crédito + desembolso. Oficina A no lo ve.

---

## Batería completa (marcar)

**Riesgo:** B = bloqueante · A = alto · M = medio · b = bajo

### Día 0

| Id | Pantalla | Qué hacer | Roles | Riesgo | OK |
|----|----------|-----------|-------|--------|----|
| D0-01 | Azure SQL | F5 `2026-11-02-prod-deltas.sql`; final todo en 1 | ADMIN | B | [ ] |
| D0-02 | `/login` | Si bloquea Acceso: flag false o INSERT IP | TODOS | B | [ ] |
| D0-03 | `/tesoreria/boveda` | Ingreso efectivo (saldo 0) | ENCARGADO | B | [ ] |
| D0-04 | `/caja/asignar` | Asignar caja al cajero | ENCARGADO, CAJERO | B | [ ] |

### Acceso e inicio

| Id | Pantalla | Qué hacer | Roles | Riesgo | OK |
|----|----------|-----------|-------|--------|----|
| ACC-01 | `/login` | Login sede, tema, teléfono 954 892 020, sin scroll | TODOS | B | [ ] |
| ACC-02 | `/login` | Combo oficinas; clave mala | TODOS | B | [ ] |
| ACC-03 | Shell | Menú RolMenu; sin caja chica ni entrada | TODOS | B | [ ] |
| INI-01 | `/inicio` | Tablero vacío sin error; luego cifras | ANALISTA, GESTOR, ADMIN, CAJERO | A | [ ] |

### Clientes

| Id | Pantalla | Qué hacer | Roles | Riesgo | OK |
|----|----------|-----------|-------|--------|----|
| CLI-01 | `/clientes` | Lista vacía → alta → buscar DNI | ANALISTA, ADMIN | B | [ ] |
| CLI-02 | `/clientes/nuevo` | Identificación DNI 8 | ANALISTA | B | [ ] |
| CLI-03 | Ubicación | Ubigeo, GPS, no perder Identificación | ANALISTA | A | [ ] |
| CLI-04 | Calificación | Tope; aval/cónyuge | ANALISTA, APROBADOR | A | [ ] |
| CLI-05 | Editar | DNI bloqueado salvo aprobador; no originar bloqueado | ANALISTA, APROBADOR | A | [ ] |

### Crédito

| Id | Pantalla | Qué hacer | Roles | Riesgo | OK |
|----|----------|-----------|-------|--------|----|
| CRE-01 | `/credito/nuevo` | Personal D, 1.er pago domingo → lunes, PEN | ANALISTA | B | [ ] |
| CRE-02 | Plan | Repetir S, Q, M; domingo → lunes | ANALISTA | B | [ ] |
| CRE-03 | `/credito/simulador` | Cotizar sin grabar; PDF; prendario no salta domingo | ANALISTA | B | [ ] |
| CRE-04 | `/credito/consulta` | Buscar DNI/nro; plan; oscuro | ANALISTA, GESTOR, ADMIN, APROBADOR | B | [ ] |
| CRE-05 | Gestión | Anular PEN; prorrogar DES | ADMIN, APROBADOR, ANALISTA | B | [ ] |
| CRE-06 | Movimientos | Tras caja, lista + PDF/CSV | ANALISTA, CAJERO, ADMIN | M | [ ] |
| CRE-07 | `/credito/aprobar` | Aprobar 4; observar; rechazar 1; ANALISTA no | APROBADOR, ADMIN, ANALISTA | B | [ ] |
| CRE-08 | Condonaciones | Solicitar / aprobar / impacto plan | ADMIN, APROBADOR | A | [ ] |
| CRE-09 | Tareas | Lista / cerrar / informe | ANALISTA, GESTOR | M | [ ] |
| CRE-10 | Parámetros simulador | Rangos se reflejan | ADMIN | M | [ ] |

### Prendario

| Id | Pantalla | Qué hacer | Roles | Riesgo | OK |
|----|----------|-----------|-------|--------|----|
| PRD-01 | `/credito/prendario` | Cola; sin menú = 403 | ANALISTA, CAJERO, GESTOR | B | [ ] |
| PRD-02 | Nuevo | Bienes, tasación; domingo **no** se salta | ANALISTA | B | [ ] |
| PRD-03 | Gestionar | Interés, renovar, liberar, foto, PDF | ANALISTA | B | [ ] |
| PRD-04 | Remate | Serie EN_ALMACEN; kardex; venta; no Entrada | ANALISTA, ADMIN | B | [ ] |

### Caja

| Id | Pantalla | Qué hacer | Roles | Riesgo | OK |
|----|----------|-----------|-------|--------|----|
| CAJ-01 | Diario Cobranzas | Sin caja: alerta. Con caja: 1 cuota + recibo | CAJERO | B | [ ] |
| CAJ-02 | Desembolsos | Desembolsar 1 vez | CAJERO, ANALISTA | B | [ ] |
| CAJ-03 | Egreso/Ingreso | Tipo, monto; arqueo cambia | CAJERO | A | [ ] |
| CAJ-04 | Arqueo | Cerrar; no operar cerrada | CAJERO, ENCARGADO | B | [ ] |
| CAJ-05 | `/caja/cobro` Cartera | GESTOR tipa lote | GESTOR, CAJERO | B | [ ] |
| CAJ-06 | Enviados | No duplicar | GESTOR | A | [ ] |
| CAJ-07 | Verificar pagos | Transferencia OK/rechazo | ENCARGADO, ADMIN | A | [ ] |
| CAJ-08 | Saldos | Lista sede; REPORTE no asigna | ENCARGADO, ADMIN, REPORTE | B | [ ] |
| CAJ-09 | Cierre diarios | Destino = bóveda de **esa** sede | ENCARGADO, ADMIN | A | [ ] |

### Tesorería

| Id | Pantalla | Qué hacer | Roles | Riesgo | OK |
|----|----------|-----------|-------|--------|----|
| TES-01 | `/tesoreria/boveda` | Saldo JWT; ingreso/egreso; no otra sede | ENCARGADO, ADMIN | B | [ ] |
| TES-02 | Movimiento bóveda | Filtro + PDF cuadra | ENCARGADO, ADMIN | A | [ ] |
| TES-03 | Fondear sucursales | Solo Central | ADMIN, ENCARGADO | A | [ ] |
| TES-04 | Red | Solo Central | ADMIN | A | [ ] |

### Almacén y ventas

| Id | Pantalla | Qué hacer | Roles | Riesgo | OK |
|----|----------|-----------|-------|--------|----|
| ALM-01 | Kardex | 0 al inicio; ingreso remate; salida venta | ADMIN, ANALISTA | A | [ ] |
| ALM-02 | Salida | Serie EN_ALMACEN; no lo vendido | ADMIN | A | [ ] |
| ALM-03 | Transferencia | Si hay 2 almacenes | ADMIN | M | [ ] |
| ALM-04 | Movimiento | Ver remate; sin Entrada compra | ADMIN | M | [ ] |
| ALM-05 | Código barras | Etiquetas / constancia | ADMIN | b | [ ] |
| VEN-01 | Venta rápida | Vender serie remate; stock baja | ADMIN, CAJERO | A | [ ] |
| VEN-02 | Orden venta | Crear / confirmar / anular | ADMIN | M | [ ] |
| VEN-03 | Lista precios | Alta/edición (catálogo conservado) | ADMIN | M | [ ] |

### Maestros, admin, agenda, PLA, informes, corte

| Id | Pantalla | Qué hacer | Roles | Riesgo | OK |
|----|----------|-----------|-------|--------|----|
| MAE-01 | `/maestros/*` | CRUD catálogo; no borrar artículo rematado | ADMIN | M | [ ] |
| ADM-01 | Usuarios | Roles **por** oficina; login distinto | ADMIN | B | [ ] |
| ADM-02 | Roles | Quitar ítem → desaparece al relogin | ADMIN | B | [ ] |
| ADM-03 | Oficinas | Listar (conservadas); cajas de la sede | ADMIN | A | [ ] |
| ADM-04 | Comunicados | Alta RED y OFICINA; se ve en Inicio | ADMIN | A | [ ] |
| AGE-01 | `/agenda` | Crear evento; no cruzar sede | ANALISTA, GESTOR, ADMIN | b | [ ] |
| PLA-01 | PLA/UIF | Parámetro conservado; candidatos RO | ADMIN, APROBADOR | A | [ ] |
| INF-01 | Hub informes | Solo menú; vacío ≠ error | ADMIN, REPORTE, APROBADOR, GESTOR | A | [ ] |
| INF-02 | Cartera/cobranza | PDF/CSV; sucursal sin “todas” | GESTOR, ADMIN, REPORTE, APROBADOR | A | [ ] |
| INF-03 | Créditos | Cada informe del menú | ADMIN, APROBADOR, REPORTE | A | [ ] |
| INF-04 | Clientes/caja/almacén | Inactivos, cajas, kardex, precios | ADMIN, REPORTE | M | [ ] |
| INF-05 | Cierre gerencial | Avance/metas; prendario sáb/dom sí | APROBADOR, ADMIN | A | [ ] |
| COR-01 | Humo SQL | D/S/Q/M domingo→lunes; prendario no | ADMIN | B | [ ] |
| COR-02 | Sucursal B | Ciclo corto sin cruzar con A | ADMIN, ENCARGADO, ANALISTA, CAJERO | B | [ ] |

---

## Criterio para llevar SQL a la base que nos pasen

Solo cuando en **Azure de prueba** pasen:

1. Día 0 completo  
2. Cuatro personales D/S/Q/M (domingo → lunes)  
3. Un prendario (domingo corrido) + un remate a kardex  
4. Desembolso, una cuota, cierre de caja a la bóveda de esa sede  
5. Una segunda oficina sin ver los datos de la primera  

**No** ejecutar en producción: `2026-11-02-vaciar-registros-azure-prueba.sql`.  
Ahí solo va el consolidado `2026-11-02-prod-deltas.sql`.
