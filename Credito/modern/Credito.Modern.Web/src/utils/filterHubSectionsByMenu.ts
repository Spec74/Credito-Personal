import type { MenuItemDto } from '../types/api'
import type { CredixHubSection } from '../components/credix/CredixHubGrid'
import { hasMenuRouteAccess } from './menuRouteAccess'

/** Deja solo enlaces del hub habilitados en el menú del usuario. */
export function filterHubSectionsByMenu(
  sections: CredixHubSection[],
  menu: MenuItemDto[],
  opts?: { includeAlwaysAllowed?: boolean; extraAllowedPaths?: string[] },
): CredixHubSection[] {
  const extras = opts?.extraAllowedPaths ?? []
  return sections
    .map((section) => ({
      ...section,
      links: section.links.filter((link) =>
        hasMenuRouteAccess(link.to, menu, extras, {
          includeAlwaysAllowed: opts?.includeAlwaysAllowed ?? false,
        }),
      ),
    }))
    .filter((section) => section.links.length > 0)
}
