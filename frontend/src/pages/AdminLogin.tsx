import { useState, type SubmitEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '../store/useAuthStore';

export const AdminLogin = () => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const login = useAuthStore((state) => state.login);
  const navigate = useNavigate();

  const handleSubmit = async (e: SubmitEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError('');
    try {
      await login(email, password);
      const user = useAuthStore.getState().user;
      if (user?.role !== 'Admin') {
        useAuthStore.getState().logout();
        setError('This account does not have admin access.');
        return;
      }
      void navigate('/admin');
    } catch {
      setError('Invalid email or password.');
    }
  };

  return (
    <div className="min-h-screen bg-slate-950 flex items-center justify-center py-12 px-4">
      <div className="bg-slate-900 border border-white/10 rounded-2xl p-8 w-full max-w-md shadow-2xl">
        <div className="text-center mb-6">
          <div className="text-sm uppercase tracking-widest text-purple-400 font-semibold">TaskFlow</div>
          <h1 className="text-2xl font-bold text-white mt-1">Admin Sign In</h1>
          <p className="text-slate-500 text-sm mt-1">Restricted area — authorized personnel only</p>
        </div>
        {error && (
          <div className="mb-4 bg-red-500/10 border border-red-500/30 text-red-300 text-sm rounded-lg px-4 py-2">
            {error}
          </div>
        )}
        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label htmlFor="admin-email" className="block text-sm font-medium text-slate-300 mb-1">Email</label>
            <input
              id="admin-email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="w-full px-4 py-2 bg-slate-800 border border-white/10 rounded-lg text-white placeholder-slate-500 focus:outline-none focus:border-purple-500"
              required
            />
          </div>
          <div>
            <label htmlFor="admin-password" className="block text-sm font-medium text-slate-300 mb-1">Password</label>
            <input
              id="admin-password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="w-full px-4 py-2 bg-slate-800 border border-white/10 rounded-lg text-white placeholder-slate-500 focus:outline-none focus:border-purple-500"
              required
            />
          </div>
          <button
            type="submit"
            className="w-full bg-gradient-to-r from-purple-600 to-fuchsia-600 text-white py-2.5 rounded-lg font-semibold hover:from-purple-500 hover:to-fuchsia-500 transition"
          >
            Sign in to Admin
          </button>
        </form>
      </div>
    </div>
  );
};

export default AdminLogin;
