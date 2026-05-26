import { useEffect, useMemo, useState } from 'react'
import {
  Alert,
  Button,
  Checkbox,
  Form,
  Input,
  Modal,
  Progress,
  Select,
  Space,
  Typography,
} from 'antd'
import {
  DeleteOutlined,
  PlusOutlined,
  UserOutlined,
} from '@ant-design/icons'
import type { TareaDetalle } from '../../api/tareas'
import { TAREAS_SUBTAREAS_PREDETERMINADAS } from '../../config/tareasSubtareasPredeterminadas'
import { formatMoney } from '../../utils/formatMoney'
import { TareasBuscarCredito } from './TareasBuscarCredito'
import type { CreditoTareaBuscar } from '../../api/tareas'

const { Paragraph, Text } = Typography

export interface SubtareaFormRow {
  key: string
  titulo: string
  completada: boolean
}

type Props = {
  open: boolean
  editId: number
  puedeEditar: boolean
  loadingDetalle: boolean
  detalle?: TareaDetalle
  guardando: boolean
  onClose: () => void
  onGuardar: (payload: {
    creditoId: number
    subtareas: SubtareaFormRow[]
  }) => void
}

export function TareaEditorModal({
  open,
  editId,
  puedeEditar,
  loadingDetalle,
  detalle,
  guardando,
  onClose,
  onGuardar,
}: Props) {
  const [creditoId, setCreditoId] = useState<number | null>(null)
  const [creditoLabel, setCreditoLabel] = useState('')
  const [subtareas, setSubtareas] = useState<SubtareaFormRow[]>([
    { key: '1', titulo: '', completada: false },
  ])

  useEffect(() => {
    if (!open) {
      return
    }
    if (editId > 0 && detalle) {
      setCreditoId(detalle.creditoId)
      setCreditoLabel(
        `${detalle.clienteDni} - ${detalle.clienteNombre} (Crédito #${detalle.creditoId} - ${formatMoney(detalle.montoCredito)})`,
      )
      setSubtareas(
        detalle.subtareas.length > 0
          ? detalle.subtareas.map((s, i) => ({
              key: String(s.subtareaId || i),
              titulo: s.titulo,
              completada: s.completada,
            }))
          : [{ key: '1', titulo: '', completada: false }],
      )
    } else if (editId === 0) {
      setCreditoId(null)
      setCreditoLabel('')
      setSubtareas([{ key: '1', titulo: '', completada: false }])
    }
  }, [open, editId, detalle])

  const progresoSub = useMemo(() => {
    const total = subtareas.filter((s) => s.titulo.trim()).length
    const ok = subtareas.filter((s) => s.titulo.trim() && s.completada).length
    return { total, ok, pct: total > 0 ? Math.round((ok / total) * 100) : 0 }
  }, [subtareas])

  const seleccionarCredito = (c: CreditoTareaBuscar) => {
    setCreditoId(c.creditoId)
    setCreditoLabel(c.label)
  }

  const agregarPredeterminada = (titulo: string) => {
    if (!titulo) return
    setSubtareas((p) => [
      ...p,
      { key: String(Date.now()), titulo, completada: false },
    ])
  }

  return (
    <Modal
      className="credito-tareas-modal"
      title={editId > 0 ? `Tarea #${editId}` : 'Nueva tarea'}
      open={open}
      onCancel={onClose}
      width={720}
      destroyOnClose
      footer={
        <Space wrap className="credito-tareas-modal__footer">
          <Button onClick={onClose}>Cancelar</Button>
          {puedeEditar || editId > 0 ? (
            <Button
              type="primary"
              className="credito-tareas-modal__guardar"
              loading={guardando || loadingDetalle}
              onClick={() => {
                if (!creditoId) return
                onGuardar({ creditoId, subtareas })
              }}
              disabled={!creditoId}
            >
              Guardar tarea
            </Button>
          ) : null}
        </Space>
      }
    >
      {loadingDetalle ? (
        <Paragraph type="secondary">Cargando detalle…</Paragraph>
      ) : null}

      {!puedeEditar && editId === 0 ? (
        <Alert
          type="info"
          showIcon
          message="Solo Administrador o Aprobador puede crear tareas."
        />
      ) : (
        <>
          <Form layout="vertical" className="credito-tareas-modal__form">
            <Form.Item label="Cliente (crédito desembolsado)" required>
              {creditoId && creditoLabel ? (
                <div className="credito-tareas-modal__credito-seleccionado">
                  <UserOutlined aria-hidden />
                  <div>
                    <Text strong>{creditoLabel}</Text>
                    {puedeEditar ? (
                      <Button type="link" size="small" onClick={() => setCreditoId(null)}>
                        Cambiar crédito
                      </Button>
                    ) : null}
                  </div>
                </div>
              ) : puedeEditar ? (
                <TareasBuscarCredito onSelect={seleccionarCredito} />
              ) : (
                <Text type="secondary">Sin crédito asignado</Text>
              )}
            </Form.Item>
          </Form>

          {puedeEditar ? (
            <>
              <div className="credito-tareas-modal__sub-head">
                <Text strong>Subtareas</Text>
                <Text type="secondary">
                  {progresoSub.ok}/{progresoSub.total} completadas
                </Text>
              </div>
              <Progress
                percent={progresoSub.pct}
                size="small"
                status={progresoSub.pct === 100 ? 'success' : 'active'}
                className="credito-tareas-modal__progress"
              />

              <Select
                className="credito-tareas-modal__preset"
                placeholder="Añadir subtarea predeterminada…"
                showSearch
                optionFilterProp="label"
                options={TAREAS_SUBTAREAS_PREDETERMINADAS.map((t) => ({
                  value: t,
                  label: t,
                }))}
                onChange={(v) => {
                  if (v) {
                    agregarPredeterminada(v)
                  }
                }}
              />

              <div className="credito-tareas-modal__sub-list">
                {subtareas.map((s, idx) => (
                  <div
                    key={s.key}
                    className={
                      s.completada
                        ? 'credito-tareas-subitem credito-tareas-subitem--done'
                        : 'credito-tareas-subitem'
                    }
                  >
                    <Checkbox
                      checked={s.completada}
                      onChange={(e) => {
                        const next = [...subtareas]
                        next[idx] = { ...next[idx], completada: e.target.checked }
                        setSubtareas(next)
                      }}
                    />
                    <Input
                      placeholder="Descripción de la subtarea"
                      value={s.titulo}
                      onChange={(e) => {
                        const next = [...subtareas]
                        next[idx] = { ...next[idx], titulo: e.target.value }
                        setSubtareas(next)
                      }}
                    />
                    <Button
                      type="text"
                      danger
                      icon={<DeleteOutlined />}
                      disabled={subtareas.length <= 1}
                      aria-label="Quitar subtarea"
                      onClick={() =>
                        setSubtareas((p) => p.filter((x) => x.key !== s.key))
                      }
                    />
                  </div>
                ))}
              </div>

              <Button
                type="dashed"
                block
                icon={<PlusOutlined />}
                onClick={() =>
                  setSubtareas((p) => [
                    ...p,
                    { key: String(Date.now()), titulo: '', completada: false },
                  ])
                }
              >
                Agregar subtarea
              </Button>
            </>
          ) : (
            <Alert
              type="info"
              showIcon
              message="Puede marcar completar desde el listado. La edición requiere rol Administrador o Aprobador."
            />
          )}
        </>
      )}
    </Modal>
  )
}
