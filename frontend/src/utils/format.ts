const eur = new Intl.NumberFormat('en-IE', {
  style: 'currency',
  currency: 'EUR',
});

export const formatPrice = (value: number) => eur.format(value);

export const FREE_SHIPPING_THRESHOLD = 50;
