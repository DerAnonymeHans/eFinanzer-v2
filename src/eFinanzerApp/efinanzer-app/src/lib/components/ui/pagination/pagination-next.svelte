<script lang="ts">
	import { Pagination as PaginationPrimitive } from "bits-ui";
	import ChevronRightIcon from '@lucide/svelte/icons/chevron-right';
	import { buttonVariants } from "#lib/components/ui/button/index.js";
	import { cn } from "#lib/type-utils.js";

	let {
		ref = $bindable(null),
		class: className,
		...restProps
	}: PaginationPrimitive.NextButtonProps = $props();
</script>

<PaginationPrimitive.NextButton
	bind:ref
	aria-label="Zur nächsten Seite"
	class={cn(buttonVariants({ variant: "ghost", size: "default" }), "pr-2!", className)}
	{...restProps}
>
	<span class="hidden sm:block">Weiter</span>
	<ChevronRightIcon data-icon="inline-end" class="cn-rtl-flip" />
</PaginationPrimitive.NextButton>
