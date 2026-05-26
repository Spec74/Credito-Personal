import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ExclamationCircleOutlined } from '@ant-design/icons'
import {
  Alert,
  Button,
  Form,
  Input,
  InputNumber,
  Modal,
  Select,
  Space,
  Tabs,
  Typography,
  message,
} from 'antd'
import {
  aceptarTransferenciaBoveda,
  asignarBovedaTemporal,
  cerrarBoveda,
  cerrarBovedaTemporal,
  fetchCajasAbiertasTransferenciaBoveda,
  fetchValidarCierreSaldos,
  ingresoEgresoBoveda,
  transferirBoveda,
  transferirBovedaCaja,
  transferirBovedaCajaChica,
  type BovedaAbiertaDto,
} from '../../api/boveda'
import { fetchTipoOperaciones } from '../../api/cajaDiario'
import { ApiError } from '../../api/errors'
import type { TipoOperacionListItem } from '../../types/api'

const { Paragraph, Text } = Typography

function errMsg(e: unknown): string {
  return e instanceof ApiError ? e.message : 'Error desconocido'
}

function invalidateBoveda(
  queryClient: ReturnType<typeof useQueryClient>,
  oficinaId: number,
  bovedaId: number,
) {
  void queryClient.invalidateQueries({ queryKey: ['boveda-abierta', oficinaId] })
  void queryClient.invalidateQueries({ queryKey: ['boveda-estado-dinero', oficinaId] })
  void queryClient.invalidateQueries({ queryKey: ['boveda-listar', oficinaId] })
  void queryClient.invalidateQueries({ queryKey: ['existe-boveda-temporal', oficinaId] })
  void queryClient.invalidateQueries({ queryKey: ['resumen-cuenta-boveda', bovedaId] })
  void queryClient.invalidateQueries({ queryKey: ['rpt-movimiento-boveda', bovedaId] })
  void queryClient.invalidateQueries({ queryKey: ['saldos-caja-diario-boveda', oficinaId] })
}

type Props = {
  oficinaId: number
  boveda: BovedaAbiertaDto
  existeTemporal: boolean
}

