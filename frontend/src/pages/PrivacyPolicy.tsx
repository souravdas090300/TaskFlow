import { LegalPage, LegalSection } from '../components/LegalPage';

const CONTACT_EMAIL = 'souravdas090300@gmail.com';

export const PrivacyPolicy = () => (
  <LegalPage title="Privacy Policy" updated="November 17, 2026">
    <LegalSection title="1. Overview">
      <p>
        TaskFlow ("we", "our", or "us") operates this online store at
        https://taskflow-das.duckdns.org. This Privacy Policy explains what information we collect,
        why we collect it, how we use and share it, how long we keep it, and the choices you have.
      </p>
      <p>
        By using TaskFlow, you agree to the practices described in this policy. If you do not agree,
        please do not use the service.
      </p>
    </LegalSection>

    <LegalSection title="2. Information We Collect">
      <p>We collect information in the following ways:</p>

      <p className="font-semibold text-white">Information you provide directly</p>
      <ul className="list-disc pl-5 space-y-2">
        <li>Account information: name, email address, password, phone number, and profile details.</li>
        <li>Order information: products purchased, order history, billing/shipping address, and delivery notes.</li>
        <li>Payment information: payment is processed by Stripe; we receive payment status and limited transaction details, but we do not store full card numbers.</li>
        <li>Communications: messages you send to support, reviews, feedback, and survey responses.</li>
        <li>Marketing preferences: if you opt in, we may store your email preferences.</li>
      </ul>

      <p className="font-semibold text-white">Information collected automatically</p>
      <ul className="list-disc pl-5 space-y-2">
        <li>Technical data such as IP address, browser type, device type, operating system, and referring URL.</li>
        <li>Usage data such as pages visited, products viewed, items added to cart, timestamps, and approximate location derived from IP address.</li>
        <li>Cookies and local storage used to keep you signed in, remember your cart, and maintain security preferences.</li>
        <li>Error and diagnostic logs used to troubleshoot and secure the service.</li>
      </ul>

      <p className="font-semibold text-white">Information from third parties</p>
      <ul className="list-disc pl-5 space-y-2">
        <li>If you sign in with Google, Google provides basic profile data such as name, email, profile picture, and account identifier.</li>
        <li>Payment processors may provide transaction status or fraud signals.</li>
        <li>Shipping or fulfillment partners may provide delivery status updates.</li>
      </ul>
    </LegalSection>

    <LegalSection title="3. How We Use Information">
      <p>We use personal information to:</p>
      <ul className="list-disc pl-5 space-y-2">
        <li>Create, maintain, and secure your account.</li>
        <li>Process orders, payments, shipping, refunds, and returns.</li>
        <li>Send order confirmations, shipping updates, receipts, and support replies.</li>
        <li>Send service messages such as password resets, email verification, and security alerts.</li>
        <li>Prevent fraud, abuse, unauthorized access, and other harmful activity.</li>
        <li>Debug errors, improve performance, and understand how the service is used.</li>
        <li>Comply with legal obligations and enforce our terms.</li>
        <li>Send optional marketing emails only if you have opted in.</li>
      </ul>
    </LegalSection>

    <LegalSection title="4. Legal Bases for Processing">
      <p>Where applicable law requires a legal basis, we rely on:</p>
      <ul className="list-disc pl-5 space-y-2">
        <li>Performance of a contract when processing is needed to provide your order or account.</li>
        <li>Consent when you opt in to marketing or optional features.</li>
        <li>Legitimate interests for security, fraud prevention, debugging, and service improvement.</li>
        <li>Legal obligation when we must retain or disclose information by law.</li>
      </ul>
    </LegalSection>

    <LegalSection title="5. Google Sign-In and Google User Data">
      <p>
        If you choose to sign in with Google, Google will provide us with basic profile information such as
        your name, email address, profile picture, and Google account identifier. We use this
        information only to create or access your TaskFlow account and provide the requested service.
      </p>
      <p>We request only the basic Google scopes needed for sign-in: openid, email, and profile.</p>
      <p>
        Our use of information received from Google APIs adheres to the Google API Services User Data
        Policy, including the{' '}
        <a
          href="https://developers.google.com/terms/api-services-user-data-policy"
          className="text-purple-400 hover:text-purple-300"
          target="_blank"
          rel="noreferrer"
        >
          Limited Use
        </a>{' '}
        requirements. Specifically:
      </p>
      <ul className="list-disc pl-5 space-y-2">
        <li>We use Google user data only to authenticate you and provide TaskFlow features you request.</li>
        <li>We do not sell Google user data or use it for advertising.</li>
        <li>We do not transfer Google user data to third parties except as necessary to provide the service, with your consent, for security or legal reasons, or as part of a merger, acquisition, or sale of assets.</li>
        <li>We do not use Google user data to develop, improve, or train generalized artificial intelligence or machine learning models.</li>
      </ul>
      <p>
        We do not receive your Google password. Google sign-in is optional; you can also register with
        an email address and password.
      </p>
    </LegalSection>

    <LegalSection title="6. How We Share Information">
      <p>We may share personal information with:</p>
      <ul className="list-disc pl-5 space-y-2">
        <li>Payment processors such as Stripe to complete transactions and prevent fraud.</li>
        <li>Email delivery providers such as Resend to send transactional and service emails.</li>
        <li>Hosting and infrastructure providers that operate the website and databases.</li>
        <li>Shipping or fulfillment partners when needed to deliver your order.</li>
        <li>Professional advisers such as accountants or lawyers when necessary.</li>
        <li>Legal authorities or other parties when required by law, court order, or to protect rights and safety.</li>
        <li>A successor entity in connection with a merger, acquisition, financing, or sale of assets.</li>
      </ul>
      <p>
        We may also share aggregated or anonymized data that cannot reasonably identify you. We do not
        sell personal information.
      </p>
    </LegalSection>

    <LegalSection title="7. Cookies and Local Storage">
      <p>
        We use cookies and browser local storage to keep you signed in, remember your cart, store
        security tokens, and remember preferences. You can clear or block cookies through your
        browser settings, but some features may stop working.
      </p>
    </LegalSection>

    <LegalSection title="8. Data Retention">
      <p>
        We keep personal information only as long as needed for the purposes described in this policy
        or as required by law. Typical retention periods include:
      </p>
      <ul className="list-disc pl-5 space-y-2">
        <li>Account data is kept while your account is active and for a limited period after deletion.</li>
        <li>Order and transaction records may be kept longer to meet tax, accounting, fraud-prevention, and legal obligations.</li>
        <li>Security and diagnostic logs are kept for a limited period and then deleted or anonymized.</li>
        <li>Email verification, password reset, and sign-in tokens expire automatically.</li>
      </ul>
    </LegalSection>

    <LegalSection title="9. Data Security">
      <p>
        We use reasonable administrative, technical, and physical safeguards designed to protect
        personal information, including encrypted connections, access controls, and secure token
        handling. No method of transmission or storage is completely secure, so we cannot guarantee
        absolute security.
      </p>
    </LegalSection>

    <LegalSection title="10. Your Rights and Choices">
      <p>Depending on where you live, you may have rights to:</p>
      <ul className="list-disc pl-5 space-y-2">
        <li>Access the personal information we hold about you.</li>
        <li>Correct inaccurate or incomplete information.</li>
        <li>Delete your account or request deletion of personal information.</li>
        <li>Receive a copy of your data in a portable format.</li>
        <li>Object to or restrict certain processing.</li>
        <li>Withdraw consent for optional processing such as marketing emails.</li>
      </ul>
      <p>
        To exercise these rights, contact us using the details below. We may need to verify your
        identity before responding.
      </p>
    </LegalSection>

    <LegalSection title="11. International Data Transfers">
      <p>
        TaskFlow may be hosted or accessed from countries other than your own. By using the service,
        you understand that your information may be transferred to and processed in those locations,
        subject to applicable safeguards.
      </p>
    </LegalSection>

    <LegalSection title="12. Children's Privacy">
      <p>
        TaskFlow is not directed to children under 13, and we do not knowingly collect personal
        information from children under 13. If we learn that we have collected such information, we
        will delete it.
      </p>
    </LegalSection>

    <LegalSection title="13. Changes to This Policy">
      <p>
        We may update this Privacy Policy from time to time. The "Last updated" date shows the most
        recent revision. If changes are material, we will provide additional notice where appropriate.
        Continued use of TaskFlow after changes are posted means you accept the updated policy.
      </p>
    </LegalSection>

    <LegalSection title="14. Contact Us">
      <p>
        Questions about this policy or your data can be sent to{' '}
        <a href={`mailto:${CONTACT_EMAIL}`} className="text-purple-400 hover:text-purple-300">
          {CONTACT_EMAIL}
        </a>
        .
      </p>
    </LegalSection>
  </LegalPage>
);

export default PrivacyPolicy;
