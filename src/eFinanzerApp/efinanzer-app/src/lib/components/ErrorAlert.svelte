<script lang="ts">
	import CircleAlertIcon from '@lucide/svelte/icons/circle-alert';
	import * as Alert from '#lib/components/ui/alert/index.js';
	import { Button } from '#lib/components/ui/button/index.js';

	interface Props {
		message: string;
		/** Shows a button to try again */
		onRetry?: () => void;
	}

	let { message, onRetry }: Props = $props();
</script>

<Alert.Root variant="destructive">
	<CircleAlertIcon />
	<Alert.Title>{message}</Alert.Title>
	{#if onRetry}
		<Alert.Action>
			<Button size="sm" variant="outline" onclick={onRetry}>Erneut versuchen</Button>
		</Alert.Action>
	{/if}
</Alert.Root>
