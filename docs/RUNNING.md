# Running Horizon FMS

This guide explains how to run the project and handle **Port Conflicts** if you have other services running.

## 🚀 Option 1: Local Development (Recommended)

The easiest way to run the project with custom ports is using the helper script.

### Default Ports
- **API**: 5295
- **Web**: 5173

### Auto-Port Discovery (Default)
The script now **automatically finds available ports** starting from the defaults.

```powershell
./setup-dev.ps1
```
*If port 5295 is taken, it tries 5296, 5297, etc.*

### Custom Start Porst
You can specify a different **starting** port if you want to be in a specific range:

```powershell
# Start looking for API ports at 6000
./setup-dev.ps1 -StartApiPort 6000
```

This automatically:
1. Starts the API on the new port (`dotnet run --urls ...`)
2. Configures the Web Frontend to proxy requests to that new API port
3. Starts the Web Frontend on your desired port

---

## 🐳 Option 2: Docker Compose

If you prefer running everything in containers, you can configure ports using an `.env` file.

1. **Create Configuration File**
   Copy the example file:
   ```bash
   cp .env.example .env
   ```

2. **Edit Ports**
   Open `.env` and change the ports to whatever you need:
   ```env
   # .env
   API_PORT=5001
   WEB_PORT=8081
   DB_PORT=5433
   ```

3. **Start Containers**
   Docker will automatically pick up the variables from `.env`:
   ```bash
   docker-compose up -d
   ```

   You can now access your app at `http://localhost:8081` (or whatever you chose).

---

## 🔧 Manual Setup (Advanced)

If you want to run commands manually without the script:

**1. Run API on Custom Port:**
```bash
cd FileManagementSystem.API
dotnet run --urls "http://localhost:5000"
```

**2. Run Web on Custom Port:**
You need to tell Vite where the API is, and what port to listen on:
```bash
cd FileManagementSystem.Web
# PowerShell
$env:API_PORT=5000; npm run dev -- --port 3000

# Bash
API_PORT=5000 npm run dev -- --port 3000
```
