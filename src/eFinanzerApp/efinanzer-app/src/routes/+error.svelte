<script lang="ts">
	import FileQuestionIcon from '@lucide/svelte/icons/file-question';
	import { page } from '$app/state';
	import * as Empty from '#lib/components/ui/empty/index.js';
</script>

<Empty.Root>
	<Empty.Header>
		<Empty.Media variant="icon">
			<FileQuestionIcon />
		</Empty.Media>
		<Empty.Title>{page.status === 404 ? 'Seite nicht gefunden' : 'Fehler'}</Empty.Title>
		{#if page.status !== 404 && page.error?.message}
			<Empty.Description>{page.error.message}</Empty.Description>
		{/if}
	</Empty.Header>
</Empty.Root>
