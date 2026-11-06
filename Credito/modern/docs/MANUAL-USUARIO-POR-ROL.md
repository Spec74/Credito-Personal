# Manual de usuario por rol — Credix / CrediConfiable

**Alcance:** operación diaria en la SPA moderna (multisucursal).  
**Fecha:** 2026-11-05  
**Complementa:** `E2E-ROLE-MODULE-MATRIX.md` (pruebas ACL) y `GUIA-PRUEBAS-GO-LIVE.md` (go-live técnico).

---

## 1. Cómo leer este manual

En la práctica casi nadie tiene **un solo** rol. El sistema combina roles del menú (`MAESTRO.Rol` + `RolMenu`) con permisos de pantalla.

| Perfil de trabajo | Roles típicos en BD | Para quién |
|-------------------|---------------------|------------|
| **Gestor / Analista** | `ANALISTA` (+ a veces `CAJA`) | Originación, clientes, consulta, prendario, tareas |
| **Cajero** | `CAJA` (+ a veces `ANALISTA`, `CAJA CENTRAL`) | Caja diario, cobros, tickets |
| **Aprobador nivel 1** | `APROBADOR 1` | Aprueba / rechaza en bandeja |
| **Aprobador nivel 2** | `APROBADOR 2` | Consulta bandeja y reportes; **no** aprueba (hace falta nivel 1 o admin) |
| **Encargado de sede** | `ENCARGADO` (+ `SALDOS CAJA`, reportes) | Asignar/abrir caja, saldos, verificar pagos, fondeo |
| **Administrador de sede** | `ADMINISTRADOR` en oficina no Central | Seguridad y operación de su sede |
| **Administrador de red** | `ADMINISTRADOR` en Central (`IndPrincipal`) | Misma denominación; alcance de red (verRed, otras sedes) |
| **Solo lectura / reportes** | `LECTURA`, `REPORTE`, `REPORTEPARCIAL`, `LECTURA_SALDO` | Consultar e imprimir, sin mutar |

**No confundir:** el rol `CAJA CENTRAL` abre menús de cajero central; la **caja física** «CAJA CENTRAL …» se define en el maestro de cajas. `LECTURA` (crédito) ≠ `LECTURA_SALDO` (saldos) ≠ `SALDOS CAJA` (menú de saldos).

Si al entrar ve **«Sin permiso»**, el menú de su usuario no incluye esa ruta: pida al administrador que revise **Admin → Roles / Usuarios**, no es un error de la pantalla.

**Regla de dinero (todos los roles):** montos y mora se muestran y cobran por **suma de cuotas** (céntimo a céntimo). No hay “mora consolidada” aparte.

---

## 2. Arranque común (todos)

1. Abrir la URL de la SPA (Vercel / intranet).
2. Iniciar sesión con usuario y clave de la **oficina correcta**.
3. En **Inicio** aparecen atajos según su rol (“Hoy”).
4. El menú lateral es el catálogo autorizado para ese usuario.
5. Comunicados y agenda están disponibles con sesión activa.

**Buenas prácticas multisucursal**

- Trabaje siempre en la oficina del token (la que eligió / le asignaron).
- No comparta sesión entre cajeros.
- Al terminar el turno: cerrar caja si corresponde y cerrar sesión.

---

## 3. Gestor / Analista

### Qué hace

Originación de créditos personales y prendarios, atención de cliente, seguimiento y consulta.

### Pantallas habituales

| Pantalla | Ruta | Para qué |
|----------|------|----------|
| Clientes | `/clientes` | Alta / búsqueda / ficha |
| Nueva solicitud | `/credito/nuevo` | Crédito personal |
| Simulador | `/credito/simulador` | Armar plan antes de solicitar |
| Consulta crédito | `/credito/consulta` | Plan, movimientos, mora, PDF |
| Prendario listado / nuevo | `/credito/prendario`, `/credito/prendario/nuevo` | Bienes, contrato, desembolso prendario |
| Tareas | `/credito/tareas` | Seguimiento operativo |

