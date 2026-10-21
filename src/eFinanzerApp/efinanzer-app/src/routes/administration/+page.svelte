<script lang="ts">
	import PendingRoleChangeRequests from './PendingRoleChangeRequests.svelte';
</script>

<!-- Unlike the other pages the full width, the table has many columns -->
<div class="flex min-w-0 flex-col gap-6">
	<h1 class="text-3xl font-semibold tracking-tight">Administration</h1>

	<PendingRoleChangeRequests />
</div>