export function BovedaOperacionesPanel({ oficinaId, boveda, existeTemporal }: Props) {
  const queryClient = useQueryClient()
  const bovedaId = boveda.bovedaId
  const operacionesDeshabilitadas = boveda.indCierre || oficinaId < 1

  const onOk = (msg: string) => {
    message.success(msg)
    invalidateBoveda(queryClient, oficinaId, bovedaId)
  }

  const oficinaOk = oficinaId >= 1

  const tiposQuery = useQuery({
    queryKey: ['tipo-operaciones'],
    queryFn: fetchTipoOperaciones,
    enabled: oficinaOk,
  })
  const tiposBoveda = (tiposQuery.data ?? []).filter((t) => t.indBoveda)

  const cajasQuery = useQuery({
    queryKey: ['cajas-transferencia-boveda', oficinaId],
    queryFn: () => fetchCajasAbiertasTransferenciaBoveda(oficinaId),
    enabled: oficinaOk,
  })

  const validacionCierre = useQuery({
    queryKey: ['validar-cierre-saldos', oficinaId],
    queryFn: () => fetchValidarCierreSaldos(oficinaId),
    enabled: oficinaOk,
  })

  const ingresoEgreso = useMutation({
    mutationFn: ingresoEgresoBoveda,
    onSuccess: (r) => onOk(`Movimiento bóveda #${r.movimientoBovedaId}`),
    onError: (e) => message.error(errMsg(e)),
  })

  const aCaja = useMutation({
    mutationFn: transferirBovedaCaja,
    onSuccess: (r) =>
      onOk(
        `Transferido a caja (mov. bóveda #${r.movimientoBovedaId}${r.movimientoCajaId ? `, caja #${r.movimientoCajaId}` : ''})`,
      ),
    onError: (e) => message.error(errMsg(e)),
  })

  const aCajaChica = useMutation({
    mutationFn: transferirBovedaCajaChica,
    onSuccess: (r) => onOk(`Transferido a caja chica (mov. #${r.movimientoBovedaId})`),
    onError: (e) => message.error(errMsg(e)),
  })

  const temporal = useMutation({
    mutationFn: asignarBovedaTemporal,
    onSuccess: (r) =>
      onOk(
        r.bovedaTemporalId
          ? `Bóveda temporal #${r.bovedaTemporalId}`
          : 'Transferencia a temporal registrada',
      ),
    onError: (e) => message.error(errMsg(e)),
  })

  const transferir = useMutation({
    mutationFn: transferirBoveda,
    onSuccess: (r) => onOk(`Transferencia (código ${r.resultCode})`),
    onError: (e) => message.error(errMsg(e)),
  })

  const aceptar = useMutation({
    mutationFn: (v: { bovedaMovTempId: number; flagAceptar: number }) =>
      aceptarTransferenciaBoveda({
        oficinaId,
        bovedaMovTempId: v.bovedaMovTempId,
        flagAceptar: v.flagAceptar,
      }),
    onSuccess: (r) => onOk(`Aceptación/rechazo (código ${r.resultCode})`),
    onError: (e) => message.error(errMsg(e)),
  })

  const cerrar = useMutation({
    mutationFn: () => cerrarBoveda(oficinaId),
    onSuccess: (r) => onOk(`Bóveda cerrada (código ${r.resultCode})`),
    onError: (e) => message.error(errMsg(e)),
  })

  const cerrarTemp = useMutation({
    mutationFn: () => cerrarBovedaTemporal(oficinaId),
    onSuccess: (r) => onOk(`Bóveda temporal cerrada (código ${r.resultCode})`),
    onError: (e) => message.error(errMsg(e)),
  })

  if (!oficinaOk) {
    return <Alert type="warning" showIcon message="Sesión sin oficina válida." />
  }

  const confirmarCierre = (temporalOnly: boolean) => {
    const validacion = validacionCierre.data
    Modal.confirm({
      title: temporalOnly ? 'Cerrar bóveda temporal' : 'Cerrar bóveda principal',
      icon: <ExclamationCircleOutlined />,
      content: (
        <>
          {validacion && !validacion.puedeCerrar ? (
            <Alert
              type="warning"
              showIcon
              style={{ marginBottom: 12 }}
              message={validacion.mensaje}
            />
          ) : null}
          <Paragraph style={{ marginBottom: 0 }}>
            Ejecuta{' '}
            {temporalOnly ? 'usp_CerrarBovedaTemporal' : 'usp_CerrarBoveda'} para la
            oficina actual.
          </Paragraph>
        </>
      ),
      okText: 'Cerrar',
      okButtonProps: { danger: true },
      cancelText: 'Cancelar',
      onOk: () =>
        temporalOnly ? cerrarTemp.mutateAsync() : cerrar.mutateAsync(),
    })
  }

  const formActions = (submitLabel: string, loading: boolean) => (
    <div className="boveda-operaciones-form__actions">
      <Button
        type="primary"
        htmlType="submit"
        loading={loading}
        disabled={operacionesDeshabilitadas}
      >
        {submitLabel}
      </Button>
    </div>
  )


  const importeDescripcionFields = (
    <>
      <Form.Item
        name="importe"
        label="Importe"
        rules={[{ required: true, type: 'number', min: 0.01 }]}
      >
        <InputNumber min={0.01} step={0.01} style={{ width: '100%' }} />
      </Form.Item>
      <Form.Item
        name="descripcion"
        label="Descripción / glosa"
        rules={[{ required: true, message: 'Obligatorio' }]}
      >
        <Input.TextArea rows={2} />
      </Form.Item>
    </>
  )

  const tabItems = [
    {
      key: 'ingreso',
      label: 'Ingreso / egreso',
      children: (
        <Form
          className="boveda-operaciones-form"
          layout="vertical"
          initialValues={{ tipoPagoId: 1 }}
          onFinish={(v) =>
            ingresoEgreso.mutate({
              oficinaId,
              importe: v.importe,
              descripcion: v.descripcion,
              tipoOperacionId: v.tipoOperacionId,
              tipoPagoId: v.tipoPagoId,
            })
          }
        >
          <Form.Item
            name="tipoOperacionId"
            label="Tipo operación (IndBoveda)"
            rules={[{ required: true }]}
          >
            <Select
              loading={tiposQuery.isLoading}
              showSearch
              optionFilterProp="label"
              options={tiposBoveda.map((t: TipoOperacionListItem) => ({
                value: t.tipoOperacionId,
                label: `${t.codigo} — ${t.denominacion} (${t.indEntrada ? 'Ingreso' : 'Egreso'})`,
              }))}
            />
          </Form.Item>
          {importeDescripcionFields}
          <Form.Item name="tipoPagoId" label="Tipo pago" rules={[{ required: true }]}>
            <Select
              options={[
                { value: 1, label: 'Efectivo (1)' },
                { value: 2, label: 'Transferencia (2)' },
              ]}
            />
          </Form.Item>
          {formActions('Registrar movimiento', ingresoEgreso.isPending)}
        </Form>
      ),
    },
    {
      key: 'caja',
      label: 'A caja diaria',
      children: (
        <Form
          className="boveda-operaciones-form"
          layout="vertical"
          onFinish={(v) =>
            aCaja.mutate({
              oficinaId,
              cajaId: v.cajaId,
              importe: v.importe,
              descripcion: v.descripcion,
            })
          }
        >
          <Form.Item name="cajaId" label="Caja abierta" rules={[{ required: true }]}>
            <Select
              loading={cajasQuery.isLoading}
              showSearch
              optionFilterProp="label"
              placeholder="Seleccione caja"
              options={(cajasQuery.data ?? []).map((c) => ({
                value: c.cajaId,
                label: c.etiqueta,
              }))}
            />
          </Form.Item>
          {importeDescripcionFields}
          {formActions('Transferir a caja', aCaja.isPending)}
        </Form>
      ),
    },
    {
      key: 'chica',
      label: 'A caja chica',
      children: (
        <Form
          className="boveda-operaciones-form"
          layout="vertical"
          onFinish={(v) =>
            aCajaChica.mutate({
              oficinaId,
              importe: v.importe,
              descripcion: v.descripcion,
            })
          }
        >
          {importeDescripcionFields}
          {formActions('Transferir a caja chica', aCajaChica.isPending)}
        </Form>
      ),
    },
    {
      key: 'temporal',
      label: 'Bóveda temporal',
      children: (
        <>
          <Paragraph type="secondary">
            <Text code>usuarioId &gt; 0</Text>: asignar encargado (crea temporal si no
            existe). <Text code>usuarioId = 0</Text>: transferir a temporal existente.
          </Paragraph>
          <Form
            className="boveda-operaciones-form"
            layout="vertical"
            initialValues={{ usuarioId: existeTemporal ? 0 : undefined }}
            onFinish={(v) =>
              temporal.mutate({
                oficinaId,
                importe: v.importe,
                descripcion: v.descripcion,
                usuarioId: v.usuarioId ?? 0,
              })
            }
          >
            <Form.Item name="usuarioId" label="Usuario encargado (0 = solo transferir)">
              <InputNumber min={0} style={{ width: '100%' }} />
            </Form.Item>
            {importeDescripcionFields}
            {formActions(
              existeTemporal ? 'Transferir a temporal' : 'Asignar temporal',
              temporal.isPending,
            )}
          </Form>
        </>
      ),
    },
    {
      key: 'interoficina',
      label: 'Transferir bóveda',
      children: (
        <>
          <Paragraph type="secondary">
            Origen: bóveda #{bovedaId}. Indique el ID de la bóveda destino (en MVC el
            combo es otra oficina; el SP usa <Text code>BovedaDestinoId</Text>).
          </Paragraph>
          <Form
            className="boveda-operaciones-form"
            layout="vertical"
            onFinish={(v) =>
              transferir.mutate({
                oficinaId,
                bovedaInicioId: bovedaId,
                bovedaDestinoId: v.bovedaDestinoId,
                glosa: v.glosa,
                monto: v.monto,
              })
            }
          >
            <Form.Item
              name="bovedaDestinoId"
              label="Bóveda destino (ID)"
              rules={[{ required: true, type: 'number', min: 1 }]}
            >
              <InputNumber style={{ width: '100%' }} />
            </Form.Item>
            <Form.Item
              name="monto"
              label="Monto"
              rules={[{ required: true, type: 'number', min: 0.01 }]}
            >
              <InputNumber min={0.01} step={0.01} style={{ width: '100%' }} />
            </Form.Item>
            <Form.Item
              name="glosa"
              label="Glosa"
              rules={[{ required: true, message: 'Obligatorio' }]}
            >
              <Input.TextArea rows={2} />
            </Form.Item>
            {formActions('Enviar transferencia', transferir.isPending)}
          </Form>
        </>
      ),
    },
    {
      key: 'aceptar',
      label: 'Aceptar / rechazar',
      children: (
        <Form
          className="boveda-operaciones-form"
          layout="vertical"
          onFinish={(v) =>
            aceptar.mutate({
              bovedaMovTempId: v.bovedaMovTempId,
              flagAceptar: v.flagAceptar,
            })
          }
        >
          <Form.Item
            name="bovedaMovTempId"
            label="BovedaMovTempId"
            rules={[{ required: true, type: 'number', min: 1 }]}
          >
            <InputNumber style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item
            name="flagAceptar"
            label="Acción"
            rules={[{ required: true }]}
            initialValue={1}
          >
            <Select
              options={[
                { value: 1, label: 'Aceptar (1)' },
                { value: 0, label: 'Rechazar (0)' },
              ]}
            />
          </Form.Item>
          {formActions('Confirmar', aceptar.isPending)}
        </Form>
      ),
    },
    {
      key: 'cierre',
      label: 'Cierre',
      children: (
        <div className="boveda-operaciones-form boveda-operaciones-form--cierre">
        <Space direction="vertical" style={{ width: '100%' }}>
          {validacionCierre.data && !validacionCierre.data.puedeCerrar ? (
            <Alert type="warning" showIcon message={validacionCierre.data.mensaje} />
          ) : null}
          <Paragraph type="secondary">
            Valida cajas con <code>validar-cierre-saldos</code> antes de cerrar la bóveda
            principal (paridad <code>BovedaController.ValidarCierre</code>).
          </Paragraph>
          <Space wrap>
            <Button
              danger
              loading={cerrar.isPending}
              disabled={boveda.indTemporal}
              onClick={() => confirmarCierre(false)}
            >
              Cerrar bóveda principal
            </Button>
            <Button
              loading={cerrarTemp.isPending}
              disabled={!boveda.indTemporal && !existeTemporal}
              onClick={() => confirmarCierre(true)}
            >
              Cerrar bóveda temporal
            </Button>
          </Space>
        </Space>
        </div>
      ),
    },
  ]

  return (
    <>
      {operacionesDeshabilitadas ? (
        <Alert
          type="info"
          showIcon
          style={{ marginBottom: 16 }}
          message="La bóveda está cerrada; no se pueden registrar movimientos."
        />
      ) : null}
      {tiposBoveda.length === 0 && !tiposQuery.isLoading ? (
        <Alert
          type="warning"
          showIcon
          style={{ marginBottom: 16 }}
          message="No hay tipos de operación con IndBoveda activos."
        />
      ) : null}
      <Tabs
        className="boveda-operaciones-tabs credix-tabs"
        type="card"
        destroyInactiveTabPane={false}
        items={tabItems}
      />
    </>
  )
}
