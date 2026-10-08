# API — visión general

Inventario largo de endpoints: [../readme.md](../readme.md).  
Specs por dominio: [ssd/README.md](ssd/README.md).  
Validación: [ssd/SSD-12-validacion.md](ssd/SSD-12-validacion.md).

## Base

- Prefijo: `/api/v1`
- Auth: JWT Bearer (`POST /auth/login`, `POST /auth/refresh`, `GET /auth/me`)
- Errores: ProblemDetails RFC 7807 (`400` validación, `401/403` auth, `409` conflicto, `422` negocio SP, `503` SQL/config)
- Salud: `GET /health` · Reloj: `GET /database-time` (`usp_FechaBD`)
- Swagger: **solo Development**

## Políticas JWT (resumen)

| Política | Uso típico |
|----------|------------|
| `CreditoUser` | Lecturas y escrituras operativas de crédito/caja/bóveda |
| `CreditoRolAdministrador` | Maestros, usuarios, parámetros |
| `CreditoRolEncargado` / `EncargadoOAdministrador` | Saldos, temporal bóveda, cierres |
| `CreditoRolAprobador1` | Bandeja aprobar |
| `CreditoRolAnularMovimientoCaja` | Anular movimiento |
| Roles menú / prendario | Según SSD del módulo |

Escrituras sensibles validan **oficina del JWT = oficina del body** (`CajaCreditoWriteGuards`).

## Mapa por dominio

| Dominio | Prefijos / ejemplos | SSD |
|---------|---------------------|-----|
| Auth / menú / oficinas | `/auth/*`, `/menu`, `/oficinas` | SSD-01 |
| Crédito | `/credito/*` (consulta, aprobar, cargos, condonar, pagar-cuotas…) | SSD-02 |
| Caja | `/credito/*` caja-diario, chica, saldos, verificar; `/cajas/*` maestro | SSD-03 |
| Bóveda | `/credito/*` boveda, transferir-boveda-* | SSD-04 |
| Clientes | `/clientes/*` | SSD-05 |
| Prendario | rutas prendario bajo `/credito` / API dedicada | SSD-06 |
| Informes | `/credito/rpt-*`, `/reportes/*`, `/ventas/rpt-*`, `/morosidad/*` | SSD-07 |
| Admin | `/usuarios/*`, `/roles/*`, oficinas CRUD | SSD-08 |
| Maestros | `/marcas`, `/modelos`, `/articulos`, `/almacenes` | SSD-09 |
| Ventas | `/ventas/*`, `/lista-precios/*` | SSD-10 |
| Almacén | `/almacen/*` | SSD-11 |

## Convenciones de escritura

1. Validar entrada (`Application/Validation` → `ProblemResults.IfInvalid`).
2. Guards JWT / scope.
3. Invocar `usp_*` o paridad BL documentada.
4. No tragarse `ArgumentException` sin reenviar el mensaje.
