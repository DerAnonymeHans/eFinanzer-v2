import apiClient from '../api/client';
import type { User } from '../api/types';

export type UserEntry =
  | { status: 'loading' }
  | { status: 'loaded'; user: User }
  | { status: 'notFound' }
  | { status: 'error' };

// Keycloak stores user names in lower case
function normalize(username: string): string {
  return username.trim().toLowerCase();
}

/**
 * Caches the info of other users for the whole session. Every user is requested at most once, no matter how many
 * components show them. Lives outside of React state, components read it via useSyncExternalStore (see useUser).
 */
export class UserCache {
  private readonly entries = new Map<string, UserEntry>();
  private readonly requests = new Map<string, Promise<UserEntry>>();
  private readonly listeners = new Set<() => void>();

  subscribe = (listener: () => void) => {
    this.listeners.add(listener);
    return () => {
      this.listeners.delete(listener);
    };
  };

  /** The cached entry, undefined if the user was never requested */
  get = (username: string): UserEntry | undefined => this.entries.get(normalize(username));

  /** Requests the user unless it is cached or already requested */
  load = (username: string): Promise<UserEntry> => {
    const key = normalize(username);
    const pending = this.requests.get(key);
    if (pending) return pending;

    const request = this.request(key);
    this.requests.set(key, request);
    return request;
  };

  /** Caches a user that was loaded elsewhere, e.g. by a search, so it is not requested again */
  remember = (user: User) => {
    const key = normalize(user.username);
    const entry: UserEntry = { status: 'loaded', user };
    this.requests.set(key, Promise.resolve(entry));
    this.set(key, entry);
  };

  private async request(key: string): Promise<UserEntry> {
    this.set(key, { status: 'loading' });

    let entry: UserEntry;
    try {
      const { data, response } = await apiClient.GET('/api/get-users/get-user', {
        params: { query: { username: key } },
      });
      if (response.ok && data) entry = { status: 'loaded', user: data };
      else if (response.status === 404) entry = { status: 'notFound' };
      else entry = { status: 'error' };
    } catch {
      entry = { status: 'error' };
    }

    // A failed request is tried again the next time the user is shown
    if (entry.status === 'error') this.requests.delete(key);
    this.set(key, entry);
    return entry;
  }

  private set(key: string, entry: UserEntry) {
    this.entries.set(key, entry);
    this.listeners.forEach((listener) => listener());
  }
}

/** Full name of a user, undefined if Keycloak knows neither first nor last name */
export function formatUserName(user: User): string | undefined {
  const name = [user.firstName, user.lastName].filter(Boolean).join(' ');
  return name || undefined;
}
