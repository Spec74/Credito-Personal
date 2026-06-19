import { useEffect, useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Alert,
  Button,
  Card,
  Checkbox,
  Col,
  Form,
  Input,
  InputNumber,
  Modal,
  Row,
  Select,
  Space,
  Switch,
  Table,
  Typography,
  Upload,
  message,
} from 'antd'
import type { ColumnsType } from 'antd/es/table'
import { getAccessToken } from '../../auth/tokenStorage'
import { buscarClientes, crearPersonaRapida } from '../../api/clientes'
import { fetchEstadoPlanPago } from '../../api/creditoPlanes'
import {
  actualizarAvalCredito,
  actualizarIrrecuperableCredito,
  actualizarTopeCredito,
  cambiarAnalistaCredito,
  condonarCredito,
  eliminarEvidenciaCredito,
  fetchCargosCredito,
  fetchCreditoContexto,
  fetchCreditoPrenda,
  fetchEvidenciasCredito,
  guardarPrendaCredito,
  guardarCargoCredito,
  modificarCentralRiesgoCredito,
  modificarTramiteAdmCredito,
  observarCredito,
  subirEvidenciaCredito,
  type CargoCreditoRow,
  type CreditoEvidencia,
} from '../../api/creditoGestion'
import { fetchValoresTabla } from '../../api/maestros'
import { fetchUsuariosGestion } from '../../api/usuariosAdmin'
import { ApiError } from '../../api/errors'
import { formatMoney } from '../../utils/formatMoney'
import {
  puedeCambiarAnalistaCreditoUi,
  puedeCondonarCreditoUi,
  puedeEditarTopeCreditoUi,
  puedeEditarTramiteCentralAvalUi,
  puedeOperarCicloCredito,
  tieneCreditoModoLectura,
} from '../../utils/creditoOperacionPermisos'
import { creditoStaleTime } from '../../utils/creditoQueryOptions'
import {
  extractCreditoPrendarioObservacion,
} from '../../utils/creditoPrendario'
import type { EstadoPlanPagoCuota } from '../../types/api'

const { Paragraph, Text } = Typography

function errMsg(e: unknown): string {
  return e instanceof ApiError ? e.message : 'Error desconocido'
}

interface Props {
  creditoId: number
  oficinaId: number
  roles: string[]
  /** Carga datos solo cuando la pestaña Gestión está visible. */
  activo?: boolean
}

