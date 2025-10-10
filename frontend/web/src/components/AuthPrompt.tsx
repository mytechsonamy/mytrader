import { useState } from 'react';
import Login from './Login';
import Register from './Register';
import './AuthPrompt.css';

interface AuthPromptProps {
  isOpen: boolean;
  onClose: () => void;
  title?: string;
  message?: string;
}

type AuthView = 'login' | 'register';

const AuthPrompt: React.FC<AuthPromptProps> = ({
  isOpen,
  onClose,
  title = 'Authentication Required',
  message = 'Please sign in to access this feature.'
}) => {
  const [authView, setAuthView] = useState<AuthView>('login');

  if (!isOpen) return null;

  const handleSuccessfulAuth = () => {
    onClose();
    // The dashboard will handle the state change automatically
  };

  const switchToRegister = () => {
    setAuthView('register');
  };

  const switchToLogin = () => {
    setAuthView('login');
  };

  return (
    <div className="auth-prompt-overlay">
      <div className="auth-prompt-modal">
        <div className="auth-prompt-header">
          <h2>{title}</h2>
          <button className="close-button" onClick={onClose} aria-label="Close">
            ×
          </button>
        </div>

        <div className="auth-prompt-content">
          <p className="auth-prompt-message">{message}</p>

          <div className="auth-form-container">
            {authView === 'login' ? (
              <Login
                onSwitchToRegister={switchToRegister}
                onSuccess={handleSuccessfulAuth}
                isModal={true}
              />
            ) : (
              <Register
                onSwitchToLogin={switchToLogin}
                onSuccess={handleSuccessfulAuth}
                isModal={true}
              />
            )}
          </div>
        </div>

        <div className="auth-prompt-footer">
          <button className="guest-continue-btn" onClick={onClose}>
            Continue as Guest
          </button>
        </div>
      </div>
    </div>
  );
};

export default AuthPrompt;