### Flujo típico — crédito personal

1. **Clientes** → buscar o registrar persona (DNI).
2. **Simulador** o **Nueva solicitud** → monto, producto, cuotas, TEM dentro de min/máx.
3. Enviar solicitud → estado **PEN**.
4. Esperar **Aprobador**.
5. Tras **APR**, si también opera caja: desembolso en **Caja diario** (o coordinar con cajero).
6. **Consulta** → imprimir plan / estado / notificación de vencimiento si aplica.

### Flujo típico — prendario

1. **Prendario → Nuevo** → cliente + tasación de bienes.
2. Completar bienes (descripción y valor). El monto del crédito **no puede superar** la tasación.
3. Solicitar → aprobar → desembolsar según permisos de caja.
4. Imprimir contrato / acta según botones de la ficha.

### Mora (consulta)

- Abrir crédito → **Mora**.
- **Mora en cuotas** = lo que se cobra (suma de la grilla).
- **Postergada** = solo si el producto aplica mora postergada y hay saldo registrado.
- **Mora total** = cuotas + postergada.
- Cobrar desde **Caja diario** (preparar cobro / seleccionar cuotas), no desde el cuadro informativo.

### Qué no debe hacer (salvo que también tenga el rol)

- Aprobar créditos (falta `APROBADOR`).
- Cerrar caja / asignar cajas (falta `ENCARGADO` / saldos).
- Administrar usuarios y roles.

---

## 4. Cajero

### Qué hace

Abrir sesión de caja, cobrar cuotas, pagos libres, desembolsos autorizados, emitir tickets.

### Pantallas habituales

| Pantalla | Ruta | Para qué |
|----------|------|----------|
| Caja diario | `/caja/diario` | Sesión del día, cobranzas, operaciones |
| Cobro / cobro en bloque | `/caja/cobro`, `/caja/cobro-bloque` | Cobro en campo / planilla |
| Consulta | `/credito/consulta` | Apoyo al cliente |

### Flujo típico — día de caja

1. Verificar que el **encargado** asignó y fondeó su caja.
2. Abrir **Caja diario** → estado **ABIERTO**.
3. Buscar cliente / crédito.
4. Seleccionar cuotas → importe recibido → tipo de pago (efectivo / digital).
5. Si paga con **varios medios**: marque **Dividir en varios medios**. En **Central** use **Yape Gerente / Yape René** e **Interbank Gerente / Interbank René** (el total de cada marca es la suma de ambas billeteras). En **sucursal** use Yape / Interbank genéricos. Plin, BCP, Nación y efectivo aplican en todas las sedes.
6. Confirmar → ticket PDF.
7. Al cierre: entregar cuadre al encargado (conteo de billetes si aplica).

**Adelanto el mismo día del desembolso:** si el cliente paga antes de su **1.er pago** (incluso el día que se desembolsó), regístrelo como pago libre / cobro normal (o cobro mixto). **No** use «Completar impagos» para eso: esa opción solo aplica a créditos cuyo 1.er pago ya llegó y hoy tenían cuota por cobrar sin movimiento.

### Cobro en bloque / campo

1. Cargar cartera del día.
2. Tipar montos o marcar cuotas.
3. Si el cliente paga con **varios medios**: **Dividir** (Central: billeteras Gerente/René de Yape e Interbank; sucursal: medios genéricos) + efectivo. La suma debe cuadrar al céntimo.
4. Enviar planilla (online u offline según cobertura).
5. Revisar **Enviados** y tickets.

### Mora al cobrar

- El total a cobrar incluye la mora de las cuotas seleccionadas (misma base que la grilla).
- Si el producto posterga mora, el sistema la registra al pagar cuotas con atraso y la liquida en el momento definido por negocio (última cuota / movimiento MOR).

### Qué no debe hacer

- Cambiar roles o oficinas.
- Condonar sin proceso (solo `ADMINISTRADOR` / flujos autorizados).
- Operar bóveda de red (admin / encargado según menú).

---

## 5. Aprobador (`APROBADOR 1` / `APROBADOR 2`)

