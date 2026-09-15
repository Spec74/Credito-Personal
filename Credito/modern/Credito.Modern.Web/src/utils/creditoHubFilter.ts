import type { CredixHubSection } from '../components/credix/CredixHubGrid'

import { esCreditoPerfilSoloBandeja } from './creditoOperacionPermisos'



const RUTA_BANDEJA_APROBAR = '/credito/aprobar'



/** Filtra tarjetas del hub de crédito para perfil APROBADOR 1 solo bandeja. */

export function filterCreditoHubSections(

  sections: CredixHubSection[],

  roles: string[],

): CredixHubSection[] {

  if (!esCreditoPerfilSoloBandeja(roles)) {

    return sections

  }

  return sections

    .map((section) => ({

      ...section,

      links: section.links.filter((l) => l.to === RUTA_BANDEJA_APROBAR),

    }))

    .filter((s) => s.links.length > 0)

}



export function filterCreditoHomeLinks<T extends { to: string }>(

  links: T[],

  roles: string[],

): T[] {

  if (!esCreditoPerfilSoloBandeja(roles)) {

    return links

  }

  return links.filter((l) => l.to === RUTA_BANDEJA_APROBAR)

}


