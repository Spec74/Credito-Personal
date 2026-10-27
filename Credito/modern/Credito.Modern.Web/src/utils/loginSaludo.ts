import { saludoSegunHora } from './loginFrase'

/**
 * Apodos de oficio (SDT: competencia, vínculo, autonomía).
 * Vocativos invariables o epicenos. Nada de “crack”, “máquina” ni caja/mora.
 */
export const LOGIN_APODOS: readonly string[] = [
  'constante',
  'puntual',
  'preciso',
  'claro',
  'firme',
  'sereno',
  'ágil',
  'presente',
  'cercano',
  'humano',
  'cálido',
  'paciente',
  'leal',
  'cordial',
  'humilde',
  'abierto',
  'cumplido',
  'prudente',
  'cabal',
  'entero',
  'templado',
  'estable',
  'sobrio',
  'juicioso',
  'certero',
  'pulcro',
  'aplicado',
  'recto',
  'honesto',
  'íntegro',
  'discreto',
  'tenaz',
  'perseverante',
  'disciplinado',
  'capaz',
  'hábil',
  'práctico',
  'concreto',
  'realista',
  'ecuánime',
  'ponderado',
  'mesurado',
  'alerta',
  'despierto',
  'lúcido',
  'sagaz',
  'diestro',
  'fino',
  'sólido',
  'exacto',
  'limpio',
  'metódico',
  'resolutivo',
  'efectivo',
  'responsable',
  'considerado',
  'respetuoso',
  'generoso',
  'solidario',
  'empático',
  'acogedor',
  'servicial',
  'comprensivo',
  'disponible',
  'emprendedor',
  'inquieto',
  'curioso',
  'valiente',
  'decidido',
  'autónomo',
  'creativo',
  'audaz',
  'pionero',
  'estratega',
  'referente',
  'apoyo',
  'criterio',
  'temple',
  'oficio',
  'tino',
  'pulso',
  'calma',
  'orden',
  'detalle',
  'juicio',
  'brío',
  'mesura',
  'templanza',
  'cuidado',
  'escucha',
  'presencia',
  'confianza',
  'temple firme',
  'buen tino',
  'buen pulso',
  'buena calma',
  'buen criterio',
  'paso firme',
  'mano serena',
  'ojo claro',
  'trato justo',
  'oído atento',
  'palabra clara',
  'gesto humano',
  'día ordenado',
  'sede presente',
]

export const LOGIN_LINEAS: readonly string[] = [
  'Hoy se nota el oficio.',
  'El mostrador ya te espera.',
  'Paso a paso.',
  'La confianza se cuida.',
  'Orden primero.',
  'Atiende con calma.',
  'Hoy cuenta cada trámite.',
  'La sede se siente.',
  'Criterio antes que prisa.',
  'Lo claro se entiende.',
  'La calma se contagia.',
  'Detalle a detalle.',
  'Una persona a la vez.',
  'La fila merece respeto.',
  'Bien hecho, en silencio.',
  'El cliente lo recuerda.',
  'Hoy, presencia.',
  'Sin prisa, sin pausa.',
  'Tu criterio abre el día.',
  'Con temple.',
  'Hoy, trato justo.',
  'La sede te necesita.',
  'Escucha primero.',
  'Hoy, orden y calma.',
  'Cada gesto cuenta.',
  'Hoy, palabra clara.',
  'El oficio es constante.',
  'Hoy, buen tino.',
  'La confianza llega despacio.',
  'Hoy, oído atento.',
  'Hazlo bien, una vez.',
  'Hoy, sede en calma.',
  'El día se gana en el mostrador.',
  'Hoy, criterio sereno.',
  'La prisa no atiende.',
  'Hoy, pulso firme.',
]

export type LoginSaludoDia = {
  saludo: string
  apodo: string
  linea: string
}

export function daySeed(now: Date): number {
  const key = `${now.getFullYear()}-${now.getMonth() + 1}-${now.getDate()}`
  let h = 2166136261
  for (let i = 0; i < key.length; i += 1) {
    h ^= key.charCodeAt(i)
    h = Math.imul(h, 16777619)
  }
  return h >>> 0
}

export function capitalizarApodo(apodo: string): string {
  return apodo.charAt(0).toUpperCase() + apodo.slice(1)
}

export function pickLoginSaludoDia(now: Date = new Date()): LoginSaludoDia {
  const seed = daySeed(now)
  const apodo = LOGIN_APODOS[seed % LOGIN_APODOS.length]!
  const linea = LOGIN_LINEAS[Math.floor(seed / 17) % LOGIN_LINEAS.length]!
  return { saludo: saludoSegunHora(now), apodo, linea }
}
