# Operaciones y runbooks

Índice: [DOCUMENTACION.md](DOCUMENTACION.md) · Cutover: [ssd/SSD-00-cutover.md](ssd/SSD-00-cutover.md).

## Runbooks canónicos

| Tema | Documento / script |
|------|---------------------|
| Subir servidores | [migration/DEPLOY-AL-SUBIR.md](migration/DEPLOY-AL-SUBIR.md) |
| Cutover A/B | [migration/PHASE-5-OPERATIONS-CUTOVER.md](migration/PHASE-5-OPERATIONS-CUTOVER.md), `deploy/scripts/preprod-cutover-checklist.ps1` |
| Strangler | [migration/STRANGLER-MIGRATION.md](migration/STRANGLER-MIGRATION.md), `deploy/docker-compose.strangler.yml` |
| Verificar config prod | `deploy/scripts/verify-production-config.ps1` |
| Smoke API | `deploy/scripts/smoke-local-api.ps1` |
| Smoke proxy | `deploy/scripts/smoke-strangler-proxy.ps1` |
| SQL de despliegue | `deploy/sql/` (aplicar **antes** de confiar en migración perezosa / TRF bancos / condonación) |

## Salud y correlación

- `GET /health` — self (+ SQL si hay connection string).
- Cabecera `X-Correlation-ID` en proxy (eco en respuestas).
- Logs API: categoría por operación (login, pagar-cuotas, bóveda, …).

## Rollback

1. No borrar API/SPA en el primer incidente.
2. Reenrutar la **rebanada** al MVC en nginx/IIS (`spa-legacy-redirects` / quitar rewrite).
3. Documentar en bitácora si hubo impacto financiero.

## Secretos

- Rotar JWT signing key y connection strings en el almacén del ambiente (no Git).
- WhatsApp / ApiPerú / Maps: claves por entorno; no token Meta de prueba en producción.

## Cuándo no retirar el MVC

- Primer día de cutover.
- Mientras quede un informe RDLC exigido por negocio.
- Si el smoke por rol (SSD-00 §7) no está firmado.
