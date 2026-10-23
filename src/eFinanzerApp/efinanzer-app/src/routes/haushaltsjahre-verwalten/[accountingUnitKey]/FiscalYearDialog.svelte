<script lang="ts" module>
	import { parseDate } from '@internationalized/date';
	import type { components } from '#lib/api/schema.js';

	type InvalidInputError = components['schemas']['InvalidInputError'];
	type OverlapsExistingError = components['schemas']['FiscalYearOverlapsExistingError'];
	type FieldName = 'key' | 'name' | 'period';

	const formFieldsByApiField: Record<string, FieldName> = {
		Key: 'key',
		Name: 'name',
		StartDate: 'period',
		EndDate: 'period'
	};

	function isOverlapError(error: unknown): error is OverlapsExistingError {
		return typeof error === 'object' && error !== null && 'overlappingKey' in error;
	}

	function isInvalidInputError(error: unknown): error is InvalidInputError {
		return typeof error === 'object' && error !== null && 'field' in error;
	}
</script>

<script lang="ts">
	import { untrack } from 'svelte';
	import { toast } from 'svelte-sonner';
	import { Button } from '#lib/components/ui/button/index.js';
	import * as Dialog from '#lib/components/ui/dialog/index.js';
	import * as Field from '#lib/components/ui/field/index.js';
	import { Input } from '#lib/components/ui/input/index.js';
	import { Spinner } from '#lib/components/ui/spinner/index.js';
	import DateRangePicker from '#lib/components/DateRangePicker.svelte';
	import apiClient from '#lib/api/client.js';
	import type { FiscalYearListItem } from '#lib/api/types.js';
	import { formatDate } from '#lib/texts/date-time.js';
	import { emptyToNull, toFieldErrors } from '#lib/utils.js';
	import { validateKey, validateMaxLength } from '#lib/texts/validation.js';
	import {
		KEY_MAX_LENGTH,
		MIN_DATE,
		NAME_MAX_LENGTH,
		toApiDate,
		validatePeriod,
		type Period
	} from './fiscal-year-validation.js';

	interface Props {
		open: boolean;
		accountingUnitKey: string;
		/** The fiscal year to edit, creates a new one if undefined */
		fiscalYear?: FiscalYearListItem;
		/** Prefilled period of a new fiscal year */
		suggestedPeriod?: Period;
		/** Called after the fiscal year was saved */
		onSaved: () => void;
	}

	let { open = $bindable(), accountingUnitKey, fiscalYear, suggestedPeriod, onSaved }: Props = $props();

	// The initial values are taken once, the dialog is recreated for another fiscal year
	const initialFiscalYear = untrack(() => fiscalYear);
	const isEdit = initialFiscalYear !== undefined;
	const hasReceipts = Number(initialFiscalYear?.receiptsCount ?? 0) > 0;

	let key = $state(initialFiscalYear?.key ?? '');
	let name = $state(initialFiscalYear?.name ?? '');
	let period = $state<Period | undefined>(
		initialFiscalYear
			? { start: parseDate(initialFiscalYear.startDate), end: parseDate(initialFiscalYear.endDate) }
			: untrack(() => suggestedPeriod)
	);
	let submitted = $state(false);
	let serverErrors = $state<Partial<Record<FieldName, string>>>({});
	let saving = $state(false);

	const errors = $derived({
		key: serverErrors.key ?? validateKey(key, KEY_MAX_LENGTH),
		name: serverErrors.name ?? validateMaxLength(name, NAME_MAX_LENGTH),
		period: serverErrors.period ?? validatePeriod(period)
	});
	const shownErrors = $derived<Partial<Record<FieldName, string>>>(submitted ? errors : serverErrors);

	function send(start: string, end: string) {
		const body = { name: emptyToNull(name), startDate: start, endDate: end };
		if (initialFiscalYear) {
			return apiClient.PUT('/api/manage-fiscal-years/update-fiscal-year', {
				params: { query: { accountingUnitKey, key: initialFiscalYear.key } },
				body
			});
		}
		return apiClient.POST('/api/manage-fiscal-years/create-fiscal-year', {
			body: { key, accountingUnitKey, ...body }
		});
	}

	async function handleSubmit(event: SubmitEvent) {
		event.preventDefault();
		submitted = true;
		if (Object.values(errors).some(Boolean) || !period?.start || !period.end) return;

		const failureText = isEdit
			? 'Haushaltsjahr konnte nicht gespeichert werden'
			: 'Haushaltsjahr konnte nicht angelegt werden';

		saving = true;
		try {
			const { response, error } = await send(toApiDate(period.start), toApiDate(period.end));

			if (response.ok) {
				toast.success(isEdit ? 'Haushaltsjahr wurde gespeichert' : 'Haushaltsjahr wurde angelegt');
				onSaved();
				open = false;
			} else if (response.status === 409 && isOverlapError(error)) {
				const other = `„${error.overlappingKey}“ (${formatDate(error.overlappingStartDate)} – ${formatDate(error.overlappingEndDate)})`;
				serverErrors = { period: `Überschneidet sich mit dem Haushaltsjahr ${other}` };
			} else if (response.status === 409 && isEdit) {
				serverErrors = { period: 'Es gibt Belege außerhalb dieses Zeitraums' };
			} else if (response.status === 409) {
				serverErrors = { key: 'Dieser Schlüssel ist bereits vergeben' };
			} else if (response.status === 400 && isInvalidInputError(error) && formFieldsByApiField[error.field]) {
				serverErrors = { [formFieldsByApiField[error.field]]: 'Ungültige Eingabe' };
			} else if (response.status === 400) {
				toast.error('Die Eingaben sind ungültig');
			} else if (response.status === 404) {
				toast.error(isEdit ? 'Das Haushaltsjahr existiert nicht mehr' : 'Der Haushalt existiert nicht mehr');
				onSaved();
				open = false;
			} else {
				toast.error(`${failureText} (HTTP ${response.status})`);
			}
		} catch {
			toast.error(failureText);
		} finally {
			saving = false;
		}
	}

	function clearServerError(field: FieldName) {
		if (serverErrors[field]) serverErrors = { ...serverErrors, [field]: undefined };
	}
