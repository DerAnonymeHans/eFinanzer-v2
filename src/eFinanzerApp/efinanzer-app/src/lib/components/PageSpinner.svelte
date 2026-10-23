<script lang="ts">
	import { Spinner } from '#lib/components/ui/spinner/index.js';
	import { cn } from '#lib/type-utils.js';

	let { class: className }: { class?: string } = $props();
</script>

<div class={cn('flex justify-center py-12', className)}>
	<Spinner class="size-6 text-primary" aria-label="Wird geladen" />
</div>
