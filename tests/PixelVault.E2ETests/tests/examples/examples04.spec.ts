import { test, expect } from '@playwright/test';

test('Example GET', async ({ request }) => {

  const response = await request.get('https://api.zippopotam.us/us/90210');

  expect(response.status()).toBe(200);

  const body = await response.json();

  expect(body.country).toBe('United States');
  expect(body.places[0]['place name']).toBe('Beverly Hills');

  const countryAbbreviation = body['country abbreviation'];
  expect(countryAbbreviation).toBe('US');
});

test('Example POST', async ({ request }) => {

  const response = await request.post('https://jsonplaceholder.typicode.com/posts', {
    data: { userId: 1, title: 'foo', body: 'bar' },
  });

  expect(response.status()).toBe(201);
});
