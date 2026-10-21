# Contrato — comunicados y agenda

**Regla:** no toca crédito, caja ni PLA. Adjunto vive en SQL.

| Tabla | Uso |
|-------|-----|
| `MAESTRO.Comunicado` | Título, cuerpo, vigencia (`FechaFin` NULL = permanente), alcance RED/OFICINA |
| `MAESTRO.ComunicadoAdjunto` | Nombre, tipo, bytes |
| `MAESTRO.ComunicadoLectura` | Quién lo vio |
| `MAESTRO.AgendaEvento` | Personal u oficina; recordatorio en minutos |
| `MAESTRO.AgendaDescarte` | Recordatorio ya cerrado por el usuario |

USP: `usp_ComunicadoListar/Obtener/Guardar/Baja/MarcarLeido`, `usp_ComunicadoAdjuntoIns/Get/Del`, `usp_AgendaListar/Guardar/Baja/Descartar`, `usp_BandejaNotificaciones`.

Alcance: `RED` (todas las sedes) o `OFICINA` + `OficinaId` de destino. El administrador elige la sede; el encargado solo la suya. Listar con `@Gestion=1` y `@EsAdmin=1` muestra toda la red para editar/retirar.
