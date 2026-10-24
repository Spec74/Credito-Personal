# Research — sistema Credix completo (no un módulo)

**Feature:** `001-experiencia-operativa` · 2026-10-20  
**Ámbito:** SSD-01 … SSD-12 (auth, clientes, crédito, prendario, caja, bóveda, informes, admin, maestros, ventas, almacén).  
**Método:** web 2025–2026 (microfinanzas multiagencia + prenda + UX enterprise) + SSD as-built + inventario [modulos.md](modulos.md).

## 0. Conclusión de investigación (sistema)

Las plataformas que ganan en microfinanzas (SwiftlyMFS, Tiwala, Fincore, zBanco, FelmuCBS) y en prenda (Caja Arequipa, gold-loan ERPs) no venden “un módulo bonito”. Venden **un core**:

1. Un cliente, una caja, un mayor (aquí: `usp_*` + JWT de oficina).
2. Oficiales de campo + ventanilla + tesorería + gerencia en **la misma** superficie.
3. Multi-sucursal de fábrica (no “oficina 1 y el resto después”).
4. Cobranza y efectivo tan importantes como originar.
5. Reportes vivos (PAR, caja, productividad), no un mall de 30 informes.

Credix **ya tiene** esas piezas de negocio. La brecha vs el mercado es de **orquestación y UX**, más un cierre de ciclo (prenda post-desembolso, wizard de sede, home por rol). Ventas/almacén son el mismo sistema el día que una sede los active: mismo estándar, misma caja.

## 1. Qué pidió gerencia (traducido a diseño)

| Frase | Implicación de producto |
|-------|-------------------------|
| Siguiente nivel, no “bonito este año” | Design tokens + patrones; no skin de temporada |
| Ordenado, visible, amigable, usable | IA por rol, una acción primaria, densidad, teclado |
| Sin ruido ni confusión | Progressive disclosure; errores locales; cero hints eternos |
| Legacy funcional pero viejo / no responsivo | Conservar atajos de poder; mobile/tablet de verdad |
| Cuellos de botella y simplificar | Menos pantallas por tarea; wizard sucursal; originación única |
| Ventaja competitiva del sector | Velocidad ventanilla + control custodia/caja + cobranza + red de sedes |
| Completar lo que falta | Ciclo prendario post-desembolso + consolidado HO |
| Multisucursal | Contexto de oficina + plantilla de alta + tablero red |

## 2. Hallazgos externos (qué usa quien gana)

### 2.1 Sistemas de crédito / LOS-LMS

