/** The API stores missing texts as null, an emptied input should not end up as "" */
export function emptyToNull(value: string | undefined | null): string | null {
  const trimmed = value?.trim();
  return trimmed ? trimmed : null;
}

/** The errors of a form as needed by Field.Error, nothing for a valid field */
export function toFieldErrors(message: string | undefined): { message: string }[] | undefined {
  return message ? [{ message }] : undefined;
}
