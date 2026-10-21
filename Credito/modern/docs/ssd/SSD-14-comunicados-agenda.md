# SSD-14 — Comunicados y agenda

**Estado:** as-built · 2026-10-21  
**Código:** `/comunicados`, `/agenda`, `/admin/comunicados` · `/api/v1/comunicados/*` · `/api/v1/agenda/*` · `/api/v1/bandeja`  
**Doc:** [spec 002](../speckit/002-comunicados-agenda/spec.md) · [research](../speckit/002-comunicados-agenda/research.md) · [contrato](../speckit/002-comunicados-agenda/contrato.md)

## 1. Propósito y actores

Canal interno del gerente (avisos, reglamentos con PDF/imagen, vigencia o permanente) y agenda con recordatorio. Campana en el chrome. El encargado publica solo a su sede.

## 2. Alcance

**Entra:** publicar (admin/encargado), bandeja de todos, adjuntos SQL, eventos personales/sede, recordatorio en campana, chrome móvil de sede.  
**No entra:** WhatsApp masivo, TEA, ROSEL, recalcular mora, correo, recurrencia RRULE.

## 3. Paridad MVC

No existe en el legado. Módulo nuevo (intranet Credix).

## 4. Contrato de datos

Tablas `MAESTRO.Comunicado`, `ComunicadoAdjunto`, `ComunicadoLectura`, `AgendaEvento`, `AgendaDescarte`.  
USP: `usp_Comunicado*`, `usp_Agenda*`, `usp_BandejaNotificaciones`.  
Script: `deploy/sql/2026-10-21-comunicados-agenda.sql` (aplicar también en Azure).

## 5. API y SPA

| API | SPA |
|-----|-----|
| GET/POST `/api/v1/comunicados` | `/comunicados` lectura · `/admin/comunicados` publicación |
| GET adjunto (JWT) | Imagen inline (blob) · PDF descarga |
| GET/POST `/api/v1/agenda` | `/agenda` mes (desktop) / lista del día (móvil) |
| GET `/api/v1/bandeja` | `CredixNotifyBell` en el header |

## 6. Seguridad

JWT con oficina y usuario. RED solo ADMIN. Adjunto no se sirve sin token. No toca crédito ni caja.

## 7. Criterios

- [ ] Aviso con fecha fin desaparece de vigentes al vencer.
- [ ] Aviso sin fecha fin sigue en bandeja.
- [ ] PDF e imagen se descargan; la imagen se ve en la ficha.
- [ ] Encargado no publica a toda la red (solo su sede) salvo ADMIN.
- [ ] Admin elige sede destino o todas las sedes.
- [ ] Editar cambia texto/vigencia/destino; eliminar lo retira de campana y bandeja.
- [ ] Evento personal no lo ve otro usuario.
- [ ] Chrome móvil: sede + Cambiar sede táctil; sin letras C/E/H ni “N roles”.

## 8. Desviaciones

[BITACORA](../migration/BITACORA-DESVIACIONES.md) — sin WhatsApp ni correo del aviso.

## 9. Pruebas

`IntranetEndpointTests` (401) · `menuRouteAccess` rutas always-allowed.

## 10. Go-live

Código en `Eber-migracion`. Falta aplicar el SQL en Azure (igual que PLA).
