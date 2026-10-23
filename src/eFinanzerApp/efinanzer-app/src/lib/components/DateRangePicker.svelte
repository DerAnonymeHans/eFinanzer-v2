<script lang="ts">
	import CalendarIcon from '@lucide/svelte/icons/calendar';
	import type { DateValue } from '@internationalized/date';
	import type { DateRange } from 'bits-ui';
	import { Button } from '#lib/components/ui/button/index.js';
	import * as Popover from '#lib/components/ui/popover/index.js';
	import { RangeCalendar } from '#lib/components/ui/range-calendar/index.js';
	import { formatDate } from '#lib/texts/date-time.js';
	import { cn } from '#lib/type-utils.js';

	interface Props {
		/** Start and end date, both inclusive */
		value: DateRange | undefined;
		id?: string;
		minValue?: DateValue;
		invalid?: boolean;
		'aria-describedby'?: string;
	}

	let {
		value = $bindable(),
		id,
		minValue,
		invalid = false,
		'aria-describedby': ariaDescribedBy
	}: Props = $props();

	let open = $state(false);

	const label = $derived.by(() => {
		if (!value?.start) return undefined;
		const start = formatDate(value.start);
		return value.end ? `${start} – ${formatDate(value.end)}` : `${start} – …`;
	});
</script>

<Popover.Root bind:open>
	<Popover.Trigger>
		{#snippet child({ props })}
			<Button
				{...props}
				{id}
				variant="outline"
				aria-invalid={invalid || undefined}
				aria-describedby={ariaDescribedBy}
				class={cn('w-full justify-start font-normal', !label && 'text-muted-foreground')}
			>
				<CalendarIcon />
				{label ?? 'Beginn – Ende wählen'}
			</Button>
		{/snippet}
	</Popover.Trigger>
	<Popover.Content class="w-auto p-0" align="start">
		<RangeCalendar
			bind:value
			locale="de-DE"
			weekStartsOn={1}
			numberOfMonths={2}
			captionLayout="dropdown"
			{minValue}
			onValueChange={(range) => {
				// Closes once the end is picked too
				if (range.start && range.end) open = false;
			}}
		/>
	</Popover.Content>
</Popover.Root>
