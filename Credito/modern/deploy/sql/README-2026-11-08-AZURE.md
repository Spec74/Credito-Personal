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
- PDF movimiento: etiquetas Yape/Interbank Gerente·René
- Remesa: disponible bóveda vs caja pendiente TRE
