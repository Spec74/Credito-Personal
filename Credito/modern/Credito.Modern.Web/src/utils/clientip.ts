/** Misma idea que el MVC (ipify en Login.cshtml) para clienteAcceso / MAESTRO.Acceso. */
export async function fetchClientPublicIp(): Promise<string | null> {
  try {
    const res = await fetch('https://api.ipify.org?format=json', {
      signal: AbortSignal.timeout(8000),
    })
    if (!res.ok) {
      return null
    }
    const data = (await res.json()) as { ip?: string }
    return data.ip?.trim() || null
  } catch {
    return null
  }
}
