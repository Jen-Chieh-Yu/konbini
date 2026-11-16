export const AuthApi = {
  login: '/api/auth/login',
  register: '/api/auth/register',
  me: '/api/auth/me',
  password: '/api/auth/password',
  emailVerifications: '/api/auth/email-verifications',
  verificationEmails: '/api/auth/verification-emails',
  passwordResetEmails: '/api/auth/password-reset-emails',
  passwordResets: '/api/auth/password-resets',
} as const