export function CreditoConsultaGestionPanel({
  creditoId,
  oficinaId,
  roles,
  activo = true,
}: Props) {
  const soloLectura = tieneCreditoModoLectura(roles)
  const puedeGestion = puedeOperarCicloCredito(roles)
  const puedeCondonar = puedeCondonarCreditoUi(roles)
  const puedeTope = puedeEditarTopeCreditoUi(roles)
  const puedeAnalista = puedeCambiarAnalistaCreditoUi(roles)
  const puedeTramite = puedeEditarTramiteCentralAvalUi(roles)
  const bloqueadoGestion = soloLectura || !puedeGestion
  const spaBase = import.meta.env.BASE_URL || '/'
  const queryClient = useQueryClient()
  const [modalCondonar, setModalCondonar] = useState(false)
  const [modalObservar, setModalObservar] = useState(false)
  const [modalCargo, setModalCargo] = useState(false)
  const [modalNuevoAval, setModalNuevoAval] = useState(false)
  const [montoCxc, setMontoCxc] = useState(0)
  const [montoCond, setMontoCond] = useState(0)
  const [obsCondonar, setObsCondonar] = useState('')
  const [observacion, setObservacion] = useState('')
  const [tipoCargoId, setTipoCargoId] = useState<number | null>(null)
  const [montoCargo, setMontoCargo] = useState(0)
  const [descCargo, setDescCargo] = useState('')
  const [cargoFinal, setCargoFinal] = useState(false)
  const [analistaId, setAnalistaId] = useState<number | null>(null)
  const [tope, setTope] = useState(0)
  const [tramiteAdm, setTramiteAdm] = useState(0)
  const [centralRiesgo, setCentralRiesgo] = useState(0)
  const [personaAvalId, setPersonaAvalId] = useState<number | null>(null)
  const [avalSearch, setAvalSearch] = useState('')
  const [nuevoAvalDni, setNuevoAvalDni] = useState('')
  const [nuevoAvalNombre, setNuevoAvalNombre] = useState('')
  const [nuevoAvalPaterno, setNuevoAvalPaterno] = useState('')
  const [nuevoAvalMaterno, setNuevoAvalMaterno] = useState('')
  const [nuevoAvalCelular, setNuevoAvalCelular] = useState('')
  const [prendario, setPrendario] = useState(false)
  const [descripcionPrenda, setDescripcionPrenda] = useState('')
  const [montoTasacionPrenda, setMontoTasacionPrenda] = useState(0)
  const [fechaRematePrenda, setFechaRematePrenda] = useState('')
  const [observacionPrenda, setObservacionPrenda] = useState('')

  const contexto = useQuery({
    queryKey: ['credito-contexto', creditoId],
    queryFn: () => fetchCreditoContexto(creditoId),
    enabled: activo,
    staleTime: creditoStaleTime.operacion,
  })

  const cargos = useQuery({
    queryKey: ['cargos-credito', oficinaId, creditoId],
    queryFn: () => fetchCargosCredito(oficinaId, creditoId),
    enabled: activo,
    staleTime: creditoStaleTime.operacion,
  })

  const evidencias = useQuery({
    queryKey: ['evidencias-credito', oficinaId, creditoId],
    queryFn: () => fetchEvidenciasCredito(oficinaId, creditoId),
    enabled: activo,
    staleTime: creditoStaleTime.operacion,
  })

  const creditoPrenda = useQuery({
    queryKey: ['credito-prenda', oficinaId, creditoId],
    queryFn: () => fetchCreditoPrenda(oficinaId, creditoId),
    enabled: activo && oficinaId > 0 && creditoId > 0,
    staleTime: creditoStaleTime.operacion,
  })

  const tiposCargo = useQuery({
    queryKey: ['valores-tabla', 2],
    queryFn: () => fetchValoresTabla(2),
    enabled: activo,
    staleTime: creditoStaleTime.master,
  })

  const planPago = useQuery({
    queryKey: ['estado-plan-pago', creditoId],
    queryFn: () => fetchEstadoPlanPago(creditoId),
    enabled: activo && puedeCondonar,
    staleTime: creditoStaleTime.operacion,
  })

  const usuariosGestion = useQuery({
    queryKey: ['usuarios-gestion-credito-ajustes'],
    queryFn: () => fetchUsuariosGestion({ page: 1, pageSize: 500, incluirInactivos: false }),
    enabled: activo && puedeAnalista,
    staleTime: creditoStaleTime.master,
  })

  const buscarAval = useMutation({
    mutationFn: (term: string) => buscarClientes(term),
  })

  const refrescar = () => {
    void queryClient.invalidateQueries({ queryKey: ['credito-contexto', creditoId] })
    void queryClient.invalidateQueries({ queryKey: ['cargos-credito', oficinaId, creditoId] })
    void queryClient.invalidateQueries({ queryKey: ['evidencias-credito', oficinaId, creditoId] })
  }

  useEffect(() => {
    if (!contexto.data) return
    setTramiteAdm(contexto.data.montoGastosAdm ?? 0)
    setCentralRiesgo(contexto.data.centralRiesgo ?? 0)
    setPersonaAvalId(contexto.data.personaAvalId ?? null)
    if (creditoPrenda.data) {
      setPrendario(true)
      setDescripcionPrenda(creditoPrenda.data.descripcion ?? '')
      setMontoTasacionPrenda(creditoPrenda.data.montoTasacion ?? 0)
      setFechaRematePrenda(creditoPrenda.data.fechaRemate?.slice(0, 10) ?? '')
      setObservacionPrenda(creditoPrenda.data.observacion ?? '')
      return
    }
    const prendarioActual = extractCreditoPrendarioObservacion(contexto.data.observacion)
    setPrendario(Boolean(prendarioActual.descripcion))
    setDescripcionPrenda(prendarioActual.descripcion ?? '')
    setMontoTasacionPrenda(prendarioActual.montoTasacion ?? 0)
    setFechaRematePrenda(prendarioActual.fechaRemate ?? '')
    setObservacionPrenda(prendarioActual.observacion ?? '')
  }, [contexto.data, creditoPrenda.data])

  const condonar = useMutation({
    mutationFn: () =>
      condonarCredito({
        oficinaId,
        creditoId,
        montoCxc,
        montoCondonacion: montoCond,
        observacion: obsCondonar,
      }),
    onSuccess: () => {
      message.success('Crédito condonado')
      setModalCondonar(false)
      refrescar()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const observar = useMutation({
    mutationFn: () => observarCredito({ oficinaId, creditoId, observacion }),
    onSuccess: () => {
      message.success('Observación guardada')
      setModalObservar(false)
      refrescar()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const guardarCargo = useMutation({
    mutationFn: () =>
      guardarCargoCredito({
        oficinaId,
        creditoId,
        tipoCargoId: tipoCargoId!,
        monto: montoCargo,
        descripcion: descCargo,
        final: cargoFinal,
      }),
    onSuccess: () => {
      message.success('Cargo registrado')
      setModalCargo(false)
      refrescar()
      void queryClient.invalidateQueries({ queryKey: ['estado-plan-pago', creditoId] })
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const subirImg = useMutation({
    mutationFn: (file: File) => subirEvidenciaCredito(oficinaId, creditoId, file),
    onSuccess: () => {
      message.success('Imagen subida')
      refrescar()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const eliminarImg = useMutation({
    mutationFn: (creditoImagenId: number) =>
      eliminarEvidenciaCredito({ oficinaId, creditoImagenId }),
    onSuccess: () => {
      message.success('Evidencia eliminada')
      refrescar()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const irrecuperable = useMutation({
    mutationFn: (val: boolean) =>
      actualizarIrrecuperableCredito({ oficinaId, creditoId, indIrrecuperable: val }),
    onSuccess: () => {
      message.success('Indicador actualizado')
      refrescar()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const cambiarAnalista = useMutation({
    mutationFn: () =>
      cambiarAnalistaCredito({ oficinaId, creditoId, analistaId: analistaId! }),
    onSuccess: () => {
      message.success('Analista actualizado')
      setAnalistaId(null)
      refrescar()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const actualizarTope = useMutation({
    mutationFn: () =>
      actualizarTopeCredito({
        oficinaId,
        personaId: contexto.data!.personaId,
        topeCredito: tope,
      }),
    onSuccess: () => message.success('Tope de crédito actualizado'),
    onError: (e) => message.error(errMsg(e)),
  })

  const guardarTramite = useMutation({
    mutationFn: () =>
      modificarTramiteAdmCredito({ oficinaId, creditoId, valor: tramiteAdm }),
    onSuccess: () => {
      message.success('Trámite administrativo actualizado')
      refrescar()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const guardarCentral = useMutation({
    mutationFn: () =>
      modificarCentralRiesgoCredito({ oficinaId, creditoId, valor: centralRiesgo }),
    onSuccess: () => {
      message.success('Central de riesgo actualizada')
      refrescar()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const guardarAval = useMutation({
    mutationFn: (avalId: number | null = personaAvalId) =>
      actualizarAvalCredito({ oficinaId, creditoId, personaAvalId: avalId }),
    onSuccess: (_r, avalId) => {
      message.success(avalId == null ? 'Aval quitado' : 'Aval actualizado')
      refrescar()
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const crearNuevoAval = useMutation({
    mutationFn: () =>
      crearPersonaRapida({
        dni: nuevoAvalDni.trim(),
        nombre: nuevoAvalNombre.trim().toUpperCase(),
        apePaterno: nuevoAvalPaterno.trim().toUpperCase(),
        apeMaterno: nuevoAvalMaterno.trim().toUpperCase(),
        celular: nuevoAvalCelular.trim() || null,
      }),
    onSuccess: (r) => {
      setPersonaAvalId(r.personaId)
      setAvalSearch(r.label)
      setModalNuevoAval(false)
      setNuevoAvalDni('')
      setNuevoAvalNombre('')
      setNuevoAvalPaterno('')
      setNuevoAvalMaterno('')
      setNuevoAvalCelular('')
      message.success('Aval creado; asignando al crédito')
      guardarAval.mutate(r.personaId)
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const guardarPrendario = useMutation({
    mutationFn: () =>
      guardarPrendaCredito({
        oficinaId,
        creditoId,
        descripcion: descripcionPrenda,
        montoTasacion: montoTasacionPrenda,
        fechaRemate: fechaRematePrenda,
        observacion: observacionPrenda || null,
      }),
    onSuccess: () => {
      message.success('Crédito prendario guardado')
      setPrendario(true)
      refrescar()
      void queryClient.invalidateQueries({ queryKey: ['credito-prenda', oficinaId, creditoId] })
    },
    onError: (e) => message.error(errMsg(e)),
  })

  const cargoCols: ColumnsType<CargoCreditoRow> = [
    { title: 'Tipo', dataIndex: 'tipoCargo', ellipsis: true },
    { title: 'Cuota', dataIndex: 'numCuota', width: 60 },
    { title: 'Descripción', dataIndex: 'descripcion', ellipsis: true },
    {
      title: 'Importe',
      dataIndex: 'importe',
      align: 'right',
      render: formatMoney,
    },
    { title: 'Estado', dataIndex: 'estado', width: 70 },
  ]

  const ctx = contexto.data
  const analistaOptions = useMemo(
    () =>
      (usuariosGestion.data?.rows ?? []).map((u) => ({
        value: u.usuarioId,
        label: `${u.nombreCompleto || u.nombreUsuario} (#${u.usuarioId})`,
      })),
    [usuariosGestion.data],
  )
  const avalOptions = useMemo(() => {
    const options = (buscarAval.data ?? []).map((c) => ({
      value: c.personaId,
      label: c.label,
    }))
    if (
      ctx?.personaAvalId &&
      ctx.personaAvalNombre &&
      !options.some((o) => o.value === ctx.personaAvalId)
    ) {
      options.unshift({
        value: ctx.personaAvalId,
        label: `${ctx.personaAvalNombre} (#${ctx.personaAvalId})`,
      })
    }
    return options
  }, [buscarAval.data, ctx?.personaAvalId, ctx?.personaAvalNombre])

  const condonacionResumen = useMemo(() => {
    const pendientes = (planPago.data ?? []).filter((c: EstadoPlanPagoCuota) =>
      c.estado?.toUpperCase() === 'PEN',
    )
    const capital = pendientes.reduce((s, c) => s + (c.amortizacion ?? c.capital ?? 0), 0)
    const interes = pendientes.reduce((s, c) => s + (c.interes ?? 0), 0)
    const mora = pendientes.reduce((s, c) => s + (c.importeMora ?? 0), 0)
    const cargos = pendientes.reduce((s, c) => s + (c.cargo ?? 0), 0)
    const descuentos = pendientes.reduce((s, c) => s + (c.descuento ?? 0), 0)
    const total = Math.max(0, capital + interes + mora + cargos - descuentos)
    return { capital, interes, mora, cargos, descuentos, total, cuotas: pendientes.length }
  }, [planPago.data])

  return (
    <>
      {soloLectura ? (
        <Alert
          type="info"
          showIcon
          style={{ marginBottom: 12 }}
          message="Modo solo lectura"
          description="Su rol no permite condonar, observar, cargos ni otras operaciones de gestión."
        />
      ) : null}

      <Card size="small" style={{ marginBottom: 16 }} loading={contexto.isLoading}>
        <Paragraph style={{ marginBottom: 8 }}>
          <Text strong>Cliente:</Text> {ctx?.personaNombre ?? '—'} (persona #{ctx?.personaId})
        </Paragraph>
        {ctx?.observacion ? (
          <Paragraph type="secondary" style={{ marginBottom: 8 }}>
            <Text strong>Observación:</Text> {ctx.observacion}
          </Paragraph>
        ) : null}
        <Space wrap>
          {puedeCondonar ? (
            <Button disabled={bloqueadoGestion} onClick={() => setModalCondonar(true)}>
              Condonar
            </Button>
          ) : null}
          <Button
            disabled={bloqueadoGestion}
            onClick={() => {
              setObservacion(ctx?.observacion ?? '')
              setModalObservar(true)
            }}
          >
            Observar
          </Button>
          <Button disabled={bloqueadoGestion} onClick={() => setModalCargo(true)}>
            Nuevo cargo
          </Button>
          <Switch
            disabled={bloqueadoGestion}
            checkedChildren="Irrecuperable"
            unCheckedChildren="Irrecuperable"
            checked={ctx?.indIrrecuperable ?? false}
            loading={irrecuperable.isPending}
            onChange={(v) => irrecuperable.mutate(v)}
          />
        </Space>
      </Card>

      <Row gutter={[16, 16]}>
        <Col xs={24} lg={12}>
          <Card title="Cargos" size="small">
            <Table<CargoCreditoRow>
              rowKey="cargoId"
              size="small"
              columns={cargoCols}
              dataSource={cargos.data ?? []}
              loading={cargos.isLoading}
              pagination={false}
            />
          </Card>
        </Col>
        <Col xs={24} lg={12}>
          <Card
            title="Evidencias"
            size="small"
            extra={
              <Upload
                disabled={bloqueadoGestion}
                showUploadList={false}
                accept="image/*"
                beforeUpload={(file) => {
                  subirImg.mutate(file)
                  return false
                }}
              >
                <Button size="small" loading={subirImg.isPending}>
                  Subir
                </Button>
              </Upload>
            }
          >
            <Space direction="vertical" style={{ width: '100%' }}>
              {(evidencias.data ?? []).map((ev: CreditoEvidencia) => (
                <EvidenciaThumb
                  key={ev.id}
                  ev={ev}
                  onDelete={() => eliminarImg.mutate(ev.id)}
                  deleting={eliminarImg.isPending}
                />
              ))}
              {!evidencias.isLoading && (evidencias.data?.length ?? 0) === 0 ? (
                <Text type="secondary">Sin imágenes</Text>
              ) : null}
            </Space>
          </Card>
        </Col>
      </Row>

      {puedeTramite || puedeAnalista || puedeTope ? (
        <Card title="Ajustes administrativos" size="small" style={{ marginTop: 16 }}>
          {puedeTramite ? (
            <Row gutter={[16, 16]}>
              <Col xs={24} md={8}>
                <Paragraph strong>Trámite administrativo</Paragraph>
                <InputNumber
                  style={{ width: '100%', marginBottom: 8 }}
                  min={0}
                  precision={2}
                  value={tramiteAdm}
                  onChange={(v) => setTramiteAdm(v ?? 0)}
                />
                <Button
                  block
                  disabled={bloqueadoGestion}
                  loading={guardarTramite.isPending}
                  onClick={() => guardarTramite.mutate()}
                >
                  Guardar trámite adm.
                </Button>
              </Col>
              <Col xs={24} md={8}>
                <Paragraph strong>Central de riesgo</Paragraph>
                <InputNumber
                  style={{ width: '100%', marginBottom: 8 }}
                  min={0}
                  precision={2}
                  value={centralRiesgo}
                  onChange={(v) => setCentralRiesgo(v ?? 0)}
                />
                <Button
                  block
                  disabled={bloqueadoGestion}
                  loading={guardarCentral.isPending}
                  onClick={() => guardarCentral.mutate()}
                >
                  Guardar central riesgo
                </Button>
              </Col>
              <Col xs={24} md={8}>
                <Paragraph strong>Aval del crédito</Paragraph>
                {ctx?.personaAvalNombre ? (
                  <Paragraph type="secondary" style={{ marginBottom: 8 }}>
                    Actual: {ctx.personaAvalNombre}
                  </Paragraph>
                ) : null}
                <Select
                  style={{ width: '100%', marginBottom: 8 }}
                  showSearch
                  allowClear
                  filterOption={false}
                  placeholder="Buscar aval por DNI, código o nombre"
                  notFoundContent={avalSearch.trim().length < 2 ? 'Ingrese al menos 2 caracteres' : null}
                  loading={buscarAval.isPending}
                  options={avalOptions}
                  value={personaAvalId ?? undefined}
                  onSearch={(term) => {
                    setAvalSearch(term)
                    if (term.trim().length >= 2) {
                      buscarAval.mutate(term.trim())
                    }
                  }}
                  onChange={(v) => setPersonaAvalId(v ?? null)}
                />
                <Space direction="vertical" style={{ width: '100%' }}>
                  <Button
                    block
                    disabled={bloqueadoGestion}
                    onClick={() => setModalNuevoAval(true)}
                  >
                    Nuevo aval
                  </Button>
                  <Button
                    block
                    disabled={!personaAvalId}
                    href={
                      personaAvalId
                        ? `${spaBase}informes/reporte-cliente?personaId=${personaAvalId}`
                        : undefined
                    }
                    target="_blank"
                  >
                    Detalle aval
                  </Button>
                  <Button
                    block
                    disabled={bloqueadoGestion}
                    loading={guardarAval.isPending}
                    onClick={() => guardarAval.mutate(personaAvalId)}
                  >
                    Guardar aval
                  </Button>
                  <Button
                    block
                    disabled={bloqueadoGestion}
                    loading={guardarAval.isPending}
                    onClick={() => {
                      setPersonaAvalId(null)
                      guardarAval.mutate(null)
                    }}
                  >
                    Quitar aval
                  </Button>
                </Space>
              </Col>
            </Row>
          ) : null}
          <Row gutter={[16, 16]} style={{ marginTop: 16 }}>
            {puedeAnalista ? (
              <Col xs={24} md={8}>
                <Paragraph strong>Cambiar analista</Paragraph>
                <Select
                  style={{ width: '100%', marginBottom: 8 }}
                  showSearch
                  allowClear
                  optionFilterProp="label"
                  loading={usuariosGestion.isLoading}
                  options={analistaOptions}
                  placeholder="Buscar gestor/analista activo"
                  value={analistaId ?? undefined}
                  onChange={(v) => setAnalistaId(v ?? null)}
                />
                <Button
                  block
                  disabled={bloqueadoGestion || !analistaId}
                  loading={cambiarAnalista.isPending}
                  onClick={() => cambiarAnalista.mutate()}
                >
                  Asignar analista
                </Button>
              </Col>
            ) : null}
            {puedeTope ? (
              <Col xs={24} md={8}>
                <Paragraph strong>Tope de crédito (persona)</Paragraph>
                <InputNumber
                  style={{ width: '100%', marginBottom: 8 }}
                  min={0}
                  value={tope}
                  onChange={(v) => setTope(v ?? 0)}
                />
                <Button
                  block
                  loading={actualizarTope.isPending}
                  disabled={bloqueadoGestion || !ctx?.personaId}
                  onClick={() => actualizarTope.mutate()}
                >
                  Guardar tope
                </Button>
              </Col>
            ) : null}
          </Row>
        </Card>
      ) : null}

      {puedeTramite ? (
        <Card title="Crédito prendario" size="small" style={{ marginTop: 16 }}>
          <Alert
            type="info"
            showIcon
            style={{ marginBottom: 12 }}
            message="Registro de prenda"
            description="La información se guarda en CREDITO.CreditoPrenda. Si existe un bloque antiguo en Observación, se usa solo como fallback para precargar."
          />
          <Row gutter={[16, 16]}>
            <Col xs={24} md={6}>
              <Paragraph strong>Marcar como prendario</Paragraph>
              <Switch
                checkedChildren="Prendario"
                unCheckedChildren="Normal"
                checked={prendario}
                disabled={bloqueadoGestion}
                onChange={setPrendario}
              />
            </Col>
            <Col xs={24} md={18}>
              <Paragraph strong>Descripción de prenda</Paragraph>
              <Input
                disabled={bloqueadoGestion || !prendario}
                placeholder="Ej. joyas, electrodoméstico, herramienta, vehículo menor"
                value={descripcionPrenda}
                onChange={(e) => setDescripcionPrenda(e.target.value)}
              />
            </Col>
            <Col xs={24} md={8}>
              <Paragraph strong>Monto tasación</Paragraph>
              <InputNumber
                style={{ width: '100%' }}
                min={0}
                precision={2}
                disabled={bloqueadoGestion || !prendario}
                value={montoTasacionPrenda}
                onChange={(v) => setMontoTasacionPrenda(v ?? 0)}
              />
            </Col>
            <Col xs={24} md={8}>
              <Paragraph strong>Fecha remate</Paragraph>
              <Input
                type="date"
                disabled={bloqueadoGestion || !prendario}
                value={fechaRematePrenda}
                onChange={(e) => setFechaRematePrenda(e.target.value)}
              />
            </Col>
            <Col xs={24} md={8}>
              <Paragraph strong>Observación prenda</Paragraph>
              <Input
                disabled={bloqueadoGestion || !prendario}
                value={observacionPrenda}
                onChange={(e) => setObservacionPrenda(e.target.value)}
              />
            </Col>
          </Row>
          <Button
            type="primary"
            style={{ marginTop: 12 }}
            disabled={
              bloqueadoGestion ||
              !prendario ||
              !descripcionPrenda.trim() ||
              montoTasacionPrenda <= 0 ||
              !fechaRematePrenda
            }
            loading={guardarPrendario.isPending}
            onClick={() => guardarPrendario.mutate()}
          >
            Guardar prendario
          </Button>
        </Card>
      ) : null}

      <Modal
        title="Nuevo aval"
        open={modalNuevoAval}
        onCancel={() => setModalNuevoAval(false)}
        onOk={() => {
          if (nuevoAvalDni.trim().length !== 8) {
            message.warning('Ingrese DNI de 8 dígitos')
            return
          }
          if (!nuevoAvalNombre.trim() || !nuevoAvalPaterno.trim() || !nuevoAvalMaterno.trim()) {
            message.warning('Complete nombres y apellidos del aval')
            return
          }
          crearNuevoAval.mutate()
        }}
        confirmLoading={crearNuevoAval.isPending || guardarAval.isPending}
        okText="Crear y asignar"
      >
        <Paragraph type="secondary">
          Paridad con <strong>Nuevo Aval</strong> del MVC. Crea la persona y la asigna como aval
          del crédito actual.
        </Paragraph>
        <Form layout="vertical">
          <Form.Item label="DNI">
            <Input
              maxLength={8}
              value={nuevoAvalDni}
              onChange={(e) => setNuevoAvalDni(e.target.value.replace(/\D/g, ''))}
            />
          </Form.Item>
          <Form.Item label="Nombres">
            <Input value={nuevoAvalNombre} onChange={(e) => setNuevoAvalNombre(e.target.value)} />
          </Form.Item>
          <Row gutter={12}>
            <Col span={12}>
              <Form.Item label="Apellido paterno">
                <Input
                  value={nuevoAvalPaterno}
                  onChange={(e) => setNuevoAvalPaterno(e.target.value)}
                />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item label="Apellido materno">
                <Input
                  value={nuevoAvalMaterno}
                  onChange={(e) => setNuevoAvalMaterno(e.target.value)}
                />
              </Form.Item>
            </Col>
          </Row>
          <Form.Item label="Celular">
            <Input
              maxLength={15}
              value={nuevoAvalCelular}
              onChange={(e) => setNuevoAvalCelular(e.target.value)}
            />
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        title="Observar crédito"
        open={modalObservar}
        onCancel={() => setModalObservar(false)}
        onOk={() => observar.mutate()}
        confirmLoading={observar.isPending}
      >
        <Input.TextArea
          rows={4}
          value={observacion}
          onChange={(e) => setObservacion(e.target.value)}
        />
      </Modal>

      <Modal
        title="Condonar crédito"
        open={modalCondonar}
        onCancel={() => setModalCondonar(false)}
        onOk={() => condonar.mutate()}
        confirmLoading={condonar.isPending}
      >
        <Space direction="vertical" style={{ width: '100%' }}>
          <Alert
            type="info"
            showIcon
            message="Desglose sugerido de deuda pendiente"
            description={
              planPago.isLoading
                ? 'Calculando cuotas pendientes...'
                : `Capital ${formatMoney(condonacionResumen.capital)} · Interés ${formatMoney(
                    condonacionResumen.interes,
                  )} · Mora ${formatMoney(condonacionResumen.mora)} · Cargos ${formatMoney(
                    condonacionResumen.cargos,
                  )} · Descuentos ${formatMoney(condonacionResumen.descuentos)}`
            }
          />
          <Row gutter={[8, 8]}>
            <Col span={8}>
              <Card size="small">
                <Text type="secondary">Cuotas pendientes</Text>
                <Paragraph strong style={{ marginBottom: 0 }}>
                  {condonacionResumen.cuotas}
                </Paragraph>
              </Card>
            </Col>
            <Col span={8}>
              <Card size="small">
                <Text type="secondary">Total deuda</Text>
                <Paragraph strong style={{ marginBottom: 0 }}>
                  {formatMoney(condonacionResumen.total)}
                </Paragraph>
              </Card>
            </Col>
            <Col span={8}>
              <Card size="small">
                <Text type="secondary">Mora + cargos</Text>
                <Paragraph strong style={{ marginBottom: 0 }}>
                  {formatMoney(condonacionResumen.mora + condonacionResumen.cargos)}
                </Paragraph>
              </Card>
            </Col>
          </Row>
          <Space wrap>
            <Button
              size="small"
              onClick={() => {
                setMontoCxc(condonacionResumen.total)
                setMontoCond(condonacionResumen.total)
              }}
            >
              Condonar total sugerido
            </Button>
            <Button
              size="small"
              onClick={() => {
                const monto = condonacionResumen.mora + condonacionResumen.cargos
                setMontoCxc(monto)
                setMontoCond(monto)
              }}
            >
              Solo mora y cargos
            </Button>
          </Space>
          <Form.Item label="Monto CxC">
            <InputNumber
              style={{ width: '100%' }}
              min={0}
              value={montoCxc}
              onChange={(v) => setMontoCxc(v ?? 0)}
            />
          </Form.Item>
          <Form.Item label="Monto condonación">
            <InputNumber
              style={{ width: '100%' }}
              min={0}
              value={montoCond}
              onChange={(v) => setMontoCond(v ?? 0)}
            />
          </Form.Item>
          <Input.TextArea
            rows={2}
            placeholder="Observación"
            value={obsCondonar}
            onChange={(e) => setObsCondonar(e.target.value)}
          />
        </Space>
      </Modal>

      <Modal
        title="Nuevo cargo"
        open={modalCargo}
        onCancel={() => setModalCargo(false)}
        onOk={() => {
          if (!tipoCargoId) {
            message.warning('Seleccione tipo de cargo')
            return
          }
          guardarCargo.mutate()
        }}
        confirmLoading={guardarCargo.isPending}
      >
        <Select
          style={{ width: '100%', marginBottom: 12 }}
          placeholder="Tipo de cargo"
          options={(tiposCargo.data ?? []).map((t) => ({
            value: t.itemId,
            label: t.denominacion,
          }))}
          value={tipoCargoId ?? undefined}
          onChange={setTipoCargoId}
        />
        <InputNumber
          style={{ width: '100%', marginBottom: 12 }}
          min={0}
          placeholder="Monto"
          value={montoCargo}
          onChange={(v) => setMontoCargo(v ?? 0)}
        />
        <Input
          style={{ marginBottom: 12 }}
          placeholder="Descripción"
          value={descCargo}
          onChange={(e) => setDescCargo(e.target.value)}
        />
        <Checkbox checked={cargoFinal} onChange={(e) => setCargoFinal(e.target.checked)}>
          Aplicar a última cuota pendiente
        </Checkbox>
      </Modal>
    </>
  )
}

function EvidenciaThumb({
  ev,
  onDelete,
  deleting,
}: {
  ev: CreditoEvidencia
  onDelete: () => void
  deleting: boolean
}) {
  const [src, setSrc] = useState<string | null>(null)
  const base = import.meta.env.VITE_API_BASE_URL as string
  const path = ev.url?.replace('/api/v1', '') ?? `/credito/evidencia-archivo/${ev.id}`

  useEffect(() => {
    let objectUrl: string | null = null
    const token = getAccessToken()
    if (!token) return

    void fetch(`${base}${path}`, {
      headers: { Authorization: `Bearer ${token}`, Accept: 'image/*' },
    })
      .then((r) => (r.ok ? r.blob() : null))
      .then((blob) => {
        if (blob) {
          objectUrl = URL.createObjectURL(blob)
          setSrc(objectUrl)
        }
      })

    return () => {
      if (objectUrl) URL.revokeObjectURL(objectUrl)
    }
  }, [base, path, ev.id])

  return (
    <Space>
      {src ? (
        <a href={src} target="_blank" rel="noreferrer">
          <img src={src} alt={ev.imagen} style={{ maxHeight: 80, maxWidth: 120 }} />
        </a>
      ) : (
        <Text>{ev.imagen}</Text>
      )}
      <Button size="small" danger loading={deleting} onClick={onDelete}>
        Eliminar
      </Button>
    </Space>
  )
}
