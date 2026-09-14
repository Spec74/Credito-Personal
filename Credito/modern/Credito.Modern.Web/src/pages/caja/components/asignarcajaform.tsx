import { useEffect, useRef, useState } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Alert, Button, Form, InputNumber, Select, Space, Typography, message } from 'antd'
import {
  asignarCaja,
  fetchCajasParaAsignar,
  fetchMontoBovedaAsignacion,
  type AsignarCajaResult,
  type CajaParaAsignarRow,
} from '../../../api/cajaAsignacion'
import { fetchCajaGestores, guardarCaja } from '../../../api/cajaMaestro'
import { ApiError } from '../../../api/errors'
import { useAuth } from '../../../auth/useAuth'
import { esCreditoAdministrador } from '../../../utils/creditoOperacionPermisos'
import { formatMoney } from '../../../utils/formatMoney'

const { Text } = Typography

type FormValues = { cajaId: number; cajeroId?: number; saldoInicial: number }

type Props = {
  oficinaId: number
  submitLabel?: string
  onCancel?: () => void
  onSuccess: (result: AsignarCajaResult) => void
  onError?: (message: string) => void
}

function cajeroLabel(c: CajaParaAsignarRow): string {
  if (c.cajeroNombre) {
    return c.cajeroNombre
  }
  if (c.cajeroId && c.cajeroId > 0) {
    return `Usuario #${c.cajeroId}`
  }
  return 'Sin cajero asignado'
}

function cajaOptionLabel(c: CajaParaAsignarRow): string {
  return `${c.denominacion} — ${cajeroLabel(c)}`
}

function cajaTieneCajero(c: CajaParaAsignarRow): boolean {
  return (c.cajeroId ?? 0) > 0
}

function invalidateAfterAsignarCaja(
  queryClient: ReturnType<typeof useQueryClient>,
): void {
  void queryClient.invalidateQueries({ queryKey: ['cajas-asignadas'] })
  void queryClient.invalidateQueries({ queryKey: ['saldos-caja-diario'] })
  void queryClient.invalidateQueries({ queryKey: ['saldos-caja-chica-diario'] })
  void queryClient.invalidateQueries({ queryKey: ['saldos-caja-diario-boveda'] })
  void queryClient.invalidateQueries({ queryKey: ['cajas-para-asignar'] })
  void queryClient.invalidateQueries({ queryKey: ['monto-boveda-asignacion'] })
  void queryClient.invalidateQueries({ queryKey: ['boveda-abierta'] })
  void queryClient.invalidateQueries({ queryKey: ['validar-cierre-saldos'] })
  void queryClient.invalidateQueries({ queryKey: ['validar-cierre-caja-chica'] })
  void queryClient.invalidateQueries({ queryKey: ['caja-diario-sesion'] })
  void queryClient.invalidateQueries({ queryKey: ['caja-chica-sesion'] })
}

