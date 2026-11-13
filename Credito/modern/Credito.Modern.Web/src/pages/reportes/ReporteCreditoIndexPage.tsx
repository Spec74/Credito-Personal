import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import {
  AuditOutlined,
  BankOutlined,
  CalendarOutlined,
  CheckCircleOutlined,
  DollarOutlined,
  FileProtectOutlined,
  FundOutlined,
  StopOutlined,
  TeamOutlined,
  UserDeleteOutlined,
  UserOutlined,
  WarningOutlined,
  WalletOutlined,
} from '@ant-design/icons'
import { Alert, Checkbox, InputNumber, Select, message } from 'antd'
import dayjs, { type Dayjs } from 'dayjs'
import { useAuth } from '../../auth/useAuth'
import { useEsCentral } from '../../hooks/useEsCentral'
import { CredixPage } from '../../components/credix'
import { CredixReportBox } from '../../components/reportes/CredixReportBox'
import { ReportExportActions } from '../../components/reportes/ReportExportActions'
import { ReportesSection } from '../../components/reportes/ReportesSection'
import { CredixDatePicker, CredixRangePicker } from '../../components/credix'
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
  const { esCentral } = useEsCentral()
  const roles = session?.roles ?? []
  const oficinaSesion = session?.oficinaId ?? 0
  const anioActual = dayjs().year()
  /** Central elige sede sin re-login; sucursal queda fijada al JWT. */
  const [sedeOficinaId, setSedeOficinaId] = useState<number | undefined>()
  const oficinaConsulta =
    esCentral && sedeOficinaId != null && sedeOficinaId > 0
      ? sedeOficinaId
      : oficinaSesion

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

  const [anuladoRango, setAnuladoRango] = useState<[Dayjs, Dayjs]>(monthRangeDefaults)
  const [saldoAnioIni, setSaldoAnioIni] = useState(dayjs().subtract(1, 'month').year())
  const [saldoMesIni, setSaldoMesIni] = useState(dayjs().subtract(1, 'month').month() + 1)
  const [saldoAnioFin, setSaldoAnioFin] = useState(dayjs().year())
  const [saldoMesFin, setSaldoMesFin] = useState(dayjs().month() + 1)
  const [riesgoAnio, setRiesgoAnio] = useState(dayjs().subtract(1, 'month').year())
  const [riesgoMes, setRiesgoMes] = useState(dayjs().subtract(1, 'month').month() + 1)

  const gestorParams = useMemo(
    () => ({
      oficinaId: oficinaConsulta,
      usuarioId:
        gestorUsuario != null && gestorUsuario > 0 ? gestorUsuario : undefined,
    }),
    [gestorUsuario, oficinaConsulta],
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
      oficinaId: oficinaConsulta,
      usuarioId,
      pOficinaId: oficinaConsulta,
      pUsuarioId: usuarioId,
      fechaIni,
      fechaFin,
      pFechaIni: fechaIni,
      pFechaFin: fechaFin,
    }
  }, [variosGestor, variosRango, oficinaConsulta])

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
      oficinaId: oficinaConsulta,
      usuarioId: variosGestor != null && variosGestor > 0 ? variosGestor : undefined,
      fechaIni: isoDate(variosRango[0]),
      fechaFin: isoDate(variosRango[1]),
    }),
    [variosGestor, variosRango, oficinaConsulta],
  )

  const onSedeChange = (v: number | undefined) => {
    setSedeOficinaId(v != null && v > 0 ? v : undefined)
  }

  if (!canViewReporteCredito(roles)) {
    return (
      <CredixPage
        title="Reportes de crédito"
        breadcrumb={reportesCreditoIndexBreadcrumb()}
      >
        <Alert
          type="warning"
          showIcon
          message="Sin permiso"
          description="Esta pantalla está disponible para roles ADMIN, APROBADOR o REPORTEPARCIAL (PARCIAL)."
        />
      </CredixPage>
    )
  }

  const showVarios = canViewReporteCreditoAprobador(roles)
  const showAdmin = canViewReporteCreditoAdmin(roles)

  return (
    <CredixPage
      title="Reportes de crédito"
      subtitle={undefined}
      breadcrumb={reportesCreditoIndexBreadcrumb()}
    >
      <div className="credix-reportes-shell">
        <div className="credix-reportes-intro">
          Cada tarjeta responde a una pregunta distinta (sin duplicar el mismo corte).{' '}
          <strong>Ver pantalla</strong>, PDF y Excel usan los mismos filtros. Excel = CSV UTF-8, salvo{' '}
          <Link to="/reportes/cobranza">Cobranza pagos</Link>. Operaciones del día:{' '}
          <Link to="/credito">Crédito → Operaciones</Link>.
        </div>

        {esCentral ? (
          <div className="credix-reportes-toolbar credix-reportes-toolbar--sede">
            <ReporteField label="Sede a consultar">
              <OficinaSelect
                value={oficinaConsulta > 0 ? oficinaConsulta : undefined}
                onChange={onSedeChange}
              />
            </ReporteField>
            <p className="credix-reportes-toolbar__meta">
              Central: elija la sede; todos los reportes de esta pantalla usan esa oficina. Sin
              selección, se usa la oficina del token de sesión.
            </p>
          </div>
        ) : null}

        <ol className="credix-reportes-steps" aria-label="Cómo usar los reportes">
          <li>{esCentral ? 'Elija la sede' : 'Confirme su oficina'}</li>
          <li>Ajuste filtros de la tarjeta</li>
          <li>Exporte o abra Ver pantalla</li>
        </ol>

        <ReportesSection
          title="Cobranza y morosidad"
          description="Morosidad por días (corte de cartera) ≠ cobro del día por gestor ≠ solo mora del día."
        >
        <CredixReportBox
          title="Morosidad por días de atraso"
          icon={<AuditOutlined />}
          actions={
            <ReportExportActions
              screenTo="/informes/credito-morosidad"
              screenSearchParams={{
                oficinaId: oficinaConsulta,
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
                        oficinaId: oficinaConsulta,
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
                        oficinaId: oficinaConsulta,
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
          <ReporteField label={esCentral ? 'Sede' : 'Oficina'}>
            <OficinaSelect
              disabled={!esCentral}
              value={oficinaConsulta > 0 ? oficinaConsulta : undefined}
              onChange={onSedeChange}
            />
          </ReporteField>
          <ReporteField label="Hasta la fecha">
            <CredixDatePicker
              size="small"
              value={moraHasta}
              onChange={(d) => {
                if (d) setMoraHasta(d)
              }}
            />
          </ReporteField>
          <ReporteField label="Días atraso inicio">
            <InputNumber size="small" min={0} value={moraIni} onChange={(v) => setMoraIni(v ?? 1)} style={{ width: '100%' }} />
          </ReporteField>
          <ReporteField label="Días atraso final">
            <InputNumber size="small" min={0} value={moraFin} onChange={(v) => setMoraFin(v ?? 9999)} style={{ width: '100%' }} />
          </ReporteField>
          <p className="credix-report-card-hint">
            Corte de cartera en atraso por rango de días (no es el cobro programado del día).
          </p>
        </CredixReportBox>

        <CredixReportBox
          title="Créditos aprobados"
          icon={<CheckCircleOutlined />}
          actions={
            <ReportExportActions
              screenTo="/informes/credito-aprobacion"
              screenSearchParams={{
                oficinaId: oficinaConsulta,
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
                        oficinaId: oficinaConsulta,
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
                        oficinaId: oficinaConsulta,
                        usuarioId: aprobGestor,
                        fechaAprobacion: isoDate(aprobFecha),
                      }),
                    ),
                },
              ]}
            />
          }
        >
          <ReporteField label={esCentral ? 'Sede' : 'Oficina'}>
            <OficinaSelect
              disabled={!esCentral}
              value={oficinaConsulta > 0 ? oficinaConsulta : undefined}
              onChange={onSedeChange}
            />
          </ReporteField>
          <ReporteField label="Gestor">
            <GestorSelect allowAll legacyList value={aprobGestor} onChange={setAprobGestor} />
          </ReporteField>
          <ReporteField label="Fecha aprobación">
            <CredixDatePicker
              size="small"
              value={aprobFecha}
              onChange={(d) => {
                if (d) setAprobFecha(d)
              }}
            />
          </ReporteField>
          <p className="credix-report-card-hint">
            Créditos aprobados en la fecha indicada (pipeline de origen, no cartera en mora).
          </p>
        </CredixReportBox>

        <h3 className="credix-reportes-block-title">Informes por gestor</h3>
        <div className="credix-reportes-toolbar">
          <ReporteField label={esCentral ? 'Sede' : 'Oficina'}>
            <OficinaSelect
              disabled={!esCentral}
              value={oficinaConsulta > 0 ? oficinaConsulta : undefined}
              onChange={onSedeChange}
            />
          </ReporteField>
          <ReporteField label="Gestor">
            <GestorSelect allowAll legacyList value={gestorUsuario} onChange={setGestorUsuario} />
          </ReporteField>
          <p className="credix-reportes-toolbar__meta">
            El gestor seleccionado aplica a todas las tarjetas de este bloque. Cobro diario exige un
            gestor concreto (no «Todos»).
          </p>
        </div>

        <CredixReportBox
          title="Cobro diario"
          icon={<CalendarOutlined />}
          actions={
            <ReportExportActions
              screenTo={gestorCobroScreenParams ? '/informes/cobro-diario' : undefined}
              screenSearchParams={gestorCobroScreenParams}
              exports={[
                {
                  label: 'PDF',
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
                  label: 'XLS',
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
              ]}
            />
          }
        >
          <p className="credix-report-card-hint">
            Agenda de cobro del día del gestor (cuotas a recaudar hoy). Exige gestor concreto.
          </p>
        </CredixReportBox>

        <CredixReportBox
          title="Cobro diario · solo mora"
          icon={<TeamOutlined />}
          actions={
            <ReportExportActions
              screenTo="/informes/cobro-diario"
              screenSearchParams={{ ...gestorScreenParams, soloMora: '1' }}
              exports={[
                {
                  label: 'PDF',
                  format: 'pdf',
                  onClick: () =>
                    runOpenReport('Cobro diario solo mora', () =>
                      downloadMorosidadGestorPdf(
                        toCobroDiarioQuery(
                          gestorParams.oficinaId,
                          gestorParams.usuarioId,
                          true,
                        ),
                      ),
                    ),
                },
                {
                  label: 'XLS',
                  format: 'xls',
                  onClick: () =>
                    runOpenReport('Cobro diario solo mora XLS', () =>
                      downloadMorosidadGestorCsv(
                        toCobroDiarioQuery(
                          gestorParams.oficinaId,
                          gestorParams.usuarioId,
                          true,
                        ),
                      ),
                    ),
                },
              ]}
            />
          }
        >
          <p className="credix-report-card-hint">
            Del cobro del día, solo cuotas ya en mora (distinto del corte «Morosidad por días»).
          </p>
        </CredixReportBox>

        <CredixReportBox
          title="Créditos observados"
          icon={<WarningOutlined />}
          actions={
            <ReportExportActions
              screenTo="/informes/creditos-observados"
              screenSearchParams={gestorScreenParams}
              exports={[
                {
                  label: 'PDF',
                  format: 'pdf',
                  onClick: () =>
                    runOpenReport('Observados PDF', () =>
                      downloadCreditoObservadoPdf(gestorApiParams),
                    ),
                },
                {
                  label: 'XLS',
                  format: 'xls',
                  onClick: () =>
                    runOpenReport('Observados XLS', () =>
                      downloadCreditoObservadoCsv(gestorApiParams),
                    ),
                },
              ]}
            />
          }
        >
          <p className="credix-report-card-hint">
            Créditos con observación operativa (no equivale a mora ni a bloqueo de cliente).
          </p>
        </CredixReportBox>

        <CredixReportBox
          title="Clientes nuevos del mes"
          icon={<UserOutlined />}
          actions={
            <ReportExportActions
              screenTo="/informes/clientes-nuevos-mes"
              screenSearchParams={gestorNuevosScreenParams}
              exports={[
                {
                  label: 'PDF',
                  format: 'pdf',
                  onClick: () =>
                    runOpenReport('Clientes nuevos', () =>
                      downloadClientesNuevosMesPdfGestor({
                        oficinaId: gestorParams.oficinaId,
                        usuarioId: gestorParams.usuarioId,
                      }),
                    ),
                },
              ]}
            />
          }
        >
          <p className="credix-report-card-hint">
            Altas del mes calendario actual por gestor (fijo al mes en curso).
          </p>
        </CredixReportBox>

        <CredixReportBox
          title="Clientes inactivos"
          icon={<UserDeleteOutlined />}
          actions={
            <ReportExportActions
              screenTo="/informes/clientes-inactivos"
              screenSearchParams={gestorInactivosScreenParams}
              exports={[
                {
                  label: 'PDF',
                  format: 'pdf',
                  onClick: () =>
                    runOpenReport('Clientes inactivos', () =>
                      downloadClientesInactivosPdfGestor({
                        oficinaId: gestorParams.oficinaId,
                        usuarioId: gestorParams.usuarioId,
                      }),
                    ),
                },
              ]}
            />
          }
        >
          <p className="credix-report-card-hint">
            Sin movimiento reciente por gestor (sin rango de fechas; distinto del listado con periodo).
          </p>
        </CredixReportBox>

        <CredixReportBox
          title="Clientes bloqueados"
          icon={<StopOutlined />}
          actions={
            <ReportExportActions
              screenTo="/informes/clientes-bloqueados"
              screenSearchParams={gestorScreenParams}
              exports={[
                {
                  label: 'PDF',
                  format: 'pdf',
                  onClick: () =>
                    runOpenReport('Clientes bloqueados', () =>
                      downloadClientesBloqueadosPdf(gestorApiParams),
                    ),
                },
              ]}
            />
          }
        >
          <p className="credix-report-card-hint">
            Clientes bloqueados en la oficina, con filtro opcional por gestor.
          </p>
        </CredixReportBox>

        <CredixReportBox
          title="Tope de crédito"
          icon={<DollarOutlined />}
          actions={
            <ReportExportActions
              screenTo="/informes/clientes-tope-credito"
              screenSearchParams={gestorScreenParams}
              exports={[
                {
                  label: 'PDF',
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
          <p className="credix-report-card-hint">
            Clientes con tope de crédito configurado, según el gestor de la barra.
          </p>
        </CredixReportBox>
        </ReportesSection>

        <ReportesSection
          title="Cartera y créditos"
          description="Listado filtrable por estado/fechas; rentabilidad; consultas de periodo (activos, cierres, condonados)."
        >
        <CredixReportBox
          title="Reporte créditos"
          icon={<FundOutlined />}
          actions={
            <ReportExportActions
              screenTo="/informes/reporte-creditos"
              screenSearchParams={{
                oficinaId: oficinaConsulta,
                usuarioId: rptGestor,
                estadoCredito: rptEstado,
                fechaIni: isoDate(rptRango[0]),
                fechaFin: isoDate(rptRango[1]),
              }}
              exports={[
                {
                  label: 'PDF',
                  format: 'pdf',
                  onClick: () =>
                    runOpenReport('Reporte créditos', () =>
                      downloadReporteCreditoPdf({
                        oficinaId: oficinaConsulta,
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
                        oficinaId: oficinaConsulta,
                        gestorId: rptGestor,
                        estadoCredito: rptEstado,
                        fechaIni: isoDate(rptRango[0]),
                        fechaFin: isoDate(rptRango[1]),
                      }),
                    ),
                },
              ]}
            />
          }
        >
          <ReporteField label={esCentral ? 'Sede' : 'Oficina'}>
            <OficinaSelect
              disabled={!esCentral}
              value={oficinaConsulta > 0 ? oficinaConsulta : undefined}
              onChange={onSedeChange}
            />
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
            <CredixRangePicker
              size="small"
              value={rptRango}
              onChange={(v) => v && setRptRango(v as [Dayjs, Dayjs])}
              format="DD/MM/YYYY"
              style={{ width: '100%' }}
            />
          </ReporteField>
          <p className="credix-report-card-hint">
            Listado por estado y rango (cualquier estado). No sustituye «Créditos activos».
          </p>
        </CredixReportBox>

        <CredixReportBox
          title="Rentabilidad de crédito"
          icon={<DollarOutlined />}
          actions={
            <ReportExportActions
              screenTo="/informes/credito-rentabilidad"
              screenSearchParams={{
                oficinaId: oficinaConsulta,
                estadoCredito: rptEstado,
                fechaIni: rentabilidadTodos ? '2018-01-01' : isoDate(rptRango[0]),
                fechaFin: rentabilidadTodos ? isoDate(dayjs()) : isoDate(rptRango[1]),
              }}
              exports={[
                {
                  label: 'PDF',
                  format: 'pdf',
                  onClick: () =>
                    runOpenReport('Rentabilidad PDF', () =>
                      downloadCreditoRentabilidadPdf({
                        oficinaId: oficinaConsulta,
                        fechaIni: rentabilidadTodos ? '2018-01-01' : isoDate(rptRango[0]),
                        fechaFin: rentabilidadTodos ? isoDate(dayjs()) : isoDate(rptRango[1]),
                        estadoCredito: rptEstado,
                      }),
                    ),
                },
                {
                  label: 'XLS',
                  format: 'xls',
                  onClick: () =>
                    runOpenReport('Rentabilidad XLS', () =>
                      downloadCreditoRentabilidadCsv({
                        oficinaId: oficinaConsulta,
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
          <ReporteField label={esCentral ? 'Sede' : 'Oficina'}>
            <OficinaSelect
              disabled={!esCentral}
              value={oficinaConsulta > 0 ? oficinaConsulta : undefined}
              onChange={onSedeChange}
            />
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
            <CredixRangePicker
              size="small"
              value={rptRango}
              onChange={(v) => v && setRptRango(v as [Dayjs, Dayjs])}
              format="DD/MM/YYYY"
              style={{ width: '100%' }}
              disabled={rentabilidadTodos}
            />
          </ReporteField>
          <ReporteField label="Alcance">
            <Checkbox
              checked={rentabilidadTodos}
              onChange={(e) => setRentabilidadTodos(e.target.checked)}
            >
              Histórico completo (desde 2018)
            </Checkbox>
          </ReporteField>
          <p className="credix-report-card-hint">
            Margen e interés generado; no es listado operativo de cuotas del día.
          </p>
        </CredixReportBox>

        {showVarios ? (
          <>
            <h3 className="credix-reportes-block-title">Consultas por periodo</h3>
            <div className="credix-reportes-toolbar">
              <ReporteField label={esCentral ? 'Sede' : 'Oficina'}>
                <OficinaSelect
                  disabled={!esCentral}
                  value={oficinaConsulta > 0 ? oficinaConsulta : undefined}
                  onChange={onSedeChange}
                />
              </ReporteField>
              <ReporteField label="Gestor">
                <GestorSelect allowAll legacyList value={variosGestor} onChange={setVariosGestor} />
              </ReporteField>
              <ReporteField label="Rango fechas">
                <CredixRangePicker
                  size="small"
                  value={variosRango}
                  onChange={(v) => v && setVariosRango(v as [Dayjs, Dayjs])}
                  format="DD/MM/YYYY"
                  style={{ width: '100%' }}
                />
              </ReporteField>
              <p className="credix-reportes-toolbar__meta">
                Oficina, gestor y rango aplican a las tarjetas de este bloque.
              </p>
            </div>

            {showAdmin ? (
              <>
                <CredixReportBox
                  title="Créditos condonados"
                  icon={<WalletOutlined />}
                  actions={
                    <ReportExportActions
                      screenTo="/informes/credito-condonado"
                      screenSearchParams={variosScreenParams}
                      exports={[
                        {
                          label: 'PDF',
                          format: 'pdf',
                          onClick: () =>
                            runOpenReport('Condonación PDF', () =>
                              downloadCreditoCondonadoPdf({
                                ...variosApi,
                                usuarioId: variosApi.usuarioId ?? session?.usuarioId ?? 0,
                              }),
                            ),
                        },
                        {
                          label: 'XLS',
                          format: 'xls',
                          onClick: () =>
                            runOpenReport('Condonación XLS', () =>
                              downloadCreditoCondonadoCsv({
                                ...variosApi,
                                usuarioId: variosApi.usuarioId ?? session?.usuarioId ?? 0,
                              }),
                            ),
                        },
                      ]}
                    />
                  }
                >
                  <p className="credix-report-card-hint">
                    Solo condonaciones registradas en el periodo (no cartera activa).
                  </p>
                </CredixReportBox>

                <CredixReportBox
                  title="Créditos activos"
                  icon={<CheckCircleOutlined />}
                  actions={
                    <ReportExportActions
                      screenTo="/informes/creditos-activos"
                      screenSearchParams={variosScreenParams}
                      exports={[
                        {
                          label: 'PDF',
                          format: 'pdf',
                          onClick: () =>
                            runOpenReport('Activos PDF', () =>
                              downloadCreditosActivosPdf(variosApi),
                            ),
                        },
                        {
                          label: 'XLS',
                          format: 'xls',
                          onClick: () =>
                            runOpenReport('Activos XLS', () =>
                              downloadCreditosActivosCsv(variosApi),
                            ),
                        },
                      ]}
                    />
                  }
                >
                  <p className="credix-report-card-hint">
                    Snapshot de cartera vigente al momento (estado activo), con gestor/rango del bloque.
                  </p>
                </CredixReportBox>

                <CredixReportBox
                  title="Cierres de crédito"
                  icon={<FileProtectOutlined />}
                  actions={
                    <ReportExportActions
                      screenTo="/informes/creditos-cierres"
                      screenSearchParams={variosScreenParams}
                      exports={[
                        {
                          label: 'PDF',
                          format: 'pdf',
                          onClick: () =>
                            runOpenReport('Cierre PDF', () =>
                              downloadCreditosCierresPdf(variosApi),
                            ),
                        },
                        {
                          label: 'XLS',
                          format: 'xls',
                          onClick: () =>
                            runOpenReport('Cierre XLS', () =>
                              downloadCreditosCierresCsv(variosApi),
                            ),
                        },
                      ]}
                    />
                  }
                >
                  <p className="credix-report-card-hint">
                    Créditos que cerraron en el periodo (salida de cartera, no cuotas del día).
                  </p>
                </CredixReportBox>

                <CredixReportBox
                  title="Morosos pagados"
                  icon={<AuditOutlined />}
                  actions={
                    <ReportExportActions
                      screenTo="/informes/creditos-morosos-pagados"
                      screenSearchParams={variosScreenParams}
                      exports={[
                        {
                          label: 'PDF',
                          format: 'pdf',
                          onClick: () =>
                            runOpenReport('Morosos pagados PDF', () =>
                              downloadCreditosMorososPagadosPdf(variosApi),
                            ),
                        },
                        {
                          label: 'XLS',
                          format: 'xls',
                          onClick: () =>
                            runOpenReport('Morosos pagados XLS', () =>
                              downloadCreditosMorososPagadosCsv(variosApi),
                            ),
                        },
                      ]}
                    />
                  }
                >
                  <p className="credix-report-card-hint">
                    Salieron de mora con pago en el rango (recuperación, no corte de atraso).
                  </p>
                </CredixReportBox>

                <CredixReportBox
                  title="Clientes nuevos (periodo)"
                  icon={<UserOutlined />}
                  actions={
                    <ReportExportActions
                      screenTo="/informes/clientes-nuevos-mes"
                      screenSearchParams={variosScreenParams}
                      exports={[
                        {
                          label: 'PDF',
                          format: 'pdf',
                          onClick: () =>
                            runOpenReport('Clientes nuevos', () =>
                              downloadClientesNuevosMesPdf(
                                toClientesNuevosMesParams(
                                  oficinaConsulta,
                                  variosGestor,
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
                  <p className="credix-report-card-hint">
                    Altas en el rango libre de la barra (no el mes fijo del bloque por gestor).
                  </p>
                </CredixReportBox>

                <CredixReportBox
                  title="Informe caja diario"
                  icon={<BankOutlined />}
                  actions={
                    <ReportExportActions
                      screenTo="/informes/caja-diario"
                      screenSearchParams={variosScreenParams}
                      exports={[
                        {
                          label: 'PDF',
                          format: 'pdf',
                          onClick: () =>
                            runOpenReport('Caja diario', () =>
                              downloadCajaDiarioInformePdf(variosApi),
                            ),
                        },
                      ]}
                    />
                  }
                >
                  <p className="credix-report-card-hint">
                    Resumen de cajas diarias del periodo/gestor (cuadre, no cobranza de cuotas).
                  </p>
                </CredixReportBox>
              </>
            ) : null}

            <CredixReportBox
              title="Clientes inactivos (periodo)"
              icon={<UserDeleteOutlined />}
              actions={
                <ReportExportActions
                  screenTo="/informes/clientes-inactivos"
                  screenSearchParams={variosScreenParams}
                  exports={[
                    {
                      label: 'PDF',
                      format: 'pdf',
                      onClick: () =>
                        runOpenReport('Clientes inactivos pagados', () =>
                          downloadClientesInactivosPdf(
                            toClientesInactivosParams(
                              oficinaConsulta,
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
              <p className="credix-report-card-hint">
                Inactivos acotados al rango de la barra (distinto del listado sin rango por gestor).
              </p>
            </CredixReportBox>
          </>
        ) : null}
        </ReportesSection>

        {showAdmin ? (
          <ReportesSection
            title="Caja y administración"
            description="Anulaciones, saldo de cartera y central de riesgos por sede. Caja chica retirada del menú."
          >
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
                            oficinaId: oficinaConsulta,
                            fechaIni: isoDate(anuladoRango[0]),
                            fechaFin: isoDate(anuladoRango[1]),
                          }),
                        ),
                    },
                  ]}
                />
              }
            >
              <ReporteField label={esCentral ? 'Sede' : 'Oficina'}>
                <OficinaSelect
                  disabled={!esCentral}
                  value={oficinaConsulta > 0 ? oficinaConsulta : undefined}
                  onChange={onSedeChange}
                />
              </ReporteField>
              <ReporteField label="Rango fechas">
                <CredixRangePicker
                  size="small"
                  value={anuladoRango}
                  onChange={(v) => v && setAnuladoRango(v as [Dayjs, Dayjs])}
                  format="DD/MM/YYYY"
                  style={{ width: '100%' }}
                />
              </ReporteField>
              <p className="credix-report-card-hint">
                Movimientos de caja anulados en el periodo (auditoría, no cobro operativo).
              </p>
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
                            oficinaId: oficinaConsulta,
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
              <ReporteField label={esCentral ? 'Sede' : 'Oficina'}>
                <OficinaSelect
                  disabled={!esCentral}
                  value={oficinaConsulta > 0 ? oficinaConsulta : undefined}
                  onChange={onSedeChange}
                />
              </ReporteField>
              <ReporteField label="Periodo">
                <div className="credix-report-period-row">
                  <Select
                    size="small"
                    value={saldoAnioIni}
                    onChange={setSaldoAnioIni}
                    options={saldoAnios}
                  />
                  <Select
                    size="small"
                    value={saldoMesIni}
                    onChange={setSaldoMesIni}
                    options={MESES}
                  />
                  <span className="credix-report-period-row__sep">→</span>
                  <Select
                    size="small"
                    value={saldoAnioFin}
                    onChange={setSaldoAnioFin}
                    options={saldoAnios}
                  />
                  <Select
                    size="small"
                    value={saldoMesFin}
                    onChange={setSaldoMesFin}
                    options={MESES}
                  />
                </div>
              </ReporteField>
              <p className="credix-report-card-hint">
                Evolución mensual de saldo de cartera de la sede (cuadre gerencial).
              </p>
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
                            oficinaId: oficinaConsulta,
                            anio: riesgoAnio,
                            mes: riesgoMes,
                          }),
                        ),
                    },
                  ]}
                />
              }
            >
              <ReporteField label={esCentral ? 'Sede' : 'Oficina'}>
                <OficinaSelect
                  disabled={!esCentral}
                  value={oficinaConsulta > 0 ? oficinaConsulta : undefined}
                  onChange={onSedeChange}
                />
              </ReporteField>
              <ReporteField label="Periodo">
                <div className="credix-report-period-row">
                  <Select
                    size="small"
                    value={riesgoAnio}
                    onChange={setRiesgoAnio}
                    options={riesgoAnios}
                  />
                  <Select
                    size="small"
                    value={riesgoMes}
                    onChange={setRiesgoMes}
                    options={MESES}
                  />
                </div>
              </ReporteField>
              <p className="credix-report-card-hint">
                Archivo regulatorio TXT del mes/sede (no es reporte de cobranza interna).
              </p>
            </CredixReportBox>
          </ReportesSection>
        ) : null}
      </div>
    </CredixPage>
  )
}
