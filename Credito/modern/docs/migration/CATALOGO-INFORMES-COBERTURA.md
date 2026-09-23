# Catálogo de informes — cobertura

Spec SSD (actores, aceptación, go-live): [SSD-07-informes.md](../ssd/SSD-07-informes.md). Este archivo y `GET /api/v1/reportes/catalogo-cobertura` son la matriz fina.

La API expone `GET /api/v1/reportes/catalogo-cobertura` para revisar la matriz de informes legacy frente a endpoints modernos.

## Estados usados

- `completo-datos`: datos disponibles en API moderna.
- `pdf-tabular-completo`: export PDF moderno con QuestPDF usando mismas columnas del CSV.
- `solo-mvc`: se conserva RDLC legacy como puente.
- `parcial`: requiere revisión de negocio o filtros adicionales.

## Política actual

Los informes críticos de crédito, caja, bóveda, almacén y ventas tienen pantalla SPA o export moderno. El PDF tabular usa `CredixLegacyReportCatalog` (título normalizado, columnas con etiqueta RDLC). Donde negocio exija el RDLC idéntico, la SPA mantiene puente a MVC mediante `VITE_LEGACY_ORIGIN`.

Actualización 2026-09-22: cobros del día y morosidad por gestor pasan a `completo-datos` (JSON+CSV+PDF). Quedan `parcial` tickets de movimiento, saldos caja chica/actual con layout RDLC distinto, y cobranza pagos (Excel sin PDF tabular).
