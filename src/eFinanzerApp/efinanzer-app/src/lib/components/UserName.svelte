<script lang="ts">
	import { Skeleton } from '#lib/components/ui/skeleton/index.js';
	import { userCache } from '#lib/stores/user-cache.svelte.js';

	interface Props {
		/** Keycloak user name, cryptic (e.g. the HTWK login), so only shown if the full name is unknown */
		username: string;
		class?: string;
	}

	let { username, class: className }: Props = $props();

	$effect(() => {
		void userCache.load(username);
	});

	const entry = $derived(userCache.get(username));
</script>

<!-- Full name of a user, loaded through the user cache. Falls back to the user name if there is none. -->
{#if !entry || entry.status === 'loading'}
	<span class={className} role="status" aria-label="Name wird geladen">
		<!-- Roughly the width of a name, so the layout does not jump once it is loaded -->
		<Skeleton class="inline-block h-4 w-30 align-middle" />
	</span>
{:else}
	<span class={className}>{userCache.getDisplayName(username)}</span>
{/if}
