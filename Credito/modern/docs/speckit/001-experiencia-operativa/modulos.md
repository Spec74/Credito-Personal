# Inventario del sistema moderno — cuellos y objetivo

**Ámbito:** toda la SPA Credix (`SSD-01` … `SSD-12`), no un módulo.  
**Fecha:** 2026-10-20.

La empresa opera **crédito personal + prendario + caja/tesorería + (en sedes que lo tengan) ventas/almacén**. La ventaja competitiva es **un solo puesto de trabajo por sucursal**, no un MVC copiado.

## Mapa de producto (lo que ya existe)

```
Auth / Inicio     Clientes        Crédito personal     Prendario
Caja / Cobro      Bóveda          Informes / Reportes  Admin / Oficinas
Maestros          Ventas *        Almacén *            Validación transversal
```

`*` Ventas y almacén están implementados; el menú vivo de oficina 1 (CREDITO) **no** los lista. En una sede que venda mercadería, el mismo Credix debe servirlos con el **mismo** estándar visual.

## Matriz por módulo

| SSD | Superficies hoy | Cuello de botella | Simplificación | Hueco para “sistema completo” |
|-----|-----------------|-------------------|----------------|-------------------------------|
| **01 Auth / inicio** | Login, `/inicio` (dashboard admin **o** analista **o** hub de módulos) | Tres aterrizajes distintos; el gestor cae a un mapa de cards | **RoleHome**: colas del rol, 1 clic a la tarea | Selector de sucursal visible; sesión caja en el chrome |
| **02 Crédito** | Hub, consulta, simulador, aprobar, tareas, condonaciones, parámetros | Consulta ≠ simulador ≠ aprobar (se pierde ficha); hub de tarjetas | Consulta = Record; aprobar = Queue; simulador = WorkCanvas | Tope/bloqueo **inline** al simular; reprogramar/prorrogar en la ficha, no “otra app” |
| **03 Caja** | Diario, chica, saldos, asignar, verificar, maestro, cobro campo | Diario es un mundo; cobro/bloque/verificar no se ven desde Hoy | Hoy cajero → diario o cobro; verificar en cola | Arqueo guiado; desembolso APR desde la misma cola; ticket siempre a 1 clic |
| **04 Bóveda** | Estado, movimientos, interoficina | Destino por ID; pestaña Pendientes incompleta | Elegir **sede**; MoneyConfirm | Vista red B-05; una bóveda abierta por oficina (B-06) |
| **05 Clientes** | Listado CRUD, ficha larga, GPS, ApiPerú | Ficha densa; alta saca del flujo de crédito | Alta rápida embebida; ficha = Record por pestañas | Cliente único de red (visión cross-sede, no editar otra oficina sin permiso) |
| **06 Prendario** | Listado, nuevo, gestionar | Nuevo → gestión → PDF | WorkCanvas único | C: LTV, remate, `usp_DevolverPrenda`, prórroga. PLA sin umbral UIF. |
| **07 Informes** | Hub + ~30 pantallas + índices + morosos + cierre gerencial | Catálogo = ruido; gerencia pide Excel | 6–8 informes “de decisión” en Hoy; el resto en búsqueda | Cierre gerencial + morosos en el tablero red, no isla ACL |
| **08 Admin** | Usuarios, roles, oficinas, comisiones | Oficina ≠ sucursal operable | Wizard provisión de sede | Roles **por oficina** visibles en la ficha de usuario (matriz sede × rol) |
| **09 Maestros** | Marcas, modelos, tipos, artículos, almacenes | CRUD genérico, OK | Mismo Page + filtros; densidad | Nomenclatura por sede solo si el negocio lo pide (hoy global) |
| **10 Ventas** | Hub, venta rápida, orden, lista precios, canje | Fuera del día de crédito; exige caja | Si hay menú: atajo en Hoy cajero | Venta y desembolso no pelean la misma caja (estado claro) |
| **11 Almacén** | Entrada (wizard), salida, transferencia, kardex | Asistente de 4 pasos (necesario) | Progress claro; no hints de más | Stock de la **sede**; transferencia inter-almacén con oficina |
| **12 Validación** | formRules + Application | Inconsistente entre páginas | Toda Page usa las mismas reglas | Mensajes cortos, junto al campo, en **todas** las rutas |

## Flujos de negocio (sistema, no menú)

Estos son los procesos que el gerente mide. Cada uno cruza módulos.

