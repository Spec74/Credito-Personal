import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Button, Calendar, DatePicker, Empty, Form, Grid, Input, Modal, Radio, Select, Switch } from 'antd'
import type { Dayjs } from 'dayjs'
import dayjs from 'dayjs'
import { fetchAgenda, saveAgenda, bajaAgenda, type AgendaEvento } from '../../api/intranet'
import { ApiError } from '../../api/errors'
import { useAuth } from '../../auth/useAuth'
import { CredixPage } from '../../components/credix'
import { esCreditoAdministrador, esCreditoEncargado } from '../../utils/creditoOperacionPermisos'

type FormValues = {
  titulo: string
  notas?: string
  cuando: Dayjs
  todoElDia: boolean
  recordatorioMinutos?: number | null
  alcance: 'PERSONAL' | 'OFICINA'
}

export function AgendaPage() {
  const { session } = useAuth()
  const screens = Grid.useBreakpoint()
  const movil = !screens.md
  const qc = useQueryClient()
  const puedeSede =
    esCreditoAdministrador(session?.roles ?? []) || esCreditoEncargado(session?.roles ?? [])
  const [mes, setMes] = useState(dayjs())
  const [dia, setDia] = useState(dayjs())
  const [open, setOpen] = useState(false)
  const [form] = Form.useForm<FormValues>()

  const rango = useMemo(() => {
    const desde = mes.startOf('month').subtract(7, 'day')
    const hasta = mes.endOf('month').add(7, 'day')
    return { desde: desde.toISOString(), hasta: hasta.toISOString() }
  }, [mes])

  const eventos = useQuery({
    queryKey: ['agenda', rango.desde, rango.hasta],
    queryFn: () => fetchAgenda(rango.desde, rango.hasta),
  })

  const porDia = useMemo(() => {
    const map = new Map<string, AgendaEvento[]>()
    for (const e of eventos.data ?? []) {
      const key = dayjs(e.fechaIni).format('YYYY-MM-DD')
      const list = map.get(key) ?? []
      list.push(e)
      map.set(key, list)
    }
    return map
  }, [eventos.data])

  const delDia = porDia.get(dia.format('YYYY-MM-DD')) ?? []

  const guardar = useMutation({
    mutationFn: (v: FormValues) =>
      saveAgenda({
        titulo: v.titulo.trim(),
        notas: v.notas?.trim() || null,
        fechaIni: (v.todoElDia ? v.cuando.startOf('day') : v.cuando).toISOString(),
        todoElDia: v.todoElDia,
        recordatorioMinutos: v.recordatorioMinutos ?? null,
        alcance: v.alcance,
      }),
    onSuccess: () => {
      setOpen(false)
      form.resetFields()
      void qc.invalidateQueries({ queryKey: ['agenda'] })
      void qc.invalidateQueries({ queryKey: ['bandeja'] })
    },
  })

  const borrar = useMutation({
    mutationFn: bajaAgenda,
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['agenda'] })
      void qc.invalidateQueries({ queryKey: ['bandeja'] })
    },
  })

  const abrirNuevo = (cuando: Dayjs) => {
    form.setFieldsValue({ cuando: cuando.hour(9).minute(0) })
    setOpen(true)
  }

  return (
    <CredixPage
      title="Agenda"
      subtitle="Recordatorios personales o de la sede. La campana avisa a la hora pactada."
      breadcrumb={[
        { title: <Link to="/inicio">Inicio</Link> },
        { title: 'Agenda' },
      ]}
      actions={
        <Button type="primary" onClick={() => abrirNuevo(dia)}>
          Nuevo
        </Button>
      }
    >
      {guardar.error instanceof ApiError ? (
        <p className="credix-field-error">{guardar.error.message}</p>
      ) : null}

      {movil ? (
        <>
          <DatePicker
            value={dia}
            onChange={(v) => {
              if (!v) return
              setDia(v)
              setMes(v)
            }}
            style={{ width: '100%', marginBottom: 12 }}
            allowClear={false}
          />
          <p className="credix-agenda-mobile__day">{dia.format('dddd D [de] MMMM')}</p>
          <ListaDia
            eventos={delDia}
            onQuitar={(id) => void borrar.mutate(id)}
            quitando={borrar.isPending}
          />
        </>
      ) : (
        <Calendar
          value={mes}
          onPanelChange={(v) => setMes(v)}
          onSelect={(v) => {
            setMes(v)
            setDia(v)
          }}
          cellRender={(value, info) => {
            if (info.type !== 'date') return info.originNode
            const list = porDia.get(value.format('YYYY-MM-DD')) ?? []
            return (
              <ul className="credix-agenda-day">
                {list.map((e) => (
                  <li key={e.eventoId}>{e.titulo}</li>
                ))}
              </ul>
            )
          }}
        />
      )}

      {!movil && delDia.length > 0 ? (
        <>
          <p className="credix-agenda-mobile__day" style={{ marginTop: 16 }}>
            {dia.format('dddd D [de] MMMM')}
          </p>
          <ListaDia
            eventos={delDia}
            onQuitar={(id) => void borrar.mutate(id)}
            quitando={borrar.isPending}
          />
        </>
      ) : null}

      <Modal
        title="Recordatorio"
        open={open}
        onCancel={() => setOpen(false)}
        onOk={() => form.submit()}
        confirmLoading={guardar.isPending}
        destroyOnClose
      >
        <Form
          form={form}
          layout="vertical"
          initialValues={{ alcance: 'PERSONAL', todoElDia: false, recordatorioMinutos: 30 }}
          onFinish={(v) => guardar.mutate(v)}
        >
          <Form.Item name="titulo" label="Qué" rules={[{ required: true, message: 'Escriba qué hay que recordar' }]}>
            <Input maxLength={160} placeholder="Ej. Cierre de caja / Visita a Huanta" />
          </Form.Item>
          <Form.Item name="cuando" label="Cuándo" rules={[{ required: true }]}>
            <DatePicker showTime={{ format: 'HH:mm' }} style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item name="todoElDia" label="Todo el día" valuePropName="checked">
            <Switch />
          </Form.Item>
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
          <Form.Item name="alcance" label="Quién">
            <Radio.Group>
              <Radio value="PERSONAL">Solo yo</Radio>
              {puedeSede ? <Radio value="OFICINA">Esta sede</Radio> : null}
            </Radio.Group>
          </Form.Item>
          <Form.Item name="notas" label="Notas">
            <Input.TextArea rows={2} maxLength={1000} />
          </Form.Item>
        </Form>
      </Modal>
    </CredixPage>
  )
}

function ListaDia({
  eventos,
  onQuitar,
  quitando,
}: {
  eventos: AgendaEvento[]
  onQuitar: (id: number) => void
  quitando: boolean
}) {
  if (eventos.length === 0) {
    return <Empty description="Nada agendado este día" />
  }
  return (
    <ul className="credix-agenda-list">
      {eventos.map((e) => (
        <li key={e.eventoId}>
          <div>
            <strong>{e.titulo}</strong>
            <div className="credix-comunicado-card__meta">
              <span>{e.todoElDia ? 'Todo el día' : dayjs(e.fechaIni).format('HH:mm')}</span>
              <span>{e.alcance === 'OFICINA' ? 'Sede' : 'Personal'}</span>
              {e.notas ? <span>{e.notas}</span> : null}
            </div>
          </div>
          {e.esMio ? (
            <Button type="link" size="small" loading={quitando} onClick={() => onQuitar(e.eventoId)}>
              Quitar
            </Button>
          ) : null}
        </li>
      ))}
    </ul>
  )
}
