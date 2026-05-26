import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { AuditOutlined, CheckCircleOutlined } from '@ant-design/icons'
import { Alert, Checkbox, DatePicker, InputNumber, Select, Space, message } from 'antd'
import dayjs, { type Dayjs } from 'dayjs'
import { useAuth } from '../../auth/useAuth'
import { CredixPage } from '../../components/credix'
import { CredixReportBox } from '../../components/reportes/CredixReportBox'
import { ReportExportActions } from '../../components/reportes/ReportExportActions'
import {
  GestorSelect,
  OficinaSelect,
  ReporteField,
} from '../../components/reportes/ReporteFiltrosMaestros'
import {
  downloadClientesBloqueadosPdf,

  downloadClientesInactivosPdfGestor,
  downloadClientesNuevosMesPdf,
  downloadClientesNuevosMesPdfGestor,
  downloadClientesInactivosPdf,
  downloadClientesTopeCreditoPdf,
  downloadCobroDiarioCsv,
  downloadCobroDiarioPdf,
  downloadCreditoObservadoCsv,
  downloadCreditoObservadoPdf,
} from '../../api/creditoPlanes'
import {
  openLegacyCentralRiesgoTxt,
  openLegacyComprobantesCajaAnulados,
  openLegacyComprobantesCajaChica,
  openLegacyCreditoAprobacion,
  openLegacyCreditoCondonado,
  openLegacyCreditoMorosidad,
  openLegacyCreditoRentabilidad,
  openLegacyCreditosActivos,
  openLegacyCreditosCierres,
  openLegacyCreditosMorososPagados,
  openLegacyCajaDiarioInforme,
  openLegacyReporteCredito,
  openLegacySaldoCarteraCajaDiario,
  type InformeRangoGestorLegacyParams,
} from '../../config/legacyReportUrls'
import {
  toGestorInformeParams,
  toCobroDiarioQuery,
  toClientesInactivosParams,
  toClientesNuevosMesParams,
} from '../../utils/gestorInformeForm'
import {
  canViewReporteCredito,
  canViewReporteCreditoAdmin,
  canViewReporteCreditoAprobador,
} from '../../utils/reporteCreditoAccess'
import { reportesCreditoIndexBreadcrumb } from '../../utils/reportesBreadcrumbs'
import { buildInformeScreenQuery } from '../../utils/informeScreenParams'
import { runOpenReport } from '../../utils/reportExport'

const ESTADOS_CREDITO = [
  { value: 'CRE', label: 'Solicitud crédito' },
  { value: 'PEN', label: 'Pendiente' },
  { value: 'DES', label: 'Desembolsado' },
  { value: 'PAG', label: 'Pagado' },
  { value: 'REP', label: 'Reprogramado' },
  { value: 'ANU', label: 'Anulado' },
]

const MESES = [
  { value: 1, label: 'Enero' },
  { value: 2, label: 'Febrero' },
  { value: 3, label: 'Marzo' },
  { value: 4, label: 'Abril' },
  { value: 5, label: 'Mayo' },
  { value: 6, label: 'Junio' },
  { value: 7, label: 'Julio' },
  { value: 8, label: 'Agosto' },
  { value: 9, label: 'Septiembre' },
  { value: 10, label: 'Octubre' },
  { value: 11, label: 'Noviembre' },
  { value: 12, label: 'Diciembre' },
]

function monthRangeDefaults(): [Dayjs, Dayjs] {
  const now = dayjs()
  return [now.startOf('month'), now.endOf('month')]
}

function legacyDate(d: Dayjs): string {
  return d.format('DD/MM/YYYY')
}

function legacyRango(params: {
  oficinaId: number
  usuarioId?: number
  fechaIni: string
  fechaFin: string
}): InformeRangoGestorLegacyParams {
  return {
    oficinaId: params.oficinaId,
    usuarioId: params.usuarioId,
    fechaIni: legacyDate(dayjs(params.fechaIni)),
    fechaFin: legacyDate(dayjs(params.fechaFin)),
  }
}

