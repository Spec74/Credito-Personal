import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Badge, Button, Popover } from 'antd'
import { BellOutlined } from '@ant-design/icons'
import { descartarAgenda, fetchBandeja, marcarComunicadoLeido, type BandejaItem } from '../../api/intranet'
import { formatRelativo } from '../../utils/formatFecha'

export function CredixNotifyBell() {
  const navigate = useNavigate()
  const qc = useQueryClient()
  const [open, setOpen] = useState(false)

  const bandeja = useQuery({
    queryKey: ['bandeja'],
    queryFn: fetchBandeja,
    staleTime: 30_000,
    refetchInterval: 60_000,
  })

  const items = bandeja.data ?? []

  const ack = useMutation({
    mutationFn: async (row: BandejaItem) => {
      if (row.tipo === 'COMUNICADO') await marcarComunicadoLeido(row.id)
      else await descartarAgenda(row.id)
    },
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['bandeja'] })
      void qc.invalidateQueries({ queryKey: ['comunicados'] })
    },
  })

  const marcarTodos = useMutation({
    mutationFn: async () => {
      await Promise.all(
        items.map((n) =>
          n.tipo === 'COMUNICADO' ? marcarComunicadoLeido(n.id) : descartarAgenda(n.id),
        ),
      )
    },
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['bandeja'] })
      void qc.invalidateQueries({ queryKey: ['comunicados'] })
    },
  })

  const abrir = (n: BandejaItem) => {
    void ack.mutateAsync(n)
    setOpen(false)
    navigate(n.tipo === 'COMUNICADO' ? `/comunicados?id=${n.id}` : `/agenda?id=${n.id}`)
  }

  return (
    <Popover
      trigger="click"
      placement="bottomRight"
      open={open}
      onOpenChange={setOpen}
      arrow={false}
      rootClassName="credix-notify-popover"
      content={
        <div className="credix-notify-panel" role="dialog" aria-label="Avisos y recordatorios">
          <div className="credix-notify-panel__head">
            <strong>{items.length === 0 ? 'Nada pendiente' : `${items.length} sin ver`}</strong>
            {items.length > 0 ? (
              <Button
                type="link"
                size="small"
                loading={marcarTodos.isPending}
                onClick={() => void marcarTodos.mutate()}
              >
                Marcar todo
              </Button>
            ) : null}
          </div>

          {items.length === 0 ? (
            <div className="credix-notify-panel__empty">
              <p>Está al día. Los avisos y recordatorios aparecen aquí.</p>
              <nav>
                <Link to="/comunicados" onClick={() => setOpen(false)}>
                  Avisos
                </Link>
                <Link to="/agenda" onClick={() => setOpen(false)}>
                  Agenda
                </Link>
              </nav>
            </div>
          ) : (
            <ul className="credix-notify-panel__list">
              {items.map((n) => (
                <li key={`${n.tipo}-${n.id}`}>
                  <button type="button" className="credix-notify-row" onClick={() => abrir(n)}>
                    <i
                      className={`credix-notify-row__dot${n.tipo === 'AGENDA' ? ' is-agenda' : ''}`}
                      aria-hidden
                    />
                    <div>
                      <strong>{n.titulo}</strong>
                      <span>{n.tipo === 'COMUNICADO' ? 'Aviso' : 'Recordatorio'}</span>
                    </div>
                    <time dateTime={n.fecha}>{formatRelativo(n.fecha)}</time>
                  </button>
                </li>
              ))}
            </ul>
          )}

          <div className="credix-notify-panel__foot">
            <Link to="/comunicados" onClick={() => setOpen(false)}>
              Ver avisos
            </Link>
            <Link to="/agenda" onClick={() => setOpen(false)}>
              Ver agenda
            </Link>
          </div>
        </div>
      }
    >
      <Badge count={items.length} size="small" overflowCount={9} offset={[-2, 4]}>
        <Button
          type="text"
          className="credix-notify-trigger"
          icon={<BellOutlined />}
          aria-label={items.length > 0 ? `Avisos, ${items.length} sin ver` : 'Avisos'}
        />
      </Badge>
    </Popover>
  )
}
