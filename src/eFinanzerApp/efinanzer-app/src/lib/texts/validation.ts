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


