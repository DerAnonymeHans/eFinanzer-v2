<script lang="ts">
	import { asset } from '$app/paths';

	let { size }: { size: number } = $props();
</script>

<!-- Decorative, the "eFinanzer" text next to it already names the app -->
<img src={asset('money-with-wings.svg')} width={size} height={size} alt="" aria-hidden="true" />
