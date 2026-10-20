import dayjs from 'dayjs';

/** Date and time of an API timestamp, e.g. "01.10.2026, 12:00 Uhr" */
export function formatDateTime(date: string): string {
  return dayjs(date).format('DD.MM.YYYY, HH:mm [Uhr]');
}
