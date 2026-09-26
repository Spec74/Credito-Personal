import type { MenuItemDto } from '../types/api'
import type { CredixHubSection } from '../components/credix/CredixHubGrid'
import { hasMenuRouteAccess } from './menuRouteAccess'

/** Deja solo enlaces del hub habilitados en el menú del usuario. */
export function filterHubSectionsByMenu(
  sections: CredixHubSection[],
  menu: MenuItemDto[],
  opts?: { includeAlwaysAllowed?: boolean },
): CredixHubSection[] {
  return sections
    .map((section) => ({
      ...section,
      links: section.links.filter((link) =>
        hasMenuRouteAccess(link.to, menu, [], {
          includeAlwaysAllowed: opts?.includeAlwaysAllowed ?? false,
        }),
      ),
    }))
    .filter((section) => section.links.length > 0)
}
