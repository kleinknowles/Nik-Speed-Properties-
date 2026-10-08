# Publish Nik Speed Properties on AWS Lightsail

This guide deploys the current web app and .NET API as Docker containers on an AWS Lightsail Linux instance. PostgreSQL runs in a separate Lightsail managed database, property media is stored in a private S3 bucket, and Caddy provides automatic HTTPS for a domain you own.

## Before creating AWS resources

- Have access to an AWS account with billing enabled. Turn on MFA for the account and use an IAM administrator identity for setup; do not create application access keys from the AWS root identity.
- Have a domain name you control. HTTPS certificates and secure password sign-in require DNS to point a domain at the server.
- This setup creates billable resources. Check the selected region and current prices before pressing **Create**. AWS currently lists Lightsail Linux instances starting at $5/month and managed databases starting at $15/month; encrypted database and storage choices can cost more. Review the live [Lightsail pricing page](https://aws.amazon.com/lightsail/pricing/) first.
- Choose the same region for the server, database and S3 bucket. Singapore (`ap-southeast-1`) is a reasonable first choice for Uganda; users can compare other regions before creating anything.
- New AWS resources start empty. The API creates its tables and three sample listings in the new database. Moving accounts, transactions or real property records from the local SQLite file is not included in this first deployment.

## Step 1 — Publish the latest source to GitHub

The Lightsail server will clone this repository. Commit and push the website changes you want to publish to the `main` branch before continuing. Confirm the GitHub repository is private if its source contains anything you do not want public. Never commit `.env.aws`, passwords, API credentials or private keys.

On the server, use the exact repository URL and branch. The guide assumes the repository URL is `https://github.com/kleinknowles/Nik-Speed-Properties-.git`.

## Step 2 — Create a Lightsail Linux server

1. Sign in at [AWS Console](https://console.aws.amazon.com/) and open **Amazon Lightsail**.
2. Select the intended AWS region (for example, **Asia Pacific (Singapore)**) in the upper-right region selector.
3. Choose **Create instance**. Select the Linux/Unix platform and an Ubuntu LTS blueprint. Give the instance a recognizable name such as `nikspeed-web`.
4. Select a plan with at least **2 GB RAM** for the Next.js server, .NET API and HTTPS proxy. Review the monthly price shown by AWS before creating it.
5. Create the instance and wait for its state to become **Running**.
6. Open **Networking → Create static IP**. Choose the same region, attach the static IP to `nikspeed-web`, and save it. Record the static IPv4 address.
7. On the instance **Networking** tab, retain SSH port 22 restricted to your own current IP. Add public TCP firewall rules for ports **80** and **443** (IPv4; add IPv6 rules too only if you publish an IPv6 DNS record). Do not expose 3000, 5188, 8080 or 5432.
8. In **Connect**, open the browser-based SSH terminal for the running server.

## Step 3 — Point your domain at the server

At your DNS provider (or a Route 53 hosted zone), add an **A** record for the exact domain you will use (for example, `www.example.com`) pointing to the Lightsail static IPv4 address. If you want the apex domain too, add a separate record and configure it to redirect to your chosen canonical domain after publishing. Do not use the instance's temporary IP address.

Wait until DNS resolves to the static IP before starting Caddy. In PowerShell, check with `Resolve-DnsName www.example.com` after replacing the example with your domain. DNS propagation can take time. Caddy will request and renew the site's TLS certificate automatically once the A record and ports 80/443 are reachable.

## Step 4 — Create the managed PostgreSQL database

1. In Lightsail, select **Databases → Create database** in the same region as the instance.
2. Choose PostgreSQL and a supported version offered in the console. Pick a database plan appropriate for your test and expected traffic. Prefer an encrypted plan for accounts and payment history; verify the plan description and cost.
3. Set the database name to `nikspeed`, and record the master username and password securely. AWS displays connection details after provisioning; copy the **private** endpoint for this instance deployment.
4. Wait for database status **Available**.
5. Keep public mode disabled. Use the Lightsail private connection from the same-region server. Do not open the database to all IP addresses.
6. Record the endpoint hostname, database username and password. Never paste them in chat or commit them.

The API uses EF Core `EnsureCreated` for a new test database. For an existing database that already has tables, prepare and review EF migrations first rather than deleting or recreating that database.

## Step 5 — Create a private S3 bucket for photos and videos

1. Open **Amazon S3 → Create bucket** and select the same region.
2. Choose a globally unique bucket name, such as `nikspeed-media-youruniquevalue`.
3. Keep **Block all public access** enabled. Enable default server-side encryption and bucket versioning if you want recovery from accidental replacement/deletion; versioning can add storage charges.
4. Create the bucket. Do not add a public bucket policy, website hosting, or public-read ACLs. The API reads the private objects and serves them to the website.
5. In **IAM → Policies**, create a policy for this bucket. Replace `YOUR_BUCKET_NAME` and use this minimum object permission policy:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "ListNikSpeedMediaBucket",
      "Effect": "Allow",
      "Action": ["s3:ListBucket"],
      "Resource": "arn:aws:s3:::YOUR_BUCKET_NAME"
    },
    {
      "Sid": "ManageNikSpeedMediaObjects",
      "Effect": "Allow",
      "Action": ["s3:GetObject", "s3:PutObject", "s3:DeleteObject"],
      "Resource": "arn:aws:s3:::YOUR_BUCKET_NAME/*"
    }
  ]
}
```

6. In **IAM → Users**, create an application-only user named `nikspeed-media`. Do not enable AWS console sign-in. Attach only the bucket policy above.
7. Create one access key for this IAM user with a workload that runs outside AWS. Choose any requested acknowledgment, download/copy the access key ID and secret access key, and store them privately. AWS reveals the secret only once. Do not send either key in chat or put it into GitHub.

## Step 6 — Install Docker and download the project on the server

In the Lightsail browser SSH terminal:

```bash
sudo apt-get update
sudo apt-get install -y docker.io docker-compose-v2 git
sudo systemctl enable --now docker
sudo usermod -aG docker "$USER"
```

Sign out of the SSH session and open it again so group membership is refreshed. Confirm Docker works:

```bash
docker --version
docker compose version
```

Clone the repository and move into it:

```bash
git clone --branch main https://github.com/kleinknowles/Nik-Speed-Properties-.git nikspeed
cd nikspeed
```

If the repository is private, use a short-lived GitHub fine-grained token with read-only access to this repository for the clone. Do not embed that token in the clone URL or save it in the project. Revoke the token after the clone; subsequent updates can use a fresh short-lived token.

## Step 7 — Configure production secrets on the server

Create the private environment file from the example:

```bash
cp .env.aws.example .env.aws
nano .env.aws
```

Set all of these values (replace every example):

- `DOMAIN` — the public DNS name for this site (for example, `www.example.com`).
- `WEB_ORIGIN` — exactly `https://` followed by that domain, with no trailing slash.
- `AWS_REGION` — the bucket region code (for Singapore, `ap-southeast-1`).
- `DATABASE_HOST`, `DATABASE_PORT`, `DATABASE_NAME`, `DATABASE_USER`, `DATABASE_PASSWORD` — the Lightsail database connection details. Use the private database endpoint.
- `JWT_KEY` — a unique random secret. Generate a plain hexadecimal value with `openssl rand -hex 48` and copy it into nano.
- `S3_BUCKET`, `S3_ACCESS_KEY_ID`, `S3_SECRET_ACCESS_KEY` — the private bucket and restricted IAM key created in Step 5.
- Leave both Flutterwave values blank while payments are unconfigured. Later, use **test** credentials first.

