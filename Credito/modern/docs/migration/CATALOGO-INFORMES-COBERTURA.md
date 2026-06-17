# Catálogo de informes — cobertura

La API expone `GET /api/v1/reportes/catalogo-cobertura` para revisar la matriz de informes legacy frente a endpoints modernos.

## Estados usados

- `completo-datos`: datos disponibles en API moderna.
- `pdf-tabular-completo`: export PDF moderno con QuestPDF usando mismas columnas del CSV.
- `solo-mvc`: se conserva RDLC legacy como puente.
- `parcial`: requiere revisión de negocio o filtros adicionales.

## Política actual

Los informes críticos de crédito, caja, bóveda, almacén y ventas tienen pantalla SPA o export moderno. Donde negocio exige layout RDLC idéntico, la SPA mantiene puente a MVC mediante `VITE_LEGACY_ORIGIN`.
