# Estrategia strangler fig

Spec SSD del corte: [SSD-00-cutover.md](../ssd/SSD-00-cutover.md).

La modernización convive con el MVC legacy. Las rutas nuevas se atienden por la SPA y la API .NET 10, mientras que funcionalidades no retiradas siguen disponibles por proxy o por enlaces RDLC legacy.

## Rebanadas migradas

- Autenticación JWT y menú.
- Crédito: consulta, simulador, aprobación, tareas, gestión y reportes.
- Clientes: listado, mantenimiento y validaciones principales.
- Caja: diario, saldos, cierre, caja chica, maestro y verificar pagos.
- Tesorería/Bóveda: estado, movimientos, transferencias, cierre e informes.
- Ventas/Almacén/Maestros/Admin: pantallas operativas y catálogos principales.
- Informes: pantallas SPA con JSON/CSV/PDF tabular y fallback RDLC cuando corresponde.

## Regla de paridad

La API moderna preserva lógica de negocio usando los mismos `usp_*`, validaciones de oficina/usuario desde token y reglas documentadas en `docs/*_migracion.md`.

## Retiro gradual

Cada ruta legacy se retira solo cuando la ruta SPA/API moderna pase smoke funcional, seguridad y validación de negocio.
