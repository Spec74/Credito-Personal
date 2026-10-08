const STORAGE_KEY = 'credix.auth.requiereCambioClave'

export function readRequiereCambioClave(): boolean {
  try {
    return sessionStorage.getItem(STORAGE_KEY) === '1'
  } catch {
    return false
  }
}

export function writeRequiereCambioClave(value: boolean): void {
  try {
    if (value) {
      sessionStorage.setItem(STORAGE_KEY, '1')
    } else {
      sessionStorage.removeItem(STORAGE_KEY)
    }
  } catch {
    /* ignore quota / private mode */
  }
}
