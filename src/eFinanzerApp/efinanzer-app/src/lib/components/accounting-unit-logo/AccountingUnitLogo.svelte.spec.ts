import { expect, test } from 'vitest';
import { page } from 'vitest/browser';
import { render } from 'vitest-browser-svelte';
import { accountingUnitLogoUrl, logoChanged } from './accounting-unit-logo.svelte.js';
import AccountingUnitLogo from './AccountingUnitLogo.svelte';

// Smallest valid PNG, a single transparent pixel
const PNG =
	'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=';

function image() {
	return page.getByTestId('accounting-unit-logo').element().querySelector('img');
}

test('shows the image', async () => {
	render(AccountingUnitLogo, { accountingUnitKey: 'fsr', src: PNG });

	await expect.poll(() => image()?.getAttribute('src')).toBe(PNG);
});

test('shows a placeholder if the image cannot be loaded, e.g. without a logo', async () => {
	render(AccountingUnitLogo, { src: 'data:image/png;base64,kaputt' });

	await expect.poll(image).toBeNull();
	expect(page.getByTestId('accounting-unit-logo').element().querySelector('svg')).not.toBeNull();
});

test('shows the placeholder without an accounting unit', async () => {
	render(AccountingUnitLogo, {});

	await expect.element(page.getByTestId('accounting-unit-logo')).toBeInTheDocument();
	expect(image()).toBeNull();
});

test('loads a changed logo again', () => {
	const before = accountingUnitLogoUrl('fsr dit');

	logoChanged('fsr dit');

	expect(before).toBe('/api/manage-accounting-units/load-accounting-unit-logo?key=fsr%20dit');
	expect(accountingUnitLogoUrl('fsr dit')).not.toBe(before);
});
