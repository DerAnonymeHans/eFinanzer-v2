<script lang="ts">
	import { Badge } from '#lib/components/ui/badge/index.js';
	import { Spinner } from '#lib/components/ui/spinner/index.js';
	import { getAuth } from '#lib/auth/auth.svelte.js';
	import { useUserAccess } from '#lib/stores/user-access.svelte.js';
	import { getRoleNames } from '#lib/texts/roles.js';
	import RoleChangeRequestsList from './RoleChangeRequestsList.svelte';

	const auth = getAuth();
	const userAccess = useUserAccess();
	const roleNames = $derived(userAccess.value ? getRoleNames(userAccess.value) : []);
</script>

<div class="flex flex-col gap-6">
	<div>
		<h1 class="text-3xl font-semibold tracking-tight">{auth.displayName}</h1>
		<div class="mt-2 flex flex-wrap items-center gap-1.5" role="group" aria-label="Aktuelle Rollen">
			{#if userAccess.loading}
				<Spinner />
			{:else if roleNames.length === 0}
				<span class="text-muted-foreground">Keine Rollen</span>
			{/if}
			{#each roleNames as name (name)}
				<Badge variant="secondary">{name}</Badge>
			{/each}
		</div>
	</div>

	<RoleChangeRequestsList />
</div>
