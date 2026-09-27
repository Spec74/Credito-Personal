# Cierre de migración strangler

Spec SSD (criterios de corte, proxy, rollback): [SSD-00-cutover.md](../ssd/SSD-00-cutover.md).  
Documentación completa del software: [DOCUMENTACION.md](../DOCUMENTACION.md) · Arquitectura: [ARCHITECTURE.md](../ARCHITECTURE.md) · Catálogo SSD: [ssd/README.md](../ssd/README.md).

## Veredicto

La migración está en estado **candidato a go-live**. La API .NET 10 y la SPA moderna cubren los módulos operativos principales del MVC legacy, manteniendo la lógica de negocio mediante los mismos procedimientos almacenados y reglas documentadas por módulo.

El **paquete documental SSD (13/13) + arquitectura + índice** está cerrado al 2026-09-27. No se declara cierre irreversible de **producción** hasta completar smoke tests con base real, proxy productivo y revisión de los puentes RDLC que el negocio decida mantener.

## Evidencia técnica

- Backend: `dotnet build Credito.Modern.sln` correcto.
- Tests backend: `dotnet test Credito.Modern.Tests/Credito.Modern.Tests.csproj`. Conteo **812** métodos `[Fact]`/`[Theory]` (2026-09-27); historial 714 → 738 (2026-09-11).
- SPA: `npm run build` correcto.
- Documentación: [DOCUMENTACION.md](../DOCUMENTACION.md) — **paquete 100% completo** (SSD 13/13 + arquitectura + glosario + ambientes + API + tests + ops).
- UI: rutas modernas para Crédito, Clientes, Caja, Tesorería/Bóveda, Ventas, Almacén, Maestros, Admin, Informes y Reportes.
- Strangler: proxy y scripts en `deploy/`; mapeo MVC → SPA en `Credito.Modern.Web/src/utils/legacyRoutes.ts`.
- Puentes aceptados: RDLC legacy vía `VITE_LEGACY_ORIGIN` solo si negocio exige el layout idéntico. Las pantallas de informe exportan por la API JWT (PDF tabular Credix).

## Pendientes no bloqueantes

- Ejecutar smoke E2E con datos reales antes de producción.
- Agregar workflow CI en raíz si el repo remoto aún no lo contiene.
- Decidir retiro gradual de RDLC legacy cuando negocio acepte PDF tabular moderno.
- Resolver vulnerabilidades npm reportadas por `npm audit` si afectan producción.

## Criterio de cierre final

La migración puede considerarse cerrada cuando preproducción valide login, menú, operaciones críticas, reportes, proxy strangler, rollback y monitoreo.
