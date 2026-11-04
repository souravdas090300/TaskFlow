# Google Sign In/Sign Up Integration Guide

**Status:** ✅ Infrastructure Ready
**Database:** ✅ Users table created
**Next Steps:** Configure Google OAuth credentials

---

## Overview

Google OAuth integration has been set up in the TaskFlow API. The system will:
- Allow users to sign in with their Google account
- Automatically create/update user records in the database
- Maintain session via cookies
- Support both sign in and sign up through the same flow

---

## Prerequisites

- Google account with Google Cloud access
- TaskFlow application running (currently running on Docker)

---

## Step 1: Create Google Cloud Project

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Click the project dropdown and select "New Project"
3. Enter project name: `TaskFlow` (or your preferred name)
4. Click "Create"
5. Wait for the project to be created, then select it

---

## Step 2: Configure OAuth Consent Screen

1. In the Google Cloud Console, go to **APIs & Services** > **OAuth consent screen**
2. Choose user type: **External** (for public-facing apps)
3. Click "Create"
4. Fill in the required information:
   - **App name**: TaskFlow
   - **User support email**: Your email
   - **Developer contact information**: Your email
5. Click "Save and Continue" (skip optional fields for now)
6. Click "Save and Continue" on Scopes page (can add later)
7. Click "Save and Continue" on Test users page
8. Click "Save and Continue" on Summary page
9. Wait for verification (you can proceed with test mode immediately)

---

## Step 3: Create OAuth 2.0 Credentials

1. Go to **APIs & Services** > **Credentials**
2. Click **+ Create Credentials** > **OAuth client ID**
3. Select application type: **Web application**
4. Enter name: `TaskFlow Web Client`
5. Configure **Authorized redirect URIs**:
   - For local development: `http://localhost:5000/signin-google`
   - For Docker: `http://localhost:5000/signin-google`
   - For production: `https://yourdomain.com/signin-google`
   - For production with custom port: `https://yourdomain.com:port/signin-google`
