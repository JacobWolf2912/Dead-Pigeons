import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import './AuthPages.css';

const RegisterPage: React.FC = () => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [fullName, setFullName] = useState('');
  const [phoneNumber, setPhoneNumber] = useState('');
  const [localError, setLocalError] = useState('');
  const [fieldErrors, setFieldErrors] = useState<{ [key: string]: string }>({});
  const [registrationPending, setRegistrationPending] = useState(false);
  const [passwordRequirements, setPasswordRequirements] = useState({
    hasMinLength: false,
    hasUppercase: false,
    hasLowercase: false,
    hasNumber: false,
    hasSpecialChar: false,
  });
  const { register, isLoading, error } = useAuth();

  // Validation patterns
  const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  const passwordPattern = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*\-_=+,.]).{6,}$/;
  const phonePattern = /^(\d{8}|\d{10}|(\+45)?[-\s]?\d{4}[-\s]?\d{4})$/;
  const fullNamePattern = /^[a-zA-Z]{2,}\s+[a-zA-Z]{2,}$/;

  const validateField = (fieldName: string, value: string): string => {
    switch (fieldName) {
      case 'fullName':
        if (!value.trim()) return 'Full name is required';
        if (!fullNamePattern.test(value.trim())) return 'Full name must include first and last name (minimum 2 characters each)';
        if (value.length > 100) return 'Full name must not exceed 100 characters';
        return '';
      case 'email':
        if (!value) return 'Email is required';
        if (!emailPattern.test(value)) return 'Please enter a valid email address';
        return '';
      case 'phoneNumber':
        if (!value) return 'Phone number is required';
        if (!phonePattern.test(value)) return 'Phone number must be 8 digits';
        return '';
      case 'password':
        if (!value) return 'Password is required';
        if (value.length < 6) return 'Password must be at least 6 characters';
        if (!passwordPattern.test(value)) {
          return 'Password must contain uppercase, lowercase, number, and special character (!@#$%^&*-_=+,.)';
        }
        return '';
      case 'confirmPassword':
        if (!value) return 'Please confirm your password';
        if (value !== password) return 'Passwords do not match';
        return '';
      default:
        return '';
    }
  };

  const handleFieldChange = (fieldName: string, value: string) => {
    const error = validateField(fieldName, value);
    setFieldErrors((prev) => ({
      ...prev,
      [fieldName]: error,
    }));

    switch (fieldName) {
      case 'fullName':
        setFullName(value);
        break;
      case 'email':
        setEmail(value);
        break;
      case 'phoneNumber':
        setPhoneNumber(value);
        break;
      case 'password':
        setPassword(value);
        // Update password requirements
        setPasswordRequirements({
          hasMinLength: value.length >= 6,
          hasUppercase: /[A-Z]/.test(value),
          hasLowercase: /[a-z]/.test(value),
          hasNumber: /\d/.test(value),
          hasSpecialChar: /[!@#$%^&*\-_=+,.]/.test(value),
        });
        if (confirmPassword) {
          const confirmError = validateField('confirmPassword', confirmPassword);
          setFieldErrors((prev) => ({
            ...prev,
            confirmPassword: confirmError,
          }));
        }
        break;
      case 'confirmPassword':
        setConfirmPassword(value);
        break;
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLocalError('');

    // Validate all fields
    const newErrors: { [key: string]: string } = {};
    newErrors.fullName = validateField('fullName', fullName);
    newErrors.email = validateField('email', email);
    newErrors.phoneNumber = validateField('phoneNumber', phoneNumber);
    newErrors.password = validateField('password', password);
    newErrors.confirmPassword = validateField('confirmPassword', confirmPassword);

    setFieldErrors(newErrors);

    // Check if any errors exist
    if (Object.values(newErrors).some((err) => err !== '')) {
      return;
    }

    try {
      await register(email, password, fullName, phoneNumber);
      setRegistrationPending(true);
    } catch (err) {
      // Error is handled by AuthContext, display it here too
      setLocalError(error || 'Registration failed. Please try again.');
    }
  };

  if (registrationPending) {
    return (
      <div className="auth-container">
        <div className="auth-box">
          <h1>Dead Pigeons Lottery</h1>
          <h2>Registration Pending</h2>
          <div className="pending-message">
            <p>Thank you for registering!</p>
            <p>Your registration has been submitted and is pending admin approval.</p>
            <p>You will receive confirmation once the admin has reviewed and approved your account.</p>
            <p className="pending-emphasis">Please wait for the Admin to approve your account.</p>
          </div>
          <Link to="/login" className="submit-btn">
            Return to Login
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="auth-container">
      <div className="auth-box">
        <h1>Dead Pigeons Lottery</h1>
        <h2>Register</h2>

        {(localError || error) && (
          <div className="error-message">{localError || error}</div>
        )}

        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label htmlFor="fullName">Full Name</label>
            <input
              id="fullName"
              type="text"
              value={fullName}
              onChange={(e) => handleFieldChange('fullName', e.target.value)}
              placeholder="Enter your full name"
              disabled={isLoading}
              className={fieldErrors.fullName ? 'input-error' : ''}
            />
            {fieldErrors.fullName && (
              <span className="field-error-message">{fieldErrors.fullName}</span>
            )}
          </div>

          <div className="form-group">
            <label htmlFor="email">Email</label>
            <input
              id="email"
              type="email"
              value={email}
              onChange={(e) => handleFieldChange('email', e.target.value)}
              placeholder="Enter your email"
              disabled={isLoading}
              className={fieldErrors.email ? 'input-error' : ''}
            />
            {fieldErrors.email && (
              <span className="field-error-message">{fieldErrors.email}</span>
            )}
          </div>

          <div className="form-group phone-group">
            <label htmlFor="phoneNumber">Phone Number</label>
            <div className="phone-input-wrapper">
              <span className="phone-prefix">+45</span>
              <input
                id="phoneNumber"
                type="tel"
                value={phoneNumber}
                onChange={(e) => handleFieldChange('phoneNumber', e.target.value)}
                disabled={isLoading}
                className={fieldErrors.phoneNumber ? 'input-error' : ''}
              />
            </div>
            {fieldErrors.phoneNumber && (
              <span className="field-error-message">{fieldErrors.phoneNumber}</span>
            )}
          </div>

          <div className="form-group">
            <label htmlFor="password">Password</label>
            <input
              id="password"
              type="password"
              value={password}
              onChange={(e) => handleFieldChange('password', e.target.value)}
              disabled={isLoading}
              className={!password && fieldErrors.password ? 'input-error' : ''}
            />
            {!password && fieldErrors.password && (
              <span className="field-error-message">{fieldErrors.password}</span>
            )}
            {password && (
              <div className="password-requirements">
                {passwordRequirements.hasMinLength &&
                passwordRequirements.hasUppercase &&
                passwordRequirements.hasLowercase &&
                passwordRequirements.hasNumber &&
                passwordRequirements.hasSpecialChar ? (
                  <span className="requirement-all-met">✓ All requirements are met!</span>
                ) : (
                  <>
                    <span className={passwordRequirements.hasMinLength ? 'requirement-met' : 'requirement-unmet'}>
                      {passwordRequirements.hasMinLength ? '✓' : '✕'} At least 6 characters
                    </span>
                    <span className={passwordRequirements.hasUppercase ? 'requirement-met' : 'requirement-unmet'}>
                      {passwordRequirements.hasUppercase ? '✓' : '✕'} Uppercase (A-Z)
                    </span>
                    <span className={passwordRequirements.hasLowercase ? 'requirement-met' : 'requirement-unmet'}>
                      {passwordRequirements.hasLowercase ? '✓' : '✕'} Lowercase (a-z)
                    </span>
                    <span className={passwordRequirements.hasNumber ? 'requirement-met' : 'requirement-unmet'}>
                      {passwordRequirements.hasNumber ? '✓' : '✕'} Number (0-9)
                    </span>
                    <span className={passwordRequirements.hasSpecialChar ? 'requirement-met' : 'requirement-unmet'}>
                      {passwordRequirements.hasSpecialChar ? '✓' : '✕'} Special Char (!@#$%^&*-_=+,.)
                    </span>
                  </>
                )}
              </div>
            )}
          </div>

          <div className="form-group">
            <label htmlFor="confirmPassword">Confirm Password</label>
            <input
              id="confirmPassword"
              type="password"
              value={confirmPassword}
              onChange={(e) => handleFieldChange('confirmPassword', e.target.value)}
              placeholder="Confirm your password"
              disabled={isLoading}
              className={fieldErrors.confirmPassword ? 'input-error' : ''}
            />
            {fieldErrors.confirmPassword && (
              <span className="field-error-message">{fieldErrors.confirmPassword}</span>
            )}
            {confirmPassword && !fieldErrors.confirmPassword && (
              <span className="field-success-message">✓ Passwords match</span>
            )}
          </div>

          <button type="submit" disabled={isLoading} className="submit-btn">
            {isLoading ? 'Creating account...' : 'Register'}
          </button>
        </form>

        <p className="auth-link">
          Already have an account? <Link to="/login">Login here</Link>
        </p>
      </div>
    </div>
  );
};

export default RegisterPage;