### Qué hace

Revisar solicitudes en **PEN** y aprobar o rechazar.

### Pantallas habituales

| Pantalla | Ruta | Para qué |
|----------|------|----------|
| Aprobar créditos | `/credito/aprobar` | Bandeja principal |
| Verificar pagos | `/caja/verificar-pagos` | Si el menú lo incluye |
| Reportes cobranza | menú Cobranza | Seguimiento |

### Flujo típico

1. **Inicio** → atajo **Aprobar** (si es perfil solo bandeja, ese es su hub).
2. Abrir solicitud → revisar cliente, monto, producto, plan.
3. **Aprobar** o **Rechazar** con observación si el sistema lo pide.
4. El gestor/cajero continúa con desembolso.

### Notas

- `APROBADOR 1` sin roles operativos → solo bandeja (no caja ni originación completa).
- Anular / prorrogar / cambiar analista / tope: según combinación con `ADMINISTRADOR` (ver permisos de consulta).

---

## 6. Encargado de sede

### Qué hace

Control de efectivo de la oficina: bóveda local / saldos, asignación de cajas, verificación de transferencias, apoyo a cierre.

### Pantallas habituales

| Pantalla | Ruta | Para qué |
|----------|------|----------|
| Saldos caja | `/caja/saldos` | Cierre, conteo, asignar caja |
| Verificar pagos | `/caja/verificar-pagos` | Conciliar digitales |
| Bóveda | `/tesoreria/boveda` | Movimientos locales; **Fondeo** (Central) o **A Central** (sucursal) |
| Fondear sedes / Red | `/tesoreria/fondear-sucursales`, `/tesoreria/red` | Solo Central: fondear varias sedes |
| Reportes crédito | menú Reportes | Control |

### Flujo típico — inicio de día

1. Revisar **bóveda / saldos**.
2. **Asignar caja** al cajero (importe inicial).
3. Confirmar que el cajero abre **Caja diario**.

### Red de efectivo (fondeo / remesa)

- **Central → sucursal (Fondeo):** elija el medio de Central (Yape/Interbank Gerente·René, BCP…). Si sale una billetera, la sucursal recibe el **genérico** de esa familia.
- **Sucursal → Central (A Central):** elija a qué medio de **Central** llega (Gerente/René, BCP…). En la sucursal se descuenta el genérico de la misma familia (Yape/Interbank). BCP/Plin/Nación/efectivo viajan 1:1. No hay envíos entre sucursales.

### Flujo típico — fin de día

1. Cajero entrega cuadre.
2. **Saldos** → validar cierre / conteo de billetes.
3. Verificar pagos digitales pendientes.
4. Cerrar según política de la sede (no cerrar si hay créditos/caja bloqueantes).
5. Si sobra saldo de red: **Bóveda → A Central** y elegir la billetera destino.

---

## 7. Administrador

### Qué hace

Configuración de la red, seguridad y operación completa de respaldo.

### Pantallas adicionales

| Pantalla | Ruta | Para qué |
|----------|------|----------|
| Usuarios / Roles | `/admin/usuarios`, `/admin/roles` | ACL |
| Oficinas / Cajas | mantenimiento | Catálogo multisucursal |
| Bóveda | `/tesoreria/boveda` | Movimientos de bóveda |
| Condonaciones | `/credito/condonaciones` | Mora a condonar |
| Dashboard admin | inicio / dashboard | Visión red |
| Informes | `/informes` | Catálogo amplio |

### Responsabilidades críticas

1. Asignar roles correctos (un usuario puede tener varios).
2. No mezclar oficina de prueba con producción.
3. Tras cambios de menú, validar con el usuario que ve las rutas esperadas.
4. Aplicar scripts SQL de despliegue en **cada** BD de sucursal / Azure (ej. drop de procs retirados).

---

## 8. Lectura y reportes

### Roles

- `LECTURA` — consulta de créditos / reportes.
- `REPORTE` / `REPORTEPARCIAL` — informes de crédito.
- `SALDOS CAJA` / `LECTURA_SALDO` — ver saldos sin operar caja completa.
- `ANULACION_MOV` — capacidad puntual de anulación (según menú).

