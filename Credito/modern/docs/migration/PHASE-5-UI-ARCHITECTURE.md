# Fase 5 — Arquitectura UI

## Capas

- `src/api`: cliente HTTP y endpoints.
- `src/auth`: sesión JWT y rutas protegidas.
- `src/pages`: pantallas por módulo.
- `src/components/credix`: componentes visuales reutilizables.
- `src/utils/legacyRoutes.ts`: mapeo MVC → SPA para el patrón strangler.

## Principios

- Mantener familiaridad funcional con MVC.
- Modernizar navegación, responsive, tablas y acciones.
- Usar fallback legacy solo cuando el layout RDLC o una ruta pendiente lo requiera.
