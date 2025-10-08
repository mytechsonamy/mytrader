/**
 * Login page
 */

import React from 'react';
import { useForm } from 'react-hook-form';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import { Card, CardHeader, CardTitle, CardContent, Button, Input } from '../components/ui';
import { useAuthStore } from '../store/authStore';
import { useNotificationHelpers } from '../store/uiStore';
import { GuestGuard } from '../components/shared/AuthGuard';
import type { LoginCredentials } from '../types';

interface LoginFormData extends LoginCredentials {}

const Login: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const { login, isLoading, error, clearError } = useAuthStore();
  const { showSuccess, showError } = useNotificationHelpers();

  const from = location.state?.from || '/';

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
    setError,
  } = useForm<LoginFormData>({
    defaultValues: {
      email: '',
      password: '',
      rememberMe: false,
    },
  });

  const onSubmit = async (data: LoginFormData) => {
    try {
      clearError();
      await login(data);
      showSuccess('Welcome back!', 'You have been successfully logged in.');
      navigate(from, { replace: true });
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Login failed';
      showError('Login Failed', message);

      // Set form-specific errors
      if (message.toLowerCase().includes('email')) {
        setError('email', { message });
      } else if (message.toLowerCase().includes('password')) {
        setError('password', { message });
      }
    }
  };

  return (
    <GuestGuard>
      <div className="min-h-screen bg-background-primary flex items-center justify-center py-12 px-4 sm:px-6 lg:px-8">
        <div className="max-w-md w-full space-y-8">
          {/* Header */}
          <div className="text-center">
            <h1 className="text-4xl font-bold text-text-primary mb-2">myTrader</h1>
            <h2 className="text-2xl font-semibold text-text-primary">Welcome Back</h2>
            <p className="mt-2 text-text-tertiary">
              Sign in to your myTrader account
            </p>
          </div>

          {/* Login Form */}
          <Card>
            <CardContent className="p-6">
              <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
                {/* Email Field */}
                <Input
                  label="Email Address"
                  type="email"
                  placeholder="Enter your email"
                  error={errors.email?.message}
                  {...register('email', {
                    required: 'Email is required',
                    pattern: {
                      value: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
                      message: 'Please enter a valid email address',
                    },
                  })}
                />

                {/* Password Field */}
                <Input
                  label="Password"
                  type="password"
                  placeholder="Enter your password"
                  error={errors.password?.message}
                  {...register('password', {
                    required: 'Password is required',
                    minLength: {
                      value: 6,
                      message: 'Password must be at least 6 characters',
                    },
                  })}
                />

                {/* Remember Me */}
                <div className="flex items-center justify-between">
                  <div className="flex items-center">
                    <input
                      id="remember-me"
                      type="checkbox"
                      className="h-4 w-4 text-brand-500 focus:ring-brand-500 border-border-default rounded"
                      {...register('rememberMe')}
                    />
                    <label htmlFor="remember-me" className="ml-2 block text-sm text-text-secondary">
                      Remember me
                    </label>
                  </div>

                  <div className="text-sm">
                    <Link
                      to="/forgot-password"
                      className="font-medium text-brand-500 hover:text-brand-600 transition-colors"
                    >
                      Forgot your password?
                    </Link>
                  </div>
                </div>

                {/* Global Error */}
                {error && (
                  <div className="p-3 bg-negative-50 border border-negative-200 rounded-md">
                    <p className="text-sm text-negative-600">{error}</p>
                  </div>
                )}

                {/* Submit Button */}
                <Button
                  type="submit"
                  fullWidth
                  loading={isLoading || isSubmitting}
                  disabled={isLoading || isSubmitting}
                >
                  {isLoading || isSubmitting ? 'Signing In...' : 'Sign In'}
                </Button>
              </form>

              {/* Divider */}
              <div className="mt-6">
                <div className="relative">
                  <div className="absolute inset-0 flex items-center">
                    <div className="w-full border-t border-border-default" />
                  </div>
                  <div className="relative flex justify-center text-sm">
                    <span className="px-2 bg-background-tertiary text-text-tertiary">
                      Or continue with
                    </span>
                  </div>
                </div>

                {/* Social Login Options */}
                <div className="mt-6 grid grid-cols-1 gap-3">
                  <Button
                    variant="outline"
                    fullWidth
                    onClick={() => {
                      // Implement Google OAuth
                      showError('Coming Soon', 'Social login will be available soon!');
                    }}
                  >
                    <svg className="w-5 h-5 mr-2" viewBox="0 0 24 24">
                      <path
                        fill="currentColor"
                        d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"
                      />
                      <path
                        fill="currentColor"
                        d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"
                      />
                      <path
                        fill="currentColor"
                        d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"
                      />
                      <path
                        fill="currentColor"
                        d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"
                      />
                    </svg>
                    Continue with Google
                  </Button>
                </div>
              </div>

              {/* Sign Up Link */}
              <div className="mt-6 text-center">
                <p className="text-sm text-text-tertiary">
                  Don't have an account?{' '}
                  <Link
                    to="/register"
                    className="font-medium text-brand-500 hover:text-brand-600 transition-colors"
                  >
                    Sign up for free
                  </Link>
                </p>
              </div>
            </CardContent>
          </Card>

          {/* Demo Account Notice */}
          <Card variant="ghost" className="text-center">
            <CardContent className="p-4">
              <p className="text-sm text-text-tertiary">
                🎯 <strong>Demo Mode:</strong> You can explore the dashboard without signing in.{' '}
                <Link
                  to="/"
                  className="text-brand-500 hover:text-brand-600 font-medium"
                >
                  Continue as Guest
                </Link>
              </p>
            </CardContent>
          </Card>
        </div>
      </div>
    </GuestGuard>
  );
};

export default Login;