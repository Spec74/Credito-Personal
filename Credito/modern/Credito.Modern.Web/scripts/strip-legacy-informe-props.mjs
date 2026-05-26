import fs from 'fs'
import path from 'path'
import { fileURLToPath } from 'url'

const root = path.join(path.dirname(fileURLToPath(import.meta.url)), '..', 'src')
const pagesDir = path.join(root, 'pages')

function walk(dir, acc = []) {
  for (const ent of fs.readdirSync(dir, { withFileTypes: true })) {
    const p = path.join(dir, ent.name)
    if (ent.isDirectory()) walk(p, acc)
    else if (ent.name.endsWith('.tsx')) acc.push(p)
  }
  return acc
}

function stripFile(file) {
  let s = fs.readFileSync(file, 'utf8')
  if (!s.includes('onLegacy')) return false

  const lines = s.split(/\r?\n/)
  const out = []
  let i = 0
  while (i < lines.length) {
    const line = lines[i]
    if (/^\s*onLegacy(Excel|Pdf)=/.test(line)) {
      i++
      continue
    }
    out.push(line)
    i++
  }
  s = out.join('\n')

  // Remove legacy helper functions (single-block)
  s = s.replace(
    /\n\s*const (legacy|abrirLegacy)\s*=\s*async\s*\([^)]*\)\s*=>\s*\{[\s\S]*?\n\s*\}\n/g,
    '\n',
  )
  s = s.replace(
    /\n\s*const abrirLegacy\w*\s*=\s*\([^)]*\)\s*=>\s*\{[\s\S]*?\n\s*\}\n/g,
    '\n',
  )
  s = s.replace(
    /\n\s*function abrirLegacy\w*\([^)]*\)\s*\{[\s\S]*?\n\s*\}\n/g,
    '\n',
  )

  // Remove imports from legacyReportUrls if unused
  const importRe =
    /import\s*\{([^}]+)\}\s*from\s*['"]\.\.\/\.\.\/config\/legacyReportUrls['"]\s*\n/g
  s = s.replace(importRe, (_, names) => {
    const kept = names
      .split(',')
      .map((n) => n.trim())
      .filter((n) => {
        const id = n.split(/\s+as\s+/)[0].trim()
        return new RegExp(`\\b${id}\\b`).test(s)
      })
    return kept.length ? `import { ${kept.join(', ')} } from '../../config/legacyReportUrls'\n` : ''
  })

  if (s !== fs.readFileSync(file, 'utf8')) {
    fs.writeFileSync(file, s)
    return true
  }
  return false
}

const changed = []
for (const f of walk(pagesDir)) {
  if (stripFile(f)) changed.push(path.relative(root, f))
}
console.log('Updated', changed.length, 'files')
for (const c of changed) console.log(' -', c)
