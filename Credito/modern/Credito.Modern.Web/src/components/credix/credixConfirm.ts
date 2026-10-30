import { Modal, type ModalFuncProps } from 'antd'
import { credixAppModal } from './CredixStaticDialogHost'

const WRAP = 'credix-modal credix-modal--confirm'

/**
 * Confirmación estándar Credix. Usa Ant App para heredar claro/oscuro.
 */
export function credixConfirm(options: ModalFuncProps) {
  const api = credixAppModal()
  const confirm = api ? api.confirm.bind(api) : Modal.confirm.bind(Modal)
  return confirm({
    centered: true,
    okText: 'Confirmar',
    cancelText: 'Cancelar',
    ...options,
    wrapClassName: [WRAP, options.wrapClassName].filter(Boolean).join(' '),
    rootClassName: [WRAP, options.rootClassName].filter(Boolean).join(' '),
  })
}
