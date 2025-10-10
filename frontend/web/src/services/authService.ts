import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_BACKEND_URL
  ? `${import.meta.env.VITE_BACKEND_URL}/api/${import.meta.env.VITE_API_VERSION || 'v1'}`
  : 'http://localhost:5002/api/v1';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  phone: string;
}

export interface UserResponse {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  phone?: string;
  telegramId?: string;
  isActive: boolean;
  isEmailVerified: boolean;
  lastLogin?: string;
  createdAt: string;
  updatedAt: string;
  plan: string;
}

export interface UserSessionResponse {
  accessToken: string;
  refreshToken: string;
  user: UserResponse;
  accessTokenExpiresAt: string;
  refreshTokenExpiresAt: string;
  tokenType: string;
  jwtId: string;
  sessionId: string;
}

class AuthService {
  private token: string | null = null;
  private refreshToken: string | null = null;

  constructor() {
    this.token = localStorage.getItem('authToken');
    this.refreshToken = localStorage.getItem('refreshToken');
    if (this.token) {
      this.setAuthHeader(this.token);
    }
  }

  private setAuthHeader(token: string) {
    axios.defaults.headers.common['Authorization'] = `Bearer ${token}`;
  }

  private removeAuthHeader() {
    delete axios.defaults.headers.common['Authorization'];
  }

  private storeTokens(sessionResponse: UserSessionResponse) {
    this.token = sessionResponse.accessToken;
    this.refreshToken = sessionResponse.refreshToken;
    localStorage.setItem('authToken', sessionResponse.accessToken);
    localStorage.setItem('refreshToken', sessionResponse.refreshToken);
    localStorage.setItem('sessionId', sessionResponse.sessionId);
    localStorage.setItem('user', JSON.stringify(sessionResponse.user));
    this.setAuthHeader(sessionResponse.accessToken);
  }

  async login(credentials: LoginRequest): Promise<UserSessionResponse> {
    try {
      console.log('Attempting login with:', credentials.email);
      const response = await axios.post(`${API_BASE_URL}/auth/login`, credentials);

      this.storeTokens(response.data);
      return response.data;
    } catch (error: any) {
      console.error('Login error:', error.response?.data || error.message);
      throw new Error(error.response?.data?.message || 'Login failed');
    }
  }

  async register(userData: RegisterRequest): Promise<UserSessionResponse> {
    try {
      console.log('Attempting registration with:', userData.email);
      const response = await axios.post(`${API_BASE_URL}/auth/register`, userData);

      this.storeTokens(response.data);
      return response.data;
    } catch (error: any) {
      console.error('Registration error:', error.response?.data || error.message);
      throw new Error(error.response?.data?.message || 'Registration failed');
    }
  }

  logout() {
    this.token = null;
    this.refreshToken = null;
    localStorage.removeItem('authToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('sessionId');
    localStorage.removeItem('user');
    this.removeAuthHeader();
  }

  isAuthenticated(): boolean {
    return !!this.token;
  }

  getToken(): string | null {
    return this.token;
  }

  getRefreshToken(): string | null {
    return this.refreshToken;
  }

  getStoredUser(): UserResponse | null {
    const userStr = localStorage.getItem('user');
    if (userStr) {
      try {
        return JSON.parse(userStr);
      } catch {
        return null;
      }
    }
    return null;
  }

  async checkHealth(): Promise<any> {
    try {
      const response = await axios.get('/health');
      return response.data;
    } catch (error: any) {
      console.error('Health check failed:', error.message);
      throw error;
    }
  }
}

export const authService = new AuthService();