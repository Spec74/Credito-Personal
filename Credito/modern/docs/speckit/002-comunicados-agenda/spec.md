# Comunicados y agenda — spec

**Spec Kit:** 002 · 2026-10-21  
**SSD:** [SSD-14](../../ssd/SSD-14-comunicados-agenda.md)  
**Investigación:** [research.md](research.md)  
**Quién pide:** gerencia. Todo el personal lo usa; solo admin/encargado publica.

## Problema

No hay un canal interno. Los reglamentos se pierden en WhatsApp. No hay agenda de sucursal ni recordatorio en pantalla. El chrome (sede / Cambiar sede) se rompe en el celular.

## Comunicados (avisos / noticias / reglamentos)

Modelo de intranet + campana (Workplace / Slack anuncios / BambooHR):

| Tipo | Vigencia | Uso |
|------|----------|-----|
| Aviso | Fecha fin | Novedad, horario, reunión |
| Permanente | Sin fecha fin | Reglamento, circular, archivo que debe quedar |

- El gerente escribe título + texto.
- Sube **imágenes** (jpg/png/webp, ≤ 2 MB) y **PDF** (≤ 8 MB).
- Alcance: **toda la red** o **solo esta sede**.
- El personal ve campana (no leídos) + bandeja `/comunicados`.
- Marcar leído no borra el aviso. El permanente sigue en la bandeja.
- La imagen se ve en la ficha; el PDF se descarga.

No es correo. No se envía a UIF ni a clientes.

## Agenda

Modelo Outlook / Google Calendar, recortado a sucursal:

- Evento **personal** (solo yo) o **de sede** (admin/encargado).
- Fecha/hora, todo el día, notas.
- Recordatorio: 15 / 30 / 60 / 1440 minutos antes (o ninguno).
- La campana muestra los que ya vencieron y no se descartaron.
- En móvil: selector de día + lista. En escritorio: mes.

No calcula cuota ni mora. No sustituye tareas de crédito.

## ACL

| Ruta | Quién |
|------|--------|
| `/comunicados`, `/agenda` | Cualquier sesión (como simulador) |
| `/admin/comunicados` | Siempre permitida (como comisiones); publica ADMIN o ENCARGADO |
| Publicar / borrar comunicado | ADMIN o ENCARGADO |
| Evento de sede | ADMIN o ENCARGADO |
| Evento personal | El dueño |
| RED | Solo ADMIN |

## Chrome móvil

- Oficina solo en la barra de sede (no se duplica junto al usuario).
- **Cambiar sede** es botón táctil (≥ 40 px).
- Caja en segunda fila.
- Densidad Compacto/Estándar/Holgado solo en escritorio (nunca letras C/E/H).
- Roles del JWT no se muestran en el header del celular.

## Datos

Tablas `MAESTRO.Comunicado*`, `MAESTRO.Agenda*`. Adjuntos en `varbinary` (Azure no tiene disco durable). Contrato: [contrato.md](contrato.md).
