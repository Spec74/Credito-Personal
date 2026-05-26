/** Exporta filas a CSV UTF-8 con BOM (compatible Excel es-PE vía sep=,). */
export function downloadTableCsv(filename: string, rows: string[][]): void {
  const bom = '\uFEFF'
  const sepHint = 'sep=,\r\n'
  const escape = (cell: string) => {
    if (/[;"\n\r]/.test(cell)) {
      return `"${cell.replace(/"/g, '""')}"`
    }
    return cell
  }
  const body = rows.map((row) => row.map(escape).join(';')).join('\r\n')
  const blob = new Blob([bom + sepHint + body], { type: 'text/csv;charset=utf-8' })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = filename
  link.click()
  URL.revokeObjectURL(url)
}
