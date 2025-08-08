#!/bin/bash

# CodeAgent Docker Runner Script
set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}CodeAgent Docker Container Manager${NC}"
echo "======================================"

# Function to check if Docker is running
check_docker() {
    if ! docker info > /dev/null 2>&1; then
        echo -e "${RED}Docker is not running. Please start Docker first.${NC}"
        exit 1
    fi
    echo -e "${GREEN}✓ Docker is running${NC}"
}

# Function to create test_workspace if it doesn't exist
ensure_test_workspace() {
    if [ ! -d "test_workspace" ]; then
        echo -e "${YELLOW}Creating test_workspace directory...${NC}"
        mkdir -p test_workspace
        echo "# Test Workspace" > test_workspace/README.md
        echo "This directory is mounted as /workspace in the CodeAgent container." >> test_workspace/README.md
    fi
    echo -e "${GREEN}✓ test_workspace directory exists${NC}"
}

# Function to create .env file if it doesn't exist
ensure_env_file() {
    if [ ! -f ".env" ]; then
        echo -e "${YELLOW}Creating .env file template...${NC}"
        cat > .env << EOL
# CodeAgent Environment Variables
# Add your API keys here (they will be loaded by docker-compose)

# OpenAI Configuration
# OPENAI_API_KEY=your-openai-api-key-here

# Anthropic Configuration  
# ANTHROPIC_API_KEY=your-anthropic-api-key-here

# Ollama Configuration (if using local Ollama)
# OLLAMA_BASE_URL=http://host.docker.internal:11434

# LM Studio Configuration (if using LM Studio)
# LMSTUDIO_BASE_URL=http://host.docker.internal:1234
EOL
        echo -e "${GREEN}✓ .env file created (add your API keys)${NC}"
    else
        echo -e "${GREEN}✓ .env file exists${NC}"
    fi
}

# Main script
case "$1" in
    build)
        check_docker
        ensure_test_workspace
        ensure_env_file
        echo -e "${YELLOW}Building Docker image...${NC}"
        docker-compose build --no-cache
        echo -e "${GREEN}✓ Build complete${NC}"
        ;;
    
    start|up)
        check_docker
        ensure_test_workspace
        ensure_env_file
        echo -e "${YELLOW}Starting CodeAgent container...${NC}"
        docker-compose up -d
        echo -e "${GREEN}✓ CodeAgent is running at http://localhost:5001${NC}"
        echo -e "${GREEN}  View logs with: docker-compose logs -f${NC}"
        ;;
    
    stop|down)
        check_docker
        echo -e "${YELLOW}Stopping CodeAgent container...${NC}"
        docker-compose down
        echo -e "${GREEN}✓ Container stopped${NC}"
        ;;
    
    restart)
        check_docker
        echo -e "${YELLOW}Restarting CodeAgent container...${NC}"
        docker-compose restart
        echo -e "${GREEN}✓ Container restarted${NC}"
        ;;
    
    logs)
        check_docker
        docker-compose logs -f
        ;;
    
    shell)
        check_docker
        echo -e "${YELLOW}Opening shell in CodeAgent container...${NC}"
        docker-compose exec codeagent /bin/bash
        ;;
    
    clean)
        check_docker
        echo -e "${YELLOW}Cleaning up containers and volumes...${NC}"
        docker-compose down -v
        echo -e "${GREEN}✓ Cleanup complete${NC}"
        ;;
    
    rebuild)
        check_docker
        ensure_test_workspace
        ensure_env_file
        echo -e "${YELLOW}Rebuilding and restarting container...${NC}"
        docker-compose down
        docker-compose build --no-cache
        docker-compose up -d
        echo -e "${GREEN}✓ Rebuild complete - CodeAgent running at http://localhost:5001${NC}"
        ;;
    
    dev)
        check_docker
        ensure_test_workspace
        ensure_env_file
        echo -e "${YELLOW}Starting development container with hot-reload...${NC}"
        docker-compose -f docker-compose.dev.yml up
        ;;
    
    dev-build)
        check_docker
        ensure_test_workspace
        ensure_env_file
        echo -e "${YELLOW}Building development container...${NC}"
        docker-compose -f docker-compose.dev.yml build --no-cache
        echo -e "${GREEN}✓ Development container built${NC}"
        ;;
    
    dev-down)
        check_docker
        echo -e "${YELLOW}Stopping development container...${NC}"
        docker-compose -f docker-compose.dev.yml down
        echo -e "${GREEN}✓ Development container stopped${NC}"
        ;;
    
    status)
        check_docker
        echo -e "${YELLOW}Container status:${NC}"
        docker-compose ps
        echo ""
        echo -e "${YELLOW}Development container status:${NC}"
        docker-compose -f docker-compose.dev.yml ps
        ;;
    
    *)
        echo "Usage: $0 {build|start|stop|restart|logs|shell|clean|rebuild|status|dev|dev-build|dev-down}"
        echo ""
        echo "Production Commands:"
        echo "  build      - Build the Docker image"
        echo "  start      - Start the container (also: up)"
        echo "  stop       - Stop the container (also: down)"
        echo "  restart    - Restart the container"
        echo "  logs       - Show container logs (follow mode)"
        echo "  shell      - Open bash shell in container"
        echo "  clean      - Remove containers and volumes"
        echo "  rebuild    - Clean rebuild and restart"
        echo "  status     - Show container status"
        echo ""
        echo "Development Commands:"
        echo "  dev        - Start development container with hot-reload"
        echo "  dev-build  - Build development container"
        echo "  dev-down   - Stop development container"
        exit 1
        ;;
esac