import React, { createContext, useContext, useState, useEffect } from 'react';
import { User } from '../types';
import { authService } from '../services/api';

interface AuthContextType {
  user: User | null;
  isLoading: boolean;
  error: string | null;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, password: string, fullName: string, phoneNumber: string) => Promise<void>;
  logout: () => void;
  isAuthenticated: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Check if user is already logged in on mount
  useEffect(() => {
    const token = localStorage.getItem('authToken');
    if (token) {
      setIsLoading(true);
      authService
        .getCurrentUser()
        .then((response) => {
          setUser(response.data.user);
          setError(null);
        })
        .catch((err) => {
          console.error('Failed to get current user:', err);
          localStorage.removeItem('authToken');
          setUser(null);
        })
        .finally(() => setIsLoading(false));
    }
  }, []);

  const login = async (email: string, password: string) => {
    setIsLoading(true);
    setError(null);
    try {
      const response = await authService.login(email, password);
      const { user: userData, token } = response.data;
      // Use the real JWT token returned from backend
      localStorage.setItem('authToken', token);
      localStorage.setItem('userRole', userData.isAdmin ? 'admin' : 'player');
      setUser(userData);
    } catch (err: any) {
      // Handle both single error string and array of errors
      let errorMessage = 'Invalid email or password';
      if (err.response?.data?.error) {
        errorMessage = err.response.data.error;
      } else if (err.response?.data?.errors) {
        errorMessage = Array.isArray(err.response.data.errors)
          ? err.response.data.errors.join(', ')
          : err.response.data.errors;
      }
      setError(errorMessage);
      throw err;
    } finally {
      setIsLoading(false);
    }
  };

  const register = async (email: string, password: string, fullName: string, phoneNumber: string) => {
    setIsLoading(true);
    setError(null);
    try {
      const response = await authService.register(email, password, fullName, phoneNumber);
      // With new flow, user is pending admin approval - no immediate login
      // Just resolve the promise without setting user data
      if (response.data.pendingId) {
        // Registration successful but pending approval
        return;
      }
      // Fallback for any user data that might be returned
      const { user: userData } = response.data;
      if (userData) {
        const token = btoa(JSON.stringify(userData));
        localStorage.setItem('authToken', token);
        setUser(userData);
      }
    } catch (err: any) {
      // Handle both single error string and array of errors
      let errorMessage = 'Registration failed. Please try again.';
      if (err.response?.data?.error) {
        errorMessage = err.response.data.error;
      } else if (err.response?.data?.errors) {
        errorMessage = Array.isArray(err.response.data.errors)
          ? err.response.data.errors.join(', ')
          : err.response.data.errors;
      }
      setError(errorMessage);
      throw err;
    } finally {
      setIsLoading(false);
    }
  };

  const logout = () => {
    localStorage.removeItem('authToken');
    localStorage.removeItem('userRole');
    setUser(null);
    setError(null);
  };

  const value: AuthContextType = {
    user,
    isLoading,
    error,
    login,
    register,
    logout,
    isAuthenticated: !!user,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
