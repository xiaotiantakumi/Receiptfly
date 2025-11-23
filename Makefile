# ポート番号を変数として定義
VITE_PORT := 5173
SWA_PORT := 4280
API_PORT := 7071
PROCESSING_FUNC_PORT := 7072

.PHONY: dev-backend-api dev-backend-worker dev-frontend dev-frontend-vite dev-frontend-swa test-e2e build up down setup start start-swa clean help

# Default target
help:
	@echo "Available commands:"
	@echo "  make start      - Start all components (Vite, Processing Function, SWA CLI)"
	@echo "  make start-swa  - Same as 'make start' (alias for consistency)"
	@echo "  make start-traditional - Start without SWA CLI (Vite + Functions only)"
	@echo "  make vite       - Start only Vite dev server"
	@echo "  make swa        - Start only SWA CLI (assumes Vite is running)"
	@echo "  make clean      - Clean up existing processes"
	@echo "  make test-e2e   - Run E2E tests"

# Start with SWA CLI (recommended)
start-swa: clean
	@echo "Starting Vite dev server, Processing Function, and SWA CLI..."
	@cd frontend/receiptfly-web && npm run dev & \
	cd backend/Receiptfly.ProcessingFunc && func start --port $(PROCESSING_FUNC_PORT) > processing-func.log 2>&1 & \
	sleep 3 && \
	swa start --config swa-cli.config.json --configuration receiptfly

# Start all components (with SWA CLI)
start: clean
	@echo "Starting all components: Vite, Processing Function, and SWA CLI..."
	@cd frontend/receiptfly-web && npm run dev & \
	cd backend/Receiptfly.ProcessingFunc && func start --port $(PROCESSING_FUNC_PORT) > processing-func.log 2>&1 & \
	sleep 3 && \
	swa start --config swa-cli.config.json --configuration receiptfly

# Traditional start (without SWA) - for backward compatibility
start-traditional: clean
	make -j 3 dev-backend-api dev-backend-worker dev-frontend-vite

clean:
	@echo "Killing processes on ports $(VITE_PORT), $(SWA_PORT), $(API_PORT), and $(PROCESSING_FUNC_PORT)..."
	@lsof -ti:$(VITE_PORT) | xargs kill -9 2>/dev/null || true
	@lsof -ti:$(SWA_PORT) | xargs kill -9 2>/dev/null || true
	@lsof -ti:$(API_PORT) | xargs kill -9 2>/dev/null || true
	@lsof -ti:$(PROCESSING_FUNC_PORT) | xargs kill -9 2>/dev/null || true
	-pkill -f "vite" 2>/dev/null || true
	-pkill -f "func start" 2>/dev/null || true
	-pkill -f "swa" 2>/dev/null || true
	@echo "Cleaned up existing processes"

dev-backend-api:
	cd backend/Receiptfly.Functions && func start --port $(API_PORT)

dev-backend-worker:
	cd backend/Receiptfly.ProcessingFunc && func start --port $(PROCESSING_FUNC_PORT)

dev-backend:
	make -j 2 dev-backend-api dev-backend-worker

dev-frontend-vite:
	cd frontend/receiptfly-web && npm run dev

dev-frontend-swa:
	@echo "Killing processes on ports $(SWA_PORT) and $(API_PORT)..."
	@lsof -ti:$(SWA_PORT) | xargs kill -9 2>/dev/null || true
	@lsof -ti:$(API_PORT) | xargs kill -9 2>/dev/null || true
	swa start --config swa-cli.config.json --configuration receiptfly

# Alias for backward compatibility
dev-frontend: dev-frontend-vite
swa: dev-frontend-swa
vite: dev-frontend-vite

test-e2e:
	cd frontend/receiptfly-web && npm run test:e2e

build:
	dotnet build backend/Receiptfly.Functions

up:
	docker compose up -d

down:
	docker compose down

setup:
	cd frontend/receiptfly-web && npm install
	dotnet restore backend/Receiptfly.Functions
