import React, { useState, useEffect } from 'react';
import { useAuthStore } from '../store/authStore';
import { Button, Input } from './ui';
import './Auth.css';

interface LoginProps {
  onSwitchToRegister?: () => void;
  onSuccess?: () => void;
  isModal?: boolean;
}

const Login: React.FC<LoginProps> = ({ onSwitchToRegister, onSuccess, isModal = false }) => {
  const { login, isLoading, error, isAuthenticated, clearError } = useAuthStore();

  useEffect(() => {
    // Call onSuccess when authentication succeeds in modal mode
    if (isAuthenticated && onSuccess) {
      onSuccess();
    }
  }, [isAuthenticated, onSuccess]);

  const [formData, setFormData] = useState({
    email: '',
    password: ''
  });

  useEffect(() => {
    // Clear error when component mounts
    clearError();
  }, [clearError]);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: value
    }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!formData.email || !formData.password) {
      return;
    }

    try {
      await login(formData);
    } catch (error) {
      // Error is handled by the store
    }
  };

  return (
    <div className={`auth-container ${isModal ? 'auth-modal' : ''}`}>
      <div className="auth-form">
        <h2 className="auth-title">Welcome Back</h2>
        <p className="auth-subtitle">Sign in to your myTrader account</p>

        {error && (
          <div className="auth-error">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="auth-form-fields">
          <div className="form-group">
            <label htmlFor="email">Email</label>
            <Input
              id="email"
              name="email"
              type="email"
              value={formData.email}
              onChange={handleChange}
              placeholder="Enter your email"
              required
              disabled={isLoading}
            />
          </div>

          <div className="form-group">
            <label htmlFor="password">Password</label>
            <Input
              id="password"
              name="password"
              type="password"
              value={formData.password}
              onChange={handleChange}
              placeholder="Enter your password"
              required
              disabled={isLoading}
            />
          </div>

          <Button
            type="submit"
            variant="primary"
            disabled={isLoading || !formData.email || !formData.password}
            className="auth-submit"
          >
            {isLoading ? 'Signing In...' : 'Sign In'}
          </Button>
        </form>

        {onSwitchToRegister && (
          <div className="auth-footer">
            <p>
              Don't have an account?{' '}
              <button
                type="button"
                onClick={onSwitchToRegister}
                className="auth-link"
                disabled={isLoading}
              >
                Sign Up
              </button>
            </p>
          </div>
        )}
      </div>
    </div>
  );
};

export default Login;