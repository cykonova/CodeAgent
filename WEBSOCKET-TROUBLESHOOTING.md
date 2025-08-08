# WebSocket/SignalR Troubleshooting Guide for Docker

## Overview
This guide helps troubleshoot WebSocket connectivity issues when running CodeAgent in Docker containers.

## Quick Test

### 1. Use the Simple Docker Compose
```bash
# Stop any running containers
docker-compose down

# Start with the simple configuration
docker-compose -f docker-compose.simple.yml up --build
```

### 2. Test Health Endpoint
```bash
curl http://localhost:5001/health
```

You should see a JSON response with environment and SignalR hub information.

### 3. Check Browser Console
1. Open http://localhost:5001 in your browser
2. Open Developer Tools (F12)
3. Go to Console tab
4. Look for errors related to:
   - WebSocket connection failures
   - SignalR connection errors
   - CORS policy violations

### 4. Check Network Tab
1. In Developer Tools, go to Network tab
2. Filter by "WS" (WebSocket)
3. Try sending a chat message
4. Look for WebSocket upgrade requests to `/hub/agent` or `/hub/collaboration`

## Common Issues and Solutions

### Issue 1: WebSocket Connection Refused

**Symptoms:**
- Error: "WebSocket connection to 'ws://localhost:5001/hub/agent' failed"
- Connection immediately closed

**Solutions:**
1. Ensure the container is running:
   ```bash
   docker ps
   ```

2. Check container logs:
   ```bash
   docker-compose -f docker-compose.simple.yml logs
   ```

3. Verify ports are exposed:
   ```bash
   docker port codeagent-web
   ```

### Issue 2: CORS Errors

**Symptoms:**
- Error: "CORS policy: No 'Access-Control-Allow-Origin' header"
- SignalR falls back to long polling

**Solutions:**
1. The CORS configuration has been updated to support Docker environments
2. Rebuild the container:
   ```bash
   docker-compose -f docker-compose.simple.yml build --no-cache
   docker-compose -f docker-compose.simple.yml up
   ```

### Issue 3: WebSocket Upgrade Failed

**Symptoms:**
- HTTP 400 Bad Request on WebSocket upgrade
- Headers missing Upgrade: websocket

**Solutions:**
1. If using a reverse proxy (nginx), ensure WebSocket headers are forwarded:
   ```nginx
   proxy_http_version 1.1;
   proxy_set_header Upgrade $http_upgrade;
   proxy_set_header Connection "upgrade";
   ```

2. Test without reverse proxy first using `docker-compose.simple.yml`

### Issue 4: Connection Timeout

**Symptoms:**
- WebSocket connects but times out after a period
- Chat stops working after being idle

**Solutions:**
1. Keep-alive has been configured in the application
2. Check Docker network settings:
   ```bash
   docker network inspect codeagent_codeagent-network
   ```

## Testing WebSocket Connectivity

### Manual WebSocket Test
```javascript
// Run this in browser console at http://localhost:5001
const ws = new WebSocket('ws://localhost:5001/health/websocket-test');
ws.onopen = () => console.log('WebSocket connected');
ws.onmessage = (e) => console.log('Message:', e.data);
ws.onerror = (e) => console.error('WebSocket error:', e);
ws.onclose = () => console.log('WebSocket closed');

// Send a test message
ws.send('Hello WebSocket');
```

### SignalR Connection Test
```javascript
// Run this in browser console at http://localhost:5001
// This tests if SignalR can establish a connection
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hub/agent")
    .configureLogging(signalR.LogLevel.Debug)
    .build();

connection.start()
    .then(() => console.log('SignalR connected'))
    .catch(err => console.error('SignalR connection failed:', err));
```

## Docker Configuration Changes Made

### 1. Program.cs Updates
- Added WebSocket middleware with keep-alive settings
- Enhanced CORS policy for Docker environments
- Added forwarded headers support for reverse proxy
- Made Kestrel listen on all interfaces when in Docker

### 2. Dockerfile Updates
- Set `DOTNET_RUNNING_IN_CONTAINER=true` environment variable
- Ensured proper port exposure

### 3. Docker Compose Updates
- Added WebSocket-specific environment variables
- Enabled debug logging for SignalR
- Created simple configuration without reverse proxy for testing

## Verification Steps

1. **Build and run the container:**
   ```bash
   ./docker-test.sh
   ```

2. **Check health endpoint:**
   ```bash
   curl http://localhost:5001/health | jq .
   ```

3. **Monitor logs in real-time:**
   ```bash
   docker-compose -f docker-compose.simple.yml logs -f
   ```

4. **Test chat functionality:**
   - Open http://localhost:5001
   - Send a test message
   - Check browser console for errors
   - Check Docker logs for SignalR debug messages

## Still Having Issues?

If WebSocket connectivity still fails:

1. **Collect diagnostic information:**
   ```bash
   # Container environment
   docker exec codeagent-web printenv | grep -E "(ASPNET|DOTNET)"
   
   # Network configuration
   docker inspect codeagent-web | jq '.[0].NetworkSettings'
   
   # Application logs
   docker-compose -f docker-compose.simple.yml logs --tail=100 > docker-logs.txt
   ```

2. **Try running without Docker:**
   ```bash
   cd src/CodeAgent.Web
   dotnet run
   ```
   Then test at http://localhost:5001

3. **Check firewall/antivirus:**
   - Some security software blocks WebSocket connections
   - Try temporarily disabling to test

## Configuration Files

- `docker-compose.simple.yml` - Simple configuration for testing
- `docker-compose.yml` - Full configuration with all services
- `nginx.conf` - Reverse proxy configuration with WebSocket support
- `docker-test.sh` - Automated testing script