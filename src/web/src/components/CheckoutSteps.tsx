const STEPS = ['Basket', 'Shipping', 'Payment', 'Confirmation'];

export function CheckoutSteps({ current }: { current: number }) {
  return (
    <ol className="checkout-steps" aria-label="Checkout progress" data-testid="checkout-steps">
      {STEPS.map((step, index) => (
        <li key={step} className={index < current ? 'done' : index === current ? 'current' : ''}
          aria-current={index === current ? 'step' : undefined}>
          <span className="step-number">{index + 1}</span> {step}
        </li>
      ))}
    </ol>
  );
}
