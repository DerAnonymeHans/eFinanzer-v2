import type { components } from './schema';

type Schemas = components['schemas'];

/** Names of all API errors, each error carries its name in `type` */
export type ApiErrorType = {
	[K in keyof Schemas]: Schemas[K] extends { type: K } ? K : never;
}[keyof Schemas];

export type ApiError<T extends ApiErrorType> = Schemas[T];

/**
 * Whether the error body is the given API error. The generated schema only knows one error type per status code,
 * so errors sharing a status code are told apart by their type.
 */
export function isApiError<T extends ApiErrorType>(error: unknown, type: T): error is ApiError<T> {
	return typeof error === 'object' && error !== null && 'type' in error && error.type === type;
}