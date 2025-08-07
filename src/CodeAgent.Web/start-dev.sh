#!/bin/bash

# CodeAgent Web Development Server Startup Script
echo "Starting CodeAgent Web Development Environment..."

# Get the directory where this script is located
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
echo "Script directory: $SCRIPT_DIR"

# Function to cleanup background processes on script exit
cleanup() {
    echo "Shutting down servers..."
    if [ ! -z "$ANGULAR_PID" ]; then
        kill $ANGULAR_PID 2>/dev/null
    fi
    if [ ! -z "$DOTNET_PID" ]; then
        kill $DOTNET_PID 2>/dev/null
    fi
    exit 0
}

# Set up trap to call cleanup function on script exit
trap cleanup SIGINT SIGTERM EXIT

# Change to the script directory (CodeAgent.Web)
cd "$SCRIPT_DIR"

# Start Angular dev server in background
echo "Starting Angular dev server on http://localhost:4200..."
cd client
if [ ! -f "package.json" ]; then
    echo "ERROR: package.json not found in $(pwd)"
    echo "Please make sure you're running this script from the CodeAgent.Web directory"
    exit 1
fi

npm start &
ANGULAR_PID=$!

# Wait for Angular server to be ready
echo "Waiting for Angular dev server to start..."
sleep 8

# Check if Angular server is running
if ! curl -s http://localhost:4200 > /dev/null 2>&1; then
    echo "WARNING: Angular dev server may not be ready yet. It might take a few more seconds."
fi

# Go back to project root (CodeAgent.Web directory)
cd "$SCRIPT_DIR"

# Start .NET server
echo "Starting .NET server on http://localhost:5001..."
if [ ! -f "CodeAgent.Web.csproj" ]; then
    echo "ERROR: CodeAgent.Web.csproj not found in $(pwd)"
    echo "Please make sure you're running this script from the CodeAgent.Web directory"
    exit 1
fi

dotnet run &
DOTNET_PID=$!

echo "Both servers are starting..."
echo "Angular: http://localhost:4200"
echo ".NET API: http://localhost:5001"
echo ""
echo "Press Ctrl+C to stop both servers"

# Wait for both processes
wait