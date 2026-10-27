/**
 * Frases de arranque en el login (una por visita).
 *
 * Lo que motiva de verdad en mostrador no es un póster de celebridad:
 * autonomía, oficio y vínculo (Deci/Ryan). Por eso el banco mezcla
 * líneas propias (confianza / atención / el día) con pocas citas cortas
 * muy conocidas. Nada de caja, mora ni procesos internos: la URL es pública.
 */
export type LoginFrase = {
  texto: string
  fuente: string | null
}

export const LOGIN_FRASES: readonly LoginFrase[] = [
  { texto: 'Hoy alguien pone su confianza en tu trabajo.', fuente: null },
  { texto: 'La sede se nota en cómo atiendes, no en el letrero.', fuente: null },
  { texto: 'Haz bien lo de hoy. El resto llega solo.', fuente: null },
  { texto: 'Cada persona que atiendes recuerda cómo la trataste.', fuente: null },
  { texto: 'La confianza se gana en silencio, trámite a trámite.', fuente: null },
  { texto: 'Empieza el día con orden. El cliente lo siente.', fuente: null },
  {
    texto: 'Siempre parece imposible hasta que se hace.',
    fuente: 'Nelson Mandela',
  },
  {
    texto: 'No es que tengamos poco tiempo, es que perdemos mucho.',
    fuente: 'Séneca',
  },
  {
    texto: 'La confianza llega a pie y se va a caballo.',
    fuente: 'Refrán',
  },
  {
    texto: 'Si quieres llegar lejos, ve acompañado.',
    fuente: 'Proverbio',
  },
  {
    texto: 'El que mueve montañas empieza apartando piedras.',
    fuente: 'Confucio',
  },
  { texto: 'El carácter se forja en lo que haces cada día.', fuente: null },
]

export function pickLoginFrase(random: () => number = Math.random): LoginFrase {
  const i = Math.floor(random() * LOGIN_FRASES.length) % LOGIN_FRASES.length
  return LOGIN_FRASES[i]!
}

export function saludoSegunHora(now: Date = new Date()): string {
  const h = now.getHours()
  if (h >= 5 && h < 12) {
    return 'Buenos días'
  }
  if (h >= 12 && h < 19) {
    return 'Buenas tardes'
  }
  return 'Buenas noches'
}

/** Fecha larga en es-PE para anclar el día de trabajo (no es un KPI). */
export function fechaLargaEsPe(now: Date = new Date()): string {
  const raw = new Intl.DateTimeFormat('es-PE', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
  }).format(now)
  return raw.charAt(0).toUpperCase() + raw.slice(1)
}
