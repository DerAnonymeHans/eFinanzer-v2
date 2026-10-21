<script lang="ts">
	import * as Tooltip from '#lib/components/ui/tooltip/index.js';
	import Page from './+page.svelte';

	let { accountingUnitKey }: { accountingUnitKey: string } = $props();
</script>

<!-- For the tests: the page with the tooltip provider, which the root layout provides in the app -->
<Tooltip.Provider>
	<Page data={{}} params={{ accountingUnitKey }} />
</Tooltip.Provider>
