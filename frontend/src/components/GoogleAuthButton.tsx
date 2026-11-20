import { useEffect, useRef, useState } from 'react';
import { useAuthStore } from '../store/useAuthStore';

type GoogleCredentialResponse = { credential: string };
type GoogleAccountsId = {
  initialize: (config: { client_id: string; callback: (response: GoogleCredentialResponse) => void }) => void;
  renderButton: (element: HTMLElement, options: Record<string, unknown>) => void;
};

declare global {
  interface Window {
    google?: { accounts?: { id?: GoogleAccountsId } };
  }
}

const GIS_SRC = 'https://accounts.google.com/gsi/client';

interface GoogleAuthButtonProps {
  onSuccess?: () => void;
  onError?: (message: string) => void;
  className?: string;
}

export const GoogleAuthButton = ({ onSuccess, onError, className }: GoogleAuthButtonProps) => {
  const clientId = import.meta.env.VITE_GOOGLE_CLIENT_ID as string | undefined;
  const buttonRef = useRef<HTMLDivElement | null>(null);
  const [scriptReady, setScriptReady] = useState(false);
  const loginWithGoogle = useAuthStore((state) => state.loginWithGoogle);
  const onSuccessRef = useRef(onSuccess);
  const onErrorRef = useRef(onError);

  useEffect(() => {
    onSuccessRef.current = onSuccess;
    onErrorRef.current = onError;
  }, [onSuccess, onError]);

  useEffect(() => {
    if (!clientId) return;
    if (document.querySelector(`script[src="${GIS_SRC}"]`)) {
      const timeout = window.setTimeout(() => setScriptReady(true), 0);
      return () => window.clearTimeout(timeout);
    }

    const script = document.createElement('script');
    script.src = GIS_SRC;
    script.async = true;
    script.defer = true;
    script.onload = () => setScriptReady(true);
    script.onerror = () => onErrorRef.current?.('Failed to load Google sign-in');
    document.head.appendChild(script);
  }, [clientId]);

  useEffect(() => {
    const googleId = window.google?.accounts?.id;
    if (!scriptReady || !clientId || !buttonRef.current || !googleId) return;

    googleId.initialize({
      client_id: clientId,
      callback: async (response) => {
        try {
          await loginWithGoogle(response.credential);
          onSuccessRef.current?.();
        } catch {
          onErrorRef.current?.('Google sign-in failed');
        }
      },
    });
    googleId.renderButton(buttonRef.current, { theme: 'outline', size: 'large', width: '100%' });
  }, [scriptReady, clientId, loginWithGoogle]);

  if (!clientId) return null;
  return <div ref={buttonRef} className={className ?? 'w-full'} />;
};
