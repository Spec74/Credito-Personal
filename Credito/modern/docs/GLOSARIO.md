# Glosario — Credito.Modern

Términos usados en SSD, arquitectura y runbooks. Índice: [DOCUMENTACION.md](DOCUMENTACION.md).

| Término | Significado |
|---------|-------------|
| **SSD** | Spec-Driven Development / especificación as-built del sistema (`docs/ssd/`) |
| **As-built** | Spec redactado **después** de implementar; no finge fecha previa al código |
| **Strangler** | Convivencia MVC + API/SPA detrás de un proxy; el legado se retira por rebanadas |
| **Cutover** | Corte operativo (Corte A API+proxy, Corte B SPA) documentado en SSD-00 |
| **Corte A / Corte B** | Checklist `preprod-cutover-checklist.ps1`: A = API+proxy; B = SPA `/app/` |
| **Menú vivo** | Resultado de `MAESTRO.usp_MenuLst` para la oficina/usuario de sesión |
| **Oficina 1 CREDITO** | Oficina piloto del menú crédito/caja; ventas/almacén pueden no estar en ese menú |
| **`usp_*`** | Procedimiento almacenado SQL; contrato de negocio (el C# no inventa cuotas/mora) |
| **JWT / Bearer** | Access token; claims `vendix:usuario_id`, `vendix:oficina_id`, `vendix:usuario_oficina_id` |
| **`$pbk2$`** | Prefijo de clave hasheada PBKDF2; el legado podía guardar clave en claro |
| **MigracionClavePerezosa** | Al login exitoso en claro, reescribe hash `$pbk2$` (Staging/piloto) |
| **Credix / CrediConfiable** | Marca UI y PDFs tabulares modernos (no ReportViewer) |
| **RDLC** | Informes ReportViewer del MVC; puente opcional vía `VITE_LEGACY_ORIGIN` |
| **ProblemDetails** | Respuesta de error RFC 7807 (`title`, `detail`, status) |
| **formRules** | Reglas Ant Design FE (`Web/src/validation/formRules.ts`) |
| **TesoreriaValidacion** | Validador BE de importes/glosas de caja y bóveda |
| **EXACT_MENU_ROUTES** | Rutas SPA que exigen ítem de menú exacto (no basta el padre del módulo) |
| **Hub** | Pantalla índice (`/caja`, `/credito`, …) que **no** sustituye permisos de ítems hijos |
| **Rebanada** | Módulo o flujo migrado al strangler de forma independiente |
| **Bitácora** | `BITACORA-DESVIACIONES.md` — única fuente de desviaciones financieras aceptadas |
