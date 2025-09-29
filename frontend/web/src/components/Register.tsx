import React, { useState, useEffect } from 'react';
import { useAuthStore } from '../store/authStore';
import { Button, Input } from './ui';
import './Auth.css';

interface RegisterProps {
  onSwitchToLogin?: () => void;
  onSuccess?: () => void;
  isModal?: boolean;
}

const Register: React.FC<RegisterProps> = ({ onSwitchToLogin, onSuccess, isModal = false }) => {
  const { register, isLoading, error, isAuthenticated, clearError } = useAuthStore();

  useEffect(() => {
    // Call onSuccess when authentication succeeds in modal mode
    if (isAuthenticated && onSuccess) {
      onSuccess();
    }
  }, [isAuthenticated, onSuccess]);

  const [formData, setFormData] = useState({
    email: '',
    password: '',
    confirmPassword: '',
    firstName: '',
    lastName: '',
    username: ''
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

    if (!formData.email || !formData.password || !formData.firstName || !formData.lastName || !formData.username) {
      return;
    }

    if (formData.password !== formData.confirmPassword) {
      // We should probably show this error in a better way
      alert('Passwords do not match');
      return;
    }

    try {
      await register({
        email: formData.email,
        password: formData.password,
        firstName: formData.firstName,
        lastName: formData.lastName,
        username: formData.username
      });
    } catch (error) {
      // Error is handled by the store
    }
  };

  return (
    <div className={`auth-container ${isModal ? 'auth-modal' : ''}`}>
      <div className="auth-form">
        <h2 className="auth-title">Create Account</h2>
        <p className="auth-subtitle">Join myTrader and start trading smarter</p>

        {error && (
          <div className="auth-error">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="auth-form-fields">
          <div className="form-row">
            <div className="form-group">
              <label htmlFor="firstName">First Name</label>
              <Input
                id="firstName"
                name="firstName"
                type="text"
                value={formData.firstName}
                onChange={handleChange}
                placeholder="First name"
                required
                disabled={isLoading}
              />
            </div>

            <div className="form-group">
              <label htmlFor="lastName">Last Name</label>
              <Input
                id="lastName"
                name="lastName"
                type="text"
                value={formData.lastName}
                onChange={handleChange}
                placeholder="Last name"
                required
                disabled={isLoading}
              />
            </div>
          </div>

          <div className="form-group">
            <label htmlFor="username">Username</label>
            <Input
              id="username"
              name="username"
              type="text"
              value={formData.username}
              onChange={handleChange}
              placeholder="Choose a username"
              required
              disabled={isLoading}
            />
          </div>

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
              placeholder="Create a password"
              required
              disabled={isLoading}
            />
          </div>

          <div className="form-group">
            <label htmlFor="confirmPassword">Confirm Password</label>
            <Input
              id="confirmPassword"
              name="confirmPassword"
              type="password"
              value={formData.confirmPassword}
              onChange={handleChange}
              placeholder="Confirm your password"
              required
              disabled={isLoading}
            />
          </div>

          <Button
            type="submit"
            variant="primary"
            disabled={
              isLoading ||
              !formData.email ||
              !formData.password ||
              !formData.firstName ||
              !formData.lastName ||
              !formData.username ||
              formData.password !== formData.confirmPassword
            }
            className="auth-submit"
          >
            {isLoading ? 'Creating Account...' : 'Create Account'}
          </Button>
        </form>

        {onSwitchToLogin && (
          <div className="auth-footer">
            <p>
              Already have an account?{' '}
              <button
                type="button"
                onClick={onSwitchToLogin}
                className="auth-link"
                disabled={isLoading}
              >
                Sign In
              </button>
            </p>
          </div>
        )}
      </div>
    </div>
  );
};

export default Register;