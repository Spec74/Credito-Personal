# Arquitectura — Credito.Modern

**Estado:** as-built · 2026-10-24  
**Audiencia:** informática / desarrollo  
**Mapa de carpetas:** [ESTRUCTURA.md](ESTRUCTURA.md) · **Specs:** [ssd/README.md](ssd/README.md) · índice: [DOCUMENTACION.md](DOCUMENTACION.md)

---

## 1. Vista general

```
┌─────────────────────────────────────────────────────────────┐
│  Usuario (navegador)                                        │
│  Credito.Modern.Web  (React + Vite + Ant Design)            │
│  base /app/  · JWT en memoria/storage                       │
└───────────────┬─────────────────────────────▲───────────────┘
                │ HTTPS /api/v1/*               │ (opcional RDLC)
                ▼                               │
┌───────────────────────────────┐   ┌──────────┴──────────────┐
│  Proxy strangler (nginx/IIS)  │   │  MVC legado Credito/Web │
│  /api/v1,/health → API        │   │  hasta cutover firmado  │
│  /app/ → SPA                  │   └─────────────────────────┘
│  resto → MVC                  │
└───────────────┬───────────────┘
                ▼
┌─────────────────────────────────────────────────────────────┐
│  Credito.Modern.Api  (.NET 10 Minimal APIs + JWT)           │
│  Policies: CreditoUser, roles MAESTRO                       │
└───────────────┬─────────────────────────────────────────────┘
                │
    ┌───────────┼───────────┬────────────────┐
    ▼           ▼           ▼                ▼
 Application  Infra      Domain         SQL Server CREDITO
 Validation   Dapper     (contratos)    usp_* / tablas
 services     SqlClient                 usp_FechaBD
```

**Strangler:** el MVC convive hasta smoke + OK de negocio ([SSD-00](ssd/SSD-00-cutover.md)).

---

## 2. Capas .NET

| Proyecto | Responsabilidad |
|----------|-----------------|
| `Credito.Modern.Domain` | Contratos / valores de dominio mínimos |
| `Credito.Modern.Application` | Casos de uso, DTOs, **Validation/** |
| `Credito.Modern.Infrastructure` | Dapper, SQL, integraciones (WhatsApp, etc.) |
| `Credito.Modern.Api` | Endpoints Minimal API, JWT, ProblemDetails |
| `Credito.Modern.Web` | SPA |
| `Credito.Modern.Tests` | Tests de endpoints / contratos |

Dependencias: Api → Application → Domain; Infrastructure implementa Application.

---

## 3. Contrato de negocio

1. La lógica monetaria vive en **SQL (`usp_*`)**. El C# orquesta, valida entrada y mapea errores.
2. Excepción declarada: donde el MVC ya escribía SQL en BL (venta rápida, parte de almacén), el moderno replica esa paridad y lo dice en el SSD del módulo.
3. Reloj de negocio: `CREDITO.usp_FechaBD`.
4. Esquema versionado / snapshot: `db/schema/`.

---

## 3.1 Una base, varias sedes

**Decisión (2026-10-24):** una sola base `CREDITO`. La sucursal no es un servidor.

| Pieza | Modelo |
|-------|--------|
| Física | Un SQL Server (local o Azure). Backup FULL + log. |
| Lógica | `MAESTRO.Oficina` + `OficinaId` en bóveda, caja, crédito, `UsuarioOficina` |
| Central | `Oficina.IndPrincipal` (Huamanga). Fondea, ve la red. |
| Sucursal | Filtro de fila (JWT `vendix:oficina_id` + USP). Autonomía de caja/bóveda, no de datos. |
| Alta de sede | Oficina + bóveda abierta + cajas + `UsuarioOficina`. Sin SQL nuevo. |

No hay una base por sucursal ni un mallado entre sedes. Fondeo (`usp_TransferirBoveda`), cierre gerencial de 19 carteras y un DNI / un login cruzan oficinas en el mismo ledger. Si cae la línea en Pichari, se endurece el enlace y (más adelante) una cola de cobros al mismo SQL; no un Express que «se reconcilia de noche».

Se revisaría el modelo físico solo ante otra RUC, 20+ sedes con reportes pesados, o días enteros offline: réplica de lectura o cola local, no partir el ledger.

---

## 4. Seguridad

| Pieza | Comportamiento |
|-------|----------------|
| Login | `POST /api/v1/auth/login` — Usuario + UsuarioOficina (+ Acceso opcional) |
| Claves | Claro legado o PBKDF2 `$pbk2$` (`UsuarioPasswordHasher`) |
| Access JWT | Claims `vendix:usuario_id`, `vendix:oficina_id`, `vendix:usuario_oficina_id`, roles |
| Refresh | `POST /auth/refresh` + versión de refresh |
| Escrituras | `CajaCreditoWriteGuards`: JWT oficina = body; crédito/caja de esa oficina |
| Menú | `MAESTRO.usp_MenuLst`; SPA `EXACT_MENU_ROUTES` para ítems sensibles |
| Production | `AllowDevToken=false`, sin query de menú, rate limit, cliente acceso |

Detalle auth: [SSD-01](ssd/SSD-01-auth-inicio.md).

---

## 5. Validación (calidad 2026)

Capa transversal documentada en [SSD-12](ssd/SSD-12-validacion.md):

| Lado | Ubicación | Rol |
|------|-----------|-----|
| BE | `Application/Validation/*` + `Api/Validation/ProblemResults` | Rechazo 400 RFC 7807 antes del SP |
| FE | `Web/src/validation/formRules.ts` | Reglas Ant Design (required, maxLength, money) |

Reglas compartidas: IDs ≥ 1, montos > 0 (2 decimales), glosa ≤ 250, denominación ≤ 100, documentos PE (DNI/RUC/celular).

---

## 6. Frontend

| Pieza | Detalle |
|-------|---------|
| Stack | React, Vite, TanStack Query, Ant Design, React Router |
| Brand | CrediConfiable / Credix (paneles, tablas, PDF tabular) |
| Auth | Token + oficina de sesión; rutas según menú |
| Proxy local | Ver `run-local.md` / scripts `deploy/` |

---

## 7. Despliegue y ambientes

Detalle tabular: [AMBIENTES.md](AMBIENTES.md). Runbooks: [OPS.md](OPS.md).

| Ambiente | Piezas |
|----------|--------|
| Development | `dotnet run` API + `npm run dev` SPA; Swagger on |
| Staging / strangler | Docker compose + nginx `:9080` |
| Production | App Service / IIS + SPA `/app/`; secretos por entorno |

---

## 8. Observabilidad y errores

- Health: `GET /health` (+ SQL si hay connection string).
- Correlation: `X-Correlation-ID` en proxy.
- Errores de validación/negocio: ProblemDetails (`title`, `detail`, status 400/409/422/503).
- No filtrar `ArgumentException.Message` al cliente (convención ProblemResults).

---

## 9. Qué no es esta arquitectura

- No es un segundo motor de cuotas/mora en C#.
- No es copia píxel a píxel de ReportViewer.
- No es microservicios: un API + una SPA + una BD CREDITO.
- No es un SQL por sucursal ni bases que se sincronizan entre sí.
