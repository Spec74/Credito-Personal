import { createContext, useContext } from 'react'

/** Texto de búsqueda activo en pantallas CredixInformePage. */
export const InformeTableContext = createContext('')

export function useInformeTableSearch(): string {
  return useContext(InformeTableContext)
}
