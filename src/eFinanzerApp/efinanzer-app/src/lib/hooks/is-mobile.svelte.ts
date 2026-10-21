import { MediaQuery } from 'svelte/reactivity';

/** Narrow screens, which get the navigation as a drawer and icon-only buttons where the text does not fit */
export const mobile = new MediaQuery('max-width: 767px');
