import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import {
  AuditOutlined,
  BankOutlined,
  CheckCircleOutlined,
  FileProtectOutlined,
  FundOutlined,
  StopOutlined,
  TeamOutlined,
  WalletOutlined,
} from '@ant-design/icons'
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
  downloadCajaDiarioInformePdf,
  downloadCentralRiesgoGenerarTxt,
  downloadClientesBloqueadosPdf,
  downloadClientesInactivosPdf,
  downloadClientesInactivosPdfGestor,
  downloadClientesNuevosMesPdf,
  downloadClientesNuevosMesPdfGestor,
  downloadClientesTopeCreditoPdf,
  downloadCobroDiarioCsv,
  downloadCobroDiarioPdf,
  downloadComprobantesCajaChicaCsv,
  downloadComprobantesCajaChicaPdf,
  downloadCreditoAprobacionCsv,
  downloadCreditoAprobacionPdf,
  downloadCreditoCondonadoCsv,
  downloadCreditoCondonadoPdf,
  downloadCreditoMorosidadCsv,
  downloadCreditoMorosidadPdf,
  downloadCreditoObservadoCsv,
  downloadCreditoObservadoPdf,
  downloadCreditoRentabilidadCsv,
  downloadCreditoRentabilidadPdf,
  downloadCreditosActivosCsv,
  downloadCreditosActivosPdf,
  downloadCreditosCierresCsv,
  downloadCreditosCierresPdf,
  downloadCreditosMorososPagadosCsv,
  downloadCreditosMorososPagadosPdf,
  downloadMorosidadGestorCsv,
  downloadMorosidadGestorPdf,
  downloadMovimientoCajaAnuladoPdf,
  downloadReporteCreditoCsv,
  downloadReporteCreditoPdf,
  downloadSaldoCarteraCajaDiarioPdf,
} from '../../api/creditoPlanes'
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
import { CREDITO_ESTADO_REPORTE_OPTIONS } from '../../utils/creditoEstados'

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

function isoDate(d: Dayjs): string {
  return d.format('YYYY-MM-DD')
}

function yearOptions(from: number, to: number): { value: number; label: string }[] {
  const end = Math.max(from, to)
  return Array.from({ length: end - from + 1 }, (_, i) => {
    const y = from + i
    return { value: y, label: String(y) }
  })
}

