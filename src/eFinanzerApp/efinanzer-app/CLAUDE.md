# eFinanzer App

## Prefer full names over user names

Keycloak user names (e.g. the HTWK login) are cryptic. Wherever a person is shown (text, messages, tooltips, confirm
dialogs, `aria-label`s), show their full name. Fall back to the user name only if no full name is known.

- Render names with `<UserName username={...} />` (`src/components/UserName.tsx`). It loads the user through the user
  store, shows a placeholder while loading and falls back to the user name.
- Need the name as a string (e.g. for an `aria-label`)? Use `useUserDisplayName(username)` from
  `src/stores/useUser.ts`, it applies the same fallback.
- For the logged-in user, use `displayName` from `useAuth()`.
- Loading a user in an event handler (e.g. to check it exists)? Use `useUserCache().load(username)` instead of calling
  `/api/get-users/get-user` directly, so the result lands in the cache.
- Don't show the user name next to the full name, it adds nothing.
