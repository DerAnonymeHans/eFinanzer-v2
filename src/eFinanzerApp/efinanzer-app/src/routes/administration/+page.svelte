<script lang="ts">
	import RoleChangeRequestsList from './RoleChangeRequestsList.svelte';
	import {useUserAccess} from "#lib/stores/user-access.svelte.ts";
	import AdministrationUsers from "./AdministrationUsers.svelte";

	const userAccess = useUserAccess();
</script>

<!-- Unlike the other pages the full width, the table has many columns -->
<div class="flex min-w-0 flex-col gap-6">
	<h1 class="text-3xl font-semibold tracking-tight">Administration</h1>

	<AdministrationUsers />

	{#if userAccess.value?.resolveRoleChangeRequests}
		<RoleChangeRequestsList />
	{/if}
</div>