export function ReporteCreditoIndexPage() {
  const { session } = useAuth()
  const roles = session?.roles ?? []
  const oficinaSesion = session?.oficinaId ?? 0
  const anioActual = dayjs().year()

  const [moraHasta, setMoraHasta] = useState(dayjs())
  const [moraIni, setMoraIni] = useState(1)
  const [moraFin, setMoraFin] = useState(9999)

  const [aprobGestor, setAprobGestor] = useState<number | undefined>()
  const [aprobFecha, setAprobFecha] = useState(dayjs())

  const [gestorUsuario, setGestorUsuario] = useState<number | undefined>()

  const [rptGestor, setRptGestor] = useState<number | undefined>()
  const [rptEstado, setRptEstado] = useState('CRE')
  const [rentabilidadTodos, setRentabilidadTodos] = useState(false)
  const [rptRango, setRptRango] = useState<[Dayjs, Dayjs]>(monthRangeDefaults)

  const [variosGestor, setVariosGestor] = useState<number | undefined>()
  const [variosRango, setVariosRango] = useState<[Dayjs, Dayjs]>(monthRangeDefaults)

  const [cajaChicaRango, setCajaChicaRango] = useState<[Dayjs, Dayjs]>(monthRangeDefaults)
  const [anuladoRango, setAnuladoRango] = useState<[Dayjs, Dayjs]>(monthRangeDefaults)
  const [saldoAnioIni, setSaldoAnioIni] = useState(dayjs().subtract(1, 'month').year())
  const [saldoMesIni, setSaldoMesIni] = useState(dayjs().subtract(1, 'month').month() + 1)
  const [saldoAnioFin, setSaldoAnioFin] = useState(dayjs().year())
  const [saldoMesFin, setSaldoMesFin] = useState(dayjs().month() + 1)
  const [riesgoAnio, setRiesgoAnio] = useState(dayjs().subtract(1, 'month').year())
  const [riesgoMes, setRiesgoMes] = useState(dayjs().subtract(1, 'month').month() + 1)

  const gestorParams = useMemo(
    () => ({
      oficinaId: oficinaSesion,
      usuarioId:
        gestorUsuario != null && gestorUsuario > 0 ? gestorUsuario : undefined,
    }),
    [gestorUsuario, oficinaSesion],
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
    const usuarioId =
      variosGestor != null && variosGestor > 0 ? variosGestor : undefined
    const fechaIni = variosRango[0].format('YYYY-MM-DD')
    const fechaFin = variosRango[1].format('YYYY-MM-DD')
    return {
      oficinaId: oficinaSesion,
      usuarioId,
      pOficinaId: oficinaSesion,
      pUsuarioId: usuarioId,
      fechaIni,
      fechaFin,
      pFechaIni: fechaIni,
      pFechaFin: fechaFin,
    }
  }, [variosGestor, variosRango, oficinaSesion])

  const saldoAnios = useMemo(
    () => yearOptions(2022, Math.max(2032, anioActual + 2)),
    [anioActual],
  )

  const riesgoAnios = useMemo(
    () => yearOptions(2014, anioActual + 1),
    [anioActual],
  )

  const variosApi = useMemo(
    () => ({
      oficinaId: oficinaSesion,
      usuarioId: variosGestor != null && variosGestor > 0 ? variosGestor : undefined,
      fechaIni: isoDate(variosRango[0]),
      fechaFin: isoDate(variosRango[1]),
    }),
    [variosGestor, variosRango, oficinaSesion],
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
      subtitle="Cada caja abre el mismo informe que el MVC, con PDF y Excel de la API. La oficina es la de su sesión (el token no autoriza otra). El gestor admite TODOS salvo cobro diario, que exige uno concreto."
      breadcrumb={reportesCreditoIndexBreadcrumb()}
    >
      <p className="credix-reportes-intro">
        Operaciones diarias en <Link to="/credito">Crédito → Operaciones</Link>.{' '}
        <strong>Ver pantalla</strong> consulta en tabla. PDF y Excel descargan el mismo dataset
        (sesión JWT). Excel es CSV UTF-8, salvo <Link to="/reportes/cobranza">Cobranza pagos</Link>.
      </p>

      <div className="credix-reporte-grid">
        <CredixReportBox
          title="Reporte morosidad"
          icon={<AuditOutlined />}
          actions={
            <ReportExportActions
              screenTo="/informes/credito-morosidad"
              screenSearchParams={{
                oficinaId: oficinaSesion,
                hastaFecha: moraHasta.format('YYYY-MM-DD'),
                diasAtrazoIni: moraIni,
                diasAtrazoFin: moraFin,
              }}
              exports={[
                {
                  label: 'PDF',
                  format: 'pdf',
                  onClick: () =>
                    runOpenReport('Morosidad PDF', () =>
                      downloadCreditoMorosidadPdf({
                        oficinaId: oficinaSesion,
                        hastaFecha: isoDate(moraHasta),
                        diasAtrazoIni: moraIni,
                        diasAtrazoFin: moraFin,
                      }),
                    ),
                },
                {
                  label: 'XLS',
                  format: 'xls',
                  onClick: () =>
                    runOpenReport('Morosidad Excel', () =>
                      downloadCreditoMorosidadCsv({
                        oficinaId: oficinaSesion,
                        hastaFecha: isoDate(moraHasta),
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
            <OficinaSelect disabled value={oficinaSesion} />
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
                oficinaId: oficinaSesion,
                usuarioId: aprobGestor,
                fechaAprobacion: aprobFecha.format('YYYY-MM-DD'),
              }}
              exports={[
                {
                  label: 'PDF',
                  format: 'pdf',
                  onClick: () =>
                    runOpenReport('Aprobados PDF', () =>
                      downloadCreditoAprobacionPdf({
                        oficinaId: oficinaSesion,
                        usuarioId: aprobGestor,
                        fechaAprobacion: isoDate(aprobFecha),
                      }),
                    ),
                },
                {
                  label: 'XLS',
                  format: 'xls',
                  onClick: () =>
                    runOpenReport('Aprobados Excel', () =>
                      downloadCreditoAprobacionCsv({
                        oficinaId: oficinaSesion,
                        usuarioId: aprobGestor,
                        fechaAprobacion: isoDate(aprobFecha),
                      }),
                    ),
                },
              ]}
            />
          }
        >
          <ReporteField label="Oficina">
            <OficinaSelect disabled value={oficinaSesion} />
          </ReporteField>
          <ReporteField label="Gestor">
            <GestorSelect allowAll legacyList value={aprobGestor} onChange={setAprobGestor} />
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
          icon={<TeamOutlined />}
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
                      downloadMorosidadGestorPdf(
                        toCobroDiarioQuery(
                          gestorParams.oficinaId,
                          gestorParams.usuarioId,
                        ),
                      ),
                    ),
                },
                {
                  label: 'Morosidad XLS',
                  format: 'xls',
                  onClick: () =>
                    runOpenReport('Morosidad gestor XLS', () =>
                      downloadMorosidadGestorCsv(
                        toCobroDiarioQuery(
                          gestorParams.oficinaId,
                          gestorParams.usuarioId,
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
            <OficinaSelect disabled value={oficinaSesion} />
          </ReporteField>
          <ReporteField label="Gestor">
            <GestorSelect allowAll legacyList value={gestorUsuario} onChange={setGestorUsuario} />
          </ReporteField>
        </CredixReportBox>

        <CredixReportBox
          title="Reporte créditos"
          icon={<FundOutlined />}
          actions={
            <ReportExportActions
              screenTo="/informes/reporte-creditos"
              screenSearchParams={{
                oficinaId: oficinaSesion,
                usuarioId: rptGestor,
                estadoCredito: rptEstado,
                fechaIni: isoDate(rptRango[0]),
                fechaFin: isoDate(rptRango[1]),
              }}
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
                  label: 'PDF',
                  format: 'pdf',
                  onClick: () =>
                    runOpenReport('Reporte créditos', () =>
                      downloadReporteCreditoPdf({
                        oficinaId: oficinaSesion,
                        gestorId: rptGestor,
                        estadoCredito: rptEstado,
                        fechaIni: isoDate(rptRango[0]),
                        fechaFin: isoDate(rptRango[1]),
                      }),
                    ),
                },
                {
                  label: 'XLS',
                  format: 'xls',
                  onClick: () =>
                    runOpenReport('Reporte créditos Excel', () =>
                      downloadReporteCreditoCsv({
                        oficinaId: oficinaSesion,
                        gestorId: rptGestor,
                        estadoCredito: rptEstado,
                        fechaIni: isoDate(rptRango[0]),
                        fechaFin: isoDate(rptRango[1]),
                      }),
                    ),
                },
                {
                  label: 'Rentabilidad PDF',
                  format: 'pdf',
                  onClick: () =>
                    runOpenReport('Rentabilidad PDF', () =>
                      downloadCreditoRentabilidadPdf({
                        oficinaId: oficinaSesion,
                        fechaIni: rentabilidadTodos ? '2018-01-01' : isoDate(rptRango[0]),
                        fechaFin: rentabilidadTodos ? isoDate(dayjs()) : isoDate(rptRango[1]),
                        estadoCredito: rptEstado,
                      }),
                    ),
                },
                {
                  label: 'Rentabilidad XLS',
                  format: 'xls',
                  onClick: () =>
                    runOpenReport('Rentabilidad XLS', () =>
                      downloadCreditoRentabilidadCsv({
                        oficinaId: oficinaSesion,
                        fechaIni: rentabilidadTodos ? '2018-01-01' : isoDate(rptRango[0]),
                        fechaFin: rentabilidadTodos ? isoDate(dayjs()) : isoDate(rptRango[1]),
                        estadoCredito: rptEstado,
                      }),
                    ),
                },
              ]}
            />
          }
        >
          <ReporteField label="Oficina">
            <OficinaSelect disabled value={oficinaSesion} />
          </ReporteField>
          <ReporteField label="Gestor">
            <GestorSelect allowAll legacyList value={rptGestor} onChange={setRptGestor} />
          </ReporteField>
          <ReporteField label="Estado">
            <Select
              size="small"
              options={CREDITO_ESTADO_REPORTE_OPTIONS}
              value={rptEstado}
              onChange={setRptEstado}
              style={{ width: '100%' }}
            />
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
            icon={<WalletOutlined />}
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
                        search: buildInformeScreenQuery(variosScreenParams).slice(1),
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
                              downloadCreditoCondonadoPdf({
                                ...variosApi,
                                usuarioId: variosApi.usuarioId ?? session?.usuarioId ?? 0,
                              }),
                            ),
                        },
                        {
                          label: 'Condonación XLS',
                          format: 'xls' as const,
                          onClick: () =>
                            runOpenReport('Condonación XLS', () =>
                              downloadCreditoCondonadoCsv({
                                ...variosApi,
                                usuarioId: variosApi.usuarioId ?? session?.usuarioId ?? 0,
                              }),
                            ),
                        },
                        {
                          label: 'Activos PDF',
                          format: 'pdf' as const,
                          onClick: () =>
                            runOpenReport('Activos PDF', () =>
                              downloadCreditosActivosPdf(variosApi),
                            ),
                        },
                        {
                          label: 'Activos XLS',
                          format: 'xls' as const,
                          onClick: () =>
                            runOpenReport('Activos XLS', () =>
                              downloadCreditosActivosCsv(variosApi),
                            ),
                        },
                        {
                          label: 'Cierre PDF',
                          format: 'pdf' as const,
                          onClick: () =>
                            runOpenReport('Cierre PDF', () =>
                              downloadCreditosCierresPdf(variosApi),
                            ),
                        },
                        {
                          label: 'Cierre XLS',
                          format: 'xls' as const,
                          onClick: () =>
                            runOpenReport('Cierre XLS', () =>
                              downloadCreditosCierresCsv(variosApi),
                            ),
                        },
                        {
                          label: 'Morosos pagados PDF',
                          format: 'pdf' as const,
                          onClick: () =>
                            runOpenReport('Morosos pagados PDF', () =>
                              downloadCreditosMorososPagadosPdf(variosApi),
                            ),
                        },
                        {
                          label: 'Morosos pagados XLS',
                          format: 'xls' as const,
                          onClick: () =>
                            runOpenReport('Morosos pagados XLS', () =>
                              downloadCreditosMorososPagadosCsv(variosApi),
                            ),
                        },
                        {
                          label: 'Clientes nuevos',
                          format: 'pdf' as const,
                          onClick: () =>
                            runOpenReport('Clientes nuevos', () =>
                              downloadClientesNuevosMesPdf(
                                toClientesNuevosMesParams(
                                  oficinaSesion,
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
                              downloadCajaDiarioInformePdf(variosApi),
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
                            oficinaSesion,
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
              <OficinaSelect disabled value={oficinaSesion} />
            </ReporteField>
            <ReporteField label="Gestor">
              <GestorSelect allowAll legacyList value={variosGestor} onChange={setVariosGestor} />
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
              icon={<BankOutlined />}
              actions={
                <ReportExportActions
                  screenTo="/informes/comprobantes-caja-chica"
                  exports={[
                    {
                      label: 'PDF',
                      format: 'pdf',
                      onClick: () =>
                        runOpenReport('Comprobantes PDF', () =>
                          downloadComprobantesCajaChicaPdf({
                            fechaIni: isoDate(cajaChicaRango[0]),
                            fechaFin: isoDate(cajaChicaRango[1]),
                          }),
                        ),
                    },
                    {
                      label: 'XLS',
                      format: 'xls',
                      onClick: () =>
                        runOpenReport('Comprobantes XLS', () =>
                          downloadComprobantesCajaChicaCsv({
                            fechaIni: isoDate(cajaChicaRango[0]),
                            fechaFin: isoDate(cajaChicaRango[1]),
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
              icon={<StopOutlined />}
              actions={
                <ReportExportActions
                  screenTo="/informes/movimientos-caja-anulados"
                  exports={[
                    {
                      label: 'Anulados PDF',
                      format: 'pdf',
                      onClick: () =>
                        runOpenReport('Anulados PDF', () =>
                          downloadMovimientoCajaAnuladoPdf({
                            oficinaId: oficinaSesion,
                            fechaIni: isoDate(anuladoRango[0]),
                            fechaFin: isoDate(anuladoRango[1]),
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
              icon={<FundOutlined />}
              actions={
                <ReportExportActions
                  screenTo="/informes/saldo-cartera-caja-diario"
                  exports={[
                    {
                      label: 'Reporte saldo cartera',
                      format: 'primary',
                      onClick: () =>
                        runOpenReport('Saldo cartera', () =>
                          downloadSaldoCarteraCajaDiarioPdf({
                            oficinaId: oficinaSesion,
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
                <OficinaSelect disabled value={oficinaSesion} />
              </ReporteField>
              <Space wrap>
                <Select
                  size="small"
                  style={{ width: 90 }}
                  value={saldoAnioIni}
                  onChange={setSaldoAnioIni}
                  options={saldoAnios}
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
                  options={saldoAnios}
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
              icon={<FileProtectOutlined />}
              actions={
                <ReportExportActions
                  screenTo="/informes/central-riesgo"
                  exports={[
                    {
                      label: 'Central de riesgo TXT',
                      format: 'primary',
                      onClick: () =>
                        runOpenReport('Central de riesgo', () =>
                          downloadCentralRiesgoGenerarTxt({
                            oficinaId: oficinaSesion,
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
                <OficinaSelect disabled value={oficinaSesion} />
              </ReporteField>
              <Space wrap>
                <Select
                  size="small"
                  style={{ width: 100 }}
                  value={riesgoAnio}
                  onChange={setRiesgoAnio}
                  options={riesgoAnios}
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
