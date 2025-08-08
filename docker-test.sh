#!/bin/bash

# Docker WebSocket/SignalR Test Script for CodeAgent

echo "🔧 CodeAgent Docker WebSocket Test Script"
echo "========================================="

# Function to check if a command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Check Docker
if ! command_exists docker; then
    echo "❌ Docker is not installed"
    exit 1
fi

if ! command_exists docker-compose; then
    echo "❌ Docker Compose is not installed"
    exit 1
fi

echo "✅ Docker and Docker Compose are installed"

# Clean up any existing containers
echo ""
echo "🧹 Cleaning up existing containers..."
docker-compose -f docker-compose.simple.yml down
docker-compose down

# Build the application
echo ""
echo "🔨 Building the Docker image..."
docker-compose -f docker-compose.simple.yml build

# Start the container
echo ""
echo "🚀 Starting the container..."
docker-compose -f docker-compose.simple.yml up -d

# Wait for the container to be ready
echo ""
echo "⏳ Waiting for the application to start..."
sleep 10

# Check if the container is running
CONTAINER_STATUS=$(docker-compose -f docker-compose.simple.yml ps -q)
if [ -z "$CONTAINER_STATUS" ]; then
    echo "❌ Container failed to start"
    docker-compose -f docker-compose.simple.yml logs
    exit 1
fi

echo "✅ Container is running"

# Test HTTP connectivity
echo ""
echo "🔍 Testing HTTP connectivity..."
HTTP_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:5001/health 2>/dev/null)

if [ "$HTTP_RESPONSE" = "200" ]; then
    echo "✅ HTTP endpoint is accessible (Status: $HTTP_RESPONSE)"
else
    echo "❌ HTTP endpoint is not accessible (Status: $HTTP_RESPONSE)"
    echo "Checking container logs..."
    docker-compose -f docker-compose.simple.yml logs --tail=50
fi

# Test WebSocket connectivity
echo ""
echo "🔍 Testing WebSocket/SignalR connectivity..."
echo "Please open http://localhost:5001 in your browser and check:"
echo "1. Open Developer Tools (F12)"
echo "2. Go to Network tab"
echo "3. Filter by WS (WebSocket)"
echo "4. Try to send a chat message"
echo "5. Check for WebSocket connection attempts"
echo ""
echo "Common issues to check:"
echo "- WebSocket upgrade headers"
echo "- CORS errors in console"
echo "- Connection timeout messages"

# Show container logs
echo ""
echo "📋 Container logs (last 20 lines):"
docker-compose -f docker-compose.simple.yml logs --tail=20

echo ""
echo "========================================="
echo "To view live logs, run:"
echo "  docker-compose -f docker-compose.simple.yml logs -f"
echo ""
echo "To stop the container, run:"
echo "  docker-compose -f docker-compose.simple.yml down"
echo ""
echo "To test with nginx proxy, run:"
echo "  docker-compose up"
echo "========================================="