import { useNavigate } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Badge, Button, Dropdown } from 'antd'
import { BellOutlined } from '@ant-design/icons'
import { descartarAgenda, fetchBandeja, marcarComunicadoLeido } from '../../api/intranet'
import { formatFecha } from '../../utils/formatFecha'

export function CredixNotifyBell() {
  const navigate = useNavigate()
  const qc = useQueryClient()
  const bandeja = useQuery({
    queryKey: ['bandeja'],
    queryFn: fetchBandeja,
    staleTime: 30_000,
    refetchInterval: 60_000,
  })

  const items = bandeja.data ?? []

  const ack = useMutation({
    mutationFn: async (row: { tipo: string; id: number }) => {
      if (row.tipo === 'COMUNICADO') await marcarComunicadoLeido(row.id)
      else await descartarAgenda(row.id)
    },
    onSuccess: () => void qc.invalidateQueries({ queryKey: ['bandeja'] }),
  })

  return (
    <Dropdown
      trigger={['click']}
      placement="bottomRight"
      overlayClassName="credix-notify-dropdown"
      menu={{
        items: [
          ...items.slice(0, 8).map((n) => ({
            key: `${n.tipo}-${n.id}`,
            label: (
              <button
                type="button"
                className="credix-notify-item"
                onClick={() => {
                  void ack.mutateAsync(n)
                  navigate(n.tipo === 'COMUNICADO' ? '/comunicados' : '/agenda')
                }}
              >
                <strong>{n.titulo}</strong>
                <span>{n.tipo === 'COMUNICADO' ? 'Aviso' : 'Agenda'} · {formatFecha(n.fecha)}</span>
              </button>
            ),
          })),
          items.length === 0
            ? { key: 'empty', label: 'Sin avisos nuevos', disabled: true }
            : {
                key: 'all',
                label: 'Ver bandeja',
                onClick: () => navigate('/comunicados'),
              },
        ],
      }}
    >
      <Badge count={items.length} size="small" overflowCount={9}>
        <Button type="text" icon={<BellOutlined />} aria-label="Avisos" />
      </Badge>
    </Dropdown>
  )
}
