/**
 * Paridad roles en `CreditoController.Creditos` (ViewBag.Aprobador1, Administrador, lectura).
 * Denominaciones alineadas con `CreditoAuthorizationPolicies` y claims JWT.
 */

function norm(roles: string[]): string[] {
  return roles.map((r) => r.trim().toUpperCase())
}

function compact(role: string): string {
  return role.trim().toUpperCase().replace(/\s+/g, '')
}

function hasRoleLike(roles: string[], expected: string): boolean {
  const exact = expected.trim().toUpperCase()
  const expectedCompacto = exact.replace(/\s+/g, '')
  return norm(roles).some((role) => {
    const compacto = role.replace(/\s+/g, '')
    return role === exact || compacto === expectedCompacto || role.startsWith(`${exact} `)
  })
}

export function tieneCreditoModoLectura(roles: string[]): boolean {
  return norm(roles).some((r) => compact(r) === 'LECTURA')
}

export function esCreditoAdministrador(roles: string[]): boolean {
  return hasRoleLike(roles, 'ADMINISTRADOR') || hasRoleLike(roles, 'ADMIN')
}

export function esCreditoAnalista(roles: string[]): boolean {
  return hasRoleLike(roles, 'ANALISTA')
}

/** Cajero: denominación viva CAJA, alias CAJERO, menú CAJA CENTRAL. */
export function esRolCaja(roles: string[]): boolean {
  return (
    hasRoleLike(roles, 'CAJA') ||
    hasRoleLike(roles, 'CAJERO') ||
    hasRoleLike(roles, 'CAJA CENTRAL')
  )
}

export function esCreditoGestor(roles: string[]): boolean {
  return hasRoleLike(roles, 'GESTOR')
}

/** Paridad API CreditoRolOperador. */
export function esCreditoOperador(roles: string[]): boolean {
  return (
    esCreditoAdministrador(roles) ||
    esCreditoEncargado(roles) ||
    esCreditoAnalista(roles) ||
    esCreditoGestor(roles) ||
    esRolCaja(roles)
  )
}

/** Inicio operativo: el analista ve su tablero; el admin conserva el hub de accesos. */
export function debeMostrarDashboardAnalista(roles: string[], vista?: string | null): boolean {
  if (!esCreditoAnalista(roles)) {
    return false
  }
  if (esCreditoAdministrador(roles)) {
    return vista === 'analista'
  }
  return true
}

/** Inicio gerencial: el administrador ve el tablero de oficina; ?vista=modulos abre el hub. */
export function debeMostrarDashboardAdmin(roles: string[], vista?: string | null): boolean {
  if (!esCreditoAdministrador(roles)) {
    return false
  }
  return vista !== 'modulos' && vista !== 'analista'
}

/**
 * APROBADOR unificado (incluye legacy APROBADOR 1 / 2 y alias cortos).
 * Los pasos PEN→AP1→APR son del ciclo del crédito, no niveles de rol.
 */
export function esCreditoAprobador(roles: string[]): boolean {
  return norm(roles).some((role) => {
    const compacto = role.replace(/\s+/g, '')
    return (
      compacto === 'APROBADOR' ||
      compacto === 'APROBADOR1' ||
      compacto === 'APROBADOR01' ||
      compacto === 'APRO1' ||
      compacto === 'APRO01' ||
      compacto === 'APROBADOR2' ||
      compacto === 'APROBADOR02' ||
      compacto === 'APRO2' ||
      compacto === 'APRO02'
    )
  })
}

/** @deprecated Usar {@link esCreditoAprobador}. */
export function esCreditoAprobador1(roles: string[]): boolean {
  return esCreditoAprobador(roles)
}

/** @deprecated Legacy; ya no hay nivel 2 distinto. */
export function esCreditoAprobador2(roles: string[]): boolean {
  return norm(roles).some((role) => {
    const compacto = role.replace(/\s+/g, '')
    return (
      compacto === 'APROBADOR2' ||
      compacto === 'APROBADOR02' ||
      compacto === 'APRO2' ||
      compacto === 'APRO02'
    )
  })
}

/** Gerente de sede (ENCARGADO) u administrador: gestión de sede. */
export function puedeGestionarSedeUi(roles: string[]): boolean {
  return esCreditoEncargado(roles) || esCreditoAdministrador(roles)
}

export function esCreditoEncargado(roles: string[]): boolean {
  return hasRoleLike(roles, 'ENCARGADO')
}

/** Consola de avisos: ADMIN o ENCARGADO (no ANALISTA/CAJA por substring). */
export function puedePublicarAvisos(roles: string[]): boolean {
  return esCreditoAdministrador(roles) || esCreditoEncargado(roles)
}