Save in nano with **Ctrl+O**, **Enter**, then **Ctrl+X**. Restrict access and confirm the file is ignored by Git:

```bash
chmod 600 .env.aws
git check-ignore .env.aws
```

The last command must print `.env.aws`. Never run `cat .env.aws`, take a screenshot of its contents, or share the values. If you need a strong database password, generate a hexadecimal value using `openssl rand -hex 24`; this avoids characters that need special quoting in a PostgreSQL connection string.

## Step 8 — Build and start the website

From the repository root on the Lightsail server:

```bash
docker compose --env-file .env.aws -f compose.aws.yaml up --build -d
```

Watch container status and startup errors:

```bash
docker compose --env-file .env.aws -f compose.aws.yaml ps
docker compose --env-file .env.aws -f compose.aws.yaml logs --tail=100 api web caddy
```

Confirm the Caddy logs show successful certificate issuance. If not, check the DNS A record, static IP, the domain in `.env.aws`, and Lightsail firewall ports 80 and 443. Never work around a certificate error by sending account passwords over HTTP.

## Step 9 — Check the deployment before sharing it

In a browser, open `https://YOUR_DOMAIN/` and check the home page, account form, browse page, property details, saved homes and dashboard. Check the public health endpoint `https://YOUR_DOMAIN/health/ready`; it should report that the database is connected. Create a test account and sign in. Create a test property only after payment/listing-credit rules allow it, upload a small image and video, then confirm they load after restarting the containers:

