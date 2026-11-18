import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';

interface LegalPageProps {
  title: string;
  updated: string;
  children: ReactNode;
}

export const LegalSection = ({ title, children }: { title: string; children: ReactNode }) => (
  <section>
    <h2 className="text-xl font-semibold text-white">{title}</h2>
    <div className="mt-3 space-y-3 text-slate-400">{children}</div>
  </section>
);

export const LegalPage = ({ title, updated, children }: LegalPageProps) => (
  <div className="min-h-screen bg-slate-950 text-slate-200">
    <section className="container mx-auto px-4 py-16 max-w-3xl">
      <Link to="/" className="text-sm text-purple-400 hover:text-purple-300 transition">
        ← Back to TaskFlow
      </Link>
      <h1 className="mt-6 text-4xl font-bold text-white tracking-tight">{title}</h1>
      <p className="mt-3 text-sm text-slate-500">Last updated: {updated}</p>
      <div className="mt-10 space-y-8">{children}</div>
      <div className="mt-16 border-t border-white/10 pt-6 flex flex-wrap gap-6 text-sm">
        <Link to="/privacy-policy" className="text-slate-400 hover:text-purple-400 transition">
          Privacy Policy
        </Link>
        <Link to="/terms-of-service" className="text-slate-400 hover:text-purple-400 transition">
          Terms of Service
        </Link>
        <Link to="/" className="text-slate-400 hover:text-purple-400 transition">
          Home
        </Link>
      </div>
    </section>
  </div>
);
