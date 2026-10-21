// Checks that mirror the backend's validation, so most mistakes are shown before the request is sent.
// Each returns the message to show, or undefined if the value is valid.

const KEY_PATTERN = /^[a-zA-Z0-9_-]+$/;

/** Key of an accounting unit or fiscal year, can not be changed later */
export function validateKey(key: string, maxLength: number): string | undefined {
	if (!key) return 'Bitte einen Schlüssel angeben';
	if (key.length > maxLength) return `Höchstens ${maxLength} Zeichen`;
	if (!KEY_PATTERN.test(key)) return 'Nur Buchstaben a-z, Ziffern, "_" und "-"';
	return undefined;
}

export function validateMaxLength(value: string, maxLength: number): string | undefined {
	return value.length > maxLength ? `Höchstens ${maxLength} Zeichen` : undefined;
}

/** The API stores missing texts as null, an emptied input should not end up as "" */
export function emptyToNull(value: string | undefined | null): string | null {
	const trimmed = value?.trim();
	return trimmed ? trimmed : null;
}

/** The errors of a form as needed by Field.Error, nothing for a valid field */
export function toFieldErrors(message: string | undefined): { message: string }[] | undefined {
	return message ? [{ message }] : undefined;
}
