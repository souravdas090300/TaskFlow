import { LegalPage, LegalSection } from '../components/LegalPage';

const CONTACT_EMAIL = 'souravdas090300@gmail.com';

export const TermsOfService = () => (
  <LegalPage title="Terms of Service" updated="November 17, 2026">
    <LegalSection title="1. Agreement to Terms">
      <p>
        These Terms of Service govern your use of TaskFlow and any products or services offered
        through this website. By accessing or using TaskFlow, you agree to these terms. If you do not
        agree, do not use the service.
      </p>
    </LegalSection>

    <LegalSection title="2. Accounts">
      <p>
        You may need an account to place orders or access certain features. You are responsible for
        keeping your login credentials confidential and for activity that occurs under your account.
        You must provide accurate information and update it when needed.
      </p>
    </LegalSection>

    <LegalSection title="3. Orders and Pricing">
      <ul className="list-disc pl-5 space-y-2">
        <li>All prices are shown in the store currency and may change before checkout is completed.</li>
        <li>An order is accepted only after payment is confirmed or another payment method is approved.</li>
        <li>We may cancel orders because of stock issues, pricing errors, suspected fraud, or other valid reasons.</li>
        <li>You are responsible for reviewing your order details before submitting payment.</li>
      </ul>
    </LegalSection>

    <LegalSection title="4. Payments">
      <p>
        Payments are processed by Stripe or another payment provider shown at checkout. By paying, you
        agree to that provider's terms. We may require additional verification for high-risk orders.
      </p>
    </LegalSection>

    <LegalSection title="5. Shipping and Delivery">
      <p>
        Delivery estimates are provided for convenience and are not guaranteed. Risk of loss passes to
        you when the carrier delivers the order unless a different arrangement is stated at checkout.
      </p>
    </LegalSection>

    <LegalSection title="6. Returns and Refunds">
      <p>
        Returns, exchanges, and refunds are handled according to the policy shown at checkout or on
        the order page. Some items may be final sale. Refunds are issued to the original payment
        method where possible.
      </p>
    </LegalSection>

    <LegalSection title="7. Acceptable Use">
      <p>You agree not to:</p>
      <ul className="list-disc pl-5 space-y-2">
        <li>Use the service for unlawful, fraudulent, or harmful activity.</li>
        <li>Attempt to bypass security, payment, or access controls.</li>
        <li>Interfere with the operation of the website or other users' accounts.</li>
        <li>Copy, scrape, or misuse content or data from the service.</li>
      </ul>
    </LegalSection>

    <LegalSection title="8. Intellectual Property">
      <p>
        The TaskFlow name, logo, website design, and original content are owned by TaskFlow or its
        licensors. Product images and descriptions may belong to their respective manufacturers or
        suppliers.
      </p>
    </LegalSection>

    <LegalSection title="9. Disclaimers">
      <p>
        The service is provided "as is" and "as available". To the maximum extent permitted by law, we
        disclaim warranties of merchantability, fitness for a particular purpose, and non-infringement.
      </p>
    </LegalSection>

    <LegalSection title="10. Limitation of Liability">
      <p>
        To the maximum extent permitted by law, TaskFlow will not be liable for indirect, incidental,
        special, consequential, or punitive damages arising from your use of the service.
      </p>
    </LegalSection>

    <LegalSection title="11. Termination">
      <p>
        We may suspend or terminate access to the service if you violate these terms, create risk for
        the service or other users, or for any other lawful reason.
      </p>
    </LegalSection>

    <LegalSection title="12. Changes to These Terms">
      <p>
        We may update these Terms of Service from time to time. The "Last updated" date shows the most
        recent revision. Continued use of TaskFlow after changes are posted means you accept the
        updated terms.
      </p>
    </LegalSection>

    <LegalSection title="13. Contact Us">
      <p>
        Questions about these terms can be sent to{' '}
        <a href={`mailto:${CONTACT_EMAIL}`} className="text-purple-400 hover:text-purple-300">
          {CONTACT_EMAIL}
        </a>
        .
      </p>
    </LegalSection>
  </LegalPage>
);

export default TermsOfService;
