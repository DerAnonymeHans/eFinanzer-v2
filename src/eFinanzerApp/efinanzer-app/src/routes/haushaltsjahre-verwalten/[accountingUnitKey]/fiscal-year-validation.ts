import { CalendarDate, parseDate, toCalendarDate, type DateValue } from '@internationalized/date';
import type { DateRange } from 'bits-ui';
import { formatDate } from '#lib/texts/date-time.js';

// Mirrors FiscalYearValidation in the backend, so most mistakes are shown before the request is sent
export const KEY_MAX_LENGTH = 10;
export const NAME_MAX_LENGTH = 10;
export const MIN_DATE = new CalendarDate(1900, 1, 1);
export const MIN_LENGTH_IN_DAYS = 100;

/** Start and end date of a fiscal year, both inclusive. Either is missing while the user is still picking. */
export type Period = DateRange;

export function validatePeriod(period: Period | undefined): string | undefined {
	if (!period?.start || !period.end) return 'Bitte einen Zeitraum angeben';
	const start = toCalendarDate(period.start);
	const end = toCalendarDate(period.end);

	if (start.compare(MIN_DATE) < 0) return `Der Beginn darf nicht vor dem ${formatDate(MIN_DATE)} liegen`;
	if (start.compare(end) >= 0) return 'Der Beginn muss vor dem Ende liegen';
	if (getLengthInDays(start, end) < MIN_LENGTH_IN_DAYS) {
		return `Das Haushaltsjahr muss mindestens ${MIN_LENGTH_IN_DAYS} Tage lang sein`;
	}
	return undefined;
}

/** Both dates are inclusive, so a period from 01.01. to 01.01. is one day long */
function getLengthInDays(start: CalendarDate, end: CalendarDate): number {
	return end.compare(start) + 1;
}

/** Date as expected by the API, e.g. "2026-10-01" */
export function toApiDate(date: DateValue): string {
	return toCalendarDate(date).toString();
}

/** Suggests the year following the latest fiscal year, so consecutive years need no date picking */
export function suggestNextPeriod(latestEndDate: string | undefined): Period | undefined {
	if (!latestEndDate) return undefined;
	const start = parseDate(latestEndDate).add({ days: 1 });
	return { start, end: start.add({ years: 1 }).subtract({ days: 1 }) };
}
