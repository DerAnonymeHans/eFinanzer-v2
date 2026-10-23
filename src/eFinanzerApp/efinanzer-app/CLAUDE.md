# eFinanzer App

SvelteKit single page app with Svelte 5 runes, Tailwind and shadcn-svelte. See README.md for scripts and structure.

## Conventions

- Build the UI from the shadcn-svelte components in `src/lib/components/ui`. Missing ones are added with
  `npx shadcn-svelte@latest add <name>`; afterwards translate their English texts (e.g. "Close") to German.
- All texts shown to users are German.
- Messages go through `toast` from `svelte-sonner`, confirmations through `confirm()` from `#lib/components/confirm-dialog/confirmDialog.svelte.js`.
- Requests use `apiClient` from `#lib/api/client.js`. A page loads its data through a `DataLoader` from
  `#lib/api/data-loading.svelte.js`, which keeps the loading and error state and drops responses of outdated requests.

## Prefer full names over user names

Keycloak user names (e.g. the HTWK login) are cryptic. Wherever a person is shown (text, messages, tooltips, confirm
dialogs, `aria-label`s), show their full name. Fall back to the user name only if no full name is known.

- Render names with `<UserName username={...} />` (`src/lib/components/UserName.svelte`). It loads the user through
  the user cache, shows a placeholder while loading and falls back to the user name.
- Need the name as a string (e.g. for an `aria-label`)? Use `userCache.getDisplayName(username)` from
  `src/lib/stores/user-cache.svelte.ts`, it applies the same fallback. It only knows users that were loaded, e.g. by a
  `<UserName>` on the same page.
- For the logged-in user, use `displayName` from `getAuth()`.
- Loading a user in an event handler (e.g. to check it exists)? Use `userCache.load(username)` instead of calling
  `/api/get-users/get-user` directly, so the result lands in the cache.
- Don't show the user name next to the full name, it adds nothing.
