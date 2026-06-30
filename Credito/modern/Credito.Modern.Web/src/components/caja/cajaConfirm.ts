import { Modal, type ModalFuncProps } from 'antd'

const ROOT = 'caja-modal-root'

export function cajaConfirm(options: ModalFuncProps) {
  return Modal.confirm({
    centered: true,
    okText: 'Confirmar',
    cancelText: 'Cancelar',
    ...options,
    rootClassName: [ROOT, 'caja-modal-root--confirm', options.rootClassName]
      .filter(Boolean)
      .join(' '),
  })
}
