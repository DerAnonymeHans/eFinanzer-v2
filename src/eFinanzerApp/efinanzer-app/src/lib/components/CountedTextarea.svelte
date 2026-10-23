<script lang="ts">
	import type { HTMLTextareaAttributes } from 'svelte/elements';
	import { Textarea } from '#lib/components/ui/textarea/index.js';
	import { cn } from '#lib/type-utils.js';

	type Props = Omit<HTMLTextareaAttributes, 'value' | 'maxlength'> & {
		value: string;
		maxlength: number;
	};

	let { value = $bindable(''), maxlength, class: className, ...restProps }: Props = $props();
</script>

<!-- Grows with its text up to about six lines and shows how many characters are left -->
<div class="flex flex-col gap-1">
	<Textarea bind:value {maxlength} class={cn('max-h-40 min-h-16', className)} {...restProps} />
	<span class="self-end text-xs text-muted-foreground" aria-hidden="true">{value.length} / {maxlength}</span>
</div>