/** Encargado no muta avisos de toda la red. */
export function puedeMutarAviso(roles: string[], alcance: string): boolean {
  if (!puedePublicarAvisos(roles)) return false
  if (alcance.trim().toUpperCase() === 'RED') return esCreditoAdministrador(roles)
  return true
}

/**
 * APROBADOR sin rol operativo (paridad API `HasRolOperador`): solo bandeja en el hub.
 */
export function esCreditoPerfilSoloBandeja(roles: string[]): boolean {
  if (!esCreditoAprobador(roles)) {
    return false
  }

  return !esCreditoOperador(roles)
}

export function puedeOperarCreditoCompleto(roles: string[]): boolean {
  if (tieneCreditoModoLectura(roles)) {
    return false
  }

  if (esCreditoPerfilSoloBandeja(roles)) {
    return false
  }

  return true
}

/** Escritura de ciclo de crédito en consulta (excluye solo bandeja y lectura). */
export function puedeOperarCicloCredito(roles: string[]): boolean {
  return puedeOperarCreditoCompleto(roles)
}

export function puedeAnularCreditoUi(roles: string[]): boolean {
  if (tieneCreditoModoLectura(roles)) {
    return false
  }

  return esCreditoAprobador(roles) || esCreditoAdministrador(roles)
}

export function puedeProrrogarCreditoUi(roles: string[]): boolean {
  if (tieneCreditoModoLectura(roles)) {
    return false
  }

  return esCreditoAprobador(roles) || esCreditoAdministrador(roles)
}

/** Paridad `btncReprogramar` / `btncCondonar` solo si ViewBag.Administrador > 0. */
export function puedeReprogramarCreditoUi(roles: string[]): boolean {
  if (tieneCreditoModoLectura(roles)) {
    return false
  }

  return esCreditoAdministrador(roles)
}

export function puedeCondonarCreditoUi(roles: string[]): boolean {
  return puedeReprogramarCreditoUi(roles)
}

export function puedeEditarTopeCreditoUi(roles: string[]): boolean {
  if (tieneCreditoModoLectura(roles)) {
    return false
  }

  return esCreditoAdministrador(roles) || esCreditoAprobador(roles)
}

/** Paridad deshabilitar analista cuando ViewBag.Aprobador1 == 0. */
export function puedeCambiarAnalistaCreditoUi(roles: string[]): boolean {
  if (tieneCreditoModoLectura(roles)) {
    return false
  }

  return esCreditoAprobador(roles) || esCreditoAdministrador(roles)
}

export function puedeEditarTramiteCentralAvalUi(roles: string[]): boolean {
  return puedeCambiarAnalistaCreditoUi(roles) && puedeOperarCreditoCompleto(roles)
}

/** API CreditoRolPrendario: analista o administrador pueden registrar/completar bienes. */
export function puedeCompletarBienesPrendarioUi(roles: string[]): boolean {
  if (tieneCreditoModoLectura(roles)) {
    return false
  }
  return esCreditoAnalista(roles) || esCreditoAdministrador(roles)
}

/** Crédito prendario por flag o producto CREDI PRENDARIO (2). */
export function esCreditoProductoPrendario(ctx: {
  esPrendario?: boolean | null
  productoId?: number | null
}): boolean {
  return Boolean(ctx.esPrendario) || ctx.productoId === 2
}

export function puedeGestionarBovedaEncargadoUi(roles: string[]): boolean {
  return esCreditoEncargado(roles) || esCreditoAdministrador(roles)
}

/** Paridad `btnDepurarCliente` cuando no está depurado. */
export function puedeDepurarClienteCredito(roles: string[], depurado: boolean): boolean {
  if (depurado) {
    return false
  }

  return puedeOperarCreditoCompleto(roles)
}

/**
 * Rehabilitar cliente depurado (API `habilitar-depurado`).
 * Paridad ficha Clientes: Aprobador 1 o Administrador.
 */
export function puedeHabilitarDepuradoCliente(roles: string[], depurado: boolean): boolean {
  if (!depurado || tieneCreditoModoLectura(roles)) {
    return false
  }

  return esCreditoAprobador(roles) || esCreditoAdministrador(roles)
}

/** Paridad `btnCrearSolicitud` + `Model.lectura == false`. */
export function puedeCrearSolicitudCreditoUi(
  roles: string[],
  ficha: {
    bloqueado: boolean
    depuradoDescripcion: string | null
    puedeCrearSolicitud: boolean
  },
): boolean {
  if (!puedeOperarCreditoCompleto(roles)) {
    return false
  }

  return ficha.puedeCrearSolicitud
}
