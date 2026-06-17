# Proxy y backlog auth MVC

## Proxy

El proxy strangler debe enrutar:

- `/api/v1/*`, `/health`, `/swagger` → `Credito.Modern.Api`.
- `/app/*` o assets SPA → `Credito.Modern.Web/dist`.
- Rutas MVC no migradas → aplicación `Web` legacy.

Los ejemplos están en `deploy/nginx/` y `deploy/iis-arr-web.config.example`.

## Auth

La API moderna usa JWT con claims `vendix:usuario_id`, `vendix:oficina_id`, `vendix:usuario_oficina_id` y roles de `MAESTRO.Rol`.

El MVC debe seguir protegido por su sesión/autenticación legacy durante la convivencia. Antes de producción, ejecutar `deploy/scripts/verify-mvc-auth-coverage.ps1` y revisar controladores sin filtro `[Autenticado]`.

## Backlog

- Validar que rutas RDLC abiertas desde SPA requieran sesión MVC válida.
- Documentar excepciones necesarias para login, assets y health checks.
- Retirar rutas MVC cuando exista pantalla SPA con smoke aprobado.
