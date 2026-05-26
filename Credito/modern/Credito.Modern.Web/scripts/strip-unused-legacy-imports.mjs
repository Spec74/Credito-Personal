import fs from 'fs'
import path from 'path'
import { fileURLToPath } from 'url'

const root = path.join(path.dirname(fileURLToPath(import.meta.url)), '..', 'src')

function walk(dir, acc = []) {
  for (const ent of fs.readdirSync(dir, { withFileTypes: true })) {
    const p = path.join(dir, ent.name)
    if (ent.isDirectory()) walk(p, acc)
    else if (ent.name.endsWith('.tsx')) acc.push(p)
  }
  return acc
}

for (const file of walk(path.join(root, 'pages'))) {
  let s = fs.readFileSync(file, 'utf8')
  const importRe =
    /import\s*\{([^}]+)\}\s*from\s*['"]\.\.\/\.\.\/config\/legacyReportUrls['"]\s*\n/g
  const newS = s.replace(importRe, (_, names) => {
    const kept = names
      .split(',')
      .map((n) => n.trim())
      .filter((n) => {
        const id = n.split(/\s+as\s+/)[0].trim()
        return new RegExp(`\\b${id}\\b`).test(s.replace(importRe, ''))
      })
    return kept.length ? `import { ${kept.join(', ')} } from '../../config/legacyReportUrls'\n` : ''
  })
  if (newS !== s) {
    fs.writeFileSync(file, newS)
    console.log('fixed', path.relative(root, file))
  }
}
