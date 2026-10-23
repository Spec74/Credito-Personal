import { Checkbox, Form, Select } from 'antd'
import type { Dayjs } from 'dayjs'
import { CredixDatePicker } from '../../../components/credix'
import { aplicarFinHora, aplicarInicio, minutosDelDia, opcionesHora } from './agendaHoras'

function popupEnBody() {
  return document.body
}

export function AgendaCuandoFields({ disabled }: { disabled?: boolean }) {
  const form = Form.useFormInstance()
  const todo = Form.useWatch('todoElDia', form) as boolean | undefined
  const cuando = Form.useWatch('cuando', form) as Dayjs | undefined
  const hasta = Form.useWatch('hasta', form) as Dayjs | undefined

  const minIni = cuando?.isValid() ? minutosDelDia(cuando) : 9 * 60
  const minFin = hasta?.isValid() ? minutosDelDia(hasta) : minIni + 60

  const setFecha = (d: Dayjs | null) => {
    if (!d) return
    const base = cuando?.isValid() ? cuando : d.hour(9).minute(0)
    const next = d.hour(base.hour()).minute(base.minute()).second(0).millisecond(0)
    form.setFieldsValue(aplicarInicio(base, hasta, next))
  }

  const setHoraIni = (minutos: number) => {
    const dia = (cuando?.isValid() ? cuando : hasta)?.startOf('day')
    if (!dia) return
    const next = dia.add(minutos, 'minute')
    form.setFieldsValue(aplicarInicio(cuando ?? next, hasta, next))
  }

  const setHoraFin = (minutos: number) => {
    if (!cuando?.isValid()) return
    form.setFieldsValue({ hasta: aplicarFinHora(cuando, minutos) })
  }

  return (
    <div className="credix-cuando">
      <Form.Item name="cuando" hidden rules={[{ required: true, message: 'Elija el día' }]} />
      <Form.Item name="hasta" hidden />

      <div className="credix-cuando__fecha">
        <CredixDatePicker
          allowClear={false}
          disabled={disabled}
          value={cuando ?? null}
          format="dddd, D [de] MMMM"
          aria-label="Día"
          onChange={setFecha}
        />
      </div>

      {todo ? null : (
        <div className="credix-cuando__horas">
          <Select
            aria-label="Empieza"
            disabled={disabled}
            value={minIni}
            options={opcionesHora(minIni)}
            onChange={setHoraIni}
            listHeight={280}
            getPopupContainer={popupEnBody}
            popupMatchSelectWidth
          />
          <span className="credix-cuando__sep" aria-hidden>
            –
          </span>
          <Select
            aria-label="Termina"
            disabled={disabled}
            value={minFin}
            options={opcionesHora(minFin)}
            onChange={setHoraFin}
            listHeight={280}
            getPopupContainer={popupEnBody}
            popupMatchSelectWidth
          />
        </div>
      )}

      <Form.Item name="todoElDia" valuePropName="checked" className="credix-cuando-allday">
        <Checkbox
          disabled={disabled}
          onChange={(e) => {
            if (e.target.checked || !cuando?.isValid()) return
            const base = cuando.hour(cuando.hour() || 9).minute(cuando.minute()).second(0)
            form.setFieldsValue(aplicarInicio(base, undefined, base))
          }}
        >
          Todo el día
        </Checkbox>
      </Form.Item>
    </div>
  )
}
