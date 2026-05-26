import type {
  AlmacenListItem,
  ArticuloListItem,
  ListaPrecioListItem,
  MarcaListItem,
  ModeloListItem,
  OficinaListItem,
  ProductoListItem,
  TipoArticuloListItem,
} from '../types/api'

function pick<T>(row: Record<string, unknown>, camel: string, pascal: string): T {
  return (row[camel] ?? row[pascal]) as T
}

/** Por si la API devolviera PascalCase en algún entorno. */
export function normalizeOficina(row: Record<string, unknown>): OficinaListItem {
  return {
    oficinaId: Number(pick(row, 'oficinaId', 'OficinaId')),
    denominacion: pick<string | null>(row, 'denominacion', 'Denominacion'),
    indPrincipal: Boolean(pick(row, 'indPrincipal', 'IndPrincipal')),
    estado: Boolean(pick(row, 'estado', 'Estado')),
  }
}

export function normalizeOficinas(rows: unknown): OficinaListItem[] {
  if (!Array.isArray(rows)) {
    return []
  }
  return rows
    .map((r) => normalizeOficina(r as Record<string, unknown>))
    .filter((o) => !Number.isNaN(o.oficinaId) && o.oficinaId > 0)
}

export function normalizeMarca(row: Record<string, unknown>): MarcaListItem {
  return {
    marcaId: Number(pick(row, 'marcaId', 'MarcaId')),
    denominacion: String(pick(row, 'denominacion', 'Denominacion') ?? ''),
    estado: Boolean(pick(row, 'estado', 'Estado')),
  }
}

export function normalizeMarcas(rows: unknown): MarcaListItem[] {
  if (!Array.isArray(rows)) {
    return []
  }
  return rows.map((r) => normalizeMarca(r as Record<string, unknown>))
}

export function normalizeModelo(row: Record<string, unknown>): ModeloListItem {
  return {
    modeloId: Number(pick(row, 'modeloId', 'ModeloId')),
    denominacion: String(pick(row, 'denominacion', 'Denominacion') ?? ''),
    marcaId: pick<number | null>(row, 'marcaId', 'MarcaId') ?? null,
    estado: Boolean(pick(row, 'estado', 'Estado')),
  }
}

export function normalizeModelos(rows: unknown): ModeloListItem[] {
  if (!Array.isArray(rows)) {
    return []
  }
  return rows.map((r) => normalizeModelo(r as Record<string, unknown>))
}

export function normalizeTipoArticulo(
  row: Record<string, unknown>,
): TipoArticuloListItem {
  return {
    tipoArticuloId: Number(pick(row, 'tipoArticuloId', 'TipoArticuloId')),
    denominacion: String(pick(row, 'denominacion', 'Denominacion') ?? ''),
    descripcion: pick<string | null>(row, 'descripcion', 'Descripcion'),
    indTieneCodigo: Boolean(pick(row, 'indTieneCodigo', 'IndTieneCodigo')),
    indMovimientoAlmacen: Boolean(
      pick(row, 'indMovimientoAlmacen', 'IndMovimientoAlmacen'),
    ),
    estado: Boolean(pick(row, 'estado', 'Estado')),
  }
}

export function normalizeTiposArticulo(rows: unknown): TipoArticuloListItem[] {
  if (!Array.isArray(rows)) {
    return []
  }
  return rows
    .map((r) => normalizeTipoArticulo(r as Record<string, unknown>))
    .filter((t) => !Number.isNaN(t.tipoArticuloId) && t.tipoArticuloId > 0)
}

export function normalizeArticulo(row: Record<string, unknown>): ArticuloListItem {
  return {
    articuloId: Number(pick(row, 'articuloId', 'ArticuloId')),
    modeloId: pick<number | null>(row, 'modeloId', 'ModeloId') ?? null,
    tipoArticuloId: pick<number | null>(row, 'tipoArticuloId', 'TipoArticuloId') ?? null,
    codArticulo: String(pick(row, 'codArticulo', 'CodArticulo') ?? ''),
    denominacion: String(pick(row, 'denominacion', 'Denominacion') ?? ''),
    descripcion: pick<string | null>(row, 'descripcion', 'Descripcion'),
    indPerecible: pick<boolean | null>(row, 'indPerecible', 'IndPerecible') ?? null,
    indImportado: pick<boolean | null>(row, 'indImportado', 'IndImportado') ?? null,
    indCanjeable: pick<boolean | null>(row, 'indCanjeable', 'IndCanjeable') ?? null,
    estado: Boolean(pick(row, 'estado', 'Estado')),
  }
}

