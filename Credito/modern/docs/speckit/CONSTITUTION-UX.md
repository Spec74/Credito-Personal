# Constitución — experiencia operativa (Credix)

**Alcance:** principios no negociables para **todo Credix Modern** (SSD-01…12: auth, clientes, crédito, prendario, caja, bóveda, informes, admin, maestros, ventas, almacén).  
**No sustituye** la constitución de negocio del [SSD README](../ssd/README.md) (`usp_*`, strangler, secretos, PDFs Credix).  
**Origen:** gerencia — siguiente nivel de **la interfaz del sistema**, usable años, sin ruido, ventaja competitiva, multisucursal. No es un facelift de un módulo.

Fecha: 2026-10-20 · Spec Kit: `/speckit.constitution` equivalente.

## Principios (no se negocian por pantalla)

1. **Trabajo primero, no catálogo.** Cada rol entra a *qué debo hacer hoy* (cola + una acción primaria). El menú es respaldo, no el producto.
2. **Una decisión por superficie.** Un título, un contexto (oficina / caja / cliente), una acción primaria. El resto es progresivo (avanzado, más datos).
3. **Cero ruido.** Sin párrafos de ayuda permanentes, sin duplicar el mismo aviso, sin badges decorativos, sin “paridad moderno” en la UI. El error aparece **junto al campo** o en un único banner accionable.
4. **Tokens, no tendencias.** Color, espacio, densidad y estados viven en `--credix-*`. No rediseñar por moda (glass, neón, ilustraciones). La marca `#114885` se mantiene.
5. **Densidad configurable, no ilegible.** Compacto / estándar / táctil. El texto no baja del cuerpo; el hit-target mínimo es 24 px (WCAG 2.2 AA 2.5.8); táctil apunta a 40–44 px (`--credix-touch-min`).
6. **El estado no es solo color.** Vencido, CRE, DES, mora: texto + icono + color. Contraste AA.
7. **Oficina es contexto, no filtro escondido.** Toda pantalla operativa muestra la sucursal activa. El JWT (`vendix:oficina_id`) es la verdad; un selector de oficina solo si el usuario tiene varias y el rol lo permite.
8. **Un dato, una vez.** Cliente, bienes, monto, caja: no se re-teclean al pasar de Nuevo → Gestión → Simulador → Caja.
9. **La BD sigue siendo el contrato.** La UX no inventa LTV, cuota, mora ni saldo. Si falta una regla (renovación, remate, hoja resumen), primero se versiona `usp_*` / tabla y luego la pantalla.
10. **Multisucursal = misma plantilla.** Huamanga, Pichari, Huanta, Andahuaylas: mismo flujo. Central ve consolidado; sucursal ve solo lo suyo. Alta de sede = wizard (oficina + bóveda + usuarios + cajas + fondeo), no un checklist oral.
11. **Irreversible se confirma; reversible se deshace.** Desembolso, remate, condonar, transferir bóveda: confirmación con importes. Editar tasación de un renglón: inline.
12. **Medible.** Cada flujo nuevo declara tiempo-a-tarea y errores de validación. Si no baja el tiempo ni el retrabajo, no es “siguiente nivel”.
13. **Español operativo, sin traductor.** La UI es es-PE. `translate="no"` en html/shell/página. Chrome Translate no debe doblar letras ni partir el flex. Shell móvil: el cuerpo pega al título (SSD-15).

## Anti-patrones (rechazar en review)

- Hubs de 12 tarjetas como única home.
- Subtítulos de medio párrafo bajo cada H1.
- Dos alertas que dicen lo mismo (hint + banner + toast).
- `max` oculto en inputs que recorta el valor y esconde el error.
- “Actualizar modelo desde la BD” / regenerar EDMX para un cambio de UI.
- Copiar pixel a pixel el MVC o un dashboard de 2026 que no sobreviva 2028.

## Relación con Spec Kit

Orden para **esta** feature y las que la implementen:

`constitution` (este archivo) → `specify` → `clarify` → `plan` → `checklist` → `tasks` → `analyze` → `implement` → `converge`.
