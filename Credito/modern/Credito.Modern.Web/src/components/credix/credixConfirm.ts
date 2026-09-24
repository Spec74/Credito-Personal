import { Modal, type ModalFuncProps } from 'antd'

/**
 * Confirmación estándar Credix (Modal.confirm con defaults corporativos).
 */
export function credixConfirm(options: ModalFuncProps) {
  return Modal.confirm({
    centered: true,
    okText: 'Confirmar',
    cancelText: 'Cancelar',
    ...options,
    rootClassName: ['credix-modal', 'credix-modal--confirm', options.rootClassName]
      .filter(Boolean)
      .join(' '),
  })
}
