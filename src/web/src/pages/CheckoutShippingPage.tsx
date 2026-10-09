import { useState, type FormEvent } from 'react';
import { Navigate, useNavigate } from 'react-router';
import { useQuery } from '@tanstack/react-query';
import { shippingApi } from '../api/api';
import { ApiError } from '../api/http';
import type { Address, ShippingMethod, ShippingQuote } from '../api/types';
import { CheckoutSteps } from '../components/CheckoutSteps';
import { ErrorMessage, Field, Notice, Spinner } from '../components/common';
import { useBasket } from '../context/BasketContext';
import { loadCheckout, saveCheckout, type CustomerDetails } from '../context/checkoutStorage';
import { formatDate, formatPrice } from '../format';

type Errors = Record<string, string[]>;

const EMAIL_PATTERN = /^[^@\s]+@[^@\s]+\.[^@\s]+$/;

/** Quick checks in the browser. The services check everything again. */
function validateCustomer(customer: CustomerDetails): Errors {
  const errors: Errors = {};
  if (!customer.fullName.trim()) errors.fullName = ['Full name is required.'];
  else if (!customer.fullName.trim().includes(' ')) errors.fullName = ['Please enter your first and last name.'];
  if (!customer.email.trim()) errors.email = ['Email address is required.'];
  else if (!EMAIL_PATTERN.test(customer.email.trim())) errors.email = ['Email address is not valid.'];
  if (!customer.phone.trim()) errors.phone = ['Phone number is required.'];
  return errors;
}

