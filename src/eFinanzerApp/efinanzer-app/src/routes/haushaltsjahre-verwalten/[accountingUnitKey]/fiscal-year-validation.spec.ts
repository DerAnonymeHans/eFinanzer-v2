import { CalendarDate } from '@internationalized/date';
import { describe, expect, test } from 'vitest';
import { suggestNextPeriod, validatePeriod } from './fiscal-year-validation';

describe('validatePeriod', () => {
	test('accepts a calendar year', () => {
		expect(validatePeriod({ start: new CalendarDate(2026, 1, 1), end: new CalendarDate(2026, 12, 31) })).toBeUndefined();
	});

	test('requires both dates', () => {
		expect(validatePeriod({ start: new CalendarDate(2026, 1, 1), end: undefined })).toBe('Bitte einen Zeitraum angeben');
		expect(validatePeriod(undefined)).toBe('Bitte einen Zeitraum angeben');
	});

	test('rejects a start before 1900', () => {
		expect(validatePeriod({ start: new CalendarDate(1899, 12, 31), end: new CalendarDate(1900, 12, 31) })).toBe(
			'Der Beginn darf nicht vor dem 01.01.1900 liegen'
		);
	});

	test('rejects an end before the start', () => {
		expect(validatePeriod({ start: new CalendarDate(2026, 6, 1), end: new CalendarDate(2026, 1, 1) })).toBe(
			'Der Beginn muss vor dem Ende liegen'
		);
	});

	test('counts both dates, so 100 days from 01.01. end on 10.04.', () => {
		expect(validatePeriod({ start: new CalendarDate(2026, 1, 1), end: new CalendarDate(2026, 4, 10) })).toBeUndefined();
		expect(validatePeriod({ start: new CalendarDate(2026, 1, 1), end: new CalendarDate(2026, 4, 9) })).toBe(
			'Das Haushaltsjahr muss mindestens 100 Tage lang sein'
		);
	});
});

test('suggests the year after the latest fiscal year', () => {
	const period = suggestNextPeriod('2026-09-30');
	expect(period?.start?.toString()).toBe('2026-10-01');
	expect(period?.end?.toString()).toBe('2027-09-30');
	expect(suggestNextPeriod(undefined)).toBeUndefined();
});
