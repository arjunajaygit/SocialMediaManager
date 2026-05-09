# SocialSync (Digital Marketing Automation Tool)

A full-stack digital marketing application designed to help users schedule, manage, and publish content across multiple social media platforms simultaneously, including LinkedIn and X/Twitter.

This project demonstrates modern .NET development, React, Clean Architecture, and complex third-party API integrations such as OAuth 2.0, media uploads, and background processing.

## 🛠 Tech Stack

- Backend: .NET 10, C# 12
- Frontend: React.js, Tailwind CSS, Vite
- Database: PostgreSQL
- ORM: Entity Framework Core
- Background Processing: Hangfire with PostgreSQL storage
- Cloud Storage: Cloudinary for media assets
- AI Integration: Azure OpenAI / Gemini for AI-assisted post generation

## ✨ Enterprise-Grade Features & Architecture

The application was engineered with scalability, security, and reliability in mind, following Clean Architecture principles.

### 1. Clean Architecture & Modular Services

The codebase is decoupled to support testability and maintainability:

- Domain: Core entities such as `User`, `Post`, and `SocialAccount`.
- Application: Business rules and interfaces such as `IPostService`, `ISocialMediaProvider`, and `IEncryptionService`.
- Infrastructure: EF Core `DbContext`, migrations, and external API implementations.
- API: Presentation layer, Hangfire configuration, rate limiting, and dependency injection setup.

### 2. Advanced Security & Encryption

- Zero plain-text tokens: OAuth access and refresh tokens are encrypted at rest and decrypted only at runtime using AES encryption.
- OAuth 2.0 and PKCE: Complete authorization flows were implemented, including PKCE challenge handling for the X API.
- Secure configuration: API keys and secrets are excluded from source control with `.gitignore`; local development can use .NET User Secrets.

### 3. Queue System & Scheduled Publishing

- Hangfire powers asynchronous background processing.
- Users can schedule posts for future dates.
- The background worker retrieves due posts, decrypts the required platform tokens, and publishes content asynchronously.

### 4. Resiliency & Retry Mechanisms

- Hangfire retries background jobs automatically for transient failures.
- Publication state is tracked per platform so a multi-platform post can recover platform-by-platform.

### 5. Rate Limiting

- Inbound protection: ASP.NET Core rate limiting protects expensive endpoints such as AI generation.
- Outbound throttling: Background jobs delay between HTTP requests to reduce 429 responses and avoid spam restrictions.

### 6. Native Media Uploads

- LinkedIn: Implements the UGC image upload flow for native media publishing.
- X/Twitter: Supports media upload flow for attaching images to posts.

## 🚀 Getting Started

### Prerequisites

- .NET 10 SDK
- Node.js and npm
- PostgreSQL running locally or via Docker
- API keys for X Developer Portal, LinkedIn Developer Portal, and Cloudinary

### Backend Setup

1. Navigate to the API directory:

   ```bash
   cd SocialMediaManager.API
   ```

2. Configure secrets. Do not place real keys in `appsettings.json`.

   ```bash
   dotnet user-secrets init
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=SocialDb;Username=postgres;Password=yourpassword"
   dotnet user-secrets set "OAuth:X:ClientId" "<YOUR_X_CLIENT_ID>"
   dotnet user-secrets set "OAuth:X:ClientSecret" "<YOUR_X_CLIENT_SECRET>"
   dotnet user-secrets set "OAuth:LinkedIn:ClientId" "<YOUR_LINKEDIN_CLIENT_ID>"
   dotnet user-secrets set "OAuth:LinkedIn:ClientSecret" "<YOUR_LINKEDIN_CLIENT_SECRET>"
   ```

3. Apply database migrations:

   ```bash
   dotnet ef database update --project ../SocialMediaManager.Infrastructure
   ```

4. Run the API:

   ```bash
   dotnet run
   ```

### Frontend Setup

1. Navigate to the frontend directory:

   ```bash
   cd ../SocialMediaManager.UI
   ```

2. Install dependencies:

   ```bash
   npm install
   ```

3. Start the Vite development server:

   ```bash
   npm run dev
   ```

## � Screenshots

### Connect Channels
Connect and manage your LinkedIn and X/Twitter accounts securely. The application displays your account usernames and allows easy disconnection when needed.

![Connect Channels](/docs/screenshots/connect-channels.png)

### Create Post
Compose posts with optional AI-generated captions using OpenAI or Gemini. Add images, select target platforms, and schedule publication directly from the interface.

![Create Post](/docs/screenshots/create-post.png)

### Scheduled Posts
View all scheduled posts with per-platform publication status. Track which posts have been published or are pending, with real-time status updates from the background job processor.

![Scheduled Posts](/docs/screenshots/scheduled-posts.png)

## �💡 Technical Challenges Overcome

- Strict API scopes: Requested the correct X scopes, including `tweet.read`, `tweet.write`, and `users.read`, to avoid authorization errors.
- Complex JSON schemas: Used dynamic dictionary-based payloads to match LinkedIn UGC API requirements.
- React StrictMode OAuth handling: Added a `useRef` guard to prevent duplicate OAuth code exchanges during development.
