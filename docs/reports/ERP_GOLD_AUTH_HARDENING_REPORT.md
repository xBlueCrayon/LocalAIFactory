# ERP Gold — Auth Hardening Report

**Sprint:** ERP-GOLD HARDENING · **Stamp:** 2026-06-21
**Blocker #3 (auth lacked lockout / password policy / anti-forgery / secure cookie): CLOSED**

## Controls delivered

| Control | Detail | Proving test |
|---------|--------|--------------|
| Failed-login lockout | 5 failed attempts → 15-minute lockout; correct password refused while locked | `Account_locks_after_max_failed_attempts` |
| Failure counter | Wrong password increments `FailedLoginCount` | `Wrong_password_increments_failure_count` |
| Reset on success | Successful login resets failure count + stamps `LastLoginUtc` | `Successful_login_resets_failure_count_and_stamps_last_login` |
| Password policy | Min 8 chars + upper + lower + digit + symbol | `Password_policy_enforces_complexity` (`[Theory]`, 6 cases) |
| Anti-forgery | `[ValidateAntiForgeryToken]` + `@Html.AntiForgeryToken()` | Playwright `auth-hardening.spec.ts` (token field present; tokenless POST rejected) |
| Hardened cookie | HttpOnly, SameSite=Lax, SecurePolicy=SameAsRequest, 30-min sliding expiry | Configured in `Program.cs` |
| Audited events | login / logout / failed-login / account-locked / password-reset | `Failed_login_is_audited`, `Lockout_is_audited`, `Logout_is_audited`, `Admin_password_reset_enforces_policy_and_rehashes` |
| No username enumeration | Generic failure response regardless of whether the user exists | (design; covered by smoke proof — wrongPassword not 302) |
| Admin SetPassword | Enforces policy + rehashes; sets `MustChangePassword` | `Admin_password_reset_enforces_policy_and_rehashes` |

## Test summary

`tests/LafErp.Tests/AuthHardeningTests.cs`: 7 `[Fact]` + 1 `[Theory]` (6 inline cases). Playwright `auth-hardening.spec.ts`: 3 tests (lockout, tokenless-POST rejected, token field rendered).

## Honest limitations

- **App-level authentication only.** No MFA, no SSO/OIDC (e.g. Entra), no Windows authentication yet — these are documented extension points and remain **external gates**.
- Password hashing is PBKDF2 (existing); no hardware-backed key store.
- Cookie `SecurePolicy=SameAsRequest` means HTTPS enforcement depends on the deployment terminating TLS (CA-signed TLS is an external gate).
