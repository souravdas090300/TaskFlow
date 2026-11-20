import { useState, type SubmitEvent } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { isAxiosError } from 'axios';
import {
  adminApi,
  type CategoryPayload,
  type UpdateCategoryPayload,
} from '../api/admin';
import type { Category } from '../api/products';

const emptyCategoryForm = {
  name: '',
  slug: '',
  description: '',
  imageUrl: '',
  parentCategoryId: '',
  displayOrder: '0',
};

const slugify = (value: string) =>
  value
    .toLowerCase()
    .trim()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/(^-|-$)/g, '');

const getErrorMessage = (error: unknown): string => {
  if (isAxiosError(error)) {
    const data = error.response?.data as { error?: unknown; errors?: Record<string, string[]> } | undefined;
    if (typeof data?.error === 'string') return data.error;
    const first = data?.errors ? Object.values(data.errors).flat()[0] : undefined;
    if (typeof first === 'string') return first;
    return error.message || 'Request failed';
  }
  return 'Request failed';
};

export default function AdminCategories() {
  const queryClient = useQueryClient();
  const [showForm, setShowForm] = useState(false);
  const [editingCategory, setEditingCategory] = useState<Category | null>(null);
  const [pageError, setPageError] = useState('');
  const [formError, setFormError] = useState('');
  const [form, setForm] = useState(emptyCategoryForm);

  const { data: categories, isLoading, error } = useQuery({
    queryKey: ['admin-categories'],
    queryFn: adminApi.getCategories,
  });

  const invalidateCatalog = () => {
    void queryClient.invalidateQueries({ queryKey: ['admin-categories'] });
    void queryClient.invalidateQueries({ queryKey: ['categories'] });
    void queryClient.invalidateQueries({ queryKey: ['admin-products'] });
    void queryClient.invalidateQueries({ queryKey: ['products'] });
  };

  const deleteMutation = useMutation({
    mutationFn: (id: string) => adminApi.deleteCategory(id),
    onSuccess: invalidateCatalog,
    onError: (mutationError: unknown) => setPageError(getErrorMessage(mutationError)),
  });

  const saveMutation = useMutation({
    mutationFn: async () => {
      const base: CategoryPayload = {
        name: form.name.trim(),
        description: form.description.trim() || undefined,
        imageUrl: form.imageUrl.trim() || undefined,
        slug: form.slug.trim(),
        parentCategoryId: form.parentCategoryId || undefined,
        displayOrder: Number(form.displayOrder || 0),
      };

      if (editingCategory) {
        const data: UpdateCategoryPayload = { ...base, isActive: true };
        return adminApi.updateCategory(editingCategory.id, data);
      }
      return adminApi.createCategory(base);
    },
    onSuccess: () => {
      invalidateCatalog();
      closeForm();
    },
    onError: (mutationError: unknown) => setFormError(getErrorMessage(mutationError)),
  });

  const openCreate = () => {
    setEditingCategory(null);
    setForm(emptyCategoryForm);
    setFormError('');
    setPageError('');
    setShowForm(true);
  };

  const openEdit = (category: Category) => {
    setEditingCategory(category);
    setForm({
      name: category.name,
      slug: category.slug,
      description: category.description ?? '',
      imageUrl: category.imageUrl ?? '',
      parentCategoryId: category.parentCategoryId ?? '',
      displayOrder: String(category.displayOrder ?? 0),
    });
    setFormError('');
    setPageError('');
    setShowForm(true);
  };

  const closeForm = () => {
    setShowForm(false);
    setEditingCategory(null);
    setForm(emptyCategoryForm);
    setFormError('');
  };

  const handleSubmit = (event: SubmitEvent<HTMLFormElement>) => {
    event.preventDefault();
    saveMutation.mutate();
  };

  const updateField = <K extends keyof typeof emptyCategoryForm>(key: K, value: (typeof emptyCategoryForm)[K]) => {
    setForm((current) => ({ ...current, [key]: value }));
  };

  if (isLoading) return <div className="p-8 text-gray-600">Loading categories...</div>;
  if (error) return <div className="p-8 text-red-600">Error loading categories</div>;

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Categories</h1>
          <p className="text-gray-600 mt-1">Organize products into storefront categories.</p>
        </div>
        <button onClick={openCreate} className="bg-purple-600 text-white px-4 py-2 rounded-lg text-sm font-semibold hover:bg-purple-700 transition">
          Add Category
        </button>
      </div>

      {pageError && <div className="bg-red-50 border border-red-200 text-red-700 rounded-lg px-4 py-3 text-sm">{pageError}</div>}

      {showForm && (
        <form onSubmit={handleSubmit} className="bg-white rounded-lg shadow-md p-6 space-y-5">
          <div className="flex items-center justify-between">
            <h2 className="text-xl font-semibold text-gray-900">{editingCategory ? 'Edit Category' : 'New Category'}</h2>
            <button type="button" onClick={closeForm} className="text-sm text-gray-500 hover:text-gray-800">Close</button>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label htmlFor="category-name" className="block text-sm font-medium text-gray-700 mb-1">Name</label>
              <input id="category-name" value={form.name} onChange={(event) => updateField('name', event.target.value)} className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" required />
            </div>
            <div>
              <label htmlFor="category-slug" className="block text-sm font-medium text-gray-700 mb-1">Slug</label>
              <div className="flex gap-2">
                <input id="category-slug" value={form.slug} onChange={(event) => updateField('slug', event.target.value)} className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" required />
                <button type="button" onClick={() => updateField('slug', slugify(form.name))} className="px-3 py-2 text-sm border border-gray-300 rounded-lg hover:bg-gray-50">Auto</button>
              </div>
            </div>
            <div>
              <label htmlFor="category-parent" className="block text-sm font-medium text-gray-700 mb-1">Parent category</label>
              <select id="category-parent" value={form.parentCategoryId} onChange={(event) => updateField('parentCategoryId', event.target.value)} className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm bg-white">
                <option value="">None</option>
                {categories?.filter((category) => category.id !== editingCategory?.id).map((category) => (
                  <option key={category.id} value={category.id}>{category.name}</option>
                ))}
              </select>
            </div>
            <div>
              <label htmlFor="category-order" className="block text-sm font-medium text-gray-700 mb-1">Display order</label>
              <input id="category-order" type="number" min="0" step="1" value={form.displayOrder} onChange={(event) => updateField('displayOrder', event.target.value)} className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" />
            </div>
            <div>
              <label htmlFor="category-image" className="block text-sm font-medium text-gray-700 mb-1">Image URL</label>
              <input id="category-image" type="url" value={form.imageUrl} onChange={(event) => updateField('imageUrl', event.target.value)} className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" placeholder="https://..." />
            </div>
            <div className="md:col-span-2">
              <label htmlFor="category-description" className="block text-sm font-medium text-gray-700 mb-1">Description</label>
              <textarea id="category-description" value={form.description} onChange={(event) => updateField('description', event.target.value)} rows={3} className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" />
            </div>
          </div>

          {formError && <div className="bg-red-50 border border-red-200 text-red-700 rounded-lg px-4 py-3 text-sm">{formError}</div>}

          <div className="flex justify-end gap-3">
            <button type="button" onClick={closeForm} className="px-4 py-2 text-sm border border-gray-300 rounded-lg hover:bg-gray-50">Cancel</button>
            <button type="submit" disabled={saveMutation.isPending} className="bg-purple-600 text-white px-4 py-2 rounded-lg text-sm font-semibold hover:bg-purple-700 disabled:bg-gray-300">
              {saveMutation.isPending ? 'Saving...' : editingCategory ? 'Save changes' : 'Create category'}
            </button>
          </div>
        </form>
      )}

      <div className="bg-white rounded-lg shadow-md overflow-hidden">
        <table className="min-w-full">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Category</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Parent</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Products</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Order</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Actions</th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {categories?.length === 0 && (
              <tr><td colSpan={5} className="px-6 py-10 text-center text-gray-500">No categories found.</td></tr>
            )}
            {categories?.map((category) => (
              <tr key={category.id}>
                <td className="px-6 py-4">
                  <div className="text-sm font-medium text-gray-900">{category.name}</div>
                  <div className="text-xs text-gray-500">{category.slug}</div>
                </td>
                <td className="px-6 py-4 text-sm text-gray-700">{category.parentCategoryName || '—'}</td>
                <td className="px-6 py-4 text-sm text-gray-700">{category.productCount ?? 0}</td>
                <td className="px-6 py-4 text-sm text-gray-700">{category.displayOrder}</td>
                <td className="px-6 py-4">
                  <div className="flex gap-2">
                    <button onClick={() => openEdit(category)} className="text-sm bg-blue-600 text-white px-3 py-1 rounded hover:bg-blue-700">Edit</button>
                    <button onClick={() => { if (window.confirm(`Delete ${category.name}?`)) deleteMutation.mutate(category.id); }} className="text-sm bg-red-600 text-white px-3 py-1 rounded hover:bg-red-700">Delete</button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