</script>

<Dialog.Root bind:open>
	<Dialog.Content>
		<form class="flex flex-col gap-6" onsubmit={handleSubmit} novalidate>
			<Dialog.Header>
				<Dialog.Title>
					{initialFiscalYear ? `Haushaltsjahr „${initialFiscalYear.name ?? initialFiscalYear.key}“ bearbeiten` : 'Haushaltsjahr anlegen'}
				</Dialog.Title>
			</Dialog.Header>

			<Field.Group>
				<Field.Field data-invalid={!!shownErrors.key || undefined}>
					<Field.Label for="fiscal-year-key">Schlüssel</Field.Label>
					<Input
						id="fiscal-year-key"
						bind:value={key}
						oninput={() => clearServerError('key')}
						maxlength={KEY_MAX_LENGTH}
						disabled={isEdit}
						aria-invalid={!!shownErrors.key || undefined}
						autofocus={!isEdit}
					/>
					{#if !isEdit}
						<Field.Description>
							Eindeutig innerhalb des Haushalts, kann später nicht geändert werden
						</Field.Description>
					{/if}
					<Field.Error errors={toFieldErrors(shownErrors.key)} />
				</Field.Field>
				<Field.Field data-invalid={!!shownErrors.name || undefined}>
					<Field.Label for="fiscal-year-name">
						Name <span class="font-normal text-muted-foreground">(optional)</span>
					</Field.Label>
					<Input
						id="fiscal-year-name"
						bind:value={name}
						oninput={() => clearServerError('name')}
						maxlength={NAME_MAX_LENGTH}
						aria-invalid={!!shownErrors.name || undefined}
						autofocus={isEdit}
					/>
					<Field.Error errors={toFieldErrors(shownErrors.name)} />
				</Field.Field>
				<Field.Field data-invalid={!!shownErrors.period || undefined}>
					<Field.Label for="fiscal-year-period">Zeitraum</Field.Label>
					<DateRangePicker
						id="fiscal-year-period"
						bind:value={() => period, (value) => {
							period = value;
							clearServerError('period');
						}}
						minValue={MIN_DATE}
						invalid={!!shownErrors.period}
					/>
					{#if hasReceipts}
						<Field.Description>Alle Belege des Haushaltsjahres müssen im Zeitraum liegen</Field.Description>
					{/if}
					<Field.Error errors={toFieldErrors(shownErrors.period)} />
				</Field.Field>
			</Field.Group>

			<Dialog.Footer>
				<Dialog.Close>
					{#snippet child({ props })}
						<Button {...props} variant="outline">Abbrechen</Button>
					{/snippet}
				</Dialog.Close>
				<Button type="submit" disabled={saving}>
					{#if saving}<Spinner />{/if}
					{isEdit ? 'Speichern' : 'Anlegen'}
				</Button>
			</Dialog.Footer>
		</form>
	</Dialog.Content>
</Dialog.Root>
