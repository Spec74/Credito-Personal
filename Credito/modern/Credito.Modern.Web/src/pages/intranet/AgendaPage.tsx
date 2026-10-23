import { useEffect, useMemo, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Button, Drawer, Form, Grid, Input, Modal, Radio, Segmented, Select, Space } from 'antd'
import { LeftOutlined, PlusOutlined, RightOutlined } from '@ant-design/icons'
import type { Dayjs } from 'dayjs'
import dayjs from 'dayjs'
import { bajaAgenda, fetchAgenda, saveAgenda, type AgendaEvento } from '../../api/intranet'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { CredixPage } from '../../components/credix'
import { esCreditoAdministrador, esCreditoEncargado } from '../../utils/creditoOperacionPermisos'
import { AgendaCuandoFields } from './agenda/AgendaCuandoFields'
import { AgendaMiniMonth } from './agenda/AgendaMiniMonth'
import { AgendaMonthView } from './agenda/AgendaMonthView'
import { AgendaSchedule } from './agenda/AgendaSchedule'
import { AgendaTimeGrid } from './agenda/AgendaTimeGrid'
import {
  DURACION_DEFAULT_MIN,
  type VistaAgenda,
  diasDeVista,
  finEvento,
  moverVista,
  tituloPeriodo,
} from './agenda/agendaLayout'
import '../../styles/credix-agenda.css'

type FormValues = {
  titulo: string
  notas?: string
  cuando: Dayjs
  hasta?: Dayjs
  todoElDia: boolean
  recordatorioMinutos?: number | null
  alcance: 'PERSONAL' | 'OFICINA'
}

const VISTA_KEY = 'credix.agenda.vista'
const CAL_KEY = 'credix.agenda.calendarios'

function leerVista(movil: boolean): VistaAgenda {
  try {
    const raw = localStorage.getItem(VISTA_KEY) as VistaAgenda | null
    if (raw === 'dia' || raw === 'mes' || raw === 'agenda' || raw === 'semana' || raw === 'tres') {
      if (movil && raw === 'semana') return 'tres'
      if (!movil && raw === 'tres') return 'semana'
      return raw
    }
  } catch {
    /* ignore */
  }
  return movil ? 'agenda' : 'semana'
}

function leerCals(): { personal: boolean; sede: boolean } {
  try {
    const raw = localStorage.getItem(CAL_KEY)
    if (raw) return JSON.parse(raw) as { personal: boolean; sede: boolean }
  } catch {
    /* ignore */
  }
  return { personal: true, sede: true }
}

