/**
 * Etiquetas amigables sobre `MAESTRO.Rol.Denominacion` (código estable en JWT/políticas).
 * Multisucursal: sede vs red se infiere de la oficina de sesión (`IndPrincipal`), no de otro RolId.
 */

export type RoleLabelContext = {
  /** Oficina de sesión / contexto es Central (`IndPrincipal`). */
  esCentral?: boolean
}

type RoleMeta = {
  /** Etiqueta corta por defecto. */
  label: string
  /** Tooltip / descripción operativa. */
  hint: string
  labelWhenCentral?: string
  labelWhenSede?: string
}

const ROLE_META: Record<string, RoleMeta> = {
  ADMINISTRADOR: {
    label: 'Administrador',
    labelWhenCentral: 'Administrador de red',
    labelWhenSede: 'Administrador de sede',
    hint:
      'Seguridad, oficinas, usuarios y overrides. En Central gestiona la red; en sucursal solo su sede. El código en BD sigue siendo ADMINISTRADOR.',
  },
  ADMIN: {
    label: 'Administrador (legacy)',
    hint: 'Alias histórico de ADMINISTRADOR. Preferir ADMINISTRADOR en altas nuevas.',
  },
  ENCARGADO: {
    label: 'Encargado de sede',
    hint: 'Asignar/abrir caja, saldos, verificar pagos y operaciones de sede.',
  },
  ANALISTA: {
    label: 'Analista / gestor de crédito',
    hint: 'Originación, clientes, consulta y prendario.',
  },
  GESTOR: {
    label: 'Gestor (alias)',
    hint: 'Alias operativo de analista/cobranza. Puede no existir como fila en Rol.',
  },
  CAJA: {
    label: 'Cajero',
    hint: 'Operación de caja diario, cobros y tickets. Denominación viva en el catálogo.',
  },
  CAJERO: {
    label: 'Cajero (alias)',
    hint: 'Alias histórico de CAJA. Preferir el rol CAJA.',
  },
  'CAJA CENTRAL': {
    label: 'Cajero de caja central (menú)',
    hint:
      'Rol de menú adicional. La caja física «central» se define en el maestro de cajas (nombre con CAJA CENTRAL), no solo con este rol.',
  },
  'APROBADOR 1': {
    label: 'Aprobador (nivel 1)',
    hint: 'Puede aprobar o rechazar en la bandeja. Escrituras de ciclo de aprobación.',
  },
  'APROBADOR 2': {
    label: 'Aprobador (nivel 2)',
    hint:
      'Consulta / segundo nivel y reportes. No aprueba en bandeja primaria; para aprobar se requiere APROBADOR 1 o ADMINISTRADOR.',
  },
  LECTURA: {
    label: 'Solo lectura (crédito)',
    hint: 'Consulta de crédito sin mutar. Distinto de LECTURA_SALDO y de SALDOS CAJA.',
  },
  LECTURA_SALDO: {
    label: 'Solo lectura (saldos)',
    hint: 'Consulta saldos/cierre sin asignar ni cerrar caja. No es LECTURA de crédito.',
  },
  'SALDOS CAJA': {
    label: 'Menú saldos de caja',
    hint: 'Habilita pantallas de saldos. Suele ir con ENCARGADO; no implica solo-lectura.',
  },
  REPORTE: {
    label: 'Reportes',
    hint: 'Acceso a informes de la sede (y filtros de Central según pantalla).',
  },
  REPORTEPARCIAL: {
    label: 'Reportes parciales',
    hint: 'Subconjunto de informes (PARCIAL).',
  },
  ANULACION_MOV: {
    label: 'Anulación de movimientos',
    hint: 'Capacidad puntual de anular movimiento de caja (junto con administrador).',
  },
}

function metaFor(denominacion: string): RoleMeta | null {
  const key = denominacion.trim().toUpperCase()
  return ROLE_META[key] ?? null
}

/** Etiqueta para combos y grillas. */
export function roleDisplayLabel(
  denominacion: string,
  ctx: RoleLabelContext = {},
): string {
  const meta = metaFor(denominacion)
  if (!meta) return denominacion
  if (ctx.esCentral === true && meta.labelWhenCentral) return meta.labelWhenCentral
  if (ctx.esCentral === false && meta.labelWhenSede) return meta.labelWhenSede
  return meta.label
}

/** Texto de ayuda (tooltip / secondary). */
export function roleDisplayHint(denominacion: string): string | undefined {
  return metaFor(denominacion)?.hint
}

/** Opción Ant Design: label visible + value = denominación o id aparte. */
export function roleSelectLabel(
  denominacion: string,
  ctx: RoleLabelContext = {},
): string {
  const friendly = roleDisplayLabel(denominacion, ctx)
  const code = denominacion.trim().toUpperCase()
  if (friendly.toUpperCase() === code) return denominacion
  return `${friendly} · ${denominacion}`
}
