<script lang="ts">
	import { toast } from 'svelte-sonner';
	import { Button } from '#lib/components/ui/button/index.js';
	import * as Dialog from '#lib/components/ui/dialog/index.js';
	import * as Field from '#lib/components/ui/field/index.js';
	import { Input } from '#lib/components/ui/input/index.js';
	import { Spinner } from '#lib/components/ui/spinner/index.js';
	import CountedTextarea from '#lib/components/CountedTextarea.svelte';
	import apiClient from '#lib/api/client.js';
	import { emptyToNull, toFieldErrors, validateKey, validateMaxLength } from '#lib/validation.js';
	import { DESCRIPTION_MAX_LENGTH, KEY_MAX_LENGTH, NAME_MAX_LENGTH } from './accounting-unit-validation.js';

	interface Props {
		open: boolean;
		/** Called after the accounting unit was created */
		onCreated: () => void;
	}

	// Recreated for every opening (see the page), so the form starts empty
	let { open = $bindable(), onCreated }: Props = $props();

	let key = $state('');
	let name = $state('');
	let description = $state('');
	/** The errors are only shown once the user tried to submit, not while typing the first characters */
	let submitted = $state(false);
	let keyTaken = $state(false);
	let saving = $state(false);

	const errors = $derived({
		key: keyTaken ? 'Dieser Schlüssel ist bereits vergeben' : validateKey(key, KEY_MAX_LENGTH),
		name: validateMaxLength(name, NAME_MAX_LENGTH),
		description: validateMaxLength(description, DESCRIPTION_MAX_LENGTH)
	});
	const shownErrors = $derived<Partial<typeof errors>>(submitted || keyTaken ? errors : {});

	async function handleSubmit(event: SubmitEvent) {
		event.preventDefault();
		submitted = true;
		if (Object.values(errors).some(Boolean)) return;

		saving = true;
		try {
			const { response } = await apiClient.POST('/api/manage-accounting-units/create-accounting-unit', {
				body: { key, name: emptyToNull(name), description: emptyToNull(description) }
			});

			if (response.ok) {
				toast.success('Haushalt wurde angelegt');
				onCreated();
				open = false;
			} else if (response.status === 409) {
				keyTaken = true;
			} else if (response.status === 400) {
				toast.error('Die Eingaben sind ungültig');
			} else {
				toast.error(`Haushalt konnte nicht angelegt werden (HTTP ${response.status})`);
			}
		} catch {
			toast.error('Haushalt konnte nicht angelegt werden');
		} finally {
			saving = false;
		}
	}
</script>

<Dialog.Root bind:open>
	<Dialog.Content>
		<form class="flex flex-col gap-6" onsubmit={handleSubmit} novalidate>
			<Dialog.Header>
				<Dialog.Title>Haushalt anlegen</Dialog.Title>
			</Dialog.Header>

			<Field.Group>
				<Field.Field data-invalid={!!shownErrors.key || undefined}>
					<Field.Label for="create-accounting-unit-key">Schlüssel</Field.Label>
					<Input
						id="create-accounting-unit-key"
						bind:value={key}
						oninput={() => (keyTaken = false)}
						maxlength={KEY_MAX_LENGTH}
						aria-invalid={!!shownErrors.key || undefined}
						autofocus
					/>
					<Field.Description>Eindeutige Kennung, kann später nicht geändert werden</Field.Description>
					<Field.Error errors={toFieldErrors(shownErrors.key)} />
				</Field.Field>
				<Field.Field data-invalid={!!shownErrors.name || undefined}>
					<Field.Label for="create-accounting-unit-name">
						Name <span class="font-normal text-muted-foreground">(optional)</span>
					</Field.Label>
					<Input
						id="create-accounting-unit-name"
						bind:value={name}
						maxlength={NAME_MAX_LENGTH}
						aria-invalid={!!shownErrors.name || undefined}
					/>
					<Field.Error errors={toFieldErrors(shownErrors.name)} />
				</Field.Field>
				<Field.Field data-invalid={!!shownErrors.description || undefined}>
					<Field.Label for="create-accounting-unit-description">
						Beschreibung <span class="font-normal text-muted-foreground">(optional)</span>
					</Field.Label>
					<CountedTextarea
						id="create-accounting-unit-description"
						bind:value={description}
						maxlength={DESCRIPTION_MAX_LENGTH}
						aria-invalid={!!shownErrors.description || undefined}
					/>
					<Field.Error errors={toFieldErrors(shownErrors.description)} />
				</Field.Field>
			</Field.Group>

			<Dialog.Footer>
				<Dialog.Close>
					{#snippet child({ props })}
						<Button {...props} variant="outline">Abbrechen</Button>
					{/snippet}
				</Dialog.Close>
				<Button type="submit" disabled={saving}>
					{#if saving}<Spinner />{/if}
					Anlegen
				</Button>
			</Dialog.Footer>
		</form>
	</Dialog.Content>
</Dialog.Root>
