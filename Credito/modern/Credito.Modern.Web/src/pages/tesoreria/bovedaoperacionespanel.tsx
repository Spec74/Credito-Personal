import { useMemo, useState, type ReactNode } from 'react'
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
  Typography,
  message,
} from 'antd'
import {
  anularTransferenciaPendienteBoveda,
  asignarBovedaTemporal,
  buscarUsuariosBoveda,
  cerrarBoveda,
  cerrarBovedaTemporal,
  fetchBovedaTransferenciasPendientes,
  fetchBovedasDestinoTransferencia,
  fetchCajasAbiertasTransferenciaBoveda,
  fetchValidarCierreBoveda,
  ingresoEgresoBoveda,
  transferirBoveda,
  transferirBovedaBancos,
  transferirBovedaCaja,
  transferirBovedaCajaChica,
  transferirBovedaAnalista,
  type BovedaAbiertaDto,
  type BovedaTransferenciaPendiente,
} from '../../api/boveda'
import { confirmMoney } from '../../components/credix'
import { formatFechaHora } from '../../utils/formatFecha'
import { formatMoney } from '../../utils/formatMoney'
import { fetchTipoOperaciones } from '../../api/cajaDiario'
import { fetchValoresTabla } from '../../api/maestros'
import { ApiError } from '../../api/errors'
import { useDebouncedValue } from '../../hooks/useDebouncedValue'
import { useSecondaryDataReady } from '../../hooks/useSecondaryDataReady'
import type { TipoOperacionListItem } from '../../types/api'
import { FIELD_MAX, glosaRules, moneyRequired } from '../../validation/formRules'
import {
  buildTipoPagoOptions,
  tipoPagoOptionRender,
} from './components/TipoPagoBrand'

const { Paragraph } = Typography

const OPS_GROUPS: { title: string; items: { key: string; label: string }[] }[] = [
  { title: 'Registrar', items: [{ key: 'ingreso', label: 'Movimiento' }] },
  {
    title: 'Transferir',
    items: [
      { key: 'caja', label: 'A caja' },
      { key: 'analista', label: 'A analista' },
      { key: 'chica', label: 'Caja chica' },
      { key: 'bancos', label: 'Entre medios' },
      { key: 'temporal', label: 'Temporal' },
    ],
  },
  {
    title: 'Red',
    items: [
      { key: 'interoficina', label: 'Fondeo' },
      { key: 'aceptar', label: 'Pendientes' },
    ],
  },
  { title: 'Cierre', items: [{ key: 'cierre', label: 'Cerrar bóveda' }] },
]

const OPS_COPY: Record<string, { title: string; blurb: string }> = {
  ingreso: {
    title: 'Registrar movimiento',
    blurb: 'Ingreso o egreso de esta bóveda, con el medio de pago.',
  },
  caja: {
    title: 'Transferir a caja',
    blurb: 'Pasa saldo a una caja diaria abierta de esta oficina.',
  },
  analista: {
    title: 'Transferir a analista',
    blurb: 'Descuenta el medio de origen y abona efectivo en la caja del analista.',
  },
  chica: {
    title: 'Caja chica',
    blurb: 'Pasa saldo a la caja chica abierta.',
  },
  bancos: {
    title: 'Entre medios',
    blurb: 'Mueve saldo entre efectivo, Yape o bancos de esta misma bóveda.',
  },
  temporal: {
    title: 'Bóveda temporal',
    blurb: 'Asigna un encargado o transfiere saldo a la temporal ya abierta.',
  },
  interoficina: {
    title: 'Fondeo entre oficinas',
    blurb: 'El saldo se acredita al instante en origen y destino. No espera aceptación.',
  },
  aceptar: {
    title: 'Pendientes',
    blurb: 'Solo filas antiguas. El fondeo no espera; aquí únicamente se anulan leftovers.',
  },
  cierre: {
    title: 'Cerrar bóveda',
    blurb: 'La principal, cuando las cajas estén cerradas. La temporal, si aplica.',
  },
}

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
  void queryClient.invalidateQueries({ queryKey: ['validar-cierre-saldos', oficinaId] })
  void queryClient.invalidateQueries({ queryKey: ['cajas-transferencia-boveda', oficinaId] })
  void queryClient.invalidateQueries({ queryKey: ['bovedas-destino-transferencia', oficinaId] })
  void queryClient.invalidateQueries({ queryKey: ['boveda-transferencias-pendientes', bovedaId] })
  void queryClient.invalidateQueries({ queryKey: ['boveda-red-posicion'] })
}

