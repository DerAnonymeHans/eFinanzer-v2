import type { User } from '../api/types';

/** Full name of a user, the user name if there is none */
export function getUserDisplayName(user: User): string {
	if (!user.firstName && !user.lastName) return user.username;
	return `${user.firstName} ${user.lastName}`;
}
