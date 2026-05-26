import { useMemo } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useMutation, useQuery } from '@tanstack/react-query'
import {
  Alert,
  Button,
  Form,
  InputNumber,
  Select,
  Typography,
  message,
} from 'antd'
import {
  asignarCaja,
  fetchCajasParaAsignar,
  fetchMontoBovedaAsignacion,
} from '../../api/cajaAsignacion'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { CredixPage, CredixPanel, type CredixStatItem } from '../../components/credix'
import { formatMoney } from '../../utils/formatMoney'

const { Paragraph } = Typography

function errMsg(err: unknown): string {
  return err instanceof ApiError ? err.message : 'Error en la operación'
}

export function AsignarCajaPage() {
  const navigate = useNavigate()
  const { session } = useAuth()
  const oficinaId = session?.oficinaId ?? 0
  const [form] = Form.useForm<{ cajaId: number; saldoInicial: number }>()

  const cajasQuery = useQuery({
    queryKey: ['cajas-para-asignar', oficinaId],
    queryFn: () => fetchCajasParaAsignar(oficinaId),
    enabled: oficinaId > 0,
  })

  const bovedaQuery = useQuery({
    queryKey: ['monto-boveda-asignacion', oficinaId],
    queryFn: () => fetchMontoBovedaAsignacion(oficinaId),
    enabled: oficinaId > 0,
  })

  const asignar = useMutation({
    mutationFn: (values: { cajaId: number; saldoInicial: number }) =>
      asignarCaja({
        oficinaId,
        cajaId: values.cajaId,
        saldoInicial: values.saldoInicial,
      }),
    onSuccess: (r) => {
      message.success(
        r.esCajaChica
          ? 'Caja chica asignada'
          : `Caja diario abierta (ID ${r.cajaDiarioId ?? '—'})`,
      )
      navigate('/caja/diario')
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const cajasDisponibles = cajasQuery.data ?? []
  const stats = useMemo((): CredixStatItem[] => [
    { value: oficinaId > 0 ? oficinaId : '—', label: 'Oficina sesión' },
    { value: cajasDisponibles.length, label: 'Cajas disponibles' },
    {
      value: bovedaQuery.data != null ? formatMoney(bovedaQuery.data.monto) : '—',
      label: 'Saldo bóveda ref.',
    },
  ], [oficinaId, cajasDisponibles.length, bovedaQuery.data])

  return (
    <CredixPage
      title="Asignar caja diario"
      subtitle="Abre la caja para el usuario actual antes de cobrar en caja diario."
      stats={stats}
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: <Link to="/caja">Caja</Link> },
        { title: 'Asignar caja' },
      ]}
    >
      {oficinaId < 1 && (
        <Alert type="warning" message="Sesión sin oficina activa." showIcon />
      )}

      {bovedaQuery.data != null && (
        <Alert
          type="info"
          showIcon
          style={{ marginBottom: 16 }}
          message={`Saldo referencial en bóveda: ${formatMoney(bovedaQuery.data.monto)}`}
        />
      )}

      <CredixPanel title="Datos de apertura">
        <Form
          form={form}
          layout="vertical"
          initialValues={{ saldoInicial: 0 }}
          onFinish={(v) => asignar.mutate(v)}
          style={{ maxWidth: 480 }}
        >
          <Form.Item
            name="cajaId"
            label="Caja"
            rules={[{ required: true, message: 'Seleccione una caja' }]}
          >
            <Select
              loading={cajasQuery.isLoading}
              placeholder="Caja disponible (cerrada)"
              options={cajasDisponibles.map((c) => ({
                value: c.cajaId,
                label: c.denominacion,
              }))}
              showSearch
              optionFilterProp="label"
            />
          </Form.Item>
          <Form.Item
            name="saldoInicial"
            label="Saldo inicial"
            rules={[{ required: true, type: 'number', min: 0 }]}
          >
            <InputNumber style={{ width: '100%' }} min={0} step={0.01} />
          </Form.Item>
          <Form.Item style={{ marginBottom: 0 }}>
            <Button
              type="primary"
              htmlType="submit"
              loading={asignar.isPending}
              block
            >
              Asignar y abrir caja
            </Button>
          </Form.Item>
        </Form>
      </CredixPanel>

      <Paragraph type="secondary" style={{ marginTop: 16 }}>
        Consulta histórica, caja chica y transferencias en{' '}
        <Link to="/caja/saldos">Saldos y cierres</Link>.
      </Paragraph>
    </CredixPage>
  )
}
