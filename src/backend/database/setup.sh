#!/bin/bash

# Product Intelligence Platform - Database Setup Script
# This script creates the database and runs all migrations

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration (can be overridden with environment variables)
DB_NAME="${DB_NAME:-product_intelligence}"
DB_USER="${DB_USER:-postgres}"
DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-5432}"

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Product Intelligence Platform${NC}"
echo -e "${GREEN}Database Setup${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""

# Check if PostgreSQL is running
echo -e "${YELLOW}Checking PostgreSQL connection...${NC}"
if ! pg_isready -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" > /dev/null 2>&1; then
    echo -e "${RED}Error: Cannot connect to PostgreSQL at $DB_HOST:$DB_PORT${NC}"
    echo "Please ensure PostgreSQL is running and credentials are correct."
    exit 1
fi
echo -e "${GREEN}✓ PostgreSQL is running${NC}"
echo ""

# Check if database exists
echo -e "${YELLOW}Checking if database exists...${NC}"
if psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -lqt | cut -d \| -f 1 | grep -qw "$DB_NAME"; then
    echo -e "${YELLOW}Database '$DB_NAME' already exists${NC}"
    read -p "Do you want to drop and recreate it? (y/N): " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        echo -e "${YELLOW}Dropping database '$DB_NAME'...${NC}"
        dropdb -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" "$DB_NAME" --if-exists
        echo -e "${GREEN}✓ Database dropped${NC}"
    else
        echo -e "${YELLOW}Skipping database creation${NC}"
    fi
fi

# Create database if it doesn't exist
if ! psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -lqt | cut -d \| -f 1 | grep -qw "$DB_NAME"; then
    echo -e "${YELLOW}Creating database '$DB_NAME'...${NC}"
    createdb -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" "$DB_NAME"
    echo -e "${GREEN}✓ Database created${NC}"
fi
echo ""

# Run migrations
echo -e "${YELLOW}Running migrations...${NC}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MIGRATIONS_DIR="$SCRIPT_DIR/migrations"

if [ ! -d "$MIGRATIONS_DIR" ]; then
    echo -e "${RED}Error: Migrations directory not found at $MIGRATIONS_DIR${NC}"
    exit 1
fi

# Execute each migration file
for migration in "$MIGRATIONS_DIR"/*.sql; do
    if [ -f "$migration" ]; then
        echo -e "  Running $(basename "$migration")..."
        if psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -f "$migration" > /dev/null 2>&1; then
            echo -e "${GREEN}  ✓ $(basename "$migration") completed${NC}"
        else
            echo -e "${RED}  ✗ $(basename "$migration") failed${NC}"
            exit 1
        fi
    fi
done
echo ""

# Verify installation
echo -e "${YELLOW}Verifying installation...${NC}"

# Check pgvector extension
if psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -tAc "SELECT 1 FROM pg_extension WHERE extname = 'vector'" | grep -q 1; then
    echo -e "${GREEN}✓ pgvector extension installed${NC}"
else
    echo -e "${RED}✗ pgvector extension not found${NC}"
fi

# Check tables
TABLE_COUNT=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -tAc "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE'")
echo -e "${GREEN}✓ $TABLE_COUNT tables created${NC}"

# Check functions
FUNCTION_COUNT=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -tAc "SELECT COUNT(*) FROM pg_proc WHERE proname LIKE 'fn_%'")
echo -e "${GREEN}✓ $FUNCTION_COUNT functions created${NC}"

# Check indexes
INDEX_COUNT=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -tAc "SELECT COUNT(*) FROM pg_indexes WHERE schemaname = 'public'")
echo -e "${GREEN}✓ $INDEX_COUNT indexes created${NC}"
echo ""

# Generate connection string
CONNECTION_STRING="Host=$DB_HOST;Port=$DB_PORT;Database=$DB_NAME;Username=$DB_USER;Password=YOUR_PASSWORD_HERE"

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Setup Complete!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo -e "Update your connection string in appsettings.json or user secrets:"
echo -e "${YELLOW}$CONNECTION_STRING${NC}"
echo ""
echo -e "To set via user secrets (recommended):"
echo -e "${YELLOW}cd src/backend/src/ProductIntelligence.API${NC}"
echo -e "${YELLOW}dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"$CONNECTION_STRING\"${NC}"
echo ""
echo -e "${YELLOW}cd ../ProductIntelligence.Workers${NC}"
echo -e "${YELLOW}dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"$CONNECTION_STRING\"${NC}"
echo ""
