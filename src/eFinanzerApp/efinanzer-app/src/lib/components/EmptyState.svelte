<script lang="ts">
	import InboxIcon from '@lucide/svelte/icons/inbox';
	import * as Empty from '#lib/components/ui/empty/index.js';

	let { description }: { description: string } = $props();
</script>

<Empty.Root class="p-8">
	<Empty.Header>
		<Empty.Media variant="icon">
			<InboxIcon />
		</Empty.Media>
		<Empty.Description>{description}</Empty.Description>
	</Empty.Header>
</Empty.Root>