export function AsignarCajaForm({
  oficinaId,
  submitLabel = 'Guardar',
  onCancel,
  onSuccess,
  onError,
}: Props) {
  const queryClient = useQueryClient()
  const { session } = useAuth()
  const esAdmin = esCreditoAdministrador(session?.roles ?? [])
  const [form] = Form.useForm<FormValues>()
  const [guardando, setGuardando] = useState(false)

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

  const gestoresQuery = useQuery({
    queryKey: ['caja-gestores'],
    queryFn: fetchCajaGestores,
    enabled: esAdmin,
  })

  const cajaIdSeleccionada = Form.useWatch('cajaId', form)
  const cajas = cajasQuery.data ?? []
  const montoBoveda = bovedaQuery.data?.monto
  const cajaSeleccionada = cajas.find((c) => c.cajaId === cajaIdSeleccionada)
  // El admin puede corregir el cajero del maestro sin salir del modal; los demás perfiles
  // solo pueden abrir cajas que ya lo tengan configurado.
  const hayCajasAsignables = esAdmin
    ? cajas.length > 0
    : cajas.some(cajaTieneCajero)

  // Propone el cajero del maestro al elegir la caja, sin pisar lo que el admin ya editó.
  const cajaConCajeroPropuesto = useRef<number | null>(null)
  useEffect(() => {
    if (cajaIdSeleccionada == null || cajaSeleccionada == null) {
      cajaConCajeroPropuesto.current = null
      return
    }
    if (cajaConCajeroPropuesto.current === cajaIdSeleccionada) {
      return
    }
    cajaConCajeroPropuesto.current = cajaIdSeleccionada
    form.setFieldValue('cajeroId', cajaSeleccionada.cajeroId ?? undefined)
  }, [cajaIdSeleccionada, cajaSeleccionada, form])

  const handleFinish = async (values: FormValues) => {
    const caja = cajas.find((c) => c.cajaId === values.cajaId)
    setGuardando(true)
    try {
      if (esAdmin && caja && (values.cajeroId ?? 0) !== (caja.cajeroId ?? 0)) {
        const guardado = await guardarCaja({
          cajaId: caja.cajaId,
          oficinaId,
          denominacion: caja.denominacion,
          cajeroId: values.cajeroId ?? null,
          estado: true,
        })
        if (!guardado.success) {
          throw new ApiError(
            guardado.mensaje ?? 'No se pudo actualizar el cajero de la caja.',
            400,
          )
        }
        void queryClient.invalidateQueries({ queryKey: ['cajas-gestion'] })
      }

      const result = await asignarCaja({
        oficinaId,
        cajaId: values.cajaId,
        saldoInicial: values.saldoInicial,
      })
      invalidateAfterAsignarCaja(queryClient)
      form.resetFields()
      onSuccess(result)
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'No se pudo asignar la caja.'
      if (onError) {
        onError(msg)
      } else {
        message.error(msg)
      }
    } finally {
      setGuardando(false)
    }
  }

  if (oficinaId < 1) {
    return <Alert type="warning" showIcon message="Sesión sin oficina activa." />
  }

  return (
    <>
      {cajasQuery.isError ? (
        <Alert
          type="error"
          showIcon
          style={{ marginBottom: 12 }}
          message={
            cajasQuery.error instanceof ApiError
              ? cajasQuery.error.message
              : 'No se pudieron cargar las cajas disponibles.'
          }
        />
      ) : null}

      {bovedaQuery.isError ? (
        <Alert
          type="warning"
          showIcon
          style={{ marginBottom: 12 }}
          message="No se pudo leer el saldo de bóveda. El servidor validará el importe al guardar."
        />
      ) : (
        <Alert
          type="info"
          showIcon
          style={{ marginBottom: 12 }}
          message={
            montoBoveda == null
              ? 'Consultando saldo de bóveda…'
              : `Saldo en bóveda: ${formatMoney(montoBoveda)}`
          }
        />
      )}

      {!cajasQuery.isLoading && cajas.length === 0 ? (
        <Alert
          type="warning"
          showIcon
          style={{ marginBottom: 12 }}
          message="No hay cajas cerradas disponibles en esta oficina."
        />
      ) : null}

      {cajas.length > 0 && !hayCajasAsignables ? (
        <Alert
          type="warning"
          showIcon
          style={{ marginBottom: 12 }}
          message="Las cajas disponibles no tienen cajero. Un administrador debe asignarlo en el maestro de cajas."
        />
      ) : null}

      {esAdmin && cajaSeleccionada && !cajaTieneCajero(cajaSeleccionada) ? (
        <Alert
          type="info"
          showIcon
          style={{ marginBottom: 12 }}
          message="Esta caja no tiene cajero. Elíjalo abajo: se guardará en el maestro antes de abrir la caja."
        />
      ) : null}

      <Form
        form={form}
        layout="vertical"
        initialValues={{ saldoInicial: 0 }}
        onFinish={(v) => void handleFinish(v)}
      >
        <Form.Item
          name="cajaId"
          label="Caja"
          rules={[{ required: true, message: 'Seleccione una caja' }]}
        >
          <Select
            loading={cajasQuery.isLoading}
            placeholder="Caja cerrada de la oficina"
            options={cajas.map((c) => ({
              value: c.cajaId,
              label: cajaOptionLabel(c),
              disabled: !esAdmin && !cajaTieneCajero(c),
            }))}
            showSearch
            optionFilterProp="label"
            notFoundContent={cajasQuery.isLoading ? 'Cargando…' : 'Sin cajas'}
          />
        </Form.Item>
        {esAdmin ? (
          <Form.Item
            name="cajeroId"
            label="Cajero de la caja"
            extra={
              <Text type="secondary">
                La caja se abre a nombre de este cajero. Si lo cambia, se actualiza el
                maestro de cajas.
              </Text>
            }
            rules={[{ required: true, message: 'Seleccione el cajero' }]}
          >
            <Select
              loading={gestoresQuery.isLoading}
              disabled={!cajaSeleccionada}
              placeholder={
                cajaSeleccionada ? 'Cajero de la caja' : 'Seleccione primero la caja'
              }
              options={(gestoresQuery.data ?? []).map((g) => ({
                value: g.usuarioId,
                label: g.nombreCompleto,
              }))}
              showSearch
              optionFilterProp="label"
            />
          </Form.Item>
        ) : (
          <Form.Item label="Cajero de la caja">
            {cajaSeleccionada ? (
              <Text strong>{cajeroLabel(cajaSeleccionada)}</Text>
            ) : (
              <Text type="secondary">Seleccione una caja.</Text>
            )}
            <div>
              <Text type="secondary" style={{ fontSize: 12 }}>
                Se toma del maestro de cajas; lo cambia un administrador.
              </Text>
            </div>
          </Form.Item>
        )}
        <Form.Item
          name="saldoInicial"
          label="Saldo inicial"
          extra={
            montoBoveda != null ? (
              <Text type="secondary">Máximo: {formatMoney(montoBoveda)} (bóveda).</Text>
            ) : null
          }
          rules={[
            { required: true, type: 'number', min: 0, message: 'Indique el saldo inicial' },
            {
              validator: async (_, value: number | null) => {
                if (value == null || montoBoveda == null) {
                  return
                }
                if (value > montoBoveda) {
                  throw new Error('Saldo Insuficiente de la boveda.')
                }
              },
            },
          ]}
        >
          <InputNumber
            style={{ width: '100%' }}
            min={0}
            max={montoBoveda}
            step={0.01}
            precision={2}
          />
        </Form.Item>
        <Form.Item style={{ marginBottom: 0 }}>
          <Space>
            <Button
              type="primary"
              htmlType="submit"
              loading={guardando}
              disabled={!hayCajasAsignables}
            >
              {submitLabel}
            </Button>
            {onCancel ? <Button onClick={onCancel}>Cancelar</Button> : null}
          </Space>
        </Form.Item>
      </Form>
    </>
  )
}
