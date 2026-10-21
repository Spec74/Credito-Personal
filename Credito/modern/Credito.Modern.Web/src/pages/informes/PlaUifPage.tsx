import { useEffect } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQuery } from '@tanstack/react-query'
import { Button, Form, Input, InputNumber, Select } from 'antd'
import { SearchOutlined } from '@ant-design/icons'
import dayjs from 'dayjs'
import {
  fetchPlaCandidatosRo,
  fetchPlaInusual,
  fetchPlaParametro,
  registrarPlaInusual,
  savePlaParametro,
  type PlaCandidatoRo,
  type PlaInusual,
} from '../../api/pla'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { CredixDataTable, CredixInformePage } from '../../components/credix'
import { formatFecha } from '../../utils/formatFecha'
import { formatMoney } from '../../utils/formatMoney'
import { reportesCreditoBreadcrumb } from '../../utils/reportesBreadcrumbs'

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

type Periodo = { anio: number; mes: number }

type ParametroForm = {
  tcVentaMesAnterior?: number | null
  tcPeriodo?: string | null
  umbralIndividualMenorUsd?: number | null
  umbralMultipleMenorUsd?: number | null
}

type InusualForm = {
  personaId: number
  creditoId?: number | null
  calificacion: string
  motivo: string
}

const CANDIDATO_COLUMNS = [
  { title: 'Tipo', dataIndex: 'tipo', width: 110 },
  { title: 'Crédito', dataIndex: 'creditoId', width: 90 },
  { title: 'Documento', dataIndex: 'numeroDocumento', width: 110 },
  { title: 'Cliente', dataIndex: 'nombreCompleto', minWidth: 180 },
  {
    title: 'Monto',
    dataIndex: 'monto',
    align: 'right' as const,
    width: 110,
    render: (v: number) => formatMoney(v),
  },
  {
    title: 'Mes persona',
    dataIndex: 'montoMes',
    align: 'right' as const,
    width: 110,
    render: (v: number | null) => formatMoney(v),
  },
  {
    title: 'Fecha',
    dataIndex: 'fechaOp',
    width: 110,
    render: (v: string) => formatFecha(v),
  },
  { title: 'RO', dataIndex: 'motivoRo', minWidth: 160 },
  {
    title: 'KYC',
    key: 'kyc',
    width: 140,
    render: (_: unknown, r: PlaCandidatoRo) =>
      [r.faltaDocumento && 'DNI', r.faltaDireccion && 'dir.', r.faltaOcupacion && 'ocup.']
        .filter(Boolean)
        .join(', ') || '—',
  },
]

const INUSUAL_COLUMNS = [
  { title: 'Fecha', dataIndex: 'fechaReg', width: 120, render: (v: string) => formatFecha(v) },
  { title: 'Calificación', dataIndex: 'calificacion', width: 120 },
  { title: 'Documento', dataIndex: 'numeroDocumento', width: 110 },
  { title: 'Cliente', dataIndex: 'nombreCompleto', minWidth: 180 },
  { title: 'Crédito', dataIndex: 'creditoId', width: 90 },
  { title: 'Motivo', dataIndex: 'motivo', minWidth: 200 },
  { title: 'Usuario', dataIndex: 'nombreUsuario', width: 120 },
]