Fuentes: [LenndOS / Junngla](https://www.junngla.com/en/lenndos), [theecode LOS/LMS](https://www.theecode.com/insights/designing-an-los-lms-you-can-actually-operate.html), Grihum × Think Design (red de sucursales, mobile campo + web operaciones).

- **Un dato, cinco puertas:** cliente, sucursal, operaciones, cumplimiento, administración. No un menú plano de 40 ítems.
- **Matriz por sede:** tasas, topes, descuentos con autor. Nosotros ya tenemos producto + `GastosAdm` + oficina JWT; falta **visibilidad** y **home por rol**.
- **Campo ≠ ventanilla:** cobrador (móvil, GPS que ya tenemos) vs analista (teclado, densidad).
- **Auditoría:** quién aprobó qué. El moderno ya registra usuario JWT; la UI debe **mostrar** el rastro en ficha.

### 2.2 Prenda / gold loan (el analog más cercano)

Fuentes: Jaguar Gold Loan, FinnOne Neo Gold, Swarna, PawnSoftware multi-branch, LombardX, y **Caja Arequipa** (mismo país, mismo producto).

Ciclo que el mercado da por cerrado y **nosotros tenemos a medias**:

```
Tasación (peso/calidad o ítem) → LTV → desembolso → custodia etiquetada
  → cobro / renovación / ampliación → vencido → aviso → remate
  → excedente o faltante → devolución del bien
```

| Pieza de mercado | Credix hoy | Hueco |
|------------------|------------|-------|
| Tasación + N bienes | Sí (suma, validación viva) | No hay metal/peso/quilataje (opcional: el negocio empeña también electrónicos) |
| LTV | Monto ≤ tasación | No hay % visible ni tope &lt; 100% parametrizable |
| Un escritorio de prenda | Nuevo + Gestión + Simulador | P-03 pendiente |
| Custodia / bóveda de bienes | Estado EN CUSTODIA | Sin recojo, sin conciliación ítem vs crédito |
| Renovación (hasta N veces, p.ej. 24 meses en Caja Arequipa) | No | Flujo + parámetro |
| Remate (aviso 60 días / diario en guía Caja Arequipa) | `FechaRemate`, WhatsApp 3d | Cola y acta de remate |
| Hoja resumen TEA/TCEA | Contrato + plan | PDF transparencia |
| Multi-sede: tasa oro / tope centralizado | Oficinas + JWT | Tablero red + fondeo consolidado (B-05) |
| PLA: sujeto obligado (Ley 29038); registro SBS 650-2024; RO/ROS D.S. 020-2017-JUS | Cliente + DNI | RO US$ 10k / 50k + bitácora ROS; no “somos banco” |

**Conclusión:** no hay que copiar un ERP indio. Hay que **cerrar el ciclo de prenda + el día de caja de cada sede**. Eso es la ventaja frente a Excel + MVC.

### 2.3 UX enterprise que no caduca

Fuentes: WCAG 2.2, [density tokens](https://www.accessible-data-interfaces.com/accessible-data-component-libraries/design-system-tokens-and-component-contracts/density-tokens-and-target-size-in-data-grids/), NN/g progressive disclosure, estudios fintech 2026 (confianza = claridad de riesgo e irreversibles).

- Tokens de **densidad** separados de **tamaño de hit**.
- Color no es el único canal (mora, LTV, caja abierta).
- Dashboards: pocos KPI; el detalle está a un clic. &gt;1000 marcas o 12 cards no es “moderno”, es ruido.
- Design system = velocidad de features **futuras**. Rediseñar cada año es el anti-objetivo de gerencia.

## 2.4 Core de microfinanzas (el analog del **sistema**, no de la prenda)

Fuentes: [SwiftlyMFS](https://swiftlymfs.com/), [Tiwala](https://tiwa.la/), [Fincore](https://www.fincore.live/), [zBanco](https://www.zbanco.online/), [FelmuCBS](https://vortexsol.com/core-banking-system.html).

| Capacidad de un MFI moderno | Credix | Acción |
|----------------------------|--------|--------|
| Lending + cash + KYC + reportes en un login | Sí (módulos) | Unirlos en RoleHome, no en 12 hubs |
| Teller / EOD / arqueo | Caja + bóveda | Wizard de día y de cierre |
| Field offline + GPS | `/caja/cobro` | Cola en Hoy |
| Multi-branch RBAC | `UsuarioOficina` + menú | Chrome + wizard + tablero red |
| PAR / mora viva | Informes + dashboards | 6 KPI, no 30 tiles |
| Inventario / tienda | SSD-10/11 implementados | Mismo Credix cuando el menú exista |
| Maker-checker | Aprobar + verificar pagos | Queue, no otra isla |

No vamos a construir un CBS de banco. Vamos a **parecer y operar** como un core de microfinanzas de sucursales: eso es la ventaja en Ayacucho.

## 3. Diagnóstico interno (cuellos de botella)

Inventario por SSD: [modulos.md](modulos.md). No es lista de endpoints.

```
Home (01)              ──► 3 aterrizajes; gestor a mapa de cards
Cliente (05)           ──► ficha larga; alta saca del crédito/caja
Originación personal   ──► simular / generar / aprobar / caja
Originación prendaria  ──► 2–3 superficies
Aprobación (02)        ──► pierde ficha
Cobro (03)             ──► diario / campo / verificar
Tesorería (04)         ──► ID bóveda; Pendientes = leftovers + Anular
Alta sede (08)         ──► 8 pasos sueltos
Informes (07)          ──► ~30 tiles
Ventas/almacén (10/11) ──► mismo Credix pero fuera del “Hoy”
Textos (todas)         ──► hints de migración
WhatsApp 3d            ──► sin idempotencia en BD
```

### Flujos actuales → objetivo

| Flujo | Hoy | Cuello | Solución óptima |
|-------|-----|--------|-----------------|
| Nuevo prendario | Cliente + bienes + crédito → Gestión/simulador | Cambio de contexto; bienes “ya no editables” + revalidar | **WorkCanvas** único (US-2) |
| Generar plan | Segunda pantalla | Retraso a PDF/contrato | Mismo canvas, panel Resultado |
| Cobro del día | Caja hub → diario o bloque | El gestor no ve “su lista” al entrar | Home rol + cola |
| Aprobar | `/credito/aprobar` | Sale de la ficha | Split: cola \| expediente |
| Fondear sede | Bóveda + ID destino | Error humano, no consolidado | B-05 + confirmación MoneyConfirm |
| Alta Andahuaylas | Checklist §5.1 | 8 sistemas mentales | Wizard 5 pasos |
| Cliente bloqueado / tope | Pantallas aparte | Se descubre tarde | Bloqueo en originación (ya hay maestros; falta *inline*) |
| Vencido prendario | Listado + wa.me | Sin cola de remate | Estados: aviso / remate / cerrado |
| Informe gerencial | Catálogo SSD-07 | Sobrecarga | 6 KPI red + export |

## 4. Solución visual (2026 y 2028)

**Nombre:** Credix Work OS (no es marca nueva; es disciplina sobre Credix).

### 4.1 Arquitectura de información

```
[ Sucursal · Usuario · Caja ]     búsqueda global (cliente / crédito / contrato)
Hoy (rol)  │  Cartera  │  Dinero  │  Bienes  │  Config
```

- **Hoy:** colas del rol (originación incompleta, por aprobar, por cobrar, por fondear, por vencer).
- **Cartera:** consulta unificada personal + prendario con facet producto.
- **Dinero:** caja, bóveda, verificar, interoficina.
- **Bienes:** custodia, devolución, remate (cuando existan).
- **Config:** maestros, usuarios, oficinas (admin).

El menú SQL **sigue existiendo** (permiso). La home **no es el menú**.

### 4.2 Patrones de pantalla (contratos UI)

| Patrón | Uso | Anatomía |
|--------|-----|----------|
| `Page` | Listados e informes | Título + 0–4 stats + filtros + tabla |
| `WorkCanvas` | Originación, ficha viva | 3 zonas: contexto \| formulario \| resultado |
| `Queue` | Aprobar, cobro, remate | Lista densa + panel detalle |
| `Record` | Cliente, crédito cerrado | Cabecera estado + pestañas (plan, bienes, caja, docs) |
| `MoneyConfirm` | Desembolso, transferir, condonar | Importe grande, oficina, irreversible |

Reglas: un H1; stats solo si cambian decisión; filtros colapsados; tablas con columna de estado textual.

### 4.3 Sistema que no se rediseña cada año

Ya tenemos `--credix-brand`, espacios, focus, success/warn/danger. Falta formalizar:

- `--credix-density-row` (compact / standard / comfortable)
- Semántica de estado: `cre` `pen` `des` `vencido` `custodia` `remate`
- Tipografía: system-ui (rápida, local, sin Google Fonts que cambien)
- Motion: solo 150–200 ms en `--credix-ease`; nada de page transitions

**No hacer:** dark mode obligatorio, ilustraciones, cards con gradiente, “AI sidebar” que tape la tabla.

### 4.4 Responsive por puesto (no “encoger todo”)

| Puesto | Viewport | Prioridad |
|--------|----------|-----------|
| Mostrador | ≥992, teclado | Densidad compacta, Enter = siguiente campo |
| Encargado | tablet 768–991 | Colas + KPI |
| Cobrador | ≤640 | Solo Hoy + cobro + GPS; no maestros |

## 5. Completar el producto (prioridad de negocio)

Orden que genera ventaja **sin** inflar el sistema:

1. Quitar ruido + WorkCanvas prendario + home de rol (este año, P0).
2. Wizard sucursal + tablero red + B-05 (abre Andahuaylas sin drama).
3. LTV visible + devolución de bien + cola de vencidos/remate (cierra el ciclo).
4. Renovación parametrizada + hoja resumen (transparencia / confianza).
5. PLA umbral / bitácora (cumplimiento, no marketing).

## 6. Riesgos

| Riesgo | Mitigación |
|--------|------------|
| “Rediseñar todo” 6 meses | Patrones + 4 flujos P0; el resto adopta al tocarse |
| Inventar reglas de renovación/remate en C# | Spec de `usp_*` primero (constitución SSD) |
| Tablero HO filtra mal la oficina | JWT + `UsuarioOficina`; pruebas por sede ≠ 1 |
| Accesibilidad vs densidad | Tokens; compacto no reduce fuente ni hit &lt; 24 px |
| Confundir legacy EDMX con SPA | Esta feature es **solo Modern** + docs; PrendarioLegacy no se “redibuja” |

## 7. Referencias

- GitHub Spec Kit: https://github.com/github/spec-kit
- SBS registro préstamos/empeño: https://www.sbs.gob.pe/supervisados-y-registros/registros/otros-registros/casas-de-cambio-prestamos-y-empenos/informacion-sobre-registro-de-casas-de-cambio-prestamos-y-o-empenos
- Caja Arequipa crédito prendario (ciclo renovación/remate): https://www.cajaarequipa.pe/personas/creditos/credito-prendario/
- WCAG 2.2 AA target size 2.5.8
- Plan interno: `docs/migration/PLAN-MULTISUCURSAL-PRENDARIO.md`
