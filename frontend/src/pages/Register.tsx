import { useState, type SubmitEvent } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuthStore } from '../store/useAuthStore';
import { GoogleAuthButton } from '../components/GoogleAuthButton';

const getErrorMessage = (error: unknown) => {
  if (typeof error === 'object' && error !== null && 'response' in error) {
    const data = (error as { response?: { data?: { error?: string; message?: string } } }).response?.data;
    return data?.error ?? data?.message ?? 'Registration failed';
  }
  return 'Registration failed';
};

const EyeIcon = () => (
  <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth={1.8} viewBox="0 0 24 24" aria-hidden="true">
    <path strokeLinecap="round" strokeLinejoin="round" d="M2.036 12.322a1.012 1.012 0 0 1 0-.639C3.423 7.51 7.36 4.5 12 4.5c4.638 0 8.573 3.007 9.963 7.178.07.207.07.431 0 .639C20.577 16.49 16.64 19.5 12 19.5c-4.638 0-8.573-3.007-9.963-7.178Z" />
    <path strokeLinecap="round" strokeLinejoin="round" d="M15 12a3 3 0 1 1-6 0 3 3 0 0 1 6 0Z" />
  </svg>
);

export const Register = () => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [agreed, setAgreed] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [error, setError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const register = useAuthStore((state) => state.register);
  const navigate = useNavigate();

  const handleSubmit = async (e: SubmitEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError('');

    if (password !== confirmPassword) {
      setError('Passwords do not match.');
      return;
    }
    if (!agreed) {
      setError('Please agree to the Terms of Service and Privacy Policy.');
      return;
    }

    setIsSubmitting(true);
    try {
      await register(email, password, firstName, lastName);
      void navigate('/');
    } catch (submitError) {
      console.error(submitError);
      setError(getErrorMessage(submitError));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="min-h-screen bg-gray-50 flex items-center justify-center py-12">
      <div className="bg-white rounded-lg shadow-md p-8 w-full max-w-md">
        <h1 className="text-2xl font-bold mb-6">Register</h1>

        {error && (
          <div className="mb-4 bg-red-50 border border-red-200 text-red-700 rounded-lg px-4 py-3 text-sm">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label htmlFor="register-first-name" className="block text-sm font-medium mb-1">First Name</label>
            <input
              id="register-first-name"
              type="text"
              value={firstName}
              onChange={(e) => setFirstName(e.target.value)}
              className="w-full px-4 py-2 border rounded-lg"
              required
            />
          </div>
          <div>
            <label htmlFor="register-last-name" className="block text-sm font-medium mb-1">Last Name</label>
            <input
              id="register-last-name"
              type="text"
              value={lastName}
              onChange={(e) => setLastName(e.target.value)}
              className="w-full px-4 py-2 border rounded-lg"
              required
            />
          </div>
          <div>
            <label htmlFor="register-email" className="block text-sm font-medium mb-1">Email</label>
            <input
              id="register-email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="w-full px-4 py-2 border rounded-lg"
              required
            />
          </div>
          <div>
            <label htmlFor="register-password" className="block text-sm font-medium mb-1">Password</label>
            <div className="relative">
              <input
                id="register-password"
                type={showPassword ? 'text' : 'password'}
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                className="w-full px-4 py-2 pr-10 border rounded-lg"
                required
                minLength={8}
              />
              <button
                type="button"
                onClick={() => setShowPassword((value) => !value)}
                className="absolute right-2 top-1/2 -translate-y-1/2 p-1 text-gray-500 hover:text-gray-700"
                aria-label={showPassword ? 'Hide password' : 'Show password'}
              >
                <EyeIcon />
              </button>
            </div>
          </div>
          <div>
            <label htmlFor="register-confirm-password" className="block text-sm font-medium mb-1">Confirm Password</label>
            <div className="relative">
              <input
                id="register-confirm-password"
                type={showConfirmPassword ? 'text' : 'password'}
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                className="w-full px-4 py-2 pr-10 border rounded-lg"
                required
                minLength={8}
              />
              <button
                type="button"
                onClick={() => setShowConfirmPassword((value) => !value)}
                className="absolute right-2 top-1/2 -translate-y-1/2 p-1 text-gray-500 hover:text-gray-700"
                aria-label={showConfirmPassword ? 'Hide confirm password' : 'Show confirm password'}
              >
                <EyeIcon />
              </button>
            </div>
          </div>

          <label className="flex items-start gap-2 text-sm text-gray-600">
            <input
              type="checkbox"
              checked={agreed}
              onChange={(e) => setAgreed(e.target.checked)}
              required
              className="mt-1"
            />
            <span>
              I agree to the <Link to="/terms-of-service" className="text-purple-600">Terms of Service</Link> and <Link to="/privacy-policy" className="text-purple-600">Privacy Policy</Link>
            </span>
          </label>

          <button
            type="submit"
            disabled={isSubmitting}
            className="w-full bg-purple-600 text-white py-2 rounded-lg font-semibold hover:bg-purple-700 disabled:bg-gray-300"
          >
            {isSubmitting ? 'Creating account...' : 'Register'}
          </button>
        </form>

        <div className="my-6 flex items-center gap-3">
          <div className="h-px flex-1 bg-gray-200" />
          <span className="text-sm text-gray-500">or</span>
          <div className="h-px flex-1 bg-gray-200" />
        </div>
        <GoogleAuthButton
          onSuccess={() => void navigate('/')}
          onError={(message) => setError(message)}
        />

        <p className="mt-4 text-center text-sm">
          Already have an account? <Link to="/login" className="text-purple-600">Login</Link>
        </p>
      </div>
    </div>
  );
};