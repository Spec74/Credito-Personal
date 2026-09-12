# SSD-06 — Crédito prendario y WhatsApp

**Estado:** as-built · 2026-09-11  
**Código:** `/credito/prendario`, `/nuevo`, `/gestionar/:personaId` · `/api/v1/prendario/*`  
**Fuente legacy:** `PrendarioController`, `PrendaBL`, `CrearSolicitudCreditoPrendario` (rama Prendario-Eber)  
**Doc de ingeniería:** [PARIDAD-PRENDARIO.md](../migration/PARIDAD-PRENDARIO.md) (reglas fijas de alta, categorías, contrato)

## 1. Propósito y actores

Cartera prendaria: listado, alta de solicitud, bienes en custodia, contrato/acta y aviso a 3 días.

| Rol | Uso |
|-----|-----|
| Analista | Único rol de menú (`PRENDARIO - Listado` / `Nuevo`) |
| Administrador | API `CreditoRolPrendario` también (analista o admin); el menú SQL es ANALISTA |
| Gestor | El hub crédito **no** abre prendario |

## 2. Alcance

**Entra**

- Listado + resumen (tarjetas DES), nueva solicitud, gestión de prendas
- Contrato y acta PDF (QuestPDF + cláusulas embebidas)
- Chat `wa.me` puntual y plantilla Cloud API a 3 días

**No entra**

- Crédito personal (producto ≠ prendario) → SSD-02
- Desembolso en caja → SSD-03
- Ficha de persona (se reutiliza SSD-05)

## 3. Paridad MVC

| Legacy | SPA | Observación |
|--------|-----|-------------|
| `Prendario` | `/credito/prendario` | Exact menu |
| `Prendario/Create` | `/credito/prendario/nuevo` | Exact; no redirige a Mantener vacío: busca o `/clientes/nuevo` |
| `Prendario/Gestionar` | `/credito/prendario/gestionar/:personaId` | Listado habilita gestionar |
| Contrato / acta RDLC | `contrato-pdf`, `acta-entrega-pdf` | 409 si no hay bienes |
| WhatsApp IIS / clave de prueba | Hosted service 08:00 Lima + `POST avisos-vencimiento/enviar` | Token user-secrets |

Identificación: `EsPrendario = 1` **o** `ProductoId = 2`. Alta fija: ver tablas en PARIDAD-PRENDARIO (monto 500, cuotas 1, interés 8, etc.).

## 4. Contrato de datos

| Procedimiento / tabla | Uso |
|-----------------------|-----|
| `CREDITO.Credito` (`EsPrendario`, `ProductoId = 2`) | Cartera (listado: `EsPrendario = 1` **o** `ProductoId = 2`) |
| `CREDITO.Prenda` | Bienes (reemplazo completo al guardar) |
| Alta solicitud / generar | Mismos valores que `CrearSolicitudCreditoPrendario` / `CrearCredito` |
| `FechaNotifWhatsapp3d` | Aviso 3 días (solo `EsPrendario = 1`) |
| Plantilla Meta `aviso_vencimiento_prendario` (es) | Cloud API v25.0 |

El C# no inventa tasación: suma solo prendas insertadas (el MVC sumaba también filas vacías).

## 5. API y SPA

Política `CreditoRolPrendario` (ANALISTA o administrador).

| Método y ruta | Pantalla |
|---------------|----------|
| `GET /api/v1/prendario/resumen`, `GET .../creditos` | Listado |
| `POST /api/v1/prendario/crear-solicitud` | Nuevo |
| `GET /api/v1/credito/prendas`, `POST /api/v1/credito/guardar-prendas` | Gestión |
| `GET .../contrato-pdf`, `GET .../acta-entrega-pdf` | Gestión |
| `GET .../avisos-vencimiento`, `GET .../estado` | Listado / estado canal |
| `POST .../avisos-vencimiento/enviar`, `POST .../marcar` | Envío / marca |

Chat WhatsApp de gestión no pasa por Cloud API (`prendarioWhatsapp.ts` → `wa.me`).

## 6. Seguridad

- `EXACT_MENU_ROUTES`: `/credito/prendario`, `/credito/prendario/nuevo`. Hub `/credito` no alcanza.
- Listado moderno **filtra oficina JWT** (el MVC no filtraba oficina).
- Token Meta nunca en Git ni en `GET .../estado`.

## 7. Criterios de aceptación

- [ ] ANALISTA con menú listado entra a `/credito/prendario`; sin menú, 403 de ruta.
- [ ] Hub crédito no abre prendario ni gestionar.
- [ ] Nuevo no crea segunda solicitud CRE abierta (bitácora alta repetida).
- [ ] Guardar prendas reemplaza el detalle; `MontoTasacion` = suma insertada.
- [ ] Contrato/acta 409 sin bienes; con bienes anexan cláusulas.
- [ ] Aviso 3 días: DES, vencen en N días, aún no notificados hoy, oficina sesión.
- [ ] Número de prueba Meta solo entrega a testers de la consola.

## 8. Desviaciones

Relato en [BITACORA-DESVIACIONES.md](../migration/BITACORA-DESVIACIONES.md):

- 2026-09-09 — script menú ANALISTA; `Prenda` vs `CreditoPrenda`; `MontoTasacion`; oficina JWT; categoría en SQL; alta repetida; CTE listado; WhatsApp hosted; `usp_Credito_Ins` + índice; contrato QuestPDF
- 2026-09-09 — prendario restringido al rol ANALISTA en la SPA

## 9. Pruebas y evidencia

- API: `prendarioendpointtests`, `prendariodocumentotests`, `whatsappvencimientoprendariotests`
- SPA: `menuRouteAccess.test.ts` (hub crédito vs prendario; listado vs nuevo), `prendarioWhatsapp.test.ts`, `resolvespapathfrommenuitem.test.ts`
- Smoke: analista abre listado; gestor no; enviar plantilla a un tester Meta

## 10. Go-live

Listo en Development (token de prueba en user-secrets). Producción: WABA y token propios (`WhatsApp__*`), no el número +1 555 de Meta.
