import { useState, type SubmitEvent } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { isAxiosError } from 'axios';
import {
  adminApi,
  type CreateProductPayload,
  type ProductImagePayload,
  type ProductVariantPayload,
  type UpdateProductPayload,
} from '../api/admin';
import type { Product, ProductImage, ProductListItem, ProductVariant } from '../api/products';
import { formatPrice } from '../utils/format';

const emptyProductForm = {
  name: '',
  slug: '',
  description: '',
  categoryId: '',
  price: '',
  stock: '',
  brand: '',
  sku: '',
  isFeatured: false,
  isNewArrival: false,
  primaryImageUrl: '',
};

const slugify = (value: string) =>
  value
    .toLowerCase()
    .trim()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/(^-|-$)/g, '');

const toImagePayload = (image: ProductImage): ProductImagePayload => ({
  imageUrl: image.imageUrl,
  altText: image.altText,
  displayOrder: image.displayOrder,
  isPrimary: image.isPrimary,
});

const toVariantPayload = (variant: ProductVariant): ProductVariantPayload => ({
  size: variant.size,
  color: variant.color,
  colorCode: variant.colorCode,
  price: variant.price,
  stock: variant.stock,
  sku: variant.sku,
  isActive: variant.isActive,
});

const buildEditImages = (images: ProductImage[], primaryUrl: string): ProductImagePayload[] => {
  const base = images.map(toImagePayload);
  const url = primaryUrl.trim();
  if (!url) return base;

  const primaryIndex = base.findIndex((image) => image.isPrimary);
  if (primaryIndex >= 0) {
    base[primaryIndex] = { ...base[primaryIndex], imageUrl: url, isPrimary: true };
    return base;
  }
  if (base.length > 0) {
    base[0] = { ...base[0], imageUrl: url, isPrimary: true };
    return base;
  }
  return [{ imageUrl: url, displayOrder: 0, isPrimary: true }];
};

const buildCreateImages = (primaryUrl: string, name: string): ProductImagePayload[] => {
  const url = primaryUrl.trim();
  if (!url) return [];
  return [{ imageUrl: url, altText: name.trim() || undefined, displayOrder: 0, isPrimary: true }];
};

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

