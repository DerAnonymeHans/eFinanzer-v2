import type { Rule } from 'antd/es/form';
import dayjs from 'dayjs';
import type { Dayjs } from 'dayjs';

// Mirrors FiscalYearValidation in the backend, so most mistakes are shown before the request is sent
export const KEY_MAX_LENGTH = 10;
export const NAME_MAX_LENGTH = 10;
export const MIN_DATE = dayjs('1900-01-01');
export const MIN_LENGTH_IN_DAYS = 100;

/** Start and end date of a fiscal year, both inclusive */
export type Period = [Dayjs, Dayjs];

export const keyRules: Rule[] = [
  { required: true, message: 'Bitte einen Schlüssel angeben' },
  { max: KEY_MAX_LENGTH, message: `Höchstens ${KEY_MAX_LENGTH} Zeichen` },
  { pattern: /^[a-zA-Z0-9_-]+$/, message: 'Nur Buchstaben a-z, Ziffern, "_" und "-"' },
];

export const nameRules: Rule[] = [{ max: NAME_MAX_LENGTH, message: `Höchstens ${NAME_MAX_LENGTH} Zeichen` }];

export const periodRules: Rule[] = [
  { required: true, message: 'Bitte einen Zeitraum angeben' },
  {
    validator: (_, period: Period | null | undefined) => {
      if (!period?.[0] || !period[1]) return Promise.resolve();
      const [start, end] = period;

      if (start.isBefore(MIN_DATE, 'day')) {
        return Promise.reject(new Error(`Der Beginn darf nicht vor dem ${formatDate(MIN_DATE)} liegen`));
      }
      if (!start.isBefore(end, 'day')) return Promise.reject(new Error('Der Beginn muss vor dem Ende liegen'));
      if (getLengthInDays(period) < MIN_LENGTH_IN_DAYS) {
        return Promise.reject(new Error(`Das Haushaltsjahr muss mindestens ${MIN_LENGTH_IN_DAYS} Tage lang sein`));
      }
      return Promise.resolve();
    },
  },
];

/** Both dates are inclusive, so a period from 01.01. to 01.01. is one day long */
function getLengthInDays([start, end]: Period): number {
  return end.startOf('day').diff(start.startOf('day'), 'day') + 1;
}

/** German date as shown to the user, e.g. "01.10.2026" */
export function formatDate(date: Dayjs | string): string {
  return dayjs(date).format('DD.MM.YYYY');
}

/** Date as expected by the API, e.g. "2026-10-01" */
export function toApiDate(date: Dayjs): string {
  return date.format('YYYY-MM-DD');
}

/** Suggests the year following the latest fiscal year, so consecutive years need no date picking */
export function suggestNextPeriod(latestEndDate: string | undefined): Period | undefined {
  if (!latestEndDate) return undefined;
  const start = dayjs(latestEndDate).add(1, 'day');
  return [start, start.add(1, 'year').subtract(1, 'day')];
}