export function AgendaPage() {
  const { session } = useAuth()
  const screens = Grid.useBreakpoint()
  const movil = screens.md === false
  const qc = useQueryClient()
  const [params, setParams] = useSearchParams()
  const puedeSede =
    esCreditoAdministrador(session?.roles ?? []) || esCreditoEncargado(session?.roles ?? [])
  const [ancla, setAncla] = useState(dayjs())
  const [vista, setVista] = useState<VistaAgenda>(() => leerVista(false))
  const [cals, setCals] = useState(leerCals)
  const [open, setOpen] = useState(false)
  const [editando, setEditando] = useState<AgendaEvento | null>(null)
  const [now, setNow] = useState(dayjs())
  const [form] = Form.useForm<FormValues>()

  useEffect(() => {
    setVista(leerVista(movil))
  }, [movil])

  useEffect(() => {
    const t = window.setInterval(() => setNow(dayjs()), 60_000)
    return () => window.clearInterval(t)
  }, [])

  const rango = useMemo(() => {
    const desde = ancla.startOf('month').subtract(7, 'day')
    const hasta = ancla.endOf('month').add(21, 'day')
    return { desde: desde.toISOString(), hasta: hasta.toISOString() }
  }, [ancla])

  const eventosQ = useQuery({
    queryKey: ['agenda', rango.desde, rango.hasta],
    queryFn: () => fetchAgenda(rango.desde, rango.hasta),
  })

  const eventos = useMemo(() => {
    return (eventosQ.data ?? []).filter((e) => {
      if (e.alcance === 'OFICINA') return cals.sede
      return cals.personal
    })
  }, [eventosQ.data, cals])

  const guardar = useMutation({
    mutationFn: (v: FormValues) => {
      const ini = v.todoElDia ? v.cuando.startOf('day') : v.cuando
      const fin = v.todoElDia
        ? v.cuando.endOf('day')
        : (v.hasta && v.hasta.isAfter(ini) ? v.hasta : ini.add(DURACION_DEFAULT_MIN, 'minute'))
      return saveAgenda({
        eventoId: editando?.eventoId ?? null,
        titulo: v.titulo.trim(),
        notas: v.notas?.trim() || null,
        fechaIni: ini.toISOString(),
        fechaFin: fin.toISOString(),
        todoElDia: v.todoElDia,
        recordatorioMinutos: v.recordatorioMinutos ?? null,
        alcance: v.alcance,
      })
    },
    onSuccess: () => {
      setOpen(false)
      setEditando(null)
      form.resetFields()
      void qc.invalidateQueries({ queryKey: ['agenda'] })
      void qc.invalidateQueries({ queryKey: ['bandeja'] })
    },
  })

  const borrar = useMutation({
    mutationFn: bajaAgenda,
    onSuccess: () => {
      setOpen(false)
      setEditando(null)
      void qc.invalidateQueries({ queryKey: ['agenda'] })
      void qc.invalidateQueries({ queryKey: ['bandeja'] })
    },
  })

  const abrirNuevo = (cuando: Dayjs, todoElDia: boolean) => {
    setEditando(null)
    form.resetFields()
    const start = todoElDia ? cuando.startOf('day') : cuando
    form.setFieldsValue({
      cuando: start,
      hasta: todoElDia ? undefined : start.add(DURACION_DEFAULT_MIN, 'minute'),
      alcance: 'PERSONAL',
      todoElDia,
      recordatorioMinutos: 30,
    })
    setOpen(true)
  }

  const abrirEvento = (e: AgendaEvento) => {
    setEditando(e)
    setAncla(dayjs(e.fechaIni))
    form.setFieldsValue({
      titulo: e.titulo,
      notas: e.notas ?? undefined,
      cuando: dayjs(e.fechaIni),
      hasta: e.todoElDia ? undefined : finEvento(e),
      todoElDia: e.todoElDia,
      recordatorioMinutos: e.recordatorioMinutos,
      alcance: e.alcance === 'OFICINA' ? 'OFICINA' : 'PERSONAL',
    })
    setOpen(true)
  }

  useEffect(() => {
    const raw = params.get('id')
    if (!raw || !eventosQ.data) return
    const id = Number(raw)
    const hit = eventosQ.data.find((e) => e.eventoId === id)
    if (!hit) return
    setEditando(hit)
    setAncla(dayjs(hit.fechaIni))
    form.setFieldsValue({
      titulo: hit.titulo,
      notas: hit.notas ?? undefined,
      cuando: dayjs(hit.fechaIni),
      hasta: hit.todoElDia ? undefined : finEvento(hit),
      todoElDia: hit.todoElDia,
      recordatorioMinutos: hit.recordatorioMinutos,
      alcance: hit.alcance === 'OFICINA' ? 'OFICINA' : 'PERSONAL',
    })
    setOpen(true)
    const next = new URLSearchParams(params)
    next.delete('id')
    setParams(next, { replace: true })
  }, [params, eventosQ.data, form, setParams])

  const confirmarQuitar = (e: AgendaEvento) => {
    Modal.confirm({
      title: '¿Quitar este evento?',
      content: e.titulo,
      okText: 'Quitar',
      okButtonProps: { danger: true },
      cancelText: 'Cancelar',
      onOk: () => borrar.mutateAsync(e.eventoId),
    })
  }

  const cambiarVista = (v: VistaAgenda) => {
    setVista(v)
    try {
      localStorage.setItem(VISTA_KEY, v)
    } catch {
      /* ignore */
    }
  }

  const cambiarCals = (next: { personal: boolean; sede: boolean }) => {
    setCals(next)
    try {
      localStorage.setItem(CAL_KEY, JSON.stringify(next))
    } catch {
      /* ignore */
    }
  }

  const irHoy = () => setAncla(dayjs())
  const soloLectura = editando != null && !editando.esMio
  const diasGrid = vista === 'dia' || vista === 'semana' || vista === 'tres' ? diasDeVista(vista, ancla) : []
  const opcionesVista: { label: string; value: VistaAgenda }[] = movil
    ? [
        { label: 'Agenda', value: 'agenda' },
        { label: 'Día', value: 'dia' },
        { label: '3 días', value: 'tres' },
        { label: 'Mes', value: 'mes' },
      ]
    : [
        { label: 'Día', value: 'dia' },
        { label: 'Semana', value: 'semana' },
        { label: 'Mes', value: 'mes' },
        { label: 'Agenda', value: 'agenda' },
      ]

  return (
    <CredixPage
      className="credix-page--agenda"
      title="Agenda"
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: 'Agenda' },
      ]}
      actions={
        movil ? undefined : (
          <Button type="primary" icon={<PlusOutlined />} onClick={() => abrirNuevo(ancla.hour(9).minute(0), false)}>
            Crear
          </Button>
        )
      }
    >
      {guardar.error instanceof ApiError ? (
        <p className="credix-field-error">{guardar.error.message}</p>
      ) : null}

      <div className="credix-gcal-toolbar">
        <div className="credix-gcal-toolbar__nav">
          <Button onClick={irHoy}>Hoy</Button>
          <Button
            icon={<LeftOutlined />}
            aria-label="Anterior"
            onClick={() => setAncla((d) => moverVista(vista, d, -1))}
          />
          <Button
            icon={<RightOutlined />}
            aria-label="Siguiente"
            onClick={() => setAncla((d) => moverVista(vista, d, 1))}
          />
          <h2>{tituloPeriodo(vista, ancla)}</h2>
        </div>
        <Segmented<VistaAgenda> value={vista} onChange={cambiarVista} options={opcionesVista} />
      </div>

      {movil ? (
        <CalendariosFiltro cals={cals} onChange={cambiarCals} compact />
      ) : null}

      <div className={`credix-gcal${movil ? ' is-movil' : ''}`}>
        {movil ? null : (
          <aside className="credix-gcal__side">
            <AgendaMiniMonth value={ancla} eventos={eventos} onChange={setAncla} />
            <CalendariosFiltro cals={cals} onChange={cambiarCals} />
          </aside>
        )}

        <div className="credix-gcal__main">
          {vista === 'mes' ? (
            <AgendaMonthView
              ancla={ancla}
              eventos={eventos}
              onCrear={abrirNuevo}
              onAbrir={abrirEvento}
              crearAlClic={!movil}
              onIrDia={(d) => {
                setAncla(d)
                cambiarVista('dia')
              }}
            />
          ) : null}
          {vista === 'agenda' ? (
            <AgendaSchedule
              desde={ancla.startOf('day')}
              dias={21}
              eventos={eventos}
              onAbrir={abrirEvento}
              onCrear={abrirNuevo}
            />
          ) : null}
          {vista === 'dia' || vista === 'semana' || vista === 'tres' ? (
            <AgendaTimeGrid
              dias={diasGrid}
              eventos={eventos}
              now={now}
              movil={movil}
              onCrear={abrirNuevo}
              onAbrir={abrirEvento}
            />
          ) : null}
        </div>
      </div>

      {movil ? (
        <button
          type="button"
          className="credix-gcal-fab"
          aria-label="Crear evento"
          onClick={() => abrirNuevo(ancla.hour(9).minute(0), false)}
        >
          <PlusOutlined />
        </button>
      ) : null}

      <Drawer
        title={editando ? (soloLectura ? 'Evento' : 'Editar evento') : 'Nuevo evento'}
        open={open}
        onClose={() => {
          setOpen(false)
          setEditando(null)
        }}
        width={movil ? '100%' : 420}
        destroyOnHidden
        extra={
          soloLectura ? null : (
            <Space>
              {editando?.esMio ? (
                <Button danger loading={borrar.isPending} onClick={() => editando && confirmarQuitar(editando)}>
                  Quitar
                </Button>
              ) : null}
              <Button type="primary" loading={guardar.isPending} onClick={() => form.submit()}>
                Guardar
              </Button>
            </Space>
          )
        }
      >
        <Form
          form={form}
          layout="vertical"
          disabled={soloLectura}
          initialValues={{ alcance: 'PERSONAL', todoElDia: false, recordatorioMinutos: 30 }}
          onFinish={(v) => guardar.mutate(v)}
        >
          <Form.Item name="titulo" label="Título" rules={[{ required: true, message: 'Escriba qué hay que recordar' }]}>
            <Input maxLength={160} placeholder="Ej. Cierre de caja / Visita a Huanta" />
          </Form.Item>
          <AgendaCuandoFields disabled={soloLectura} />
          <Form.Item name="recordatorioMinutos" label="Avisarme">
            <Select
              allowClear
              options={[
                { value: 15, label: '15 minutos antes' },
                { value: 30, label: '30 minutos antes' },
                { value: 60, label: '1 hora antes' },
                { value: 1440, label: '1 día antes' },
              ]}
            />
          </Form.Item>
          <Form.Item name="alcance" label="Calendario">
            <Radio.Group>
              <Radio value="PERSONAL">Personal</Radio>
              {puedeSede ? <Radio value="OFICINA">Esta sede</Radio> : null}
            </Radio.Group>
          </Form.Item>
          <Form.Item name="notas" label="Notas">
            <Input.TextArea rows={3} maxLength={1000} />
          </Form.Item>
        </Form>
      </Drawer>
    </CredixPage>
  )
}

function CalendariosFiltro({
  cals,
  onChange,
  compact = false,
}: {
  cals: { personal: boolean; sede: boolean }
  onChange: (next: { personal: boolean; sede: boolean }) => void
  compact?: boolean
}) {
  return (
    <div className={`credix-gcal-cals${compact ? ' credix-gcal-cals--row' : ''}`}>
      {compact ? null : <p>Calendarios</p>}
      <label>
        <input
          type="checkbox"
          checked={cals.personal}
          onChange={(e) => onChange({ ...cals, personal: e.target.checked })}
        />
        <i className="credix-gcal-swatch" />
        Personal
      </label>
      <label>
        <input
          type="checkbox"
          checked={cals.sede}
          onChange={(e) => onChange({ ...cals, sede: e.target.checked })}
        />
        <i className="credix-gcal-swatch is-sede" />
        Esta sede
      </label>
    </div>
  )
}
