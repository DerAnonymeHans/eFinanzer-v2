<script lang="ts">
	import { onMount, type Snippet } from 'svelte';
	import CircleXIcon from '@lucide/svelte/icons/circle-x';
	import { Button } from '#lib/components/ui/button/index.js';
	import * as Empty from '#lib/components/ui/empty/index.js';
	import { Spinner } from '#lib/components/ui/spinner/index.js';
	import { getAuthState, setAuth } from '#lib/auth/auth.svelte.js';
	import keycloak, { initKeycloak } from '#lib/auth/keycloak.js';

	let { children }: { children: Snippet } = $props();

	let failed = $state(false);

	// Everything is redirected to the Keycloak login first, the children are only rendered for a logged-in user
	onMount(() => {
		initKeycloak()
			.then((authenticated) => {
				// Without a session Keycloak redirects to its login page, so the spinner stays until then
				if (!authenticated) return;
				const token = keycloak.tokenParsed ?? {};
				const username: string = token.preferred_username ?? token.sub ?? '';
				setAuth({
					username,
					displayName: token.name ?? username,
					logout: () => keycloak.logout({ redirectUri: window.location.origin + '/' })
				});
			})
			.catch(() => (failed = true));
	});
</script>

{#if failed}
	<div class="flex min-h-screen items-center justify-center p-4">
		<Empty.Root>
			<Empty.Header>
				<Empty.Media variant="icon" class="text-destructive">
					<CircleXIcon />
				</Empty.Media>
				<Empty.Title>Anmeldung fehlgeschlagen</Empty.Title>
				<Empty.Description>Der Anmeldedienst ist gerade nicht erreichbar.</Empty.Description>
			</Empty.Header>
			<Empty.Content>
				<Button onclick={() => window.location.reload()}>Erneut versuchen</Button>
			</Empty.Content>
		</Empty.Root>
	</div>
{:else if !getAuthState()}
	<div class="flex min-h-screen flex-col items-center justify-center gap-3 p-4 text-muted-foreground">
		<Spinner class="size-8 text-primary" aria-label="Anmeldung läuft" />
		Anmeldung…
	</div>
{:else}
	{@render children()}
{/if}