```bash
docker compose --env-file .env.aws -f compose.aws.yaml restart
```

Confirm the same media still loads. Set up a Flutterwave **test** secret and webhook hash in `.env.aws` only if testing checkout, and register `https://YOUR_DOMAIN/api/payments/flutterwave/webhook` in the Flutterwave test account. Restart the API after changing the file:

```bash
docker compose --env-file .env.aws -f compose.aws.yaml up -d --force-recreate api
```

Do not enable live payments until the business account, settlement settings, webhook, refunds and end-to-end verification are ready.

## Step 10 — Backups, updates and monitoring

- In Lightsail **Databases**, enable the available automatic backup/snapshot option and document the database restore procedure. Periodically test restoring a backup.
- Keep S3 versioning on if you need recovery for deleted/overwritten media. Set a lifecycle policy only after deciding retention and cost requirements.
- Enable Lightsail automatic snapshots for the server; server snapshots do not replace PostgreSQL backups or S3 object backups.
- To deploy a later GitHub update, back up first, then on the instance run `git pull --ff-only` followed by `docker compose --env-file .env.aws -f compose.aws.yaml up --build -d`. Review logs and test health before considering the update complete.
- Monitor AWS Billing and set a monthly budget alert before traffic grows. Check AWS current regional prices and any outbound data-transfer or storage overages.
- Keep SSH restricted to your IP. Do not expose PostgreSQL or container ports to the internet. Rotate IAM keys and database/JWT secrets through a planned update; never put them in source control.

## Rollback / removal

Before deleting any resource, make and verify database and media backups. Stopping or deleting the instance, database, static IP, S3 bucket and snapshots have different billing/data consequences. Do not delete the bucket or database as part of routine troubleshooting.

## AWS references

- [Lightsail Linux instance plans](https://docs.aws.amazon.com/lightsail/latest/userguide/amazon-lightsail-bundles.html)
- [Attach a static IP to a Lightsail instance](https://docs.aws.amazon.com/lightsail/latest/userguide/lightsail-create-static-ip.html)
- [Create a Lightsail PostgreSQL database](https://docs.aws.amazon.com/lightsail/latest/userguide/amazon-lightsail-creating-a-database.html)
- [Connect to a Lightsail PostgreSQL database](https://docs.aws.amazon.com/lightsail/latest/userguide/amazon-lightsail-connecting-to-your-postgres-database.html)
- [Lightsail instance firewall rules](https://docs.aws.amazon.com/lightsail/latest/userguide/understanding-firewall-and-port-mappings-in-amazon-lightsail.html)
- [Amazon S3 pricing](https://aws.amazon.com/s3/pricing/)
- [AWS pricing calculator](https://calculator.aws/)