export function PlaUifPage() {
  const { session } = useAuth()
  const [periodo] = Form.useForm<Periodo>()
  const [parametroForm] = Form.useForm<ParametroForm>()
  const [inusualForm] = Form.useForm<InusualForm>()

  const parametro = useQuery({
    queryKey: ['pla-parametro'],
    queryFn: fetchPlaParametro,
    staleTime: 60_000,
  })

  useEffect(() => {
    const prev = dayjs().subtract(1, 'month')
    periodo.setFieldsValue({ anio: prev.year(), mes: prev.month() + 1 })
  }, [periodo])

  useEffect(() => {
    if (!parametro.data) return
    parametroForm.setFieldsValue({
      tcVentaMesAnterior: parametro.data.tcVentaMesAnterior,
      tcPeriodo: parametro.data.tcPeriodo,
      umbralIndividualMenorUsd: parametro.data.umbralIndividualMenorUsd,
      umbralMultipleMenorUsd: parametro.data.umbralMultipleMenorUsd,
    })
  }, [parametro.data, parametroForm])

  const consulta = useMutation({
    mutationFn: async (v: Periodo) => {
      if (!session?.oficinaId) throw new Error('Sin oficina en sesión')
      const [candidatos, bitacora] = await Promise.all([
        fetchPlaCandidatosRo(session.oficinaId, v.anio, v.mes),
        fetchPlaInusual(session.oficinaId, v.anio, v.mes),
      ])
      return { candidatos, bitacora }
    },
  })

  const guardarParametro = useMutation({
    mutationFn: (v: ParametroForm) =>
      savePlaParametro({
        tcVentaMesAnterior: v.tcVentaMesAnterior ?? null,
        tcPeriodo: v.tcPeriodo?.trim() || null,
        umbralIndividualMenorUsd: v.umbralIndividualMenorUsd ?? null,
        umbralMultipleMenorUsd: v.umbralMultipleMenorUsd ?? null,
      }),
    onSuccess: () => parametro.refetch(),
  })

  const registrar = useMutation({
    mutationFn: async (v: InusualForm) => {
      if (!session?.oficinaId) throw new Error('Sin oficina en sesión')
      return registrarPlaInusual({
        oficinaId: session.oficinaId,
        personaId: v.personaId,
        creditoId: v.creditoId,
        calificacion: v.calificacion,
        motivo: v.motivo.trim(),
      })
    },
    onSuccess: () => {
      inusualForm.resetFields()
      const v = periodo.getFieldsValue()
      if (v.anio && v.mes) consulta.mutate(v)
    },
  })

  const p = parametro.data
  const error =
    consulta.error instanceof ApiError
      ? consulta.error.message
      : guardarParametro.error instanceof ApiError
        ? guardarParametro.error.message
        : registrar.error instanceof ApiError
          ? registrar.error.message
          : parametro.error instanceof ApiError
            ? parametro.error.message
            : undefined

  return (
    <CredixInformePage
      title="PLA / UIF"
      breadcrumb={reportesCreditoBreadcrumb('PLA / UIF')}
      subtitle={
        p
          ? `${p.fuenteNorma}. RO individual US$ ${formatMoney(p.individualEfectivoUsd)} · múltiples US$ ${formatMoney(p.multipleEfectivoUsd)}. ROS sin monto.`
          : 'Piso legal UIF. No se inventa un monto en soles.'
      }
      error={error}
      stats={[
        {
          label: 'Sobre umbral RO',
          value: consulta.data?.candidatos.filter((r) => r.superaUmbral).length ?? '—',
        },
        {
          label: 'Operaciones del mes',
          value: consulta.data?.candidatos.length ?? '—',
        },
        {
          label: 'Bitácora',
          value: consulta.data?.bitacora.length ?? '—',
        },
      ]}
      filters={
        <Form
          form={periodo}
          layout="inline"
          onFinish={(v) => consulta.mutate(v)}
        >
          <Form.Item name="anio" label="Año" rules={[{ required: true }]}>
            <InputNumber min={2000} max={2100} />
          </Form.Item>
          <Form.Item name="mes" label="Mes" rules={[{ required: true }]}>
            <Select options={MESES} style={{ minWidth: 140 }} />
          </Form.Item>
          <Button type="primary" htmlType="submit" icon={<SearchOutlined />} loading={consulta.isPending}>
            Consultar
          </Button>
        </Form>
      }
      panelTitle="Candidatos RO"
      tableResultCount={consulta.data?.candidatos.length}
      resultCountLabel="operación"
    >
      <Form
        form={parametroForm}
        layout="inline"
        onFinish={(v) => guardarParametro.mutate(v)}
        style={{ marginBottom: 12 }}
      >
        <Form.Item name="tcVentaMesAnterior" label="TC SBS venta (mes ant.)">
          <InputNumber min={0.0001} step={0.0001} />
        </Form.Item>
        <Form.Item name="tcPeriodo" label="Periodo TC">
          <Input placeholder="2026-09" maxLength={7} style={{ width: 100 }} />
        </Form.Item>
        <Form.Item name="umbralIndividualMenorUsd" label="Piso menor US$">
          <InputNumber min={0.01} />
        </Form.Item>
        <Form.Item name="umbralMultipleMenorUsd" label="Múltiple menor US$">
          <InputNumber min={0.01} />
        </Form.Item>
        <Button htmlType="submit" loading={guardarParametro.isPending}>
          Guardar TC
        </Button>
      </Form>

      <CredixDataTable<PlaCandidatoRo>
        rowKey={(r, i) => `${r.tipo}-${r.creditoId ?? 0}-${r.personaId ?? 0}-${i}`}
        columns={CANDIDATO_COLUMNS}
        dataSource={consulta.data?.candidatos ?? []}
        loading={consulta.isPending}
        pagination={false}
        onRow={(r) => ({
          onClick: () => {
            if (r.personaId) {
              inusualForm.setFieldsValue({
                personaId: r.personaId,
                creditoId: r.creditoId ?? undefined,
              })
            }
          },
        })}
      />

      <Form
        form={inusualForm}
        layout="inline"
        onFinish={(v) => registrar.mutate(v)}
        initialValues={{ calificacion: 'INUSUAL' }}
        style={{ marginTop: 16, marginBottom: 12 }}
      >
        <Form.Item name="personaId" label="Persona" rules={[{ required: true }]}>
          <InputNumber min={1} />
        </Form.Item>
        <Form.Item name="creditoId" label="Crédito">
          <InputNumber min={1} />
        </Form.Item>
        <Form.Item name="calificacion" label="Calificación" rules={[{ required: true }]}>
          <Select
            style={{ minWidth: 140 }}
            options={[
              { value: 'INUSUAL', label: 'Inusual' },
              { value: 'SOSPECHOSA', label: 'Sospechosa' },
              { value: 'DESCARTADA', label: 'Descartada' },
            ]}
          />
        </Form.Item>
        <Form.Item name="motivo" label="Motivo" rules={[{ required: true }]}>
          <Input maxLength={500} style={{ minWidth: 220 }} />
        </Form.Item>
        <Button htmlType="submit" loading={registrar.isPending}>
          Anotar
        </Button>
      </Form>

      <CredixDataTable<PlaInusual>
        rowKey="plaInusualId"
        columns={INUSUAL_COLUMNS}
        dataSource={consulta.data?.bitacora ?? []}
        pagination={false}
      />

      <p style={{ marginTop: 12 }}>
        El ROS a UIF lo envía el oficial por ROSEL.{' '}
        <Link to="/informes">Volver a informes</Link>
      </p>
    </CredixInformePage>
  )
}
