import { useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { authApi } from '../api/auth';

export const VerifyEmail = () => {
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token') ?? '';
  const [status, setStatus] = useState<'loading' | 'ok' | 'error'>('loading');

  useEffect(() => {
    if (!token) return;

    let cancelled = false;
    authApi
      .verifyEmail(token)
      .then(() => {
        if (!cancelled) setStatus('ok');
      })
      .catch(() => {
        if (!cancelled) setStatus('error');
      });

    return () => {
      cancelled = true;
    };
  }, [token]);

  return (
    <div className="min-h-screen bg-gray-50 flex items-center justify-center py-12">
      <div className="bg-white rounded-lg shadow-md p-8 w-full max-w-md text-center">
        {token && status === 'loading' && <p className="text-gray-600">Verifying your email…</p>}
        {status === 'ok' && (
          <>
            <h1 className="text-2xl font-bold mb-2">Email verified</h1>
            <p className="text-sm text-gray-600 mb-4">Your email address has been confirmed.</p>
            <Link to="/" className="text-purple-600 font-semibold">Continue shopping</Link>
          </>
        )}
        {(!token || status === 'error') && (
          <>
            <h1 className="text-2xl font-bold mb-2">Verification failed</h1>
            <p className="text-sm text-gray-600 mb-4">
              This verification link is invalid or has expired.
            </p>
            <Link to="/login" className="text-purple-600 font-semibold">Back to login</Link>
          </>
        )}
      </div>
    </div>
  );
};

export default VerifyEmail;
