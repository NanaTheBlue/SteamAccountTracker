# Hetzner Server Setup (One-Time)

This is the **one-time** manual setup on your Hetzner VPS. After this, every push to `master` deploys automatically.

---

## 1. Buy / Create the VPS

In the Hetzner Cloud console, create a server:
- **OS**: Ubuntu 24.04
- **Type**: CPX11 (2 vCPU / 2GB RAM) is enough to start
- **SSH Key**: Add your personal SSH key so you can log in

---

## 2. SSH In

```bash
ssh root@<your-server-ip>
```

---

## 3. Install Docker

```bash
curl -fsSL https://get.docker.com | sh
```

Verify:
```bash
docker --version
docker compose version
```

---

## 4. Create a Deploy SSH Key (for GitHub Actions)

On your **local machine** (not the server), generate a dedicated keypair:

```bash
ssh-keygen -t ed25519 -C "github-actions-deploy" -f ~/.ssh/cheaterwatch_deploy
```

This creates:
- `~/.ssh/cheaterwatch_deploy` — **private key** (goes into GitHub Secrets)
- `~/.ssh/cheaterwatch_deploy.pub` — **public key** (goes onto the server)

Copy the public key to the server:
```bash
ssh-copy-id -i ~/.ssh/cheaterwatch_deploy.pub root@<your-server-ip>
```

Or manually append it:
```bash
cat ~/.ssh/cheaterwatch_deploy.pub | ssh root@<your-server-ip> "cat >> ~/.ssh/authorized_keys"
```

---

## 5. Add GitHub Secrets

In your GitHub repo → **Settings → Secrets and variables → Actions → New repository secret**:

| Secret name | Value |
|---|---|
| `SERVER_HOST` | Your droplet IP (e.g. `123.456.789.0`) |
| `SERVER_SSH_PRIVATE_KEY` | Contents of `~/.ssh/cheaterwatch_deploy` (the private key file) |

---

## 6. Point Your Domain at the Server

In your DNS provider, add an **A record**:
```
your-domain.com  →  <your-server-ip>
```

Let it propagate (usually a few minutes). Caddy won't be able to get a TLS cert until DNS resolves.

---

## 7. Clone the Repo on the Server

```bash
mkdir -p /opt/cheaterwatch
git clone https://github.com/<your-username>/cheaterwatch.git /opt/cheaterwatch
cd /opt/cheaterwatch
```

---

## 8. Create the `.env` File

```bash
nano /opt/cheaterwatch/.env
```

Paste and fill in real values:

```env
# Your domain (used by Caddy for TLS)
DOMAIN=your-domain.com

# Strong random password — never reuse your dev password
DB_PASSWORD=a-very-strong-password-here

# From https://steamcommunity.com/dev/apikey
STEAM_API_KEY=your_steam_api_key

# Random secret shared with the Cloudflare Worker
WORKER_KEY=generate-a-random-secret-here

# Your frontend URL (Cloudflare Pages URL or custom domain)
FRONTEND_URL=https://your-frontend.com
```

Generate a random secret for `WORKER_KEY`:
```bash
openssl rand -hex 32
```

---

## 9. First Deploy

```bash
cd /opt/cheaterwatch
docker compose -f docker-compose.prod.yml up -d --build
```

Watch the logs:
```bash
docker compose -f docker-compose.prod.yml logs -f
```

---

## 10. Verify

```bash
# Health check
curl https://your-domain.com/healthz

# Should return: Healthy
```

Caddy will automatically obtain a Let's Encrypt certificate on first request. Allow ~30 seconds for it to provision.

---

## Ongoing: How Auto-Deploy Works

After this setup, every push to `master`:

1. GitHub Actions runs all CI tests
2. If tests pass, it SSHes into your server as `root`
3. Runs `git pull && docker compose -f docker-compose.prod.yml up -d --build`
4. Old containers are replaced with zero manual intervention

You can watch deployments in **GitHub → Actions → CI/CD**.

---

## Rollback

If a bad deploy goes out:

```bash
ssh root@<your-server-ip>
cd /opt/cheaterwatch
git log --oneline -10          # find the last good commit
git checkout <good-sha>
docker compose -f docker-compose.prod.yml up -d --build
```
