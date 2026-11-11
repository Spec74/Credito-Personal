# Azure / prod — scripts 2026-11-08

## Batch único (recomendado)

```powershell
sqlcmd -S <server> -d CREDITO -U <user> -P <pass> -I -i deploy\sql\2026-11-08-AZURE-tesoreria-batch.sql
```

Incluye:
- `usp_ResumenCuentaBoveda` — solo medios con saldo ≠ 0; siempre 1 fila
- `usp_ReprogramarCredito` — retorna `NuevoCreditoId` + `Deuda`

## Scripts sueltos (si ya aplicó uno)

| Archivo | Qué hace |
|---------|----------|
| `2026-11-08-resumen-boveda-filtro-cero.sql` | Solo resumen bóveda |
| `2026-11-07-reprogramar-return-ids.sql` | Solo reprogramar IDs |

## App (ya en rama)

- Cierre/conteo: billetes = solo efectivo; digitales tipados al TRE
- PDF movimiento / tickets: etiquetas Yape/Interbank Gerente·René; ticket bóveda/caja con oficina, agente y medio reales — [docs/tickets_comprobantes.md](../../docs/tickets_comprobantes.md)
- Remesa: disponible bóveda vs caja pendiente TRE
- UI 2026-11-09: responsive avisos + dark saldos ([SSD-15](../../docs/ssd/SSD-15-movil-shell.md) / [SSD-19](../../docs/ssd/SSD-19-color-scheme.md))

## Post 2026-11-08 (conteo / céntimos)

```powershell
sqlcmd -S <server> -d CREDITO -U <user> -P <pass> -I -i deploy\sql\2026-11-11-usp-cerrar-cajas-diarios-centimos.sql
```

- Reafirma `ROUND(..., 2)`, rechaza `@Sobrante < 0`, omite TRE en cero.
- App: API valida cierre masivo (409) antes del USP; UI revalida + moneda 5 céntimos.
