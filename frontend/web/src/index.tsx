import React from 'react';
import ReactDOM from 'react-dom/client';
import App from './App';
import reportWebVitals from './reportWebVitals';

// Import global styles
import './styles/globals.css';

// Performance monitoring
const isDevelopment = import.meta.env.DEV;

// Root element
const rootElement = document.getElementById('root');

if (!rootElement) {
  throw new Error(
    'Failed to find the root element. Make sure there is an element with id="root" in your HTML.'
  );
}

// Create root and render app
const root = ReactDOM.createRoot(rootElement);

root.render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);

// Report web vitals
if (isDevelopment) {
  // Log performance metrics in development
  reportWebVitals(console.log);
} else {
  // Send to analytics in production
  reportWebVitals();
}