type Props = {
  oficinaId: number
  boveda: BovedaAbiertaDto
  existeTemporal: boolean
}

export function BovedaOperacionesPanel({ oficinaId, boveda, existeTemporal }: Props) {
  const queryClient = useQueryClient()
  const [usuarioTerm, setUsuarioTerm] = useState('')
  const [activeTab, setActiveTab] = useState('ingreso')
  const bovedaId = boveda.bovedaId
  const operacionesDeshabilitadas = boveda.indCierre || oficinaId < 1
  const usuarioTermDebounced = useDebouncedValue(usuarioTerm.trim(), 350)
  const secondaryReady = useSecondaryDataReady(oficinaId >= 1)

  const onOk = (msg: string) => {
    message.success(msg)
    invalidateBoveda(queryClient, oficinaId, bovedaId)
  }

  const oficinaOk = oficinaId >= 1

  /** Catálogo del tab por defecto: tras paint para no competir con LCP del shell. */
  const tiposQuery = useQuery({
    queryKey: ['tipo-operaciones'],
    queryFn: fetchTipoOperaciones,
    enabled: oficinaOk && secondaryReady,
    staleTime: 5 * 60_000,
  })
  const tiposBoveda = (tiposQuery.data ?? []).filter((t) => t.indBoveda)

  const tiposPagoQuery = useQuery({
    queryKey: ['valores-tabla', 13],
    queryFn: () => fetchValoresTabla(13),
    enabled: oficinaOk && secondaryReady,
    staleTime: 5 * 60_000,
  })
  const tipoPagoOptions = useMemo(
    () => buildTipoPagoOptions(tiposPagoQuery.data ?? []),
    [tiposPagoQuery.data],
  )

  const tipoPagoSelectProps = {
    showSearch: true,
    optionFilterProp: 'label' as const,
    optionRender: tipoPagoOptionRender,
    labelRender: (props: { label?: ReactNode }) =>
      typeof props.label === 'string'
        ? tipoPagoOptionRender({ label: props.label })
        : props.label,
  }

  const needsCajas =
    activeTab === 'caja' || activeTab === 'analista' || activeTab === 'chica'
  const needsDestinos = activeTab === 'interoficina'
  const needsCierre = activeTab === 'cierre'

  const cajasQuery = useQuery({
    queryKey: ['cajas-transferencia-boveda', oficinaId],
    queryFn: () => fetchCajasAbiertasTransferenciaBoveda(oficinaId),
    enabled: oficinaOk && needsCajas,
  })

  const bovedasDestinoQuery = useQuery({
    queryKey: ['bovedas-destino-transferencia', oficinaId],
    queryFn: () => fetchBovedasDestinoTransferencia(oficinaId),
    enabled: oficinaOk && needsDestinos,
  })

  const transferenciasPendientesQuery = useQuery({
    queryKey: ['boveda-transferencias-pendientes', bovedaId],
    queryFn: () => fetchBovedaTransferenciasPendientes(bovedaId),
    enabled: oficinaOk && bovedaId > 0 && secondaryReady,
    staleTime: 15_000,
  })
  const pendientes = transferenciasPendientesQuery.data ?? []
  const pendientesCount = pendientes.length

  const usuariosQuery = useQuery({
    queryKey: ['buscar-usuarios-boveda-temporal', usuarioTermDebounced],
    queryFn: () => buscarUsuariosBoveda(usuarioTermDebounced),
    enabled:
      oficinaOk &&
      activeTab === 'temporal' &&
      !existeTemporal &&
      usuarioTermDebounced.length >= 2,
  })

  const validacionCierre = useQuery({
    queryKey: ['validar-cierre-boveda', oficinaId],
    queryFn: () => fetchValidarCierreBoveda(oficinaId),
    enabled: oficinaOk && needsCierre,
  })
  const cierrePrincipalBloqueado =
    boveda.indTemporal ||
    (needsCierre &&
      (validacionCierre.isLoading ||
        validacionCierre.isError ||
        validacionCierre.data?.puedeCerrar === false))

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

  const aAnalista = useMutation({
    mutationFn: transferirBovedaAnalista,
    onSuccess: (r) =>
      onOk(
        `Transferido a analista (embudo efectivo · mov. #${r.movimientoBovedaId}${r.movimientoCajaId ? `, caja #${r.movimientoCajaId}` : ''})`,
      ),
    onError: (e) => message.error(errMsg(e)),
  })

  const aCajaChica = useMutation({
    mutationFn: transferirBovedaCajaChica,
    onSuccess: (r) => onOk(`Transferido a caja chica (mov. #${r.movimientoBovedaId})`),
    onError: (e) => message.error(errMsg(e)),
  })

  const entreBancos = useMutation({
    mutationFn: transferirBovedaBancos,
    onSuccess: (r) => onOk(r.mensaje || 'Transferencia realizada con éxito.'),
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

  const anularPendiente = useMutation({
    mutationFn: (bovedaMovTempId: number) =>
      anularTransferenciaPendienteBoveda({ oficinaId, bovedaMovTempId }),
    onSuccess: (r) => onOk(`Pendiente anulada (código ${r.resultCode})`),
    onError: (e) => message.error(errMsg(e)),
  })

  const pedirFondeo = (v: {
    bovedaDestinoId: number
    monto: number
    glosa: string
    tipoPagoId?: number
  }) => {
    const dest = bovedasDestinoQuery.data?.find((d) => d.bovedaId === v.bovedaDestinoId)
    const medio =
      tipoPagoOptions.find((o) => o.value === (v.tipoPagoId && v.tipoPagoId > 0 ? v.tipoPagoId : 1))
        ?.label ?? 'efectivo'
    confirmMoney({
      title: dest ? `Fondear ${dest.oficina}` : 'Fondear / transferir ahora',
      amount: v.monto,
      officeLabel: dest
        ? `Bóveda #${bovedaId} → ${dest.oficina} · #${dest.bovedaId}`
        : `Bóveda #${bovedaId}`,
      consequence: `Viaja ${medio}. Se acredita de inmediato en origen y destino. No espera aceptación.`,
      okText: 'Transferir',
      onOk: async () => {
        await transferir.mutateAsync({
          oficinaId,
          bovedaInicioId: bovedaId,
          bovedaDestinoId: v.bovedaDestinoId,
          glosa: v.glosa,
          monto: v.monto,
          tipoPagoId: v.tipoPagoId && v.tipoPagoId > 0 ? v.tipoPagoId : 1,
        })
      },
    })
  }

  const pedirAnularPendiente = (t: BovedaTransferenciaPendiente) => {
    confirmMoney({
      title: 'Anular pendiente',
      amount: t.monto,
      officeLabel: t.oficinaOrigen,
      consequence:
        'Borra la fila temporal y el movimiento de origen. No acredita en esta bóveda.',
      okText: 'Anular',
      onOk: async () => {
        await anularPendiente.mutateAsync(t.bovedaMovTempId)
      },
    })
  }

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
    if (!temporalOnly && validacion?.puedeCerrar === false) {
      Modal.warning({
        title: 'Cierre bloqueado',
        content: validacion.mensaje || 'Debe completar el cierre de cajas antes de cerrar la bóveda.',
      })
      return
    }

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
            {temporalOnly
              ? 'Se cerrará la bóveda temporal de esta oficina. Confirme solo si ya no hay operaciones pendientes en ella.'
              : 'Se cerrará la bóveda principal de esta oficina. Confirme solo si las cajas están en orden.'}
          </Paragraph>
        </>
      ),
      okText: 'Cerrar',
      okButtonProps: {
        danger: true,
        disabled: !temporalOnly && validacion?.puedeCerrar === false,
      },
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
        size="large"
        loading={loading}
        disabled={operacionesDeshabilitadas}
      >
        {submitLabel}
      </Button>
    </div>
  )


  const importeField = (
    <Form.Item name="importe" label="Importe" rules={moneyRequired()}>
      <InputNumber min={0.01} step={0.01} style={{ width: '100%' }} />
    </Form.Item>
  )
  const descripcionField = (
    <Form.Item name="descripcion" label="Descripción / glosa" rules={glosaRules}>
      <Input.TextArea rows={2} maxLength={FIELD_MAX.glosa} showCount />
    </Form.Item>
  )
  const importeDescripcionFields = (
    <>
      {importeField}
      {descripcionField}
    </>
  )

  const panes: Record<string, ReactNode> = {
    ingreso: (
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
            label="Tipo de operación"
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
          <div className="boveda-operaciones-form__row">
            {importeField}
            <Form.Item name="tipoPagoId" label="Tipo de pago" rules={[{ required: true }]}>
              <Select
                loading={tiposPagoQuery.isLoading}
                placeholder="Efectivo, Yape, banco…"
                options={tipoPagoOptions}
                {...tipoPagoSelectProps}
              />
            </Form.Item>
          </div>
          {descripcionField}
          {formActions('Registrar movimiento', ingresoEgreso.isPending)}
        </Form>
    ),
    caja: (
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
          <div className="boveda-operaciones-form__row">
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
          {importeField}
          </div>
          {descripcionField}
          {formActions('Transferir a caja', aCaja.isPending)}
        </Form>
    ),
    analista: (
        <Form
            className="boveda-operaciones-form"
            layout="vertical"
            initialValues={{ tipoPagoOrigenId: 2 }}
            onFinish={(v) =>
              aAnalista.mutate({
                oficinaId,
                usuarioAnalistaId: v.usuarioAnalistaId,
                tipoPagoOrigenId: v.tipoPagoOrigenId,
                importe: v.importe,
                descripcion: v.descripcion,
              })
            }
          >
            <div className="boveda-operaciones-form__row">
            <Form.Item
              name="tipoPagoOrigenId"
              label="Banco / medio de origen"
              rules={[{ required: true }]}
            >
              <Select
                options={tipoPagoOptions.filter((o) => o.value !== 1)}
                placeholder="Seleccione medio (no efectivo)"
                {...tipoPagoSelectProps}
              />
            </Form.Item>
            <Form.Item
              name="usuarioAnalistaId"
              label="Analista (caja abierta)"
              rules={[{ required: true }]}
            >
              <Select
                loading={cajasQuery.isLoading}
                showSearch
                optionFilterProp="label"
                placeholder="Seleccione analista"
                options={(cajasQuery.data ?? [])
                  .filter((c) => c.usuarioAsignadoId > 0)
                  .map((c) => ({
                    value: c.usuarioAsignadoId,
                    label: c.etiqueta,
                  }))}
              />
            </Form.Item>
            </div>
            {importeField}
            {descripcionField}
            {formActions('Transferir a analista', aAnalista.isPending)}
          </Form>
    ),
    chica: (
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
    bancos: (
        <Form
            className="boveda-operaciones-form"
            layout="vertical"
            onFinish={(v) =>
              entreBancos.mutate({
                oficinaId,
                tipoPagoOrigenId: v.tipoPagoOrigenId,
                tipoPagoDestinoId: v.tipoPagoDestinoId,
                importe: v.importe,
                glosa: v.glosa,
              })
            }
          >
            <div className="boveda-operaciones-form__row">
            <Form.Item
              name="tipoPagoOrigenId"
              label="Origen"
              rules={[{ required: true, message: 'Seleccione el origen' }]}
            >
              <Select
                loading={tiposPagoQuery.isLoading}
                placeholder="Medio de pago origen"
                options={tipoPagoOptions}
                {...tipoPagoSelectProps}
              />
            </Form.Item>
            <Form.Item
              name="tipoPagoDestinoId"
              label="Destino"
              dependencies={['tipoPagoOrigenId']}
              rules={[
                { required: true, message: 'Seleccione el destino' },
                ({ getFieldValue }) => ({
                  validator(_, value) {
                    if (value != null && value === getFieldValue('tipoPagoOrigenId')) {
                      return Promise.reject(
                        new Error(
                          'El banco de origen y el de destino no pueden ser iguales.',
                        ),
                      )
                    }
                    return Promise.resolve()
                  },
                }),
              ]}
            >
              <Select
                loading={tiposPagoQuery.isLoading}
                placeholder="Medio de pago destino"
                options={tipoPagoOptions}
                {...tipoPagoSelectProps}
              />
            </Form.Item>
            </div>
            <Form.Item name="importe" label="Importe" rules={moneyRequired()}>
              <InputNumber min={0.01} step={0.01} style={{ width: '100%' }} />
            </Form.Item>
            <Form.Item name="glosa" label="Glosa" rules={glosaRules}>
              <Input.TextArea rows={2} maxLength={FIELD_MAX.glosa} showCount />
            </Form.Item>
            {formActions('Transferir entre bancos', entreBancos.isPending)}
          </Form>
    ),
    temporal: (
          <Form
            className="boveda-operaciones-form"
            layout="vertical"
            initialValues={{
              usuarioId: existeTemporal ? 0 : undefined,
              tipoPagoId: 1,
            }}
            onFinish={(v) =>
              temporal.mutate({
                oficinaId,
                importe: v.importe,
                descripcion: v.descripcion,
                usuarioId: existeTemporal ? 0 : v.usuarioId,
                tipoPagoId: v.tipoPagoId && v.tipoPagoId > 0 ? v.tipoPagoId : 1,
              })
            }
          >
            {existeTemporal ? (
              <Alert
                type="info"
                showIcon
                style={{ marginBottom: 12 }}
                message="Ya existe una bóveda temporal abierta. Esta operación solo transferirá saldo a la temporal actual."
              />
            ) : (
              <Form.Item
                name="usuarioId"
                label="Encargado de bóveda temporal"
                rules={[{ required: true, message: 'Seleccione un encargado' }]}
                extra="Busque por nombre o DNI; ya no es necesario escribir el ID del usuario."
              >
                <Select
                  showSearch
                  filterOption={false}
                  onSearch={setUsuarioTerm}
                  loading={usuariosQuery.isFetching}
                  placeholder="Buscar usuario encargado"
                  options={(usuariosQuery.data ?? []).map((u) => ({
                    value: u.personaId,
                    label: u.label,
                  }))}
                  notFoundContent={
                    usuarioTermDebounced.length < 2
                      ? 'Escriba al menos 2 caracteres'
                      : 'Sin usuarios encontrados'
                  }
                />
              </Form.Item>
            )}
            <Form.Item
              name="tipoPagoId"
              label="Medio"
              rules={[{ required: true, message: 'Seleccione el medio' }]}
            >
              <Select
                loading={tiposPagoQuery.isLoading}
                placeholder="Efectivo, Yape, banco…"
                options={tipoPagoOptions}
                {...tipoPagoSelectProps}
              />
            </Form.Item>
            {importeDescripcionFields}
            {formActions(
              existeTemporal ? 'Transferir a temporal' : 'Asignar temporal',
              temporal.isPending,
            )}
          </Form>
    ),
    interoficina: (
        <>
          {bovedasDestinoQuery.data?.length === 0 ? (
            <Alert
              type="warning"
              showIcon
              style={{ marginBottom: 12 }}
              message="No hay bóvedas principales abiertas en otras oficinas."
            />
          ) : null}
          <Form
            className="boveda-operaciones-form"
            layout="vertical"
            initialValues={{ tipoPagoId: 1 }}
            onFinish={pedirFondeo}
          >
            <div className="boveda-operaciones-form__row">
            <Form.Item
              name="bovedaDestinoId"
              label="Oficina / bóveda destino"
              rules={[{ required: true, message: 'Seleccione una bóveda destino' }]}
            >
              <Select
                loading={bovedasDestinoQuery.isLoading}
                showSearch
                optionFilterProp="label"
                placeholder="Seleccione destino"
                options={(bovedasDestinoQuery.data ?? []).map((d) => ({
                  value: d.bovedaId,
                  label: `${d.oficina} · Bóveda #${d.bovedaId} · Saldo S/ ${Number(d.saldoFinal).toFixed(2)}`,
                }))}
              />
            </Form.Item>
            <Form.Item
              name="tipoPagoId"
              label="Medio"
              rules={[{ required: true, message: 'Seleccione el medio' }]}
            >
              <Select
                loading={tiposPagoQuery.isLoading}
                placeholder="Efectivo, Yape, banco…"
                options={tipoPagoOptions}
                {...tipoPagoSelectProps}
              />
            </Form.Item>
            </div>
            <Form.Item name="monto" label="Monto" rules={moneyRequired('Monto mayor a cero')}>
              <InputNumber min={0.01} step={0.01} style={{ width: '100%' }} />
            </Form.Item>
            <Form.Item name="glosa" label="Glosa" rules={glosaRules}>
              <Input.TextArea rows={2} maxLength={FIELD_MAX.glosa} showCount />
            </Form.Item>
            {formActions('Transferir ahora', transferir.isPending)}
          </Form>
        </>
    ),
    aceptar: (
        <>
          {transferenciasPendientesQuery.isLoading ? (
            <Alert type="info" showIcon message="Buscando filas en espera…" />
          ) : pendientesCount === 0 ? (
            <Alert
              type="success"
              showIcon
              message="Nada en espera hacia esta bóveda."
              description="Para enviar saldo a otra sede use Fondeo. Queda acreditado de inmediato."
            />
          ) : (
            <ul className="boveda-pendientes">
              {pendientes.map((t) => (
                <li key={t.bovedaMovTempId} className="boveda-pendientes__item">
                  <div className="boveda-pendientes__meta">
                    <strong>{t.oficinaOrigen}</strong>
                    <span className="boveda-pendientes__monto">S/ {formatMoney(t.monto)}</span>
                    <span>
                      {t.descripcion?.trim() || 'Sin glosa'} · {t.usuarioReg} ·{' '}
                      {formatFechaHora(t.fechaReg)}
                    </span>
                  </div>
                  <Button
                    danger
                    disabled={operacionesDeshabilitadas}
                    loading={anularPendiente.isPending}
                    onClick={() => pedirAnularPendiente(t)}
                  >
                    Anular
                  </Button>
                </li>
              ))}
            </ul>
          )}
        </>
    ),
    cierre: (
        <div className="boveda-operaciones-form boveda-operaciones-form--cierre">
        <Space direction="vertical" style={{ width: '100%' }}>
          {validacionCierre.data && !validacionCierre.data.puedeCerrar ? (
            <Alert type="warning" showIcon message={validacionCierre.data.mensaje} />
          ) : validacionCierre.data?.puedeCerrar ? (
            <Alert
              type="success"
              showIcon
              message="Cajas en orden y Σ medios = saldo. Puede cerrar la principal."
            />
          ) : validacionCierre.isError ? (
            <Alert
              type="error"
              showIcon
              message="No se pudo validar el cierre. Actualice la página antes de cerrar."
            />
          ) : null}
          <Space wrap>
            <Button
              danger
              size="large"
              loading={cerrar.isPending}
              disabled={cierrePrincipalBloqueado}
              onClick={() => confirmarCierre(false)}
            >
              Cerrar bóveda principal
            </Button>
            <Button
              size="large"
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
  }

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
          message="No hay tipos de operación de bóveda activos. Revise el catálogo de operaciones."
        />
      ) : null}
      <div className="boveda-ops" translate="no">
        <nav className="boveda-ops-nav" aria-label="Operaciones de bóveda">
          {OPS_GROUPS.map((group) => (
            <div key={group.title} className="boveda-ops-nav__group">
              <p className="boveda-ops-nav__title">{group.title}</p>
              {group.items.map((item) => {
                const label =
                  item.key === 'aceptar' && pendientesCount > 0
                    ? `${item.label} (${pendientesCount})`
                    : item.label
                return (
                <button
                  key={item.key}
                  type="button"
                  className={
                    item.key === activeTab
                      ? 'boveda-ops-nav__btn is-active'
                      : 'boveda-ops-nav__btn'
                  }
                  aria-current={item.key === activeTab ? 'true' : undefined}
                  onClick={() => setActiveTab(item.key)}
                >
                  {label}
                </button>
                )
              })}
            </div>
          ))}
        </nav>
        <div className="boveda-ops-stage">
          {OPS_COPY[activeTab] ? (
            <header className="boveda-ops-stage__head">
              <h3>{OPS_COPY[activeTab].title}</h3>
              <p>{OPS_COPY[activeTab].blurb}</p>
            </header>
          ) : null}
          {panes[activeTab]}
        </div>
      </div>
    </>
  )
}
