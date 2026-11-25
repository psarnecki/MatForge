import type { SelectGroup } from '../components/Select'
import type { LookupsDto } from '../types'

// Groups techniques by category for the selected discipline
export function techniqueGroups(lookups: LookupsDto, disciplineId: string): SelectGroup[] {
  return lookups.techniqueCategories
    .filter((category) => category.disciplineIds.includes(Number(disciplineId)))
    .map((category) => ({
      label: category.name,
      options: lookups.techniques
        .filter((technique) => technique.categoryId === category.id)
        .map((technique) => ({ value: String(technique.id), label: technique.name })),
    }))
    .filter((group) => group.options.length > 0)
}
