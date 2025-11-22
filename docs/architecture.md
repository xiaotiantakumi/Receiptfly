# Architecture & Tech Stack (Low Cost / Indie Stack)

## Design Philosophy
**"Zero Fixed Cost"**: Prioritize services with generous free tiers to keep running costs at $0 until the app scales.
**Modern & Simple**: Use managed services for "hard" parts (Auth, DB, Storage) to focus on app logic.

## Technology Stack

### Frontend
- **Framework**: React (SPA)
- **Build Tool**: Vite
- **Language**: TypeScript
- **Hosting**: **Vercel** or **Cloudflare Pages** (Free, high performance).

### Backend
- **Framework**: ASP.NET Core Web API (C#)
- **Runtime**: .NET 8
- **Deployment**: Docker Container
- **Hosting**: **Fly.io** (Free allowance for small VMs) or **Render** (Free tier, spins down on idle).
    - *Why?* C# requires a container runtime. These platforms offer the easiest "git push to deploy" experience for containers with free tiers.

### Infrastructure (BaaS - Backend as a Service)
**Supabase** is recommended as the core infrastructure suite. It offers a massive free tier (500MB DB, 50k MAU Auth, 1GB Storage).

- **Database**: **Supabase (PostgreSQL)**.
    - *Integration*: Connect via Entity Framework Core (Npgsql).
- **Authentication**: **Supabase Auth**.
    - *Integration*: Easy to use SDK for React; JWT validation in C#.
- **Storage**: **Supabase Storage** (for Receipt Images).

### AI / OCR (Receipt Processing)
- **Primary Option**: **Azure AI Document Intelligence** (Free Tier: 500 pages/month).
    - *Why?* Specialized for receipts, very high accuracy. The free tier is sufficient for personal/dev use.
- **Alternative**: **OpenAI API (GPT-4o)**.
    - *Why?* Pay-per-use. Can be cheaper if volume is very low, but Azure Doc Intelligence is more structured for receipts.

## Architecture Diagram

```mermaid
graph TD
    User[User] -->|HTTPS| CDN[Vercel (Frontend)]
    User -->|HTTPS| API[Fly.io (ASP.NET Core API)]
    
    subgraph "Supabase (Free Tier)"
        Auth[Auth Service]
        DB[(PostgreSQL DB)]
        Storage[Blob Storage]
    end
    
    subgraph "External AI"
        OCR[Azure AI Doc Intelligence]
    end

    CDN -->|Auth SDK| Auth
    API -->|Verify JWT| Auth
    API -->|EF Core| DB
    API -->|Upload/Read| Storage
    API -->|Analyze Image| OCR
```

## Cost Estimate (Monthly)
| Service | Tier | Cost | Notes |
| :--- | :--- | :--- | :--- |
| **Frontend (Vercel)** | Hobby | **$0** | Unlimited bandwidth for personal use. |
| **Backend (Fly.io)** | Hobby | **$0** | Up to 3 small VMs (shared CPU) free. |
| **Database (Supabase)** | Free | **$0** | 500MB storage included. |
| **Auth (Supabase)** | Free | **$0** | 50,000 Monthly Active Users. |
| **OCR (Azure)** | Free | **$0** | 500 pages/month. |
| **Total** | | **$0** | *Scales with usage.* |
