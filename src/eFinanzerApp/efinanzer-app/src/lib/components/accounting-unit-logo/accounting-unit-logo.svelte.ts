// Mirrors ManageAccountingUnitLogoTask in the backend
export const LOGO_EXTENSIONS = ['.png', '.jpg', '.jpeg', '.svg'];
export const LOGO_MAX_SIZE_IN_BYTES = 5 * 1024 * 1024;

/** For the `accept` attribute of a file input */
export const LOGO_ACCEPT = LOGO_EXTENSIONS.join(',');

/**
 * Counts the logo changes per accounting unit. Part of the logo URL, so a changed logo is loaded again instead of
 * being taken from the browser cache.
 */
const logoVersions = $state<Record<string, number>>({});

/**
 * URL of the logo of an accounting unit, for an `<img>`. The endpoint needs no login, so the browser can load it
 * without the access token. Responds with 404 if the unit has no logo.
 */
export function accountingUnitLogoUrl(key: string): string {
	const version = logoVersions[key];
	const query = `key=${encodeURIComponent(key)}${version ? `&v=${version}` : ''}`;
	return `${import.meta.env.VITE_API_BASE_URL ?? ''}/api/manage-accounting-units/load-accounting-unit-logo?${query}`;
}

/** Call after the logo of an accounting unit was changed, so it is loaded again wherever it is shown */
export function logoChanged(key: string) {
	logoVersions[key] = (logoVersions[key] ?? 0) + 1;
}

export function validateLogo(file: File | null | undefined): string | undefined {
	if (!file) return undefined;
	const extension = file.name.slice(file.name.lastIndexOf('.')).toLowerCase();
	if (!file.name.includes('.') || !LOGO_EXTENSIONS.includes(extension)) return 'Nur PNG-, JPG- oder SVG-Dateien';
	if (file.size === 0) return 'Die Datei ist leer';
	if (file.size > LOGO_MAX_SIZE_IN_BYTES) return 'Höchstens 5 MB';
	return undefined;
}
