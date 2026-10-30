import { Modal } from 'antd'
import type { ComponentProps, ReactNode } from 'react'

type ModalProps = ComponentProps<typeof Modal>

/**
 * Modal estándar Credix: centrado, responsive y con tipografía corporativa.
 * Para `Modal.confirm` usar `confirmMoney` / `credixConfirm` (Ant App + tema).
 */
export function CredixModal({
  children,
  className,
  okText = 'Aceptar',
  cancelText = 'Cancelar',
  centered = true,
  destroyOnHidden = true,
  maskClosable = false,
  ...props
}: ModalProps & { children?: ReactNode }) {
  return (
    <Modal
      centered={centered}
      destroyOnHidden={destroyOnHidden}
      maskClosable={maskClosable}
      okText={okText}
      cancelText={cancelText}
      className={['credix-modal', className].filter(Boolean).join(' ')}
      {...props}
    >
      {children}
    </Modal>
  )
}
