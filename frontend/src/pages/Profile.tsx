import { useState, type SubmitEvent } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useAuthStore } from '../store/useAuthStore';
import { authApi, type User } from '../api/auth';
import { addressesApi, type AddressDto } from '../api/addresses';
import { AddressForm } from '../components/AddressForm';
import { getApiErrorMessage } from '../utils/errors';

const inputClass = 'w-full px-4 py-2 border rounded-lg';
const labelClass = 'block text-sm font-medium mb-1';

const ProfileContent = ({ user, setUser }: { user: User; setUser: (user: User | null) => void }) => {
  const queryClient = useQueryClient();

  const [firstName, setFirstName] = useState(user.firstName);
  const [lastName, setLastName] = useState(user.lastName);
  const [phone, setPhone] = useState(user.phone ?? '');
  const [avatarUrl, setAvatarUrl] = useState(user.avatarUrl ?? '');
  const [profileMessage, setProfileMessage] = useState('');
  const [profileError, setProfileError] = useState('');

  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [passwordMessage, setPasswordMessage] = useState('');
  const [passwordError, setPasswordError] = useState('');

  const [editingAddress, setEditingAddress] = useState<AddressDto | 'new' | null>(null);
  const [addressError, setAddressError] = useState('');

  const { data: addresses = [], isLoading: addressesLoading } = useQuery({
    queryKey: ['addresses'],
    queryFn: addressesApi.list,
    enabled: !!user,
  });

  const refreshAddresses = () => {
    void queryClient.invalidateQueries({ queryKey: ['addresses'] });
    void queryClient.invalidateQueries({ queryKey: ['checkout'] });
  };

  const handleProfileSubmit = async (e: SubmitEvent<HTMLFormElement>) => {
    e.preventDefault();
    setProfileError('');
    setProfileMessage('');
    try {
      const updated = await authApi.updateProfile({
        firstName: firstName.trim(),
        lastName: lastName.trim(),
        phone: phone.trim() || undefined,
        avatarUrl: avatarUrl.trim() || undefined,
      });
      setUser(updated);
      setProfileMessage('Profile updated');
    } catch (error) {
      setProfileError(getApiErrorMessage(error, 'Could not update profile'));
    }
  };

  const handlePasswordSubmit = async (e: SubmitEvent<HTMLFormElement>) => {
    e.preventDefault();
    setPasswordError('');
    setPasswordMessage('');
    if (newPassword !== confirmPassword) {
      setPasswordError('Passwords do not match');
      return;
    }
    try {
      await authApi.changePassword({ currentPassword, newPassword });
      setPasswordMessage('Password changed');
      setCurrentPassword('');
      setNewPassword('');
      setConfirmPassword('');
    } catch (error) {
      setPasswordError(getApiErrorMessage(error, 'Could not change password'));
    }
  };

  const handleDeleteAddress = async (id: string) => {
    if (!window.confirm('Delete this address?')) return;
    setAddressError('');
    try {
      await addressesApi.remove(id);
      refreshAddresses();
    } catch (error) {
      setAddressError(getApiErrorMessage(error, 'Could not delete address'));
    }
  };

  return (
    <div className="min-h-screen bg-gray-50 py-8">
      <div className="container mx-auto px-4 max-w-4xl space-y-6">
        <h1 className="text-3xl font-bold">Profile</h1>

        <section className="bg-white rounded-lg shadow-md p-6 border border-gray-200">
          <h2 className="text-xl font-bold mb-4">Account</h2>
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-6 text-sm">
            <div><span className="text-gray-500">Email</span><p className="font-medium">{user.email}</p></div>
            <div><span className="text-gray-500">Role</span><p className="font-medium">{user.role}</p></div>
            <div><span className="text-gray-500">Member since</span><p className="font-medium">{new Date(user.createdAt).toLocaleDateString()}</p></div>
          </div>

          {profileMessage && <div className="mb-4 bg-green-50 border border-green-200 text-green-700 rounded-lg px-4 py-3 text-sm">{profileMessage}</div>}
          {profileError && <div className="mb-4 bg-red-50 border border-red-200 text-red-700 rounded-lg px-4 py-3 text-sm">{profileError}</div>}

          <form onSubmit={handleProfileSubmit} className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label htmlFor="profile-first-name" className={labelClass}>First Name</label>
              <input id="profile-first-name" value={firstName} onChange={(e) => setFirstName(e.target.value)} className={inputClass} required />
            </div>
            <div>
              <label htmlFor="profile-last-name" className={labelClass}>Last Name</label>
              <input id="profile-last-name" value={lastName} onChange={(e) => setLastName(e.target.value)} className={inputClass} required />
            </div>
            <div>
              <label htmlFor="profile-phone" className={labelClass}>Phone</label>
              <input id="profile-phone" value={phone} onChange={(e) => setPhone(e.target.value)} className={inputClass} />
            </div>
            <div>
              <label htmlFor="profile-avatar" className={labelClass}>Avatar URL</label>
              <input id="profile-avatar" value={avatarUrl} onChange={(e) => setAvatarUrl(e.target.value)} className={inputClass} />
            </div>
            <div className="sm:col-span-2">
              <button type="submit" className="bg-purple-600 text-white px-6 py-2 rounded-lg font-semibold hover:bg-purple-700">Save profile</button>
            </div>
          </form>
        </section>

        <section className="bg-white rounded-lg shadow-md p-6 border border-gray-200">
          <h2 className="text-xl font-bold mb-4">Change password</h2>
          {passwordMessage && <div className="mb-4 bg-green-50 border border-green-200 text-green-700 rounded-lg px-4 py-3 text-sm">{passwordMessage}</div>}
          {passwordError && <div className="mb-4 bg-red-50 border border-red-200 text-red-700 rounded-lg px-4 py-3 text-sm">{passwordError}</div>}
          <form onSubmit={handlePasswordSubmit} className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <div>
              <label htmlFor="current-password" className={labelClass}>Current password</label>
              <input id="current-password" type="password" value={currentPassword} onChange={(e) => setCurrentPassword(e.target.value)} className={inputClass} required />
            </div>
            <div>
              <label htmlFor="new-password" className={labelClass}>New password</label>
              <input id="new-password" type="password" value={newPassword} onChange={(e) => setNewPassword(e.target.value)} className={inputClass} required minLength={8} />
            </div>
            <div>
              <label htmlFor="confirm-password" className={labelClass}>Confirm password</label>
              <input id="confirm-password" type="password" value={confirmPassword} onChange={(e) => setConfirmPassword(e.target.value)} className={inputClass} required minLength={8} />
            </div>
            <div className="sm:col-span-3">
              <button type="submit" className="bg-purple-600 text-white px-6 py-2 rounded-lg font-semibold hover:bg-purple-700">Change password</button>
            </div>
          </form>
        </section>

        <section className="bg-white rounded-lg shadow-md p-6 border border-gray-200">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-xl font-bold">Addresses</h2>
            {editingAddress === null && (
              <button type="button" onClick={() => setEditingAddress('new')} className="text-sm font-semibold text-purple-600 hover:text-purple-700">Add address</button>
            )}
          </div>

          {addressError && <div className="mb-4 bg-red-50 border border-red-200 text-red-700 rounded-lg px-4 py-3 text-sm">{addressError}</div>}

          {editingAddress !== null && (
            <div className="mb-6 rounded-lg border border-gray-200 p-4 bg-gray-50">
              {editingAddress === 'new' ? (
                <AddressForm
                  defaultType="Shipping"
                  defaultIsDefault={addresses.length === 0}
                  onSaved={() => { setEditingAddress(null); refreshAddresses(); }}
                  onCancel={() => setEditingAddress(null)}
                />
              ) : (
                <AddressForm
                  initial={editingAddress}
                  onSaved={() => { setEditingAddress(null); refreshAddresses(); }}
                  onCancel={() => setEditingAddress(null)}
                />
              )}
            </div>
          )}

          {addressesLoading ? (
            <p className="text-gray-500">Loading addresses...</p>
          ) : addresses.length === 0 ? (
            <p className="text-gray-500">No addresses yet.</p>
          ) : (
            <div className="space-y-3">
              {addresses.map((address) => (
                <div key={address.id} className="rounded-lg border border-gray-200 p-4 flex flex-col sm:flex-row sm:items-center gap-4">
                  <div className="flex-1">
                    <div className="flex items-center gap-2 mb-1">
                      <span className="text-xs font-semibold uppercase tracking-wide bg-purple-100 text-purple-700 rounded-full px-2 py-0.5">{address.type}</span>
                      {address.isDefault && <span className="text-xs font-semibold text-green-700">Default</span>}
                    </div>
                    <p className="font-medium">{address.firstName} {address.lastName}</p>
                    <p className="text-sm text-gray-600">{address.addressLine1}{address.addressLine2 ? `, ${address.addressLine2}` : ''}</p>
                    <p className="text-sm text-gray-600">{address.city}, {address.state} {address.postalCode}, {address.country}</p>
                    {address.phone && <p className="text-sm text-gray-600">{address.phone}</p>}
                  </div>
                  <div className="flex gap-2">
                    <button type="button" onClick={() => setEditingAddress(address)} className="px-3 py-1.5 text-sm border rounded-lg hover:bg-gray-50">Edit</button>
                    <button type="button" onClick={() => void handleDeleteAddress(address.id)} className="px-3 py-1.5 text-sm border border-red-200 text-red-600 rounded-lg hover:bg-red-50">Delete</button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </section>
      </div>
    </div>
  );
};

export const Profile = () => {
  const { user, setUser } = useAuthStore();
  if (!user) return <div className="min-h-screen bg-gray-50 flex items-center justify-center">Loading...</div>;
  return <ProfileContent user={user} setUser={setUser} />;
};
