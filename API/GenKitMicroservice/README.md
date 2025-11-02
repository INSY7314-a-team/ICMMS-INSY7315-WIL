# GenKit Microservice

A Node.js microservice that integrates Google's GenKit AI with the ICCMS .NET API.

## Setup

1. Install dependencies:

```bash
npm install
```

2. Set environment variables:

```bash
cp .env.example .env
# Edit .env with your configuration
```

3. Run the service:

```bash
# Development
npm run dev

# Production
npm start
```

## API Endpoints

- `POST /api/ai/text` - Process text with AI
- `POST /api/ai/image` - Process images with AI
- `POST /api/ai/generate` - Generate structured responses
- `GET /health` - Health check

## Integration with .NET API

Call this microservice from your .NET API using HttpClient.
