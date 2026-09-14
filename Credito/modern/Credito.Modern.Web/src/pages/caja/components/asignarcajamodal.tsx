import { Modal } from 'antd'
import type { AsignarCajaResult } from '../../../api/cajaAsignacion'
import { AsignarCajaForm } from './asignarcajaform'

type Props = {
  open: boolean
  oficinaId: number
  onClose: () => void
  onSuccess: (result: AsignarCajaResult) => void
}

export function AsignarCajaModal({ open, oficinaId, onClose, onSuccess }: Props) {
  return (
    <Modal
      className="caja-asignar-modal"
      title="Asignar caja"
      open={open}
      onCancel={onClose}
      footer={null}
      destroyOnHidden
      width={480}
    >
      {open ? (
        <AsignarCajaForm
          oficinaId={oficinaId}
          submitLabel="Guardar"
          onCancel={onClose}
          onSuccess={onSuccess}
        />
      ) : null}
    </Modal>
  )
}
