import { parseDate, type DateValue } from '@internationalized/date';

const pad = (value: number) => String(value).padStart(2, '0');

/** Date and time of an API timestamp in local time, e.g. "01.10.2026, 12:00 Uhr" */
export function formatDateTime(timestamp: string): string {
	const date = new Date(timestamp);
	const day = `${pad(date.getDate())}.${pad(date.getMonth() + 1)}.${date.getFullYear()}`;
	return `${day}, ${pad(date.getHours())}:${pad(date.getMinutes())} Uhr`;
}

/** German date as shown to the user, e.g. "01.10.2026" */
export function formatDate(date: DateValue | string): string {
	const { day, month, year } = typeof date === 'string' ? parseDate(date) : date;
	return `${pad(day)}.${pad(month)}.${String(year).padStart(4, '0')}`;
}
