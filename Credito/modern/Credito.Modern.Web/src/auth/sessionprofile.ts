const USER_KEY = 'credito.profile.user'
const OFFICE_KEY = 'credito.profile.office'

export function saveLoginProfile(nombreUsuario: string, oficinaLabel: string): void {
  sessionStorage.setItem(USER_KEY, nombreUsuario)
  sessionStorage.setItem(OFFICE_KEY, oficinaLabel)
}

export function getLoginProfile(): { nombreUsuario: string | null; oficinaLabel: string | null } {
  return {
    nombreUsuario: sessionStorage.getItem(USER_KEY),
    oficinaLabel: sessionStorage.getItem(OFFICE_KEY),
  }
}

export function clearLoginProfile(): void {
  sessionStorage.removeItem(USER_KEY)
  sessionStorage.removeItem(OFFICE_KEY)
}
