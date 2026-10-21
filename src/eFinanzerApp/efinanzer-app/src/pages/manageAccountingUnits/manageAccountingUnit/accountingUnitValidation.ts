import type { Rule } from 'antd/es/form';

// Mirrors AccountingUnitValidation in the backend, so most mistakes are shown before the request is sent
export const KEY_MAX_LENGTH = 10;
export const NAME_MAX_LENGTH = 15;
export const DESCRIPTION_MAX_LENGTH = 200;

export const keyRules: Rule[] = [
  { required: true, message: 'Bitte einen Schlüssel angeben' },
  { max: KEY_MAX_LENGTH, message: `Höchstens ${KEY_MAX_LENGTH} Zeichen` },
  { pattern: /^[a-zA-Z0-9_-]+$/, message: 'Nur Buchstaben a-z, Ziffern, "_" und "-"' },
];

export const nameRules: Rule[] = [{ max: NAME_MAX_LENGTH, message: `Höchstens ${NAME_MAX_LENGTH} Zeichen` }];

export const descriptionRules: Rule[] = [
  { max: DESCRIPTION_MAX_LENGTH, message: `Höchstens ${DESCRIPTION_MAX_LENGTH} Zeichen` },
];

/** The API stores missing texts as null, an emptied input should not end up as "" */
export function emptyToNull(value: string | undefined | null): string | null {
  const trimmed = value?.trim();
  return trimmed ? trimmed : null;
}
