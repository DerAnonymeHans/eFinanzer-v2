<script lang="ts">
	import ShieldXIcon from '@lucide/svelte/icons/shield-x';
	import * as Empty from '#lib/components/ui/empty/index.js';

	let { description }: { description: string } = $props();
</script>

<!-- Shown instead of a page the user may not see. The API checks the rights again for every request. -->
<Empty.Root>
	<Empty.Header>
		<Empty.Media variant="icon">
			<ShieldXIcon />
		</Empty.Media>
		<Empty.Title>Kein Zugriff</Empty.Title>
		<Empty.Description>{description}</Empty.Description>
	</Empty.Header>
</Empty.Root>
