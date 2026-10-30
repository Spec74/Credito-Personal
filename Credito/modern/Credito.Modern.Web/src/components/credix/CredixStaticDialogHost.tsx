import { App } from 'antd'
import type { HookAPI } from 'antd/es/modal/useModal'

let modalApi: HookAPI | null = null

/** Captura modal de Ant App para que Modal.confirm herede el tema (claro/oscuro). */
export function CredixStaticDialogHost() {
  const { modal } = App.useApp()
  modalApi = modal
  return null
}

export function credixAppModal(): HookAPI | null {
  return modalApi
}
