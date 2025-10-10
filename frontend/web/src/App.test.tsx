import { render, screen } from '@testing-library/react';
import { Provider } from 'react-redux';
import { store } from './store';
import App from './App';
import { describe, it, expect } from 'vitest';

describe('App', () => {
  it('renders without crashing', () => {
    render(
      <Provider store={store}>
        <App />
      </Provider>
    );
    expect(screen.getByText(/myTrader/i)).toBeInTheDocument();
  });
});
