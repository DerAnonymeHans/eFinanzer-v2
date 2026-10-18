import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import App from './App';

// The matchMedia mock in setupTests matches no breakpoint, so the mobile layout is rendered.
test('opens the navigation drawer on mobile', async () => {
  render(<App />);
  userEvent.click(screen.getByRole('button', { name: 'Navigation öffnen' }));
  expect(await screen.findByRole('navigation', { name: 'Hauptnavigation' })).toBeInTheDocument();
});
