import dayjs from 'dayjs'

/** Convierte DD/MM/YYYY (MVC) a YYYY-MM-DD (API). */
export function legacyDateToApi(dmy: string): string {
  const d = dayjs(dmy, 'DD/MM/YYYY', true)
  return d.isValid() ? d.format('YYYY-MM-DD') : dmy
}
