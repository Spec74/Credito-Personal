# Investigación — canal interno y agenda de sucursal

**Spec Kit:** 002 · 2026-10-21  
**Para:** gerencia Crediconfiable (red Huamanga, Pichari, Huanta, Andahuaylas).  
**No es:** correo, WhatsApp masivo, ni canal hacia clientes o UIF.

## Qué piden las empresas que ya lo tienen

Tres productos distintos conviven en un Work OS. Mezclarlos en un solo “muro” fracasa.

| Superficie | Ejemplos | Qué hace | Qué no hace |
|------------|----------|----------|-------------|
| **Anuncio con caducidad** | Workplace from Meta, Slack `#announcements`, Microsoft Viva / Teams, Factorial, Buk | Aviso corto, autor, sede o red, fecha de fin. La campana marca no leídos. | No sustituye el expediente de crédito. |
| **Biblioteca permanente** | SharePoint / Confluence / BambooHR Files / “Reglamentos” de Buk | Circular, reglamento, PDF o imagen que **no vence**. Quien entra después también lo ve. | No es un chat. Marcar leído no lo borra. |
| **Agenda operativa** | Outlook, Google Calendar, Teams Shifts | Evento personal o de sede, hora o todo el día, recordatorio 15/30/60/1440 min. | No calcula cuota, mora ni caja. |

Patrón de inbox (Slack / Outlook / Workplace):

1. **Campana** = solo lo no leído o el recordatorio que ya venció.
2. **Bandeja** = todos los vigentes, incluidos los permanentes.
3. **Descartar** un recordatorio no borra el evento; solo cierra el aviso en pantalla.

## Decisiones para Credix

| Decisión | Elección | Por qué |
|----------|----------|---------|
| Un módulo o dos | Dos rutas: `/comunicados` y `/agenda` + campana común | El gerente publica normas; el cajero agenda un cobro. Si se mezclan, la bandeja se ensucia. |
| Caducidad | `FechaFin` o NULL (permanente) | El reglamento “siempre presente” es el caso que gerencia nombró. |
| Alcance del aviso | RED (todas las sedes, solo ADMIN) o **una sede elegida** | Workplace/Viva apuntan por ubicación. El gerente en Huamanga puede avisar solo a Pichari. El encargado no elige otra sede. |
| Alcance de agenda | PERSONAL / OFICINA | Igual que un calendario compartido de sucursal, no un Exchange de empresa. |
| Archivos | Imagen ≤ 2 MB, PDF ≤ 8 MB, **varbinary en SQL** | App Service de Azure no tiene disco durable entre instancias. |
| Canal de llegada | Campana en el chrome + bandeja | El personal no abre el correo. WhatsApp queda fuera (no hay contrato ni plantilla). |
| Recordatorio | Minutos antes; la campana lo muestra cuando `FechaIni − minutos ≤ ahora` y no está descartado | Outlook/Google. Sin push nativo ni mail (no hay SMTP de avisos internos). |
| Quién publica | ADMIN o ENCARGADO | Gerente y encargado de sede. El resto solo lee. |

## Qué recortamos a propósito

- Recurrencia semanal tipo Outlook (RRULE): no. Un evento por fecha.
- Invitados por usuario: no. Alcance sede o yo.
- WhatsApp / SMS / correo del aviso: no. Sin plantilla ni costo.
- Editor enriquecido (HTML): no. Texto + imagen + PDF. Menos XSS, más usable en el celular.
- Disco local / blob Azure: no en esta entrega. SQL basta para reglamentos pequeños.

## UX que debe sentirse “de empresa”

- Publicar: título, texto, ¿toda la red o esta sede?, ¿vence o queda?, adjuntar, publicar.
- Leer: lista con “Nuevo”, vigencia o “Siempre visible”, imagen en pantalla, PDF para descargar.
- Agenda escritorio: mes con puntos. Agenda móvil: día elegido + lista (el calendario Ant Design no cabe en 360 px).
- Chrome móvil: sede y **Cambiar sede** táctiles; sin letras C/E/H ni “N roles”.