```
1. Abrir el día (sucursal)
   Bóveda abierta → asignar/fondear cajas → cajero en sesión
   Hoy: Caja + Bóveda. Cuello: 3 menús. Objetivo: checklist en RoleHome.

2. Onboarding cliente
   Buscar DNI → ficha / rápido → listo para crédito o venta
   Hoy: sale a /clientes/nuevo. Objetivo: drawer o canvas embebido.

3. Originación crédito personal
   Simular → solicitar → generar → aprobar → desembolsar en caja
   Cuello: 4 superficies. Objetivo: WorkCanvas + Queue aprobación + cola desembolso.

4. Originación prendaria
   Bienes + crédito → CRE → plan/PDF → (caja)
   Cuello: 2–3 pantallas. Objetivo: un canvas (igual patrón que 3).

5. Cobranza
   Cartera del día → cobro mostrador o campo (GPS) → verificar transferencias
   Cuello: diario / bloque / verificar. Objetivo: un hilo desde Hoy.

6. Tesorería del día
   E/S, chica, arqueo, cierre cajas → bóveda → (opcional) devolver a central
   Cuello: cierre oficina ≠ 1 sin smoke. Objetivo: wizard de cierre + B-02.

7. Custodia / mercadería
   Prenda en custodia  |  stock almacén (si la sede vende)
   Cuello: no hay “Bienes” unificado. Objetivo: Record de ítem (prenda o serie).

8. Gobierno de la red
   Alta sede, usuarios×oficina, fondeo, tablero comparativo, informes
   Cuello: checklist oral + 30 reportes. Objetivo: wizard + 6 KPI + búsqueda.
```

## Estándar visual único (todas las páginas)

No hay “Credix para crédito” y “otra cosa para caja”. **Todas** las ~95 páginas adoptan:

| Elemento | Regla |
|----------|--------|
| Título | 1 H1, verbo o sustantivo corto (`Caja del día`, no `Gestión de…`) |
| Contexto | Oficina + (caja si aplica) en el chrome |
| Acción primaria | 1 botón principal (derecha del head o sticky móvil) |
| Stats | 0–4, solo si cambian la decisión; no KPI de adorno |
| Filtros | Barra colapsable; no 8 combos siempre abiertos |
| Tabla | Estado con texto; acciones en menú `⋯` si hay >2 |
| Formulario | Grid 12; labels arriba; error bajo el campo |
| Ayuda | 0 párrafos. Tooltip `?` de una línea si hace falta |
| Confirmación de dinero | `MoneyConfirm` (importe grande + oficina + irreversible) |
| Vacío | Una frase + la acción (`No hay cuotas hoy` → `Ir a cartera`) |
| Responsive | Mostrador / tablet / cobrador según el **puesto**, no “encoger” |

Adopción: Oleada A define los patrones; **cada PR que toque una página** la deja en estándar (no un big-bang de 95 archivos).

## Completar el sistema (huecos transversales)

Prioridad de negocio, no de “módulo favorito”:

| # | Hueco | Módulos | Por qué es ventaja |
|---|--------|---------|---------------------|
| 1 | Home por rol + chrome de sucursal | 01, todos | Tiempo a la primera tarea |
| 2 | WorkCanvas originación (personal **y** prendario) | 02, 06, 05 | Ventanilla más rápida que la competencia |
| 3 | Hilo de cobro + desembolso | 03, 02 | Menos efectivo mal aplicado |
| 4 | Día de tesorería / wizard sede / tablero red | 04, 08, 01, 07 | Multisucursal real |
| 5 | Ficha única de cliente/crédito (Record) | 02, 05, 06 | Deja de “buscar de nuevo” |
| 6 | Informes de decisión vs archivo | 07 | Gerencia sin Excel de 30 pestañas |
| 7 | Ciclo prendario post-desembolso | 06 | Completar producto prenda |
| 8 | Ventas+almacén en el mismo día de caja (sedes que vendan) | 10, 11, 03 | Un sistema, no dos empresas |
| 9 | Matriz usuario × sede × rol | 08 | No más “admin en Huamanga y mudo en Pichari” |
| 10 | PLA/umbral y rastro visible | 05, 08 | Completar cumplimiento (UIF), no teatro |

## Qué no es “falta”

- Recalcular mora/cuota en el front.
- Copiar RDLC.
- App nativa.
- Un skin distinto por módulo.