export function normalizeArticulos(rows: unknown): ArticuloListItem[] {
  if (!Array.isArray(rows)) {
    return []
  }
  return rows
    .map((r) => normalizeArticulo(r as Record<string, unknown>))
    .filter((a) => !Number.isNaN(a.articuloId) && a.articuloId > 0)
}

export function normalizeAlmacen(row: Record<string, unknown>): AlmacenListItem {
  const fecha = pick<string | null>(row, 'fechaApertura', 'FechaApertura')
  return {
    almacenId: Number(pick(row, 'almacenId', 'AlmacenId')),
    oficinaId: pick<number | null>(row, 'oficinaId', 'OficinaId') ?? null,
    denominacion: String(pick(row, 'denominacion', 'Denominacion') ?? ''),
    descripcion: pick<string | null>(row, 'descripcion', 'Descripcion'),
    indEstadoApertura:
      pick<boolean | null>(row, 'indEstadoApertura', 'IndEstadoApertura') ?? null,
    fechaApertura: fecha ?? null,
    estado: Boolean(pick(row, 'estado', 'Estado')),
  }
}

export function normalizeAlmacenes(rows: unknown): AlmacenListItem[] {
  if (!Array.isArray(rows)) {
    return []
  }
  return rows
    .map((r) => normalizeAlmacen(r as Record<string, unknown>))
    .filter((a) => !Number.isNaN(a.almacenId) && a.almacenId > 0)
}

export function normalizeListaPrecio(
  row: Record<string, unknown>,
): ListaPrecioListItem {
  return {
    listaPrecioId: Number(pick(row, 'listaPrecioId', 'ListaPrecioId')),
    articuloId: pick<number | null>(row, 'articuloId', 'ArticuloId') ?? null,
    monto: pick<number | null>(row, 'monto', 'Monto') ?? null,
    descuento: pick<number | null>(row, 'descuento', 'Descuento') ?? null,
    estado: Boolean(pick(row, 'estado', 'Estado')),
    puntos: pick<number | null>(row, 'puntos', 'Puntos') ?? null,
    puntosCanje: pick<number | null>(row, 'puntosCanje', 'PuntosCanje') ?? null,
  }
}

export function normalizeListaPrecios(rows: unknown): ListaPrecioListItem[] {
  if (!Array.isArray(rows)) {
    return []
  }
  return rows
    .map((r) => normalizeListaPrecio(r as Record<string, unknown>))
    .filter((p) => !Number.isNaN(p.listaPrecioId) && p.listaPrecioId > 0)
}

export function normalizeProducto(row: Record<string, unknown>): ProductoListItem {
  return {
    productoId: Number(pick(row, 'productoId', 'ProductoId')),
    denominacion: String(pick(row, 'denominacion', 'Denominacion') ?? ''),
    interesMinima: Number(pick(row, 'interesMinima', 'InteresMinima') ?? 0),
    interesMaxima: Number(pick(row, 'interesMaxima', 'InteresMaxima') ?? 0),
    diasGracia: Number(pick(row, 'diasGracia', 'DiasGracia') ?? 0),
    importeMoratorio: Number(pick(row, 'importeMoratorio', 'ImporteMoratorio') ?? 0),
    estado: Boolean(pick(row, 'estado', 'Estado')),
    indMora: Boolean(pick(row, 'indMora', 'IndMora')),
  }
}

export function normalizeProductos(rows: unknown): ProductoListItem[] {
  if (!Array.isArray(rows)) {
    return []
  }
  return rows
    .map((r) => normalizeProducto(r as Record<string, unknown>))
    .filter((p) => !Number.isNaN(p.productoId) && p.productoId > 0)
}