export default function AdminProducts() {
  const queryClient = useQueryClient();
  const [search, setSearch] = useState('');
  const [categoryFilter, setCategoryFilter] = useState('');
  const [showForm, setShowForm] = useState(false);
  const [editingProduct, setEditingProduct] = useState<Product | null>(null);
  const [loadingEdit, setLoadingEdit] = useState(false);
  const [pageError, setPageError] = useState('');
  const [formError, setFormError] = useState('');
  const [form, setForm] = useState(emptyProductForm);

  const { data: products, isLoading, error } = useQuery({
    queryKey: ['admin-products', search, categoryFilter],
    queryFn: () =>
      adminApi.getProducts({
        page: 1,
        pageSize: 100,
        search: search || undefined,
        categoryId: categoryFilter || undefined,
        sortBy: 'name',
      }),
  });

  const { data: categories } = useQuery({
    queryKey: ['admin-categories'],
    queryFn: adminApi.getCategories,
  });

  const invalidateCatalog = () => {
    void queryClient.invalidateQueries({ queryKey: ['admin-products'] });
    void queryClient.invalidateQueries({ queryKey: ['products'] });
    void queryClient.invalidateQueries({ queryKey: ['admin-categories'] });
    void queryClient.invalidateQueries({ queryKey: ['categories'] });
  };

  const deleteMutation = useMutation({
    mutationFn: (id: string) => adminApi.deleteProduct(id),
    onSuccess: invalidateCatalog,
    onError: (mutationError: unknown) => setPageError(getErrorMessage(mutationError)),
  });

  const saveMutation = useMutation({
    mutationFn: () => {
      const base = {
        name: form.name.trim(),
        description: form.description.trim(),
        slug: form.slug.trim(),
        price: Number(form.price),
        stock: Number(form.stock),
        brand: form.brand.trim() || undefined,
        sku: form.sku.trim() || undefined,
        isFeatured: form.isFeatured,
        isNewArrival: form.isNewArrival,
        categoryId: form.categoryId,
      };

      if (editingProduct) {
        const data: UpdateProductPayload = {
          ...base,
          isActive: editingProduct.isActive,
          images: buildEditImages(editingProduct.images, form.primaryImageUrl),
          variants: editingProduct.variants.map(toVariantPayload),
        };
        return adminApi.updateProduct(editingProduct.id, data);
      }

      const data: CreateProductPayload = {
        ...base,
        images: buildCreateImages(form.primaryImageUrl, form.name),
        variants: [],
      };
      return adminApi.createProduct(data);
    },
    onSuccess: () => {
      invalidateCatalog();
      closeForm();
    },
    onError: (mutationError: unknown) => setFormError(getErrorMessage(mutationError)),
  });

  const openCreate = () => {
    setEditingProduct(null);
    setForm(emptyProductForm);
    setFormError('');
    setPageError('');
    setShowForm(true);
  };

  const openEdit = async (item: ProductListItem) => {
    setLoadingEdit(true);
    setPageError('');
    try {
      const full = await adminApi.getProduct(item.id);
      setEditingProduct(full);
      setForm({
        name: full.name,
        slug: full.slug,
        description: full.description,
        categoryId: full.categoryId,
        price: String(full.price),
        stock: String(full.stock),
        brand: full.brand ?? '',
        sku: full.sku ?? '',
        isFeatured: full.isFeatured,
        isNewArrival: full.isNewArrival,
        primaryImageUrl: full.images.find((image) => image.isPrimary)?.imageUrl ?? full.images[0]?.imageUrl ?? '',
      });
      setFormError('');
      setShowForm(true);
    } catch (mutationError) {
      setPageError(getErrorMessage(mutationError));
    } finally {
      setLoadingEdit(false);
    }
  };

  const closeForm = () => {
    setShowForm(false);
    setEditingProduct(null);
    setForm(emptyProductForm);
    setFormError('');
  };

  const handleSubmit = (event: SubmitEvent<HTMLFormElement>) => {
    event.preventDefault();
    saveMutation.mutate();
  };

  const updateField = <K extends keyof typeof emptyProductForm>(key: K, value: (typeof emptyProductForm)[K]) => {
    setForm((current) => ({ ...current, [key]: value }));
  };

  let submitLabel = 'Create product';
  if (editingProduct) submitLabel = 'Save changes';
  if (saveMutation.isPending) submitLabel = 'Saving...';

  if (isLoading) return <div className="p-8 text-gray-600">Loading products...</div>;
  if (error) return <div className="p-8 text-red-600">Error loading products</div>;

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Products</h1>
          <p className="text-gray-600 mt-1">Create and manage the storefront catalog.</p>
        </div>
        <button
          onClick={openCreate}
          className="bg-purple-600 text-white px-4 py-2 rounded-lg text-sm font-semibold hover:bg-purple-700 transition"
        >
          Add Product
        </button>
      </div>

      {pageError && <div className="bg-red-50 border border-red-200 text-red-700 rounded-lg px-4 py-3 text-sm">{pageError}</div>}

      <div className="flex flex-wrap gap-3">
        <input
          type="text"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          placeholder="Search products..."
          className="flex-1 min-w-56 px-4 py-2 border border-gray-300 rounded-lg text-sm"
        />
        <select
          value={categoryFilter}
          onChange={(event) => setCategoryFilter(event.target.value)}
          className="px-4 py-2 border border-gray-300 rounded-lg text-sm bg-white"
        >
          <option value="">All categories</option>
          {categories?.map((category) => (
            <option key={category.id} value={category.id}>
              {category.name}
            </option>
          ))}
        </select>
      </div>

      {showForm && (
        <form onSubmit={handleSubmit} className="bg-white rounded-lg shadow-md p-6 space-y-5">
          <div className="flex items-center justify-between">
            <h2 className="text-xl font-semibold text-gray-900">{editingProduct ? 'Edit Product' : 'New Product'}</h2>
            <button type="button" onClick={closeForm} className="text-sm text-gray-500 hover:text-gray-800">
              Close
            </button>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label htmlFor="product-name" className="block text-sm font-medium text-gray-700 mb-1">Name</label>
              <input id="product-name" value={form.name} onChange={(event) => updateField('name', event.target.value)} className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" required />
            </div>
            <div>
              <label htmlFor="product-slug" className="block text-sm font-medium text-gray-700 mb-1">Slug</label>
              <div className="flex gap-2">
                <input id="product-slug" value={form.slug} onChange={(event) => updateField('slug', event.target.value)} className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" required />
                <button type="button" onClick={() => updateField('slug', slugify(form.name))} className="px-3 py-2 text-sm border border-gray-300 rounded-lg hover:bg-gray-50">Auto</button>
              </div>
            </div>
            <div>
              <label htmlFor="product-category" className="block text-sm font-medium text-gray-700 mb-1">Category</label>
              <select id="product-category" value={form.categoryId} onChange={(event) => updateField('categoryId', event.target.value)} className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm bg-white" required>
                <option value="">Select category</option>
                {categories?.map((category) => (
                  <option key={category.id} value={category.id}>{category.name}</option>
                ))}
              </select>
            </div>
            <div>
              <label htmlFor="product-price" className="block text-sm font-medium text-gray-700 mb-1">Price</label>
              <input id="product-price" type="number" min="0.01" step="0.01" value={form.price} onChange={(event) => updateField('price', event.target.value)} className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" required />
            </div>
            <div>
              <label htmlFor="product-stock" className="block text-sm font-medium text-gray-700 mb-1">Stock</label>
              <input id="product-stock" type="number" min="0" step="1" value={form.stock} onChange={(event) => updateField('stock', event.target.value)} className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" required />
            </div>
            <div>
              <label htmlFor="product-brand" className="block text-sm font-medium text-gray-700 mb-1">Brand</label>
              <input id="product-brand" value={form.brand} onChange={(event) => updateField('brand', event.target.value)} className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" />
            </div>
            <div>
              <label htmlFor="product-sku" className="block text-sm font-medium text-gray-700 mb-1">SKU</label>
              <input id="product-sku" value={form.sku} onChange={(event) => updateField('sku', event.target.value)} className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" />
            </div>
            <div>
              <label htmlFor="product-image" className="block text-sm font-medium text-gray-700 mb-1">Primary image URL</label>
              <input id="product-image" type="url" value={form.primaryImageUrl} onChange={(event) => updateField('primaryImageUrl', event.target.value)} className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" placeholder="https://..." />
            </div>
            <div className="md:col-span-2">
              <label htmlFor="product-description" className="block text-sm font-medium text-gray-700 mb-1">Description</label>
              <textarea id="product-description" value={form.description} onChange={(event) => updateField('description', event.target.value)} rows={4} className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" required />
            </div>
          </div>

          <div className="flex flex-wrap gap-6">
            <label className="flex items-center gap-2 text-sm text-gray-700">
              <input type="checkbox" checked={form.isFeatured} onChange={(event) => updateField('isFeatured', event.target.checked)} /> Featured
            </label>
            <label className="flex items-center gap-2 text-sm text-gray-700">
              <input type="checkbox" checked={form.isNewArrival} onChange={(event) => updateField('isNewArrival', event.target.checked)} /> New arrival
            </label>
          </div>

          {formError && <div className="bg-red-50 border border-red-200 text-red-700 rounded-lg px-4 py-3 text-sm">{formError}</div>}

          <div className="flex justify-end gap-3">
            <button type="button" onClick={closeForm} className="px-4 py-2 text-sm border border-gray-300 rounded-lg hover:bg-gray-50">Cancel</button>
            <button type="submit" disabled={saveMutation.isPending} className="bg-purple-600 text-white px-4 py-2 rounded-lg text-sm font-semibold hover:bg-purple-700 disabled:bg-gray-300">
              {submitLabel}
            </button>
          </div>
        </form>
      )}

      <div className="bg-white rounded-lg shadow-md overflow-hidden">
        <table className="min-w-full">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Product</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Category</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Price</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Stock</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Flags</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Actions</th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {products?.length === 0 && (
              <tr><td colSpan={6} className="px-6 py-10 text-center text-gray-500">No products found.</td></tr>
            )}
            {products?.map((product) => (
              <tr key={product.id}>
                <td className="px-6 py-4">
                  <div className="flex items-center gap-3">
                    {product.primaryImageUrl ? (
                      <img src={product.primaryImageUrl} alt={product.name} className="h-10 w-10 rounded object-cover" />
                    ) : (
                      <div className="h-10 w-10 rounded bg-gray-100" />
                    )}
                    <div>
                      <div className="text-sm font-medium text-gray-900">{product.name}</div>
                      <div className="text-xs text-gray-500">{product.slug}</div>
                    </div>
                  </div>
                </td>
                <td className="px-6 py-4 text-sm text-gray-700">{product.categoryName}</td>
                <td className="px-6 py-4 text-sm font-medium text-gray-900">{formatPrice(product.price)}</td>
                <td className="px-6 py-4 text-sm text-gray-700">{product.stock}</td>
                <td className="px-6 py-4 text-sm">
                  <div className="flex flex-wrap gap-1">
                    {product.isFeatured && <span className="px-2 py-0.5 rounded bg-purple-100 text-purple-700 text-xs">Featured</span>}
                    {product.isNewArrival && <span className="px-2 py-0.5 rounded bg-blue-100 text-blue-700 text-xs">New</span>}
                  </div>
                </td>
                <td className="px-6 py-4">
                  <div className="flex gap-2">
                    <button onClick={() => { void openEdit(product); }} disabled={loadingEdit} className="text-sm bg-blue-600 text-white px-3 py-1 rounded hover:bg-blue-700 disabled:bg-gray-300">Edit</button>
                    <button onClick={() => { if (window.confirm(`Delete ${product.name}?`)) deleteMutation.mutate(product.id); }} className="text-sm bg-red-600 text-white px-3 py-1 rounded hover:bg-red-700">Delete</button>
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