export function ReporteCreditoIndexPage() {
  const { session } = useAuth()
  const roles = session?.roles ?? []
  const oficinaSesion = session?.oficinaId ?? 0

  const [moraOficina, setMoraOficina] = useState<number | undefined>(oficinaSesion)
  const [moraHasta, setMoraHasta] = useState(dayjs())
  const [moraIni, setMoraIni] = useState(1)
  const [moraFin, setMoraFin] = useState(9999)

  const [aprobOficina, setAprobOficina] = useState<number | undefined>(oficinaSesion)
  const [aprobGestor, setAprobGestor] = useState<number | undefined>()
  const [aprobFecha, setAprobFecha] = useState(dayjs())

  const [gestorOficina, setGestorOficina] = useState<number | undefined>(oficinaSesion)
  const [gestorUsuario, setGestorUsuario] = useState<number | undefined>()

  const [rptOficina, setRptOficina] = useState<number | undefined>(oficinaSesion)
  const [rptGestor, setRptGestor] = useState<number | undefined>()
  const [rptEstado, setRptEstado] = useState('CRE')
  const [rentabilidadTodos, setRentabilidadTodos] = useState(false)
  const [rptRango, setRptRango] = useState<[Dayjs, Dayjs]>(monthRangeDefaults)

  const [variosOficina, setVariosOficina] = useState<number | undefined>(oficinaSesion)
  const [variosGestor, setVariosGestor] = useState<number | undefined>()
  const [variosRango, setVariosRango] = useState<[Dayjs, Dayjs]>(monthRangeDefaults)

  const [cajaChicaRango, setCajaChicaRango] = useState<[Dayjs, Dayjs]>(monthRangeDefaults)
  const [anuladoRango, setAnuladoRango] = useState<[Dayjs, Dayjs]>(monthRangeDefaults)
  const [saldoOficina, setSaldoOficina] = useState<number | undefined>(oficinaSesion)
  const [saldoAnioIni, setSaldoAnioIni] = useState(dayjs().subtract(1, 'month').year())
  const [saldoMesIni, setSaldoMesIni] = useState(dayjs().subtract(1, 'month').month() + 1)
  const [saldoAnioFin, setSaldoAnioFin] = useState(dayjs().year())
  const [saldoMesFin, setSaldoMesFin] = useState(dayjs().month() + 1)
  const [riesgoOficina, setRiesgoOficina] = useState<number | undefined>(oficinaSesion)
  const [riesgoAnio, setRiesgoAnio] = useState(dayjs().year())
  const [riesgoMes, setRiesgoMes] = useState(dayjs().month() + 1)

  const gestorParams = useMemo(
    () => ({
      oficinaId: gestorOficina ?? oficinaSesion,
      usuarioId:
        gestorUsuario != null && gestorUsuario > 0 ? gestorUsuario : undefined,
    }),
    [gestorOficina, gestorUsuario, oficinaSesion],
  )

  const gestorScreenParams = useMemo(
    () => ({
      oficinaId: gestorParams.oficinaId,
      usuarioId: gestorParams.usuarioId,
      pOficinaId: gestorParams.oficinaId,
      pUsuarioId: gestorParams.usuarioId,
    }),
    [gestorParams.oficinaId, gestorParams.usuarioId],
  )

  const gestorCobroScreenParams = useMemo(
    () =>
      gestorParams.usuarioId != null && gestorParams.usuarioId > 0
        ? gestorScreenParams
        : undefined,
    [gestorParams.usuarioId, gestorScreenParams],
  )

  const gestorNuevosScreenParams = useMemo(() => {
    const ini = dayjs().startOf('month').format('YYYY-MM-DD')
    const fin = dayjs().endOf('month').format('YYYY-MM-DD')
    return {
      ...gestorScreenParams,
      fechaIni: ini,
      fechaFin: fin,
      pFechaIni: ini,
      pFechaFin: fin,
    }
  }, [gestorScreenParams])

  const gestorInactivosScreenParams = useMemo(
    () => ({
      ...gestorScreenParams,
      sinRango: '1',
    }),
    [gestorScreenParams],
  )

  const gestorApiParams = useMemo(
    () => toGestorInformeParams(gestorParams.oficinaId, gestorParams.usuarioId),
    [gestorParams.oficinaId, gestorParams.usuarioId],
  )

  const variosScreenParams = useMemo(() => {
    const oficinaId = variosOficina ?? oficinaSesion
    const usuarioId =
      variosGestor != null && variosGestor > 0 ? variosGestor : undefined
    const fechaIni = variosRango[0].format('YYYY-MM-DD')
    const fechaFin = variosRango[1].format('YYYY-MM-DD')
    return {
      oficinaId,
      usuarioId,
      pOficinaId: oficinaId,
      pUsuarioId: usuarioId,
      fechaIni,
      fechaFin,
      pFechaIni: fechaIni,
      pFechaFin: fechaFin,
    }
  }, [variosOficina, variosGestor, variosRango, oficinaSesion])







  const saldoAnios = useMemo(
    () => Array.from({ length: 11 }, (_, i) => 2022 + i),
    [],
  )

  const variosLegacy = useMemo(
    () =>
      legacyRango({
        oficinaId: variosOficina ?? oficinaSesion,
        usuarioId: variosGestor,
        fechaIni: variosRango[0].format('YYYY-MM-DD'),
        fechaFin: variosRango[1].format('YYYY-MM-DD'),
      }),
    [variosOficina, variosGestor, variosRango, oficinaSesion],
  )

  if (!canViewReporteCredito(roles)) {
    return (
      <CredixPage
        title="Reportes de crédito"
        breadcrumb={[
          { title: <Link to="/inicio">Inicio</Link> },
          { title: 'Reportes' },
          { title: 'Crédito' },
        ]}
      >
        <Alert
          type="warning"
          showIcon
          message="Sin permiso"
          description="Esta pantalla está disponible para roles ADMIN, APROBADOR o PARCIAL, igual que en el sistema anterior."
        />
      </CredixPage>
    )
  }

  const showVarios = canViewReporteCreditoAprobador(roles)
  const showAdmin = canViewReporteCreditoAdmin(roles)

  return (
    <CredixPage
      title="Reportes de crédito"
      subtitle="Informes por oficina y gestor. Use Ver pantalla para consultar en tabla; PDF y Excel (CSV) descargan con su sesión."
      breadcrumb={reportesCreditoIndexBreadcrumb()}
    >
      <p className="credix-reportes-intro">
        Operaciones diarias en <Link to="/credito">Crédito → Operaciones</Link>.{' '}
        <strong>Ver pantalla</strong> abre el informe moderno (filtros, búsqueda en tabla, exportación
        rápida). Los botones PDF / Excel generan el mismo dataset que la tabla; Excel es CSV UTF-8
        salvo <Link to="/reportes/cobranza">Cobranza pagos</Link> (.xlsx nativo). Colores y columnas
        siguen la marca Credix (#114885).
      </p>

      <div className="credix-reporte-grid">
        <CredixReportBox
          title="Reporte morosidad"
          icon={<AuditOutlined />}
          actions={
            <ReportExportActions
              screenTo="/informes/credito-morosidad"
              screenSearchParams={{
                oficinaId: moraOficina ?? oficinaSesion,
                hastaFecha: moraHasta.format('YYYY-MM-DD'),
                diasAtrazoIni: moraIni,
                diasAtrazoFin: moraFin,
              }}
              exports={[
                {
                  label: 'PDF morosidad',
                  format: 'pdf',
                  onClick: () =>
                    runOpenReport('Morosidad PDF', () =>
                      openLegacyCreditoMorosidad({
                        oficinaId: moraOficina ?? 0,
                        hastaFecha: legacyDate(moraHasta),
                        diasAtrazoIni: moraIni,
                        diasAtrazoFin: moraFin,
                      }),
                    ),
                },
              ]}
            />
          }
        >
          <ReporteField label="Oficina">
            <OficinaSelect allowAll value={moraOficina} onChange={setMoraOficina} />
          </ReporteField>
          <ReporteField label="Hasta la fecha">
            <DatePicker
              size="small"
              value={moraHasta}
              onChange={(d) => d && setMoraHasta(d)}
              format="DD/MM/YYYY"
              style={{ width: '100%' }}
            />
          </ReporteField>
          <ReporteField label="Días atraso inicio">
            <InputNumber size="small" min={0} value={moraIni} onChange={(v) => setMoraIni(v ?? 1)} style={{ width: '100%' }} />
          </ReporteField>
          <ReporteField label="Días atraso final">
            <InputNumber size="small" min={0} value={moraFin} onChange={(v) => setMoraFin(v ?? 9999)} style={{ width: '100%' }} />
          </ReporteField>
        </CredixReportBox>

        <CredixReportBox
          title="Créditos aprobados"
          icon={<CheckCircleOutlined />}
          actions={
            <ReportExportActions
              screenTo="/informes/credito-aprobacion"
              screenSearchParams={{
                oficinaId: aprobOficina ?? oficinaSesion,
                usuarioId: aprobGestor,
                fechaAprobacion: aprobFecha.format('YYYY-MM-DD'),
              }}
              exports={[
                {
                  label: 'PDF',
                  format: 'pdf',
                  onClick: () =>
                    runOpenReport('Aprobados PDF', () =>
                      openLegacyCreditoAprobacion({
                        oficinaId: aprobOficina ?? oficinaSesion,
                        usuarioId: aprobGestor,
                        fecha: legacyDate(aprobFecha),
                        formato: 'PDF',
                      }),
                    ),
                },
                {
                  label: 'XLS',
                  format: 'xls',
                  onClick: () =>
                    runOpenReport('Aprobados Excel', () =>
                      openLegacyCreditoAprobacion({
                        oficinaId: aprobOficina ?? oficinaSesion,
                        usuarioId: aprobGestor,
                        fecha: legacyDate(aprobFecha),
                        formato: 'Excel',
                      }),
                    ),
                },
              ]}
            />
          }
        >
          <ReporteField label="Oficina">
            <OficinaSelect allowAll value={aprobOficina} onChange={setAprobOficina} />
          </ReporteField>
          <ReporteField label="Gestor">
            <GestorSelect allowAll value={aprobGestor} onChange={setAprobGestor} />
          </ReporteField>
          <ReporteField label="Fecha aprobación">
            <DatePicker
              size="small"
              value={aprobFecha}
              onChange={(d) => d && setAprobFecha(d)}
              format="DD/MM/YYYY"
              style={{ width: '100%' }}
            />
          </ReporteField>
        </CredixReportBox>

        <CredixReportBox
          title="Cobro diario, observados y morosos por gestor"
          className="credix-report-card--wide"
          actions={
            <ReportExportActions
              screenTo="/informes/morosidad-gestor"
              screenLabel="Morosidad en pantalla"
              screenSearchParams={gestorScreenParams}
              screenLinks={[
                {
                  label: 'Clientes nuevos',
                  to: '/informes/clientes-nuevos-mes',
                  searchParams: gestorNuevosScreenParams,
                },
                {
                  label: 'Inactivos',
                  to: '/informes/clientes-inactivos',
                  searchParams: gestorInactivosScreenParams,
                },
                {
                  label: 'Bloqueados',
                  to: '/informes/clientes-bloqueados',
                  searchParams: gestorScreenParams,
                },
                {
                  label: 'Tope crédito',
                  to: '/informes/clientes-tope-credito',
                  searchParams: gestorScreenParams,
                },
              ]}
              extra={
                gestorCobroScreenParams ? (
                  <span className="credix-report-screen-links">
                    <Link
                      to={{
                        pathname: '/informes/cobro-diario',
                        search: buildInformeScreenQuery(gestorCobroScreenParams).slice(1),
                      }}
                    >
                      Cobro diario
                    </Link>
                    <Link
                      to={{
                        pathname: '/informes/creditos-observados',
                        search: buildInformeScreenQuery(gestorScreenParams).slice(1),
                      }}
                    >
                      Observados
                    </Link>
                  </span>
                ) : (
                  <span className="credix-report-screen-links">
                    <Link
                      to={{
                        pathname: '/informes/creditos-observados',
                        search: buildInformeScreenQuery(gestorScreenParams).slice(1),
                      }}
                    >
                      Observados
                    </Link>
                  </span>
                )
              }
              exports={[
                {
                  label: 'Cobro diario PDF',
                  format: 'pdf',
                  title: 'Requiere gestor seleccionado',
                  disabled: !gestorUsuario,
                  onClick: () => {
                    if (!gestorUsuario) {
                      message.warning('Seleccione un gestor para cobro diario')
                      return
                    }
                    runOpenReport('Cobro diario PDF', () =>
                      downloadCobroDiarioPdf(
                        toCobroDiarioQuery(gestorParams.oficinaId, gestorUsuario),
                      ),
                    )
                  },
                },
                {
                  label: 'Cobro diario XLS',
                  format: 'xls',
                  disabled: !gestorUsuario,
                  onClick: () => {
                    if (!gestorUsuario) {
                      message.warning('Seleccione un gestor')
                      return
                    }
                    runOpenReport('Cobro diario XLS', () =>
                      downloadCobroDiarioCsv(
                        toCobroDiarioQuery(gestorParams.oficinaId, gestorUsuario),
                      ),
                    )
                  },
                },
                {
                  label: 'Morosidad PDF',
                  format: 'pdf',
                  onClick: () =>
                    runOpenReport('Morosidad gestor', () =>
                      downloadCobroDiarioPdf(
                        toCobroDiarioQuery(
                          gestorParams.oficinaId,
                          gestorParams.usuarioId,
                          true,
                        ),
                      ),
                    ),
                },
                {
                  label: 'Morosidad XLS',
                  format: 'xls',
                  onClick: () =>
                    runOpenReport('Morosidad gestor XLS', () =>
                      downloadCobroDiarioCsv(
                        toCobroDiarioQuery(
                          gestorParams.oficinaId,
                          gestorParams.usuarioId,
                          true,
                        ),
                      ),
                    ),
                },
                {
                  label: 'Obs PDF',
                  format: 'pdf',
                  onClick: () =>
                    runOpenReport('Observados PDF', () =>
                      downloadCreditoObservadoPdf(gestorApiParams),
                    ),
                },
                {
                  label: 'Obs XLS',
                  format: 'xls',
                  onClick: () =>
                    runOpenReport('Observados XLS', () =>
                      downloadCreditoObservadoCsv(gestorApiParams),
                    ),
                },
                {
                  label: 'Clientes nuevos',
                  format: 'pdf',
                  onClick: () =>
                    runOpenReport('Clientes nuevos', () =>
                      downloadClientesNuevosMesPdfGestor({
                        oficinaId: gestorParams.oficinaId,
                        usuarioId: gestorParams.usuarioId,
                      }),
                    ),
                },
                {
                  label: 'Clientes inactivos',
                  format: 'pdf',
                  onClick: () =>
                    runOpenReport('Clientes inactivos', () =>
                      downloadClientesInactivosPdfGestor({
                        oficinaId: gestorParams.oficinaId,
                        usuarioId: gestorParams.usuarioId,
                      }),
                    ),
                },
                {
                  label: 'Bloqueados',
                  format: 'pdf',
                  onClick: () =>
                    runOpenReport('Clientes bloqueados', () =>
                      downloadClientesBloqueadosPdf(gestorApiParams),
                    ),
                },
                {
                  label: 'Tope crédito',
                  format: 'pdf',
                  onClick: () =>
                    runOpenReport('Tope crédito', () =>
                      downloadClientesTopeCreditoPdf(gestorApiParams),
                    ),
                },
              ]}
            />
          }
        >
          <ReporteField label="Oficina">
            <OficinaSelect allowAll value={gestorOficina} onChange={setGestorOficina} />
          </ReporteField>
          <ReporteField label="Gestor">
            <GestorSelect allowAll value={gestorUsuario} onChange={setGestorUsuario} />
          </ReporteField>
        </CredixReportBox>

        <CredixReportBox
          title="Reporte créditos"
          actions={
            <ReportExportActions
              screenTo="/informes/reporte-creditos"
              extra={
                <Checkbox
                  checked={rentabilidadTodos}
                  onChange={(e) => setRentabilidadTodos(e.target.checked)}
                >
                  TODOS (rentabilidad)
                </Checkbox>
              }
              exports={[
                {
                  label: 'Reporte créditos',
                  format: 'primary',
                  onClick: () =>
                    runOpenReport('Reporte créditos', () =>
                      openLegacyReporteCredito({
                        oficinaId: rptOficina ?? oficinaSesion,
                        gestorId: rptGestor,
                        estadoCredito: rptEstado,
                        fechaIni: legacyDate(rptRango[0]),
                        fechaFin: legacyDate(rptRango[1]),
                      }),
                    ),
                },
                {
                  label: 'Rentabilidad PDF',
                  format: 'pdf',
                  onClick: () =>
                    runOpenReport('Rentabilidad PDF', () =>
                      openLegacyCreditoRentabilidad({
                        oficinaId: rptOficina ?? oficinaSesion,
                        fechaIni: rentabilidadTodos
                          ? '01/01/2018'
                          : legacyDate(rptRango[0]),
                        fechaFin: rentabilidadTodos
                          ? legacyDate(dayjs())
                          : legacyDate(rptRango[1]),
                        estadoCredito: rptEstado,
                        indTodos: rentabilidadTodos,
                        formato: 'PDF',
                      }),
                    ),
                },
                {
                  label: 'Rentabilidad XLS',
                  format: 'xls',
                  onClick: () =>
                    runOpenReport('Rentabilidad XLS', () =>
                      openLegacyCreditoRentabilidad({
                        oficinaId: rptOficina ?? oficinaSesion,
                        fechaIni: rentabilidadTodos
                          ? '01/01/2018'
                          : legacyDate(rptRango[0]),
                        fechaFin: rentabilidadTodos
                          ? legacyDate(dayjs())
                          : legacyDate(rptRango[1]),
                        estadoCredito: rptEstado,
                        indTodos: rentabilidadTodos,
                        formato: 'Excel',
                      }),
                    ),
                },
              ]}
            />
          }
        >
          <ReporteField label="Oficina">
            <OficinaSelect allowAll value={rptOficina} onChange={setRptOficina} />
          </ReporteField>
          <ReporteField label="Gestor">
            <GestorSelect allowAll value={rptGestor} onChange={setRptGestor} />
          </ReporteField>
          <ReporteField label="Estado">
            <Select size="small" options={ESTADOS_CREDITO} value={rptEstado} onChange={setRptEstado} style={{ width: '100%' }} />
          </ReporteField>
          <ReporteField label="Fechas">
            <DatePicker.RangePicker
              size="small"
              value={rptRango}
              onChange={(v) => v && setRptRango(v as [Dayjs, Dayjs])}
              format="DD/MM/YYYY"
              style={{ width: '100%' }}
            />
          </ReporteField>
        </CredixReportBox>

        {showVarios ? (
          <CredixReportBox
            title="Reporte varios"
            className="credix-report-card--wide"
            actions={
              <ReportExportActions
                screenTo="/informes/creditos-activos"
                screenLabel="Créditos activos"
                extra={
                  <span className="credix-report-screen-links">
                    <Link
                      to={{
                        pathname: '/informes/clientes-nuevos-mes',
                        search: new URLSearchParams(
                          Object.entries(variosScreenParams)
                            .filter(([, v]) => v != null)
                            .map(([k, v]) => [k, String(v)]),
                        ).toString(),
                      }}
                    >
                      Clientes nuevos
                    </Link>
                    <Link
                      to={{
                        pathname: '/informes/clientes-inactivos',
                        search: buildInformeScreenQuery(variosScreenParams).slice(1),
                      }}
                    >
                      Inactivos (con rango)
                    </Link>
                    <Link
                      to={{
                        pathname: '/informes/clientes-bloqueados',
                        search: buildInformeScreenQuery(variosScreenParams).slice(1),
                      }}
                    >
                      Bloqueados
                    </Link>
                    <Link
                      to={{
                        pathname: '/informes/clientes-tope-credito',
                        search: buildInformeScreenQuery(variosScreenParams).slice(1),
                      }}
                    >
                      Clientes con tope crédito
                    </Link>
                  </span>
                }
                exports={[
                  ...(showAdmin
                    ? [
                        {
                          label: 'Condonación PDF',
                          format: 'pdf' as const,
                          onClick: () =>
                            runOpenReport('Condonación PDF', () =>
                              openLegacyCreditoCondonado(variosLegacy, 'PDF'),
                            ),
                        },
                        {
                          label: 'Condonación XLS',
                          format: 'xls' as const,
                          onClick: () =>
                            runOpenReport('Condonación XLS', () =>
                              openLegacyCreditoCondonado(variosLegacy, 'Excel'),
                            ),
                        },
                        {
                          label: 'Activos XLS',
                          format: 'xls' as const,
                          onClick: () =>
                            runOpenReport('Activos XLS', () =>
                              openLegacyCreditosActivos({
                                ...variosLegacy,
                                formato: 'Excel',
                              }),
                            ),
                        },
                        {
                          label: 'Cierre PDF',
                          format: 'pdf' as const,
                          onClick: () =>
                            runOpenReport('Cierre PDF', () =>
                              openLegacyCreditosCierres({
                                ...variosLegacy,
                                formato: 'PDF',
                              }),
                            ),
                        },
                        {
                          label: 'Cierre XLS',
                          format: 'xls' as const,
                          onClick: () =>
                            runOpenReport('Cierre XLS', () =>
                              openLegacyCreditosCierres({
                                ...variosLegacy,
                                formato: 'Excel',
                              }),
                            ),
                        },
                        {
                          label: 'Morosos pagados PDF',
                          format: 'pdf' as const,
                          onClick: () =>
                            runOpenReport('Morosos pagados PDF', () =>
                              openLegacyCreditosMorososPagados({
                                ...variosLegacy,
                                formato: 'PDF',
                              }),
                            ),
                        },
                        {
                          label: 'Morosos pagados XLS',
                          format: 'xls' as const,
                          onClick: () =>
                            runOpenReport('Morosos pagados XLS', () =>
                              openLegacyCreditosMorososPagados({
                                ...variosLegacy,
                                formato: 'Excel',
                              }),
                            ),
                        },
                        {
                          label: 'Clientes nuevos',
                          format: 'pdf' as const,
                          onClick: () =>
                            runOpenReport('Clientes nuevos', () =>
                              downloadClientesNuevosMesPdf(
                                toClientesNuevosMesParams(
                                  variosOficina ?? oficinaSesion,
                                  variosGestor,
                                  variosRango[0].format('YYYY-MM-DD'),
                                  variosRango[1].format('YYYY-MM-DD'),
                                ),
                              ),
                            ),
                        },
                        {
                          label: 'Caja diario',
                          format: 'pdf' as const,
                          onClick: () =>
                            runOpenReport('Caja diario', () =>
                              openLegacyCajaDiarioInforme({
                                oficinaId: variosLegacy.oficinaId,
                                usuarioId: variosLegacy.usuarioId,
                                fechaIni: variosLegacy.fechaIni,
                                fechaFin: variosLegacy.fechaFin,
                                formato: 'PDF',
                              }),
                            ),
                        },
                      ]
                    : []),
                  {
                    label: 'Clientes inactivos pagados',
                    format: 'pdf',
                    onClick: () =>
                      runOpenReport('Clientes inactivos pagados', () =>
                        downloadClientesInactivosPdf(
                          toClientesInactivosParams(
                            variosOficina ?? oficinaSesion,
                            variosGestor ?? session?.usuarioId,
                            variosRango[0].format('YYYY-MM-DD'),
                            variosRango[1].format('YYYY-MM-DD'),
                          ),
                        ),
                      ),
                  },
                ]}
              />
            }
          >
            <ReporteField label="Oficina">
              <OficinaSelect allowAll value={variosOficina} onChange={setVariosOficina} />
            </ReporteField>
            <ReporteField label="Gestor">
              <GestorSelect allowAll value={variosGestor} onChange={setVariosGestor} />
            </ReporteField>
            <ReporteField label="Rango fechas">
              <DatePicker.RangePicker
                size="small"
                value={variosRango}
                onChange={(v) => v && setVariosRango(v as [Dayjs, Dayjs])}
                format="DD/MM/YYYY"
                style={{ width: '100%' }}
              />
            </ReporteField>
          </CredixReportBox>
        ) : null}

        {showAdmin ? (
          <div className="credix-reporte-grid credix-reporte-grid--secondary">
            <CredixReportBox
              title="Comprobantes caja chica"
              actions={
                <ReportExportActions
                  screenTo="/informes/comprobantes-caja-chica"
                  exports={[
                    {
                      label: 'PDF',
                      format: 'pdf',
                      onClick: () =>
                        runOpenReport('Comprobantes PDF', () =>
                          openLegacyComprobantesCajaChica({
                            fechaIni: legacyDate(cajaChicaRango[0]),
                            fechaFin: legacyDate(cajaChicaRango[1]),
                            formato: 'PDF',
                          }),
                        ),
                    },
                    {
                      label: 'XLS',
                      format: 'xls',
                      onClick: () =>
                        runOpenReport('Comprobantes XLS', () =>
                          openLegacyComprobantesCajaChica({
                            fechaIni: legacyDate(cajaChicaRango[0]),
                            fechaFin: legacyDate(cajaChicaRango[1]),
                            formato: 'Excel',
                          }),
                        ),
                    },
                  ]}
                />
              }
            >
              <ReporteField label="Rango fechas">
                <DatePicker.RangePicker
                  size="small"
                  value={cajaChicaRango}
                  onChange={(v) => v && setCajaChicaRango(v as [Dayjs, Dayjs])}
                  format="DD/MM/YYYY"
                  style={{ width: '100%' }}
                />
              </ReporteField>
            </CredixReportBox>

            <CredixReportBox
              title="Movimientos anulados"
              actions={
                <ReportExportActions
                  screenTo="/informes/movimientos-caja-anulados"
                  exports={[
                    {
                      label: 'Anulados PDF',
                      format: 'pdf',
                      onClick: () =>
                        runOpenReport('Anulados PDF', () =>
                          openLegacyComprobantesCajaAnulados({
                            oficinaId: oficinaSesion,
                            fechaIni: legacyDate(anuladoRango[0]),
                            fechaFin: legacyDate(anuladoRango[1]),
                            formato: 'PDF',
                          }),
                        ),
                    },
                  ]}
                />
              }
            >
              <ReporteField label="Rango fechas">
                <DatePicker.RangePicker
                  size="small"
                  value={anuladoRango}
                  onChange={(v) => v && setAnuladoRango(v as [Dayjs, Dayjs])}
                  format="DD/MM/YYYY"
                  style={{ width: '100%' }}
                />
              </ReporteField>
            </CredixReportBox>

            <CredixReportBox
              title="Saldo cartera"
              actions={
                <ReportExportActions
                  screenTo="/informes/saldo-cartera-caja-diario"
                  exports={[
                    {
                      label: 'Reporte saldo cartera',
                      format: 'primary',
                      onClick: () =>
                        runOpenReport('Saldo cartera', () =>
                          openLegacySaldoCarteraCajaDiario({
                            oficinaId: saldoOficina ?? oficinaSesion,
                            anioIni: saldoAnioIni,
                            mesIni: saldoMesIni,
                            anioFin: saldoAnioFin,
                            mesFin: saldoMesFin,
                          }),
                        ),
                    },
                  ]}
                />
              }
            >
              <ReporteField label="Oficina">
                <OficinaSelect allowAll value={saldoOficina} onChange={setSaldoOficina} />
              </ReporteField>
              <Space wrap>
                <Select
                  size="small"
                  style={{ width: 90 }}
                  value={saldoAnioIni}
                  onChange={setSaldoAnioIni}
                  options={saldoAnios.map((y) => ({
                    value: y,
                    label: String(y),
                  }))}
                />
                <Select
                  size="small"
                  style={{ width: 110 }}
                  value={saldoMesIni}
                  onChange={setSaldoMesIni}
                  options={MESES}
                />
                <span>→</span>
                <Select
                  size="small"
                  style={{ width: 90 }}
                  value={saldoAnioFin}
                  onChange={setSaldoAnioFin}
                  options={saldoAnios.map((y) => ({
                    value: y,
                    label: String(y),
                  }))}
                />
                <Select
                  size="small"
                  style={{ width: 110 }}
                  value={saldoMesFin}
                  onChange={setSaldoMesFin}
                  options={MESES}
                />
              </Space>
            </CredixReportBox>

            <CredixReportBox
              title="Central de riesgos"
              actions={
                <ReportExportActions
                  screenTo="/informes/central-riesgo"
                  exports={[
                    {
                      label: 'Central de riesgo TXT',
                      format: 'primary',
                      onClick: () =>
                        runOpenReport('Central de riesgo', () =>
                          openLegacyCentralRiesgoTxt({
                            oficinaId: riesgoOficina ?? oficinaSesion,
                            anio: riesgoAnio,
                            mes: riesgoMes,
                          }),
                        ),
                    },
                  ]}
                />
              }
            >
              <ReporteField label="Oficina">
                <OficinaSelect allowAll value={riesgoOficina} onChange={setRiesgoOficina} />
              </ReporteField>
              <Space wrap>
                <Select
                  size="small"
                  style={{ width: 100 }}
                  value={riesgoAnio}
                  onChange={setRiesgoAnio}
                  options={Array.from({ length: 12 }, (_, i) => 2014 + i).map((y) => ({
                    value: y,
                    label: String(y),
                  }))}
                />
                <Select
                  size="small"
                  style={{ width: 120 }}
                  value={riesgoMes}
                  onChange={setRiesgoMes}
                  options={MESES}
                />
              </Space>
            </CredixReportBox>
          </div>
        ) : null}
      </div>
    </CredixPage>
  )
}
