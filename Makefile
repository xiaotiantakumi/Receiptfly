.PHONY: dev-backend dev-frontend test-e2e build up down setup start

start:
	make -j 2 dev-backend dev-frontend

dev-backend:
	cd backend/Receiptfly.Functions && func start

dev-frontend:
	cd frontend/receiptfly-web && npm run dev

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
