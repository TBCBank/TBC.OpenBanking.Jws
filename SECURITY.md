# Security policy

## Scope and limitations

This repository implements detached JWS signing and verification for HTTP messages. It is not a complete Open Banking service and does not provide OAuth2/OIDC, consent management, transaction authorization, replay protection, rate limiting, durable audit storage, or a partner-specific certificate allowlist.

The SDK contains no fixed bank endpoint, forced proxy, or automatic data-forwarding destination. The example clients use the HTTPS endpoint supplied by the operator and disable automatic HTTP redirect following so a signed request is not silently resent to another location. Applications integrating the handler should likewise configure their primary `HttpClientHandler` with `AllowAutoRedirect = false` if redirects are not explicitly part of their protocol. The consent example's `TPP-Redirect-URI` is a value sent in the request header to the configured provider; it is not an instruction for this SDK to navigate or forward the request to that URI.

The verifier builds a certificate chain using the platform's configured trust. A certificate included in a JWS `x5c` header is untrusted input until chain validation and the integration's partner identity policy have both succeeded. Configure the host trust store and apply the network's required trust-anchor, certificate-purpose, and organization-identity rules before accepting traffic. A valid signature proves possession of a key; it does not by itself authorize a payment or consent.

Audit events are metadata-only and are delivered through `IAuditSink`. The application must supply a durable, access-controlled, tamper-evident sink and monitor failures. The sink is synchronous and its exceptions propagate. Do not put request bodies, authorization values, personal data, private keys, or exception messages in audit records.

## Supported algorithms

The library supports the RSA PKCS#1, RSA-PSS, and ECDSA JWS algorithm families exposed by `SupportedAlgorithms` (`RS256/384/512`, `PS256/384/512`, and `ES256/384/512`). Configure an explicit allowlist appropriate to the partner profile; do not select an algorithm from untrusted input without enforcing policy.

## Key and certificate handling

- Never commit private keys, PKCS#12 files, passwords, tokens, or production certificates with private keys.
- Load private keys from a protected operating-system store or managed key provider. For hardware-backed or remote signing, provide an `ISigner` implementation and protect the private key outside this repository.
- Restrict filesystem permissions and access to signing credentials; rotate keys before expiry and have a tested revocation/replacement procedure.
- Keep TLS certificate validation enabled. HTTPS for an endpoint does not replace JWS signature validation, and JWS does not replace TLS.
- Do not enable verbose logging that captures signed payloads, headers, tokens, certificate details, or customer data. Audit records should contain only the metadata documented by `AuditEvent`.

## Reporting a vulnerability

Please do not publish credentials or exploitable details in a public issue. If private vulnerability reporting is enabled for this repository, use the repository's **Security** tab to submit a report. Otherwise, contact the maintainers through a private channel and include reproduction steps, affected versions, and impact without sending real credentials or customer data.

## Deployment checklist

Before production use, review the bank/network's current signing profile, trust anchors, certificate purpose and identity rules, revocation behavior, timestamp tolerances, protected HTTP headers, and key-storage requirements. Add integration tests for partner certificates, certificate rotation and revocation, malformed or replayed requests, audit-store outages, and end-to-end failure handling.