### Uso

1. Entrar a **Informes** o **Reportes** del menú.
2. Filtrar por oficina / fechas / gestor.
3. Exportar PDF/CSV cuando el botón exista.
4. No modificar créditos ni caja.

---

## 9. Matriz rápida “¿quién hace qué?”

| Acción | Analista | Cajero | Aprobador | Encargado | Admin |
|--------|:--------:|:------:|:---------:|:---------:|:-----:|
| Alta cliente / solicitud | ✓ | ◐ | | | ✓ |
| Simular crédito | ✓ | ◐ | | | ✓ |
| Aprobar / rechazar | | | ✓ | | ✓ |
| Desembolsar / cobrar | ◐ | ✓ | | | ✓ |
| Ver mora / PDF notificación | ✓ | ✓ | ◐ | ◐ | ✓ |
| Asignar / cerrar caja | | | | ✓ | ✓ |
| Bóveda / red | | | | ◐ | ✓ |
| Condonar | | | | | ✓ |
| Usuarios y roles | | | | | ✓ |

◐ = solo si además tiene el rol/menú correspondiente.

---

## 10. Problemas frecuentes

| Síntoma | Qué revisar |
|---------|-------------|
| Sin permiso en una pantalla | Menú del rol en Admin → Roles |
| Caja no abre | Asignación y fondeo del encargado |
| Remesa no muestra Gerente/René | Desplegar API + aplicar `2026-11-06-usp-transferir-boveda-map-billeteras.sql` |
| No puede enviar a otra sucursal | Correcto: solo Central ↔ sucursal |
| Mora “distinta” entre pantallas | Debe coincidir la **suma de cuotas**; recargar (Ctrl+F5) |
| Ticket / PDF viejo | Reiniciar API tras despliegue |
| Oficina incorrecta | Usuario ligado a otra sede; no inventar oficina en URL |
| Pago digital no cuadra | Encargado → Verificar pagos |

---

## 11. Capacitación sugerida (por sede)

| Día | Perfil | Práctica |
|-----|--------|----------|
| 1 | Todos | Login, inicio, menú, comunicados |
| 1 | Analista | Cliente → simulador → solicitud PEN |
| 1 | Aprobador | Aprobar / rechazar esa solicitud |
| 2 | Cajero + Encargado | Asignar caja → desembolso → cobro 1 cuota → ticket |
| 2 | Analista | Consulta, mora, PDF notificación vencimiento |
| 3 | Encargado | Verificar pagos, saldos, ensayo de cierre (sin cerrar prod a la ligera) |
| 3 | Admin | Usuarios/roles, un informe gerencial, bóveda demo |

Checklist técnico de go-live: `GUIA-PRUEBAS-GO-LIVE.md`.  
Evidencia ACL automatizada: `E2E-ROLE-MODULE-MATRIX.md`.

---

## 12. Estado de roles (referencia)

Roles **activos** en catálogo (`MAESTRO.Rol.Estado = 1`):  
`ADMINISTRADOR`, `CAJA`, `ANALISTA`, `APROBADOR 1`, `APROBADOR 2`, `CAJA CENTRAL`, `LECTURA`, `ENCARGADO`, `REPORTE`, `REPORTEPARCIAL`, `ANULACION_MOV`, `SALDOS CAJA`, `LECTURA_SALDO`.

Roles **inactivos** (catálogo histórico, no usar en alta nueva):  
`VENDEDOR`, `ALMACEN`, `PROMOTOR`.

**Políticas API (escritura):** `CreditoRolOperador` acepta `CAJA` / `CAJERO` / `CAJA CENTRAL` además de admin, encargado, analista y gestor. Aprobar/rechazar exige `APROBADOR 1` o administrador; `APROBADOR 2` no abre esa política.

La asignación real de menús se administra en **Admin → Roles**; este manual describe la operación esperada, no sustituye el ACL de cada usuario.
