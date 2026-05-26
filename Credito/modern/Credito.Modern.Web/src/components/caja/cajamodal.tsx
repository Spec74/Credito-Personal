import { Modal, type ModalFuncProps, type ModalProps } from 'antd'

const ROOT = 'caja-modal-root'

export function CajaModal({ rootClassName, centered = true, ...props }: ModalProps) {
  return (
    <Modal
      centered={centered}
      destroyOnClose
      {...props}
      rootClassName={[ROOT, rootClassName].filter(Boolean).join(' ')}
    />
  )
}

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
