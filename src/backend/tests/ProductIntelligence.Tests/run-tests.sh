#!/bin/bash

# Script to run integration tests against a running API
# Usage: ./run-tests.sh [API_URL]
#
# Examples:
#   ./run-tests.sh                                    # Uses default http://localhost:5000
#   ./run-tests.sh http://localhost:5000              # Explicit local URL
#   ./run-tests.sh https://api-staging.company.com    # Staging environment
#   ./run-tests.sh https://api.company.com            # Production (be careful!)

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Get API base URL from argument or use default
API_BASE_URL="${1:-http://localhost:5000}"

echo -e "${YELLOW}======================================${NC}"
echo -e "${YELLOW}Integration Test Runner${NC}"
echo -e "${YELLOW}======================================${NC}"
echo -e "Target API: ${GREEN}$API_BASE_URL${NC}"
echo ""

# Check if API is accessible
echo -e "${YELLOW}Checking API health...${NC}"
if curl -s -f "$API_BASE_URL/health" > /dev/null 2>&1; then
    echo -e "${GREEN}✓ API is running and healthy${NC}"
else
    echo -e "${RED}✗ API is not accessible at $API_BASE_URL${NC}"
    echo -e "${YELLOW}Please start the API first:${NC}"
    echo -e "  cd src/backend"
    echo -e "  dotnet run --project src/ProductIntelligence.API"
    exit 1
fi

echo ""
echo -e "${YELLOW}Running integration tests...${NC}"
echo ""

# Export API base URL for tests
export API_BASE_URL="$API_BASE_URL"

# Navigate to test directory
cd "$(dirname "$0")"

# Run tests
if dotnet test --logger "console;verbosity=normal"; then
    echo ""
    echo -e "${GREEN}======================================${NC}"
    echo -e "${GREEN}✓ All tests passed!${NC}"
    echo -e "${GREEN}======================================${NC}"
    exit 0
else
    echo ""
    echo -e "${RED}======================================${NC}"
    echo -e "${RED}✗ Some tests failed${NC}"
    echo -e "${RED}======================================${NC}"
    exit 1
fi
