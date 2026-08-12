# MyMCP Usage Rules (Cross-Team / Cross-Organization)

**Repository:** `getdipakkumar2008-coder/creatingmcpcsharp`  
**Document:** `usageRules.md`  
**Version:** 1.0  
**Date:** August 12, 2026  
**Owner:** Platform Architecture

---

## 1. Purpose

This document defines **who can use MyMCP after deployment**, how developers and applications can onboard, and the security/governance controls required for use **within the team, across the organization, and by external partner organizations**.

---

## 2. Who Can Use This Application?

By default, after production deployment, **only explicitly approved identities** can use MyMCP.

## 2.1 Internal Team (same project team)
Allowed when:
- User is in approved Azure AD/Entra ID security group(s)
- User/application is granted correct RBAC role
- Network path is authorized (private VNet/VPN/corporate network)

## 2.2 Internal Organization (other teams in same company)
Allowed when:
- Request is approved by MyMCP service owner
- Calling app has managed identity/service principal
- Consumer team signs usage and data-handling agreement
- Traffic follows approved interface (private endpoint or API gateway)

## 2.3 External Organizations (partners/vendors)
Allowed only when:
- Formal business approval exists
- Security and legal review completed
- Access via controlled API gateway (no direct DB or private network access)
- Dedicated credentials/keys and quotas are assigned
- Auditing and revocation path are in place

---

## 3. Supported Consumer Types

1. **Human users (developers/ops)**
   - Access for testing, diagnostics, and controlled operations
2. **Internal applications/services**
   - Preferred: managed identity + private networking
3. **External applications**
   - Preferred: API Management + OAuth2/client credentials or signed API key model

---

## 4. Access Models

## 4.1 Private Internal Model (default)
- MyMCP is reachable only from internal Azure networks
- No public internet access
- Auth via Entra ID + managed identity
- Best for sensitive employee-data workloads

## 4.2 Controlled Cross-Org Model
- Expose through **Azure API Management** as facade
- Enforce:
  - OAuth2/JWT validation
  - rate limiting and quota
  - request/response logging
  - IP restrictions (if required)
- Separate products/plans for each org/team

## 4.3 Public/Open Model (not recommended for employee data)
- If ever required, must include:
  - strong authentication
  - abuse protection
  - legal terms and SLA
  - data minimization and masking

---

## 5. Mandatory Usage Policies

All consumers must follow these rules:

1. **Least Privilege Access**
   - Request only required scope/role
2. **No Shared Credentials**
   - Per-user/per-app identity only
3. **No Direct Database Access for Consumers**
   - Consumers call service APIs/tools only
4. **Data Protection**
   - Do not log sensitive employee fields in plaintext
5. **Auditability**
   - All access must be traceable to identity and application
6. **Rate Limits**
   - Consumers must respect platform quotas and retry policy
7. **Backward Compatibility**
   - Consumers should tolerate non-breaking schema/tool evolution
8. **Incident Reporting**
   - Suspected abuse or data leak reported immediately

---

## 6. Team/Organization Onboarding Process

## Step 1: Access Request
Consumer provides:
- Team/org name
- Business use case
- Data fields needed
- Expected request volume (RPS/day)
- Environment(s): dev/staging/prod

## Step 2: Architecture & Security Review
MyMCP owner validates:
- legitimacy of use case
- required access scope
- network/auth model
- compliance requirements

## Step 3: Provisioning
Platform team provisions:
- Entra ID group membership or app registration
- managed identity/service principal
- API Management subscription (if cross-org/external)
- quotas/rate limits and monitoring

## Step 4: Contract & Testing
- Share API/tool contract and usage examples
- Consumer completes integration tests in staging
- Observability checks and alert routing confirmed

## Step 5: Production Go-Live
- Approval sign-off
- Access enabled in production
- Usage monitored for first 2 weeks (hypercare)

---

## 7. Environment Access Matrix

| Environment | Internal Team | Other Internal Teams | External Orgs |
|-------------|---------------|----------------------|---------------|
| **Dev**     | Yes           | Case-by-case         | No            |
| **Staging** | Yes           | Yes (approved)       | Limited pilot |
| **Prod**    | Yes           | Yes (approved)       | Yes (only via APIM + legal/security approvals) |

---

## 8. Authentication & Authorization Standards

## 8.1 Preferred identity methods
1. Managed Identity (Azure-hosted internal apps)
2. Service Principal with certificate auth
3. OAuth2 client credentials (via APIM)

## 8.2 Authorization pattern
- Role-based access control (RBAC)
- Example roles:
  - `MyMCP.Reader` (query-only)
  - `MyMCP.Operator` (operational diagnostics)
  - `MyMCP.Admin` (platform control, limited membership)

## 8.3 Credential lifecycle
- Secret/key rotation every 90 days (or shorter per policy)
- Immediate revocation on team/offboarding events

---

## 9. Network & Connectivity Rules

1. Production access must originate from approved networks
2. Private endpoint is mandatory for SQL and Key Vault
3. Public ingress must be fronted by API gateway + WAF controls
4. Egress restrictions and DNS controls enforced via hub-spoke network model

---

## 10. Operational Usage Limits (Baseline)

- Default per-consumer throttle: defined in APIM product policy
- Burst and sustained limits set per business criticality
- Timeouts and retries must follow platform standards
- Consumers must implement exponential backoff for transient failures

---

## 11. Logging, Monitoring, and Compliance

- Log consumer identity, timestamp, endpoint/tool, status code
- Retain logs per organizational policy
- Alert on:
  - repeated auth failures
  - abnormal traffic spikes
  - data exfiltration indicators
- Periodic access review (monthly/quarterly)

---

## 12. Versioning & Change Management for Consumers

- Announce non-breaking changes in release notes
- Deprecation notice period (recommended: 60–90 days)
- Breaking changes require:
  - architecture review
  - migration guide
  - staged rollout and consumer validation

---

## 13. Offboarding / Access Revocation

Access is revoked when:
- project ends
- contract expires
- policy violation occurs
- inactive usage beyond retention threshold

Revocation actions:
- disable app registration/subscription
- remove RBAC/group membership
- rotate affected credentials
- confirm with audit trail

---

## 14. Consumer Quick Start (Internal)

1. Submit onboarding request
2. Get Entra ID group/identity assignment
3. Receive endpoint and auth details
4. Test in staging
5. Move to production after approval

---

## 15. Final Policy Statement

MyMCP is **not open by default**. Access is granted only to approved users and applications with least-privilege controls, audited usage, and compliance to security/network standards. Cross-organization use is supported only through governed onboarding and controlled API exposure.
