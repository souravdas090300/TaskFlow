export const getApiErrorMessage = (error: unknown, fallback = 'Something went wrong') => {
  if (typeof error === 'object' && error !== null && 'response' in error) {
    const response = (error as { response?: { data?: { error?: string; message?: string } } }).response;
    return response?.data?.error ?? response?.data?.message ?? fallback;
  }
  return fallback;
};
