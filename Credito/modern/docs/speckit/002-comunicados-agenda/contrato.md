# Contrato — comunicados y agenda

**Regla:** no toca crédito, caja ni PLA. Adjunto vive en SQL.

| Tabla | Uso |
|-------|-----|
| `MAESTRO.Comunicado` | Título, cuerpo, vigencia (`FechaFin` NULL = permanente), alcance RED/OFICINA |
| `MAESTRO.ComunicadoAdjunto` | Nombre, tipo, bytes |
| `MAESTRO.ComunicadoLectura` | Quién lo vio |
| `MAESTRO.AgendaEvento` | Personal u oficina; recordatorio en minutos |
| `MAESTRO.AgendaDescarte` | Recordatorio ya cerrado por el usuario |

USP: `usp_ComunicadoListar/Obtener/Guardar/Baja/MarcarLeido`, `usp_ComunicadoAdjuntoIns/Get`, `usp_AgendaListar/Guardar/Baja/Descartar`, `usp_BandejaNotificaciones`.