6. Click **Create**
7. Copy the **Client ID** and **Client Secret** (you'll need these)
8. Click **OK**

---

## Step 4: Add Credentials to TaskFlow

1. Open the `.env` file in the TaskFlow root directory
2. Replace the placeholder values with your Google credentials:

```env
# Google OAuth Configuration
GOOGLE_CLIENT_ID=your-actual-client-id-here.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=your-actual-client-secret-here
```

**Example:**
```env
GOOGLE_CLIENT_ID=123456789-abcdefghijklmnop.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=GOCSPX-abcdefghijklmnopqrstuvwxyz123456
```

3. Save the file

---

## Step 5: Restart the Application

Since the credentials are loaded from environment variables, restart the containers:

```bash
docker-compose down
docker-compose up -d
```

---

## Step 6: Test the Integration

### 6.1. Test Sign In

1. Open your browser and navigate to:
   ```
   http://localhost:5000/login
   ```

2. You should be redirected to Google's sign-in page

3. Sign in with your Google account

4. Authorize the application (if prompted)

5. You'll be redirected back to the application

### 6.2. Check Authentication Status

After signing in, check if you're authenticated:

```bash
curl http://localhost:5000/auth/user
```

Expected response (authenticated):
```json
{
  "isAuthenticated": true,
  "name": "Your Name",
  "email": "your.email@gmail.com",
  "claims": [...]
}
```

### 6.3. Test Sign Out

Navigate to:
```
http://localhost:5000/logout
```

This will clear the authentication cookie and redirect you.

---

## How It Works

### Authentication Flow

1. **User clicks "Sign in with Google"** → Redirects to `/login`
2. **API redirects to Google OAuth** → User authenticates with Google
3. **Google redirects back** → `/signin-google` with authorization code
4. **API exchanges code for tokens** → Gets user information from Google
5. **API creates/updates user in database** → Stores Google ID, email, name, picture
6. **API sets authentication cookie** → Session established
7. **User is redirected to return URL** → Logged in state

### User Entity

The User entity stores:
- `Id`: Unique identifier (GUID)
- `GoogleId`: Google's unique user ID (for identity matching)
- `Email`: User's email address (unique)
- `Name`: User's display name
- `PictureUrl`: Profile picture URL (optional)
- `CreatedAt`: Account creation timestamp
- `LastLoginAt`: Last login timestamp
- `IsActive`: Account status flag

### Database Table

The Users table has been created with:
- Primary key on `Id`
- Unique index on `GoogleId` (prevents duplicate accounts)
- Unique index on `Email` (prevents duplicate emails)
- Default values for timestamps and active status

---

## API Endpoints

### Authentication Endpoints

| Endpoint | Method | Description | Auth Required |
|----------|--------|-------------|---------------|
| `/login` | GET | Initiates Google sign-in | No |
| `/logout` | GET | Signs out current user | No |
| `/auth/user` | GET | Gets current user info | Yes |

### Example Usage

#### Sign In
```bash
# Browser
http://localhost:5000/login?returnUrl=/dashboard
```

#### Sign Out
```bash
# Browser
http://localhost:5000/logout?returnUrl=/
```

#### Get Current User
```bash
curl http://localhost:5000/auth/user \
  -H "Cookie: .AspNetCore.Cookies=your-cookie-value"
```

---

## Configuration Details

### appsettings.json (Local Development)

```json
{
  "Authentication": {
    "Google": {
      "ClientId": "YOUR_GOOGLE_CLIENT_ID",
      "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET"
    }
  }
}
```

### Environment Variables (Docker/Production)

Set via `.env` file or environment:
```env
Authentication__Google__ClientId=your-client-id
Authentication__Google__ClientSecret=your-client-secret
```

### Production Considerations

1. **Never commit credentials** to version control
2. **Use different credentials** for development and production
3. **Set up proper redirect URIs** for production domain
4. **Enable HTTPS** in production (Google requires HTTPS for OAuth)
5. **Configure domain verification** in Google Console

---

## Troubleshooting

### Issue: "Google ClientId and ClientSecret must be configured"

**Cause:** Missing or invalid credentials in configuration

**Solution:**
1. Verify `.env` file exists and is properly formatted
2. Check that `GOOGLE_CLIENT_ID` and `GOOGLE_CLIENT_SECRET` are set
3. Restart containers after updating `.env`

### Issue: "redirect_uri_mismatch" error from Google

**Cause:** Redirect URI not configured in Google Console

**Solution:**
1. Go to Google Console > Credentials
2. Edit your OAuth 2.0 client ID
3. Add your redirect URI to Authorized redirect URIs
4. Ensure exact match (including trailing slash if present)

### Issue: "access_denied" error

**Cause:** User denied authorization or OAuth consent screen not configured

**Solution:**
1. Verify OAuth consent screen is configured
2. Check if you're using a test user (required for external apps in test mode)
3. Add your email as a test user in the OAuth consent screen

### Issue: Database not creating user on first login

**Cause:** Database migration not applied or connection issue

**Solution:**
1. Check database logs: `docker-compose logs db`
2. Verify migration was applied: Check API logs for "Applying migration"
3. Test database connection: `curl http://localhost:5000/db-health`

---

## Security Best Practices

1. **Store secrets securely**: Use environment variables or secret management
2. **Enable HTTPS**: Required for production OAuth
3. **Validate email**: Google provides verified emails
4. **Use short-lived sessions**: Configure cookie expiration
5. **Implement CSRF protection**: Already handled by ASP.NET Core
6. **Rate limiting**: Prevent brute force attacks on login
7. **Log authentication events**: Monitor for suspicious activity

---

## Next Steps

### Frontend Integration

To integrate with the React frontend:

1. **Add Sign In Button** in React:
```tsx
<button onClick={() => window.location.href = '/api/login'}>
  Sign in with Google
</button>
```

2. **Check Authentication Status**:
```tsx
fetch('/api/auth/user')
  .then(res => res.json())
  .then(data => {
    if (data.isAuthenticated) {
      // User is logged in
    }
  });
```

3. **Handle Sign Out**:
```tsx
const handleLogout = () => {
  window.location.href = '/api/logout';
};
```

### Additional Features to Add

1. **User Profile Management** - Allow users to update their profile
2. **Role-Based Access** - Add admin/user roles
3. **Email Verification** - Optional, since Google emails are verified
4. **Multi-Factor Authentication** - For enhanced security
5. **Session Management** - Configure cookie policies
6. **User Impersonation** - For admin support

---

## Testing Checklist

- [ ] Google Cloud project created
- [ ] OAuth consent screen configured
- [ ] OAuth 2.0 credentials created
- [ ] Client ID and Client Secret added to `.env`
- [ ] Application restarted with new credentials
- [ ] Sign in flow works (redirects to Google)
- [ ] User can sign in successfully
- [ ] User record created in database
- [ ] `/auth/user` returns correct user info
- [ ] Sign out flow works
- [ ] Cookie is cleared after sign out

---

## Production Deployment Notes

### Google Console Configuration for Production

1. **Add production domain** to Authorized redirect URIs:
   - `https://yourdomain.com/signin-google`
   - `https://yourdomain.com/api/signin-google` (if using /api prefix)

2. **Complete OAuth verification**:
   - Provide privacy policy URL
   - Provide terms of service URL
   - Add your domain as authorized domain

3. **Enable HTTPS**:
   - SSL/TLS certificate required
   - Configure in Nginx (see DEPLOYMENT_DOCKER.md)

### Environment Variables

For production, set environment variables in your hosting platform:
- Azure App Service: Application Settings
- Docker/Kubernetes: Secrets or ConfigMaps
- VPS: Environment file with restricted permissions

---

**Status:** Google OAuth infrastructure complete. Ready for credential configuration.
