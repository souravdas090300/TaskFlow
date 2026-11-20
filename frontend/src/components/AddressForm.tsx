import { useState, type ChangeEvent, type SubmitEvent } from 'react';
import { addressesApi, type AddressDto, type AddressPayload } from '../api/addresses';
import { getApiErrorMessage } from '../utils/errors';

interface AddressFormProps {
  initial?: AddressDto;
  defaultType?: 'Shipping' | 'Billing';
  showType?: boolean;
  defaultIsDefault?: boolean;
  submitLabel?: string;
  onSaved: (address: AddressDto) => void;
  onCancel?: () => void;
}

const inputClass = 'w-full px-4 py-2 border rounded-lg';
const labelClass = 'block text-sm font-medium mb-1';

export const AddressForm = ({
  initial,
  defaultType = 'Shipping',
  showType = true,
  defaultIsDefault = false,
  submitLabel,
  onSaved,
  onCancel,
}: AddressFormProps) => {
  const [form, setForm] = useState(() => ({
    type: initial?.type ?? defaultType,
    firstName: initial?.firstName ?? '',
    lastName: initial?.lastName ?? '',
    addressLine1: initial?.addressLine1 ?? '',
    addressLine2: initial?.addressLine2 ?? '',
    city: initial?.city ?? '',
    state: initial?.state ?? 'Malta',
    postalCode: initial?.postalCode ?? '',
    country: initial?.country ?? 'Malta',
    phone: initial?.phone ?? '',
    isDefault: initial?.isDefault ?? defaultIsDefault,
  }));
  const [error, setError] = useState('');
  const [saving, setSaving] = useState(false);

  const setField = (field: keyof typeof form) => (e: ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    const value = e.target.type === 'checkbox' ? (e.target as HTMLInputElement).checked : e.target.value;
    setForm((current) => ({ ...current, [field]: value }));
  };

  const handleSubmit = async (e: SubmitEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError('');
    setSaving(true);
    try {
      const payload: AddressPayload = {
        type: form.type,
        firstName: form.firstName.trim(),
        lastName: form.lastName.trim(),
        addressLine1: form.addressLine1.trim(),
        addressLine2: form.addressLine2.trim() || undefined,
        city: form.city.trim(),
        state: form.state.trim(),
        postalCode: form.postalCode.trim(),
        country: form.country.trim(),
        phone: form.phone.trim() || undefined,
        isDefault: form.isDefault,
      };
      const saved = initial ? await addressesApi.update(initial.id, payload) : await addressesApi.create(payload);
      onSaved(saved);
    } catch (submitError) {
      setError(getApiErrorMessage(submitError, 'Could not save address'));
    } finally {
      setSaving(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 rounded-lg px-4 py-3 text-sm">{error}</div>
      )}

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        {showType && (
          <div>
            <label htmlFor="address-type" className={labelClass}>Type</label>
            <select id="address-type" value={form.type} onChange={setField('type')} className={inputClass} required>
              <option value="Shipping">Shipping</option>
              <option value="Billing">Billing</option>
            </select>
          </div>
        )}
        <div>
          <label htmlFor="address-first-name" className={labelClass}>First Name</label>
          <input id="address-first-name" value={form.firstName} onChange={setField('firstName')} className={inputClass} required />
        </div>
        <div>
          <label htmlFor="address-last-name" className={labelClass}>Last Name</label>
          <input id="address-last-name" value={form.lastName} onChange={setField('lastName')} className={inputClass} required />
        </div>
        <div className="sm:col-span-2">
          <label htmlFor="address-line1" className={labelClass}>Address Line 1</label>
          <input id="address-line1" value={form.addressLine1} onChange={setField('addressLine1')} className={inputClass} required />
        </div>
        <div className="sm:col-span-2">
          <label htmlFor="address-line2" className={labelClass}>Address Line 2</label>
          <input id="address-line2" value={form.addressLine2} onChange={setField('addressLine2')} className={inputClass} />
        </div>
        <div>
          <label htmlFor="address-city" className={labelClass}>City</label>
          <input id="address-city" value={form.city} onChange={setField('city')} className={inputClass} required />
        </div>
        <div>
          <label htmlFor="address-state" className={labelClass}>State</label>
          <input id="address-state" value={form.state} onChange={setField('state')} className={inputClass} required />
        </div>
        <div>
          <label htmlFor="address-postal" className={labelClass}>Postal Code</label>
          <input id="address-postal" value={form.postalCode} onChange={setField('postalCode')} className={inputClass} required />
        </div>
        <div>
          <label htmlFor="address-country" className={labelClass}>Country</label>
          <input id="address-country" value={form.country} onChange={setField('country')} className={inputClass} required />
        </div>
        <div>
          <label htmlFor="address-phone" className={labelClass}>Phone</label>
          <input id="address-phone" value={form.phone} onChange={setField('phone')} className={inputClass} />
        </div>
        <label className="sm:col-span-2 flex items-center gap-2 text-sm text-gray-600">
          <input type="checkbox" checked={form.isDefault} onChange={setField('isDefault')} />
          Set as default {form.type.toLowerCase()} address
        </label>
      </div>

      <div className="flex gap-3">
        <button
          type="submit"
          disabled={saving}
          className="flex-1 bg-purple-600 text-white py-2 rounded-lg font-semibold hover:bg-purple-700 disabled:bg-gray-300"
        >
          {saving ? 'Saving...' : (submitLabel ?? (initial ? 'Save address' : 'Add address'))}
        </button>
        {onCancel && (
          <button type="button" onClick={onCancel} className="px-4 py-2 border rounded-lg text-sm font-medium hover:bg-gray-50">
            Cancel
          </button>
        )}
      </div>
    </form>
  );
};
