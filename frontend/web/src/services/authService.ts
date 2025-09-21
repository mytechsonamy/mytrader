import axios from 'axios';

const API_BASE_URL = 'http://localhost:8080/api';

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

export interface AuthResponse {
  token: string;
  user: {
    id: string;
    email: string;
    firstName: string;
    lastName: string;
  };
}

class AuthService {
  private token: string | null = null;

  constructor() {
    this.token = localStorage.getItem('authToken');
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

  async login(credentials: LoginRequest): Promise<AuthResponse> {
    try {
      console.log('Attempting login with:', credentials.email);
      const response = await axios.post(`${API_BASE_URL}/auth/login`, credentials);
      
      const token = response.data.token;
      if (token) {
        this.token = token;
        localStorage.setItem('authToken', token);
        this.setAuthHeader(token);
      }
      
      return response.data;
    } catch (error: any) {
      console.error('Login error:', error.response?.data || error.message);
      throw new Error(error.response?.data?.message || 'Login failed');
    }
  }

  async register(userData: RegisterRequest): Promise<AuthResponse> {
    try {
      console.log('Attempting registration with:', userData.email);
      const response = await axios.post(`${API_BASE_URL}/auth/register`, userData);
      
      const token = response.data.token;
      if (token) {
        this.token = token;
        localStorage.setItem('authToken', token);
        this.setAuthHeader(token);
      }
      
      return response.data;
    } catch (error: any) {
      console.error('Registration error:', error.response?.data || error.message);
      throw new Error(error.response?.data?.message || 'Registration failed');
    }
  }

  logout() {
    this.token = null;
    localStorage.removeItem('authToken');
    this.removeAuthHeader();
  }

  isAuthenticated(): boolean {
    return !!this.token;
  }

  getToken(): string | null {
    return this.token;
  }

  async checkHealth(): Promise<any> {
    try {
      const response = await axios.get(`http://localhost:8080/health`);
      return response.data;
    } catch (error: any) {
      console.error('Health check failed:', error.message);
      throw error;
    }
  }
}

export const authService = new AuthService();