import { Modal, type ModalProps } from 'antd'

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