export function CheckoutShippingPage() {
  const { basket, isLoading } = useBasket();
  const navigate = useNavigate();
  const countries = useQuery({ queryKey: ['countries'], queryFn: shippingApi.getCountries });

  const saved = loadCheckout();
  const [customer, setCustomer] = useState<CustomerDetails>(saved?.customer ?? { fullName: '', email: '', phone: '' });
  const [address, setAddress] = useState<Address>(
    saved?.address ?? { line1: '', line2: '', city: '', region: '', postalCode: '', country: 'US' },
  );
  const [errors, setErrors] = useState<Errors>({});
  const [quote, setQuote] = useState<ShippingQuote | null>(null);
  const [method, setMethod] = useState<ShippingMethod | null>(saved?.shippingMethod ?? null);
  const [quoteError, setQuoteError] = useState<unknown>(null);
  const [calculating, setCalculating] = useState(false);

  if (isLoading || !basket) return <Spinner />;
  if (!basket.canCheckout) return <Navigate to="/basket" replace />;

  const country = countries.data?.find((c) => c.code === address.country);

  const changeCustomer = (field: keyof CustomerDetails, value: string) => setCustomer({ ...customer, [field]: value });
  const changeAddress = (field: keyof Address, value: string) => {
    // A new address needs a new shipping quote.
    setQuote(null);
    setMethod(null);
    setAddress({ ...address, [field]: value, ...(field === 'country' ? { region: '' } : {}) });
  };

  const calculateShipping = async (event: FormEvent) => {
    event.preventDefault();
    setQuoteError(null);
    const customerErrors = validateCustomer(customer);
    setErrors(customerErrors);
    if (Object.keys(customerErrors).length > 0) return;

    setCalculating(true);
    try {
      const created = await shippingApi.createQuote({
        address,
        itemCount: basket.itemCount,
        orderSubtotal: basket.total,
        shipNotBefore: basket.latestReleaseDate,
      });
      setQuote(created);
      setMethod(created.options[0]?.method ?? null);
    } catch (e) {
      if (e instanceof ApiError && e.status === 400) {
        const fieldErrors: Errors = {};
        for (const [key, messages] of Object.entries(e.fieldErrors)) {
          fieldErrors[key.replace(/^address\./, '')] = messages;
        }
        setErrors(fieldErrors);
      } else {
        setQuoteError(e);
      }
    } finally {
      setCalculating(false);
    }
  };

  const continueToPayment = () => {
    if (!quote || !method) return;
    saveCheckout({ basketId: basket.id, customer, address: quote.address, quoteId: quote.id, shippingMethod: method });
    navigate('/checkout/payment');
  };

  return (
    <div className="checkout">
      <CheckoutSteps current={1} />
      <h1>Shipping details</h1>
      <form className="checkout-form" onSubmit={calculateShipping} noValidate data-testid="shipping-form">
        <fieldset>
          <legend>Contact</legend>
          <Field id="fullName" label="Full name" error={errors.fullName}>
            <input id="fullName" autoComplete="name" value={customer.fullName}
              onChange={(e) => changeCustomer('fullName', e.target.value)} />
          </Field>
          <div className="field-row">
            <Field id="email" label="Email address" error={errors.email}>
              <input id="email" type="email" autoComplete="email" value={customer.email}
                onChange={(e) => changeCustomer('email', e.target.value)} />
            </Field>
            <Field id="phone" label="Phone number" error={errors.phone} hint="US or Canadian number, e.g. (212) 555-0123">
              <input id="phone" type="tel" autoComplete="tel" value={customer.phone}
                onChange={(e) => changeCustomer('phone', e.target.value)} />
            </Field>
          </div>
        </fieldset>

        <fieldset>
          <legend>Shipping address</legend>
          <Field id="country" label="Country" error={errors.country}>
            <select id="country" value={address.country} onChange={(e) => changeAddress('country', e.target.value)}>
              {countries.data?.map((c) => <option key={c.code} value={c.code}>{c.name}</option>)}
            </select>
          </Field>
          <Field id="line1" label="Address line 1" error={errors.line1}>
            <input id="line1" autoComplete="address-line1" value={address.line1} onChange={(e) => changeAddress('line1', e.target.value)} />
          </Field>
          <Field id="line2" label="Address line 2 (optional)" error={errors.line2}>
            <input id="line2" autoComplete="address-line2" value={address.line2 ?? ''} onChange={(e) => changeAddress('line2', e.target.value)} />
          </Field>
          <div className="field-row">
            <Field id="city" label="City" error={errors.city}>
              <input id="city" autoComplete="address-level2" value={address.city} onChange={(e) => changeAddress('city', e.target.value)} />
            </Field>
            <Field id="region" label={country?.regionLabel ?? 'State'} error={errors.region}>
              <select id="region" value={address.region} onChange={(e) => changeAddress('region', e.target.value)}>
                <option value="">Select...</option>
                {country?.regions.map((r) => <option key={r.code} value={r.code}>{r.name}</option>)}
              </select>
            </Field>
            <Field id="postalCode" label={country?.postalCodeLabel ?? 'ZIP code'} error={errors.postalCode}
              hint={country ? `e.g. ${country.postalCodeExample}` : undefined}>
              <input id="postalCode" autoComplete="postal-code" value={address.postalCode}
                onChange={(e) => changeAddress('postalCode', e.target.value)} />
            </Field>
          </div>
        </fieldset>

        {!quote && (
          <button type="submit" className="button button-primary" disabled={calculating} data-testid="calculate-shipping">
            {calculating ? 'Calculating...' : 'Calculate shipping'}
          </button>
        )}
        <ErrorMessage error={quoteError} testId="shipping-error" />
      </form>

      {quote && (
        <section className="shipping-options" data-testid="shipping-options">
          <h2>Delivery</h2>
          {quote.notices.map((notice) => <Notice key={notice} testId="shipping-notice">{notice}</Notice>)}
          <div role="radiogroup" aria-label="Shipping method">
            {quote.options.map((option) => (
              <label key={option.method} className={`option-card ${method === option.method ? 'option-selected' : ''}`}
                data-testid={`shipping-option-${option.method}`}>
                <input type="radio" name="shippingMethod" value={option.method} checked={method === option.method}
                  onChange={() => setMethod(option.method)} />
                <span className="option-name">{option.method}</span>
                <span className="option-eta" data-testid="delivery-estimate">Arrives {formatDate(option.estimatedDeliveryDate)}</span>
                <span className="option-cost" data-testid="shipping-cost">{option.isFree ? 'FREE' : formatPrice(option.cost)}</span>
              </label>
            ))}
          </div>
          <div className="button-row">
            <button type="button" className="button button-secondary" onClick={() => setQuote(null)} data-testid="edit-address">
              Edit address
            </button>
            <button type="button" className="button button-primary" disabled={!method} onClick={continueToPayment}
              data-testid="continue-to-payment">
              Continue to payment
            </button>
          </div>
        </section>
      )}
    </div>
  );
}
