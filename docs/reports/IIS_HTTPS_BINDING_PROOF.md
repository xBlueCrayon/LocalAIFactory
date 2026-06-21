# IIS HTTPS Binding Proof (Phase 1)

**Date:** 2026-06-21 · **Site:** `LocalAIFactoryPilot` · **HTTPS:** `https://localhost:8443`

`scripts/deployment-drill/13-iis-https-binding-proof.ps1 -Execute` added an HTTPS binding to the IIS pilot
site using a **self-signed localhost** certificate.

| Item | Value |
|---|---|
| Certificate | self-signed, `CN=localhost`, `Cert:\LocalMachine\My` (thumbprint `F8FB749F…`) |
| Binding | `https/*:8443:` on `LocalAIFactoryPilot` (HTTP `:8095` left intact) |
| SSL cert association | `netsh http add sslcert ipport=0.0.0.0:8443 certhash=… appid={9f0e0da5-…}` |

## HTTPS probe (served through IIS over TLS)

| Route | Status | Server header |
|---|---:|---|
| `https://localhost:8443/` | **200** | `Microsoft-IIS/10.0` |
| `/Support` | **200** | `Microsoft-IIS/10.0` |
| `/Readiness` | **200** | `Microsoft-IIS/10.0` |
| `/BaseKnowledge` | **200** | `Microsoft-IIS/10.0` |
| `/BaseKnowledge?q=OCR` | **200** | 57 matches |

The app is served **over TLS through IIS**. Probes use `-SkipCertificateCheck` because the cert is
self-signed.

## Honest limitations

- **Self-signed localhost certificate = LOCAL PILOT TLS only.** This is **not** production TLS and makes
  **no** CA-trust claim. A production deployment needs a CA-issued certificate (internal PKI or public CA)
  and a real DNS name, plus HSTS / TLS-version / cipher hardening.
- This proves the **HTTPS hosting path through IIS works** (binding, cert association, TLS handshake,
  app served over TLS) — the certificate trust chain is the remaining production step.

## Rollback

```powershell
appcmd set site LocalAIFactoryPilot /-bindings.[protocol='https',bindingInformation='*:8443:']
netsh http delete sslcert ipport=0.0.0.0:8443
# remove the cert from Cert:\LocalMachine\My if desired
```
