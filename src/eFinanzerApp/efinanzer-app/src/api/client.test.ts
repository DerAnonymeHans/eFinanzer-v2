import apiClient from './client';

test('sends requests to the generated API paths', async () => {
  const fetch = jest.fn().mockResolvedValue(
    new Response(JSON.stringify({ userName: 'max' }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' },
    }),
  );

  const { data } = await apiClient.GET('/api/get-users/get-user', {
    params: { query: { username: 'max' } },
    baseUrl: 'http://localhost',
    fetch,
  });

  expect(fetch.mock.calls[0][0].url).toBe('http://localhost/api/get-users/get-user?username=max');
  expect(data).toEqual({ userName: 'max' });
});
