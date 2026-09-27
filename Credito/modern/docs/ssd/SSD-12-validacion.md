# SSD-12 — Validación FE/BE transversal

**Estado:** as-built · 2026-09-27  
**Código:** `Credito.Modern.Application/Validation/*` · `Credito.Modern.Api/Validation/ProblemResults.cs` · `Credito.Modern.Web/src/validation/formRules.ts`  
**Fuente legacy:** el MVC no tenía capa uniforme; el moderno **exige** validación durable aunque el legado omitiera controles  
**Doc de ingeniería:** este spec + [ARCHITECTURE.md](../ARCHITECTURE.md) §5

## 1. Propósito y actores

Garantizar que escrituras rechacen datos inválidos **antes** de tocar SQL, con mensajes claros en español y la misma filosofía en UI y API.

| Rol | Uso |
|-----|-----|
| Desarrollo | Añadir reglas en `*Validacion` / `formRules`; no validar solo en el endpoint con `if` sueltos |
| QA / negocio | Criterios: campo vacío, monto ≤ 0, texto demasiado largo → 400 / error de formulario |
| Informática | Auditoría de calidad 2026 (no depende de “el legado tampoco validaba”) |

## 2. Alcance

**Entra**

- Reglas atómicas: `IdRules`, `StringRules`, `MoneyRules`, `DateRules`, `ValidationGate`
- Validadores de módulo: `ClienteValidacion`, `UsuarioValidacion`, `MaestroValidacion`, `TareaValidacion`, `TesoreriaValidacion`, `CreditoProductoValidacion`, `PrendarioValidacion` (si aplica)
- Edge API: `ProblemResults.IfInvalid` / `FromArgument` / `Conflict`
- SPA: `formRules.ts` (`moneyRequired`, `glosaRules`, `denominacionRules`, `FIELD_MAX`, documentos PE)

**No entra**

- Reglas de negocio de cuotas/mora/saldos (siguen en `usp_*`)
- Autorización JWT / roles (SSD-01 y guards)
- Desviaciones financieras (bitácora)

## 3. Paridad MVC

| Legacy | Moderno | Observación |
|--------|---------|-------------|
| Validaciones dispersas o ausentes en BL/UI | Capa compartida Application + formRules | **Mejora intencional** respecto al legado |
| Mensajes genéricos / swallow | ProblemDetails con `detail` usable | Convención: no ocultar `ArgumentException` |

## 4. Contrato de datos

No introduce `usp_*`. Limita lo que llega a los SP:

| Regla | Límite típico |
|-------|----------------|
| Denominación | máx. 100 |
| Glosa / descripción operativa | máx. 250 |
| Observación | máx. 500 |
| Importe | > 0, ≤ 99 999 999.99, ≤ 2 decimales |
| IDs | entero ≥ 1 |
| DNI / RUC / celular PE | 8 / 11 / 9 dígitos (9…) |

## 5. API y SPA

| Superficie | Validación |
|------------|------------|
| Guardar cliente / usuario / maestros | `ClienteValidacion` / `UsuarioValidacion` / `MaestroValidacion` |
| Bóveda (movimientos, bancos, inter-oficina, temporal) | `TesoreriaValidacion` |
| Caja (E/S, transferir saldos, chica, rendición, guardar caja) | `TesoreriaValidacion` + guards |
| Pagar cuotas (`importeRecibido`) | `MoneyRules.RequirePositive` |
| Cargo / observar crédito | `CreditoProductoValidacion` |
| Guardar artículo | `CreditoProductoValidacion.ValidarGuardarArticulo` |
| SPA caja / bóveda / maestros / admin / ventas | `formRules` en `Form.Item` |

## 6. Seguridad

La validación **no** sustituye:

- Match JWT oficina ↔ body
- Roles de escritura
- Scope crédito/caja de la oficina

Un request inválido → **400** antes de guards de negocio cuando aplica; un request sin permiso → **401/403**.

## 7. Criterios de aceptación

- [x] Existe `Application/Validation` con gate + money/string/id.
- [x] Escrituras de bóveda/caja/transferencias usan `TesoreriaValidacion` o equivalente (no solo `importe <= 0`).
- [x] Glosas obligatorias respetan `MaxGlosa` (250).
- [x] SPA de bóveda, caja E/S, transferir saldos, caja chica, artículos usan `formRules`.
- [x] `ProblemResults` reenvía mensajes de `ArgumentException` al cliente.
- [x] Tests unitarios FE: `formRules.test.ts`.
- [ ] Smoke preprod: intento de guardar glosa > 250 y monto 0 → rechazo UI y, si se bypasea UI, 400 API.

## 8. Desviaciones

Ninguna financiera. Mejora de calidad respecto al MVC documentada aquí (no en bitácora de cifras).

## 9. Pruebas y evidencia

- FE: `Credito.Modern.Web/src/validation/formRules.test.ts`
- BE: build `Credito.Modern.Api`; endpoints de pago/tesorería/artículos ejercitan las reglas en tests existentes de operación
- Commits: `6899450`, `02bfd0a`, `c1a08ed`, `dd9fb51` (rama `Eber-migracion`)

## 10. Go-live

**Listo en código (Development).** Incluir en humo de preprod un caso negativo de validación (monto 0 / glosa vacía) por módulo caja y bóveda.
