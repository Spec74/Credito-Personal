/**

 * Paridad roles en `CreditoController.Creditos` (ViewBag.Aprobador1, Administrador, lectura).

 * Denominaciones alineadas con `CreditoAuthorizationPolicies` y claims JWT.

 */



function norm(roles: string[]): string[] {

  return roles.map((r) => r.trim().toUpperCase())

}

function hasRoleLike(roles: string[], expected: string): boolean {
  const exact = expected.trim().toUpperCase()
  return norm(roles).some((role) => {
    const compacto = role.replace(/\s+/g, '')
    const expectedCompacto = exact.replace(/\s+/g, '')
    return role === exact || compacto === expectedCompacto || role.startsWith(`${exact} `)
  })
}



const ROLES_CREDITO_OPERACION_COMPLETA = [

  'ADMINISTRADOR',

  'ADMIN',

  'ENCARGADO',

  'GESTOR',

  'CAJERO',

  'PARCIAL',

] as const



export function tieneCreditoModoLectura(roles: string[]): boolean {

  return norm(roles).includes('LECTURA')

}



export function esCreditoAdministrador(roles: string[]): boolean {

  return hasRoleLike(roles, 'ADMINISTRADOR') || hasRoleLike(roles, 'ADMIN')

}



export function esCreditoAprobador1(roles: string[]): boolean {

  return norm(roles).some((role) => {
    const compacto = role.replace(/\s+/g, '')
    return (
      compacto === 'APROBADOR1' ||
      compacto === 'APROBADOR01' ||
      compacto === 'APRO1' ||
      compacto === 'APRO01' ||
      (role.startsWith('APROBADOR') && /\b0?1\b/.test(role))
    )
  })

}



export function esCreditoEncargado(roles: string[]): boolean {

  return hasRoleLike(roles, 'ENCARGADO')

}



/**

 * APROBADOR 1 sin rol operativo (admin, encargado, gestor, caja): solo bandeja de aprobación en el hub.

 */

export function esCreditoPerfilSoloBandeja(roles: string[]): boolean {

  if (!esCreditoAprobador1(roles)) {

    return false

  }

  const r = norm(roles)

  return !r.some((x) =>

    (ROLES_CREDITO_OPERACION_COMPLETA as readonly string[]).includes(x),

  )

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

  return esCreditoAprobador1(roles) || esCreditoAdministrador(roles)

}



export function puedeProrrogarCreditoUi(roles: string[]): boolean {

  if (tieneCreditoModoLectura(roles)) {

    return false

  }

  return esCreditoAprobador1(roles) || esCreditoAdministrador(roles)

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

  return esCreditoAdministrador(roles) || esCreditoAprobador1(roles)

}



/** Paridad deshabilitar analista cuando ViewBag.Aprobador1 == 0. */

export function puedeCambiarAnalistaCreditoUi(roles: string[]): boolean {

  if (tieneCreditoModoLectura(roles)) {

    return false

  }

  return esCreditoAprobador1(roles) || esCreditoAdministrador(roles)

}



export function puedeEditarTramiteCentralAvalUi(roles: string[]): boolean {

  return puedeCambiarAnalistaCreditoUi(roles) && puedeOperarCreditoCompleto(roles)

}



export function puedeGestionarBovedaEncargadoUi(roles: string[]): boolean {

  return esCreditoEncargado(roles) || esCreditoAdministrador(roles)

}



/** Paridad `btnDepurarCliente` cuando no está depurado. */

export function puedeDepurarClienteCredito(

  roles: string[],

  depurado: boolean,

): boolean {

  if (depurado) {

    return false

  }

  return puedeOperarCreditoCompleto(roles)

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


