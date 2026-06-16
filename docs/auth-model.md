# Authentication & Refresh Token Model

JWT access tokens + **rotating** refresh tokens. Designed so a leaked refresh token has a
small blast radius and access tokens stay short-lived.

## Where the code lives

| Concern | Location |
|---|---|
| Entities (`User`, `RefreshToken`, `Role`) | `Logistics.Domain/Identity` |
| Use cases (Register / Login / Refresh / Revoke) | `Logistics.Application/Identity/Commands/*` |
| Interfaces (`IPasswordHasher`, `IJwtTokenGenerator`, `ISecureTokenGenerator`, repos) | `Logistics.Application/Identity` |
| Implementations (PBKDF2 hasher, JWT generator, token generator) | `Logistics.Infrastructure/Identity` |
| Neo4j repos + Cypher | `Logistics.Infrastructure/Persistence/Neo4j/{Repositories,Cypher}` |
| HTTP endpoints | `Logistics.Api/Controllers/AuthController.cs` |
| JWT bearer wiring | `Logistics.Api/Extensions/AuthenticationExtensions.cs` |

## Token strategy

- **Access token** — JWT, HMAC-SHA256, 15 min (configurable). Carries `sub`, `email`,
  `role`, `jti`. Stateless: validated by signature, never looked up in the DB.
- **Refresh token** — 256-bit random string. Only its **SHA-256 hash** is stored
  (`RefreshToken.TokenHash`); a DB leak therefore exposes no usable tokens.
- **Rotation (revoke-on-use)** — every successful `/refresh` revokes the presented token
  and issues a brand-new one, recording `ReplacedByTokenId` for an audit chain.
- **Passwords** — PBKDF2-SHA256, 100k iterations, per-password random salt, constant-time
  compare. Format: `{iterations}.{salt}.{hash}`.

## Endpoints

| Method | Route | Auth | Purpose |
|---|---|---|---|
| POST | `/api/auth/register` | anonymous | Create an account (default role `Viewer`) |
| POST | `/api/auth/login` | anonymous | Exchange credentials → access + refresh tokens |
| POST | `/api/auth/refresh` | anonymous* | Rotate refresh token → new pair |
| POST | `/api/auth/revoke` | anonymous* | Logout (revoke a refresh token) |

\* These carry the refresh token in the body, not a bearer header.

## Flow

```
login ─┬─▶ access JWT (15m)  ──▶ Authorization: Bearer <jwt>
       └─▶ refresh token (7d, stored hashed)
                │  access token expires
                ▼
            POST /refresh { refreshToken }
                │  old token revoked + replaced
                ▼
            new access + new refresh token
```

## Roles & authorization

`Role` enum: `Viewer < Operator < Admin`. Applied with `[Authorize]` /
`[Authorize(Roles = "Operator,Admin")]` — e.g. shipment status changes require Operator+.

## Graph model

```
(:User {id, email, passwordHash, displayName, role, createdAt})
   -[:HAS_TOKEN]->
(:RefreshToken {id, tokenHash, expiresAt, createdAt, revokedAt, replacedByTokenId})
```

Constraints (created on startup): unique `User.id`, unique `User.email`,
unique `RefreshToken.id`, index on `RefreshToken.tokenHash`.

## Production checklist

- [ ] Move `Auth:SigningKey` to a secret store / env var (never commit a real key).
- [ ] Serve only over HTTPS; consider delivering the refresh token as an `HttpOnly`,
      `Secure`, `SameSite=Strict` cookie instead of a JSON body.
- [ ] Add a background job to purge expired/revoked `RefreshToken` nodes.
- [ ] Consider reuse-detection: if a revoked token is presented, revoke the whole chain.
- [ ] Add rate limiting on `/login` and `/refresh`.
