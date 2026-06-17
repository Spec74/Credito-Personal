# Cierre de migración strangler

## Veredicto

La migración está en estado **candidato a go-live**. La API .NET 10 y la SPA moderna cubren los módulos operativos principales del MVC legacy, manteniendo la lógica de negocio mediante los mismos procedimientos almacenados y reglas documentadas por módulo.

No se declara cierre irreversible hasta completar smoke tests con base real, proxy productivo y revisión de los puentes RDLC que el negocio decida mantener.

## Evidencia técnica

- Backend: `dotnet build Credito.Modern.sln` correcto.
- Tests backend: `dotnet test Credito.Modern.Tests/Credito.Modern.Tests.csproj` con 714 pruebas correctas.
- SPA: `npm run build` correcto.
- UI: rutas modernas para Crédito, Clientes, Caja, Tesorería/Bóveda, Ventas, Almacén, Maestros, Admin, Informes y Reportes.
- Strangler: proxy y scripts en `deploy/`; mapeo MVC → SPA en `Credito.Modern.Web/src/utils/legacyRoutes.ts`.
- Puentes aceptados: RDLC legacy vía `VITE_LEGACY_ORIGIN` para reportes donde el layout exacto sigue siendo requerido.

## Pendientes no bloqueantes

- Ejecutar smoke E2E con datos reales antes de producción.
- Agregar workflow CI en raíz si el repo remoto aún no lo contiene.
- Decidir retiro gradual de RDLC legacy cuando negocio acepte PDF tabular moderno.
- Resolver vulnerabilidades npm reportadas por `npm audit` si afectan producción.

## Criterio de cierre final

La migración puede considerarse cerrada cuando preproducción valide login, menú, operaciones críticas, reportes, proxy strangler, rollback y monitoreo.
