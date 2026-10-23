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

## Superficies 2026-10-23 — de “lista básica” a Work OS

La primera entrega cumplía el contrato (publicar / leer / recordatorio) pero **se sentía un formulario**. Revisión contra bandejas reales:

| Superficie | Qué se copió | Fuentes | Qué se recortó |
|------------|--------------|---------|----------------|
| **Campana** | Cajón propio (no menú Ant). Una fila = título + tipo + tiempo relativo. Un punto de no leído. “Marcar todo”. Pie: Ver avisos / Ver agenda. Vacío: “Nada pendiente”. | [Notification center (Courier)](https://www.courier.com/blog/in-app-notification-center-design), [PatternFly drawer](https://www.patternfly.org/components/notification-drawer/design-guidelines/) | Sin swipe, sin toast de cada aviso, sin preferencias por canal. |
| **Bandeja avisos** | Master-detail: lista izquierda + lectura derecha. En móvil, una sola cara y **Volver**. Búsqueda, Todos / Nuevos / Siempre. Punto + título más pesado = no leído (un solo señal). Estado vacío del panel: “Elija un aviso”. | [Master-detail](https://uxpatternsguide.com/patterns/master-detail/), Sketch inbox, Outlook lectura | Sin marcar no leído otra vez, sin pin, sin HTML. |
| **Publicar** | La lista es la pantalla. Redactar / editar abre **drawer** (como usuarios/roles). El encargado no muta RED. | Workplace compose, Credix admin drawers | Sin vista previa HTML. |
| **Agenda** | Escritorio: mini-mes + lista del día + “Después”. Móvil: **tira de 14 días** (Outlook/Todoist) + agenda del día; se saltan los vacíos en “Después”. Tocar evento = editar. Recordatorio visible. | [Outlook Agenda mobile](https://saee.cc/outlook-calendar) (98 % se queda en agenda, no en mes), Google Calendar upcoming | Sin RRULE, sin Day grid, sin invitados. |

## Agenda 2026-10-23 — límite Google Calendar

La lista + mini-mes no es un calendario. Gerencia pidió **el límite de Google Calendar**, usable en PC y celular. Fuentes: [ayuda GCal escritorio](https://support.google.com/calendar/answer/72143), [vistas Android](https://support.google.com/calendar/answer/6110849), [system design week grid](https://completefrontend.com/system-design/problems/google-calendar).

| Pieza Google | En Credix | Recorte consciente |
|--------------|-----------|--------------------|
| Vistas Día / Semana / Mes / Agenda (Schedule) | Igual. En móvil, **3 días** en lugar de Semana (7 columnas de 40 px son ilegibles). | Año. Recurrencia RRULE. Invitados. |
| Grilla de horas + franja *todo el día* | Sí. 00–23, scroll al ahora. Clic en hueco = crear (snap 30 min). Clic en todo el día = evento sin hora. | Sin arrastrar para alargar (arrastre en táctil pelea con el scroll). |
| Línea de ahora (roja, 2007+) | Sí, solo en la columna de hoy; se mueve cada minuto. | — |
| Mini-mes a la izquierda + calendarios con color | Personal `#114885` / Sede ámbar. Se ocultan. Semana empieza **lunes** (Perú). | Sin más calendarios (cumpleaños, feriados). |
| FAB móvil + Crear en PC | FAB 56 px esquina inferior. Drawer con empieza / termina. | Bottom sheet a 1/4 de pantalla: el drawer Credix ya es el patrón del sistema. |
| Duración | `FechaFin` ya estaba en el USP; ahora se envía (1 h por defecto). | Sin zonas horarias. |

Medible: crear un evento desde un hueco de la semana = 1 clic + título + Guardar (≤ 3 acciones). En 360 px la vista por defecto es **Agenda** (lista, como GCal Android).

Medible (constitución §12): abrir un aviso desde la campana ≤ 2 toques; agendar en móvil ≤ 4 campos visibles; leer en 360 px sin scroll horizontal.
