<script lang="ts">
	import './layout.css';
	import type { LayoutProps } from './$types';
	import ConfirmDialog from '#lib/components/confirm-dialog/ConfirmDialog.svelte';
	import { Toaster } from '#lib/components/ui/sonner/index.js';
	import * as Tooltip from '#lib/components/ui/tooltip/index.js';
	import AppLayout from '#lib/layout/AppLayout.svelte';
	import AuthGate from '#lib/auth/AuthGate.svelte';

	let { children }: LayoutProps = $props();
</script>

<Tooltip.Provider>
	<AuthGate>
		<AppLayout>
			{@render children()}
		</AppLayout>
	</AuthGate>
</Tooltip.Provider>

<!-- Opened through confirm() and toast() from anywhere in the app -->
<ConfirmDialog />
<Toaster position="top-center" />
