# Render Deployment Guide

This guide walks you through deploying Horizon FMS to Render.

## Prerequisites

- GitHub repository with your code
- Render account (free tier works)
- Cloudinary account for file storage

## Step 1: Create PostgreSQL Database

1. Go to Render Dashboard → New → PostgreSQL
2. Configure:
   - Name: `horizon-fms-db`
   - Database: `horizon_fms`
   - User: `horizon_user`
   - Region: Oregon (or your preferred region)
   - Plan: Free
3. Click "Create Database"
4. **IMPORTANT**: Copy the "External Database URL" - you'll need this later
   - Format: `postgresql://user:password@host:port/database`

## Step 2: Create Redis Instance

1. Go to Render Dashboard → New → Redis
2. Configure:
   - Name: `horizon-fms-redis`
   - Region: Oregon (same as database)
   - Plan: Free
   - Max Memory Policy: `allkeys-lru`
3. Click "Create Redis"
4. **IMPORTANT**: Copy the "Redis Connection String" - you'll need this later

## Step 3: Deploy API Service

### Option A: Using render.yaml (Recommended)

1. Push your code to GitHub (including `render.yaml`)
2. Go to Render Dashboard → New → Blueprint
3. Connect your GitHub repository
4. Render will detect `render.yaml` and create all services
5. **CRITICAL**: After creation, manually set environment variables (see Step 4)

### Option B: Manual Setup

1. Go to Render Dashboard → New → Web Service
2. Connect your GitHub repository
3. Configure:
   - Name: `horizon-fms-api`
   - Environment: Docker
   - Region: Oregon
   - Branch: main (or your default branch)
   - Dockerfile Path: `./FileManagementSystem.API/Dockerfile`
   - Docker Context: `.` (root directory)
   - Plan: Free
4. Set environment variables (see Step 4)
5. Click "Create Web Service"

## Step 4: Configure Environment Variables

In your Render API service settings, add these environment variables:

### Required Variables

```bash
# Port Configuration
PORT=10000
ASPNETCORE_URLS=http://+:10000
ASPNETCORE_ENVIRONMENT=Production

# Database Connection (CRITICAL - use your actual database URL from Step 1)
ConnectionStrings__DefaultConnection=postgresql://user:password@host:port/database

# Redis Connection (use your actual Redis URL from Step 2)
ConnectionStrings__Redis=your-redis-connection-string

# Cloudinary Settings (get from your Cloudinary dashboard)
CloudinarySettings__CloudName=your-cloud-name
CloudinarySettings__ApiKey=your-api-key
CloudinarySettings__ApiSecret=your-api-secret
CloudinarySettings__IsEnabled=true

# CORS Origins (update with your actual frontend URLs)
AllowedOrigins__0=https://your-app.vercel.app
AllowedOrigins__1=https://your-custom-domain.com
AllowedOrigins__2=https://*.vercel.app
```

### How to Get Database Connection String

1. Go to your PostgreSQL database in Render
2. Click "Info" tab
3. Copy "External Database URL"
4. Paste it as the value for `ConnectionStrings__DefaultConnection`

### How to Get Redis Connection String

1. Go to your Redis instance in Render
2. Click "Info" tab
3. Copy "Redis Connection String"
4. Paste it as the value for `ConnectionStrings__Redis`

## Step 5: Verify Deployment

1. Wait for the build to complete (15-20 minutes for .NET Docker on free tier)
2. Check the logs for these messages:
   ```
   [DB CONFIG] Successfully converted to Npgsql format
   [DB CONFIG] Configuring DbContext with PostgreSQL (Npgsql)
   [HEALTH CHECK] Adding PostgreSQL health check
   [HEALTH CHECK] Adding Redis health check
   ```
3. Visit your API health endpoint: `https://your-api.onrender.com/health`
4. Should return: `Healthy`

## Step 6: Deploy Frontend to Vercel

See `docs/VERCEL_DEPLOYMENT.md` for frontend deployment instructions.

## Troubleshooting

### Issue: "Format of the initialization string does not conform to specification"

**Cause**: Database connection string is empty or malformed.

**Solution**:
1. Check that `ConnectionStrings__DefaultConnection` is set in Render environment variables
2. Verify the format: `postgresql://user:password@host:port/database`
3. Make sure there are no extra spaces or line breaks
4. Redeploy after fixing

### Issue: "Health check redis with status Unhealthy"

**Cause**: Redis connection string is incorrect or Redis instance isn't ready.

**Solution**:
1. Verify `ConnectionStrings__Redis` is set correctly
2. Check that Redis instance is running in Render dashboard
3. Make sure both API and Redis are in the same region

### Issue: Build takes too long or times out

**Cause**: Render free tier has limited resources.

**Solution**:
1. Be patient - .NET Docker builds take 15-20 minutes on free tier
2. Check build logs for actual errors
3. Consider upgrading to paid tier for faster builds

### Issue: Database migrations fail

**Cause**: Database isn't accessible or connection string is wrong.

**Solution**:
1. Verify database is running in Render dashboard
2. Check connection string format
3. Look for `[DB CONFIG]` messages in logs to see what's happening

## Monitoring

### View Logs

1. Go to your API service in Render dashboard
2. Click "Logs" tab
3. Look for `[DB CONFIG]` and `[HEALTH CHECK]` messages at startup

### Check Health

Visit: `https://your-api.onrender.com/health`

Should return JSON with status of all services:
- Database (PostgreSQL)
- Redis
- Storage (Cloudinary)

## Cost Optimization

### Free Tier Limits

- PostgreSQL: 90 days free, then $7/month
- Redis: 90 days free, then $10/month
- Web Service: Free forever (with limitations)

### Recommendations

1. Use Cloudinary free tier for storage (25 GB)
2. Monitor usage in Render dashboard
3. Set up billing alerts
4. Consider upgrading to paid tier for production use

## Security Checklist

- [ ] Database connection string is set as environment variable (not in code)
- [ ] Cloudinary credentials are set as environment variables
- [ ] CORS origins are configured correctly
- [ ] Health check endpoint is accessible
- [ ] Logs don't contain sensitive information
- [ ] SSL/TLS is enabled (automatic on Render)

## Next Steps

1. Set up custom domain (optional)
2. Configure monitoring and alerts
3. Set up automated backups for database
4. Review and optimize performance
5. Set up CI/CD pipeline for automated deployments

---

**Need Help?**
- Render Docs: https://render.com/docs
- Render Community: https://community.render.com
- GitHub Issues: https://github.com/your-repo/issues
