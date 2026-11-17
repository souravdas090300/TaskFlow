import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { adminApi, type PaymentIssue } from '../api/admin';

const typeBadge = (type: string) => {
  const styles: Record<string, string> = {
    OrderNotFound: 'bg-red-100 text-red-800',
    OrderClosed: 'bg-orange-100 text-orange-800',
    DoubleCharge: 'bg-purple-100 text-purple-800',
    StockConflict: 'bg-yellow-100 text-yellow-800',
  };
  return styles[type] ?? 'bg-gray-100 text-gray-800';
};

const IssueCard = ({ issue, onResolved }: { issue: PaymentIssue; onResolved: () => void }) => {
  const [note, setNote] = useState('');
  const [error, setError] = useState('');

  const resolveMutation = useMutation({
    mutationFn: () => adminApi.resolvePaymentIssue(issue.id, note.trim()),
    onSuccess: onResolved,
    onError: (e: any) => {
      const msg = e?.response?.data?.error;
      setError(typeof msg === 'string' ? msg : 'Failed to resolve issue. Please try again.');
    },
  });

  const canSubmit = note.trim().length > 0 && !resolveMutation.isPending;

  return (
    <div className="bg-white rounded-lg shadow-md p-5 border-l-4 border-purple-500">
      <div className="flex flex-wrap items-start justify-between gap-3 mb-3">
        <div>
          <div className="flex items-center gap-3 flex-wrap">
            <span className={`px-2.5 py-1 rounded-full text-xs font-semibold ${typeBadge(issue.type)}`}>
              {issue.type}
            </span>
            {issue.orderNumber && (
              <span className="text-sm font-medium text-gray-700">
                Order{' '}
                {issue.orderId ? (
                  <Link to={`/admin/orders`} className="text-purple-600 hover:underline">
                    {issue.orderNumber}
                  </Link>
                ) : (
                  issue.orderNumber
                )}
              </span>
            )}
          </div>
          <p className="text-xs text-gray-500 mt-1">
            {new Date(issue.createdAt).toLocaleString()}
          </p>
        </div>
        <div className="text-xs text-gray-500 font-mono break-all max-w-xs">
          PI: {issue.paymentIntentId}
        </div>
      </div>

      <p className="text-sm text-gray-700 bg-gray-50 rounded-lg px-3 py-2 mb-4">{issue.details}</p>

      <div className="flex gap-2">
        <input
          type="text"
          value={note}
          onChange={(e) => {
            setNote(e.target.value);
            setError('');
          }}
          placeholder="Resolution note (required) — e.g. refunded in Stripe, re_1ABC..."
          className="flex-1 px-3 py-2 border border-gray-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-purple-500 focus:border-transparent"
        />
        <button
          onClick={() => resolveMutation.mutate()}
          disabled={!canSubmit}
          className="bg-purple-600 text-white px-4 py-2 rounded-lg text-sm font-semibold hover:bg-purple-700 transition disabled:bg-gray-300 disabled:cursor-not-allowed"
        >
          {resolveMutation.isPending ? 'Resolving…' : 'Resolve'}
        </button>
      </div>
      {error && <p className="mt-2 text-sm text-red-600">{error}</p>}
    </div>
  );
};

export default function AdminPaymentIssues() {
  const queryClient = useQueryClient();
  const { data: issues, isLoading, error } = useQuery({
    queryKey: ['admin-payment-issues'],
    queryFn: adminApi.getPaymentIssues,
  });

  const handleResolved = () => {
    void queryClient.invalidateQueries({ queryKey: ['admin-payment-issues'] });
  };

  return (
    <div>
      <div className="mb-6">
        <h1 className="text-3xl font-bold text-gray-900">Payment Issues</h1>
        <p className="text-gray-600 mt-1">
          Open issues from Stripe webhook processing — investigate and resolve each one.
        </p>
      </div>

      {isLoading && <div className="p-8 text-gray-600">Loading payment issues...</div>}
      {error && <div className="p-8 text-red-600">Error loading payment issues</div>}

      {!isLoading && !error && (!issues || issues.length === 0) && (
        <div className="bg-white rounded-lg shadow-md p-10 text-center">
          <div className="text-4xl mb-3">✅</div>
          <p className="text-gray-700 font-medium">No open payment issues</p>
          <p className="text-gray-500 text-sm mt-1">All caught up — nothing needs attention right now.</p>
        </div>
      )}

      <div className="space-y-4">
        {issues?.map((issue) => (
          <IssueCard key={issue.id} issue={issue} onResolved={handleResolved} />
        ))}
      </div>
    </div>
  );
}
