type FormValue = string | number | boolean | Blob | null | undefined | FormValue[] | { [key: string]: FormValue };

/**
 * Serializes a request body as multipart/form-data, for endpoints that take a file upload. Use it as `bodySerializer`
 * of the API client.
 *
 * Nested values get the field names the backend's form binding expects: `usersWithRoles[0].roles[1]`. Null and
 * undefined are left out, as form data has no null; the backend reads a missing field as null and a missing list as
 * empty.
 */
export function toFormData(body: Record<string, FormValue>): FormData {
	const form = new FormData();
	for (const [key, value] of Object.entries(body)) append(form, key, value);
	return form;
}

function append(form: FormData, name: string, value: FormValue) {
	if (value === null || value === undefined) return;

	if (value instanceof Blob) {
		form.append(name, value, value instanceof File ? value.name : undefined);
	} else if (Array.isArray(value)) {
		value.forEach((item, index) => append(form, `${name}[${index}]`, item));
	} else if (typeof value === 'object') {
		for (const [key, item] of Object.entries(value)) append(form, `${name}.${key}`, item);
	} else {
		form.append(name, String(value));
	}
}
