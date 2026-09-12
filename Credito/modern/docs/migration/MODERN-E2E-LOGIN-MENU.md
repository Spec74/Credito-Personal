# Modern E2E — Login y menú

Spec SSD: [SSD-01-auth-inicio.md](../ssd/SSD-01-auth-inicio.md).

## Flujo

1. `POST /api/v1/auth/login` valida usuario, oficina y acceso.
2. La SPA guarda sesión JWT en `AuthProvider`.
3. `GET /api/v1/auth/me` confirma claims.
4. Menú moderno se resuelve desde datos legacy y `legacyRoutes.ts`.

## Validación

Probar usuarios gestor, cajero, encargado, aprobador y gerente. Confirmar que cada rol ve solo rutas permitidas y que los ítems MVC conocidos redirigen a SPA cuando existe pantalla moderna.
