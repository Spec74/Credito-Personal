# Contrato PLA / UIF — T-C7

**Fecha:** 2026-10-21  
**Regla:** no se inventa un monto en soles “de gerencia”. El piso es la norma.

La empresa de préstamos y empeño es **sujeto obligado** (Ley N° 29038 art. 3).  
Inscripción en el Registro SBS: Resolución SBS N° 650-2024 (reemplaza 6338-2012 para el registro).  
Operación PLA: Reglamento de la Ley N° 27693, D.S. N° 020-2017-JUS.

## Dos obligaciones distintas

| Pieza | ¿Hay umbral? | Qué es | Qué hace el sistema | Qué no hace |
|-------|--------------|--------|---------------------|-------------|
| **RO** — Registro de operaciones | Sí, en **dólares** | Anotar la operación el mismo día. Conservar 10 años. Enviarlo a SBS/UIF en el formato que ellos pidan. Entrar al RO **no** significa lavado. | Detecta candidatos y guarda rastro. | No inventa S/. No sustituye el envío oficial. |
| **ROS** — Reporte de operaciones sospechosas | **No** | Comunicar a UIF-Perú vía **oficial de cumplimiento** (ROSEL), en ≤ 24 h de calificada, **sin importar el monto**. | Bitácora interna: inusual / sospechosa / descartada + motivo. | No envía el ROS. Eso es el oficial en ROSEL. |

## Pisos del RO (art. 24.4 del Reglamento)

En defecto de norma sectorial más estricta (la prepublicación de empeño con US$ 2 500 **no** se usa: no está aprobada como contrato):

1. Operación **individual** ≥ **US$ 10 000** (o equivalente).  
2. Operaciones **múltiples** de la misma persona, en **todas** las sedes, en un **mes calendario** ≥ **US$ 50 000** (cuentan como una).  
3. Tipo de cambio: **promedio de venta SBS del mes anterior** a la operación.  
4. Gerencia / oficial **puede bajar** el umbral por riesgo (24.4.c). **No puede subirlo** para saltarse el RO.

Equivalente en soles = `umbral_usd × TcVentaMesAnterior`. Sin ese TC no se clasifica “supera / no supera”.

## Qué hay en BD

| Objeto | Rol |
|--------|-----|
| `CREDITO.PlaParametro` | Una fila. Pisos legales fijos + TC mensual + umbrales menores opcionales. |
| `CREDITO.usp_PlaObtenerParametro` / `usp_PlaGuardarParametro` | Leer / guardar TC y umbral menor. Rechaza umbral menor > piso legal. |
| `CREDITO.usp_PlaCandidatosRo` | Desembolsos y cobranzas `CUO` de la sede (JWT). Marca si supera el piso **cuando hay TC**. |
| `CREDITO.PlaInusual` + `usp_PlaRegistrarInusual` / `usp_PlaListarInusual` | Constancia del análisis (art. 25). No es el ROS enviado. |

## Qué sigue siendo de gerencia / oficial (no del código)

- Designar oficial de cumplimiento y acreditarlo ante UIF.  
- Manual PLA y señales de alerta internas.  
- Cargar cada mes el TC SBS (promedio venta del mes anterior).  
- Si quieren un piso **más bajo** que US$ 10 000 / 50 000, el oficial lo carga.  
- Enviar RO y ROS por los canales SBS (ROSEL).

## KYC que ya existe (no se inventa ficha)

`MAESTRO.Persona` (DNI, domicilio, estado civil, cónyuge) + `MAESTRO.Cliente` (ocupación). El candidato RO señala si faltan documento, dirección u ocupación.
