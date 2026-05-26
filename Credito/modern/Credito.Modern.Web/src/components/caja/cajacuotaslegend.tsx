/** Leyenda de colores de cuotas (paridad visual MVC). */
export function CajaCuotasLegend() {
  return (
    <ul className="caja-cuotas-legend" aria-label="Leyenda de estados de cuota">
      <li>
        <span className="caja-cuotas-legend__swatch caja-cuota-row--pendiente" />
        Pendiente (cobrable)
      </li>
      <li>
        <span className="caja-cuotas-legend__swatch caja-cuota-row--mora" />
        Con mora / atraso (cobrable)
      </li>
      <li>
        <span className="caja-cuotas-legend__swatch caja-cuota-row--pagada" />
        Pagada (no seleccionable)
      </li>
      <li>
        <span className="caja-cuotas-legend__swatch caja-cuota-row--creada" />
        Creada (no seleccionable)
      </li>
      <li>
        <span className="caja-cuotas-legend__swatch caja-cuota-row--resumen" />
        Totales
      </li>
    </ul>
  )
}
