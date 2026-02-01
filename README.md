# LenderMatch AI

**Intelligent Equipment Finance Matching Platform**

A full-stack application that matches business loan applications with eligible lenders based on configurable lending criteria. The system evaluates applications against multiple lender policies and programs, providing detailed match results with fit scores.

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![React](https://img.shields.io/badge/React-19-61DAFB?logo=react)
![TypeScript](https://img.shields.io/badge/TypeScript-5.9-3178C6?logo=typescript)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15-4169E1?logo=postgresql)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker)

---

## 📋 Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Tech Stack](#tech-stack)
- [Prerequisites](#prerequisites)
- [Project Structure](#project-structure)
- [Installation](#installation)
  - [Local Development](#local-development)
  - [Docker Deployment](#docker-deployment)
- [API Documentation](#api-documentation)
- [UI Features](#ui-features)
- [Database Schema](#database-schema)
- [Configuration](#configuration)

---

## Overview

LenderMatch AI is an equipment finance matching platform that:

- **Evaluates** loan applications against multiple lender policies
- **Matches** applicants with eligible lending programs
- **Scores** matches based on fit criteria
- **Explains** why applications qualify or get rejected
- **Persists** applications for historical lookup and re-evaluation

### Key Features

| Feature | Description |
|---------|-------------|
| Multi-Lender Matching | Evaluate applications against all configured lenders simultaneously |
| Program-Level Analysis | Each lender can have multiple programs with different criteria |
| Fit Scoring | Numeric score (0-100) indicating how well an application matches |
| Rejection Reasoning | Detailed explanations for why an application doesn't qualify |
| Application Persistence | Save and retrieve applications by ID |
| Re-evaluation | Re-run saved applications against updated lender policies |
| Validation | Pre-validate applications before submission |
| Statistics Dashboard | View system-wide metrics |

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                              CLIENT (Port 3000)                         │
│                                                                         │
│   ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐   │
│   │  Dashboard  │  │ Application │  │   Results   │  │  Policies   │   │
│   │    View     │  │    Form     │  │    View     │  │    View     │   │
│   └─────────────┘  └─────────────┘  └─────────────┘  └─────────────┘   │
│                                                                         │
│                         React 19 + TypeScript + Vite                    │
│                              Tailwind CSS                               │
└────────────────────────────────────┬────────────────────────────────────┘
                                     │ HTTP/REST
                                     │ (Nginx Proxy in Docker)
                                     ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                              API (Port 8080)                            │
│                                                                         │
│   ┌─────────────────────────────────────────────────────────────────┐   │
│   │                      MatchController                            │   │
│   │  POST /api/match          - Submit application                  │   │
│   │  GET  /api/match/{id}     - Get application by ID               │   │
│   │  POST /api/match/{id}/re-evaluate - Re-evaluate application     │   │
│   │  GET  /api/match/lenders  - Get all lenders with policies       │   │
│   │  POST /api/match/validate - Validate without persisting         │   │
│   │  GET  /api/match/statistics - Get system statistics             │   │
│   └─────────────────────────────────────────────────────────────────┘   │
│                                     │                                   │
│   ┌─────────────────────────────────▼───────────────────────────────┐   │
│   │                      MatchingService                            │   │
│   │  - Feature Derivation (Credit Tier, Business Type, etc.)        │   │
│   │  - Lender-Level Checks (State, Industry restrictions)           │   │
│   │  - Program-Level Checks (FICO, Amount, Time in Business)        │   │
│   │  - Fit Score Calculation                                        │   │
│   └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│                         .NET 8 Web API + EF Core 8                      │
└────────────────────────────────────┬────────────────────────────────────┘
                                     │ Npgsql
                                     ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                           DATABASE (Port 5432)                          │
│                                                                         │
│   ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────────┐    │
│   │ Applications│  │   Lenders   │  │    LendingPrograms          │    │
│   │             │  │             │  │                             │    │
│   │ - Business  │  │ - Name      │  │ - MinFico, MaxAmount        │    │
│   │ - Guarantor │  │ - Restricted│  │ - MinTimeInBusiness         │    │
│   │ - Request   │  │   States[]  │  │ - MinPayNet, MinRevenue     │    │
│   └─────────────┘  └─────────────┘  └─────────────────────────────┘    │
│                                                                         │
│                            PostgreSQL 15                                │
└─────────────────────────────────────────────────────────────────────────┘
```

### Request Flow

1. **User** fills out the application form in the React frontend
2. **Frontend** sends POST request to `/api/match`
3. **API** validates input and derives features (credit tier, business type, etc.)
4. **MatchingService** evaluates application against all lenders and programs
5. **Results** are persisted to database and returned with fit scores
6. **Frontend** displays eligible/ineligible lenders with detailed reasoning

---

## Tech Stack

### Backend
| Technology | Version | Purpose |
|------------|---------|---------|
| .NET | 8.0 | Runtime & SDK |
| ASP.NET Core | 8.0 | Web API Framework |
| Entity Framework Core | 8.0.10 | ORM |
| Npgsql.EntityFrameworkCore.PostgreSQL | 8.0.4 | PostgreSQL Provider |
| PostgreSQL | 15 | Database |

### Frontend
| Technology | Version | Purpose |
|------------|---------|---------|
| React | 19.1.0 | UI Framework |
| TypeScript | 5.9.3 | Type Safety |
| Vite | 7.2.4 | Build Tool |
| Tailwind CSS | 4.1.11 | Styling |
| Axios | 1.13.4 | HTTP Client |

### DevOps
| Technology | Purpose |
|------------|---------|
| Docker | Containerization |
| Docker Compose | Multi-container orchestration |
| Nginx | Reverse proxy & static file serving |

---

## Prerequisites

### For Local Development

| Requirement | Version | Download |
|-------------|---------|----------|
| .NET SDK | 8.0+ | [Download](https://dotnet.microsoft.com/download/dotnet/8.0) |
| Node.js | 20+ | [Download](https://nodejs.org/) |
| PostgreSQL | 15+ | [Download](https://www.postgresql.org/download/) |

### For Docker Deployment

| Requirement | Version | Download |
|-------------|---------|----------|
| Docker Desktop | Latest | [Download](https://www.docker.com/products/docker-desktop) |

---

## Project Structure

```
LenderMatchPlatform/
│
├── docker-compose.yml          # Multi-container Docker setup
├── LenderMatch.slnx            # Solution file
├── README.md                   # This file
│
├── LenderMatch.API/            # Backend (.NET 8 Web API)
│   ├── Controllers/
│   │   └── MatchController.cs  # All 6 API endpoints
│   ├── Services/
│   │   └── MatchingService.cs  # Core matching logic
│   ├── Data/
│   │   ├── AppDbContext.cs     # EF Core DbContext
│   │   └── DbSeeder.cs         # Seed data (lenders, programs)
│   ├── Entities/
│   │   └── Models.cs           # Domain models
│   ├── Migrations/             # EF Core migrations
│   ├── Dockerfile              # API container build
│   ├── Program.cs              # Application entry point
│   ├── appsettings.json        # Production config
│   └── appsettings.Development.json  # Dev config
│
└── client/                     # Frontend (React + TypeScript)
    ├── src/
    │   ├── App.tsx             # Main application component
    │   ├── App.css             # Custom styles
    │   ├── main.tsx            # Entry point
    │   └── index.css           # Tailwind imports
    ├── public/                 # Static assets
    ├── Dockerfile              # Client container build
    ├── nginx.conf              # Nginx configuration
    ├── package.json            # NPM dependencies
    ├── vite.config.ts          # Vite configuration
    ├── tsconfig.json           # TypeScript config
    └── tailwind.config.js      # Tailwind config (if present)
```

---

## Installation

### Local Development

#### 1. Database Setup

Create a PostgreSQL database:

```sql
CREATE DATABASE lendermatch;
```

#### 2. Backend Setup

```powershell
# Navigate to API directory
cd LenderMatch.API

# Restore packages
dotnet restore

# Update connection string in appsettings.Development.json if needed
# Default: Host=localhost;Port=5432;Database=lendermatch;Username=postgres;Password=password

# Run migrations
dotnet ef database update

# Start the API (runs on http://localhost:8080)
dotnet run
```

#### 3. Frontend Setup

```powershell
# Navigate to client directory
cd client

# Install dependencies
npm install

# Start development server (runs on http://localhost:5173)
npm run dev
```

#### 4. Access the Application

- **Frontend**: http://localhost:5173
- **API**: http://localhost:8080
- **API Health Check**: http://localhost:8080/api/match/statistics

---

### Docker Deployment

#### 1. Build and Run

```powershell
# From the root directory (LenderMatchPlatform/)
docker-compose up --build
```

#### 2. Access the Application

| Service | URL |
|---------|-----|
| Frontend | http://localhost:3000 |
| API (via proxy) | http://localhost:3000/api/* |
| API (direct) | http://localhost:8080 |
| Database | localhost:5432 |

#### 3. Stop Containers

```powershell
docker-compose down

# To also remove volumes (database data):
docker-compose down -v
```

---

## API Documentation

### Base URL

| Environment | URL |
|-------------|-----|
| Local Development | `http://localhost:8080` |
| Docker (via Nginx) | `http://localhost:3000` |

### Endpoints

#### 1. Submit Application

**POST** `/api/match`

Submit a new loan application for matching.

**Request Body:**
```json
{
  "business": {
    "businessName": "Acme Construction Inc",
    "industry": "Construction",
    "state": "TX",
    "yearsInBusiness": 8,
    "annualRevenue": 2500000
  },
  "guarantor": {
    "name": "John Smith",
    "ficoScore": 720,
    "hasBankruptcy": false,
    "hasTaxLiens": false,
    "bankruptcyDischargeYears": 0
  },
  "creditProfile": {
    "payNetScore": 685,
    "tradeLineCount": 5,
    "hasComparableDebt": true
  },
  "request": {
    "amount": 150000,
    "termMonths": 60,
    "equipmentType": "Excavator",
    "equipmentYear": 2022,
    "equipmentMileage": null
  }
}
```

**Response:** `200 OK`
```json
{
  "applicationId": 1,
  "evaluatedAt": "2026-02-01T17:33:51.594777Z",
  "isValid": true,
  "validationErrors": [],
  "derivedFeatures": {
    "equipmentAgeYears": 4,
    "businessType": "Construction",
    "isTrucking": false,
    "isMedical": false,
    "isStartup": false,
    "creditTier": "A",
    "hasPayNetScore": true,
    "hasCreditIssues": false,
    "bankruptcyDischargeYears": 0,
    "hasComparableDebt": true,
    "tradeLineCount": 5,
    "loanSizeCategory": "Very Large",
    "equipmentCategory": "Construction Equipment"
  },
  "matches": [
    {
      "lenderName": "Apex Commercial Capital",
      "isEligible": true,
      "qualifiedPrograms": [
        "Standard A Rate",
        "Standard B Rate",
        "Medical A Rate",
        "Medical B Rate",
        "A+ Rate"
      ],
      "bestMatchingProgram": "Standard A Rate",
      "rejectionReasons": [],
      "programMatchReasons": [
        "FICO score 720 exceeds minimum by 20 points.",
        "Time in business 8.0 years exceeds minimum by 3.0 years.",
        "PayNet score 685 exceeds minimum by 25 points."
      ],
      "failurePoint": "",
      "fitScore": 91,
      "evaluatedAt": "2026-02-01T17:33:51.6019893Z"
    },
    {
      "lenderName": "Falcon Equipment Finance",
      "isEligible": true,
      "qualifiedPrograms": [
        "A Credit",
        "B Credit",
        "C Credit",
        "D Credit",
        "E Credit",
        "Trucking A/B"
      ],
      "bestMatchingProgram": "A Credit",
      "rejectionReasons": [],
      "programMatchReasons": [
        "FICO score 720 exceeds minimum by 40 points.",
        "Time in business 8.0 years exceeds minimum by 5.0 years.",
        "PayNet score 685 exceeds minimum by 25 points.",
        "Equipment age 4 years is within acceptable range."
      ],
      "failurePoint": "",
      "fitScore": 90,
      "evaluatedAt": "2026-02-01T17:33:51.6020365Z"
    },
    {
      "lenderName": "Stearns Bank",
      "isEligible": true,
      "qualifiedPrograms": [
        "Tier 2 (With PayNet)",
        "Tier 3 (With PayNet)",
        "Corp Only Tier 2",
        "Corp Only Tier 3",
        "Corp PayNet Tier 3"
      ],
      "bestMatchingProgram": "Tier 2 (With PayNet)",
      "rejectionReasons": [],
      "programMatchReasons": [
        "FICO score 720 exceeds minimum by 10 points.",
        "Time in business 8.0 years exceeds minimum by 5.0 years.",
        "PayNet score 685 exceeds minimum by 10 points."
      ],
      "failurePoint": "",
      "fitScore": 88,
      "evaluatedAt": "2026-02-01T17:33:51.6020174Z"
    },
    {
      "lenderName": "Citizens Bank",
      "isEligible": true,
      "qualifiedPrograms": [
        "Tier 3 Full Financials"
      ],
      "bestMatchingProgram": "Tier 3 Full Financials",
      "rejectionReasons": [],
      "programMatchReasons": [],
      "failurePoint": "",
      "fitScore": 78,
      "evaluatedAt": "2026-02-01T17:33:51.6020745Z"
    },
    {
      "lenderName": "Advantage+ Financing",
      "isEligible": false,
      "qualifiedPrograms": [],
      "bestMatchingProgram": "",
      "rejectionReasons": [
        "Loan amount $150,000 exceeds maximum $75,000 for Standard Non-Trucking Program.",
        "Loan amount $150,000 exceeds maximum $75,000 for Start-Up Program."
      ],
      "programMatchReasons": [],
      "failurePoint": "Program Requirements",
      "fitScore": 0,
      "evaluatedAt": "2026-02-01T17:33:51.6019346Z"
    }
  ],
  "eligibleCount": 4,
  "totalEvaluated": 5
}
```

---

#### 2. Get Application by ID

**GET** `/api/match/{id}`

Retrieve a previously submitted application.

**Response:** `200 OK`
```json
{
  "id": 1,
  "submittedAt": "2026-02-01T10:30:00Z",
  "business": { ... },
  "guarantor": { ... },
  "creditProfile": { ... },
  "request": { ... }
}
```

**Error Response:** `404 Not Found`
```json
{
  "message": "Application not found"
}
```

---

#### 3. Re-evaluate Application

**POST** `/api/match/{id}/re-evaluate`

Re-run matching for an existing application against current lender policies.

**Response:** Same as Submit Application (`MatchingWorkflowResult`)

---

#### 4. Get All Lenders

**GET** `/api/match/lenders`

Retrieve all lenders with their programs and policies.

**Response:** `200 OK`
```json
[
  {
    "id": 1,
    "name": "Advantage+ Financing",
    "programs": [
      {
        "id": 1,
        "lenderId": 1,
        "lender": null,
        "name": "Standard Non-Trucking Program",
        "minAmount": 10000,
        "maxAmount": 75000,
        "minFico": 680,
        "minPayNet": null,
        "minTimeInBusinessYears": 3,
        "minRevenue": null,
        "maxEquipmentAgeYears": null,
        "excludeTrucking": true
      },
      {
        "id": 2,
        "lenderId": 1,
        "lender": null,
        "name": "Start-Up Program",
        "minAmount": 10000,
        "maxAmount": 75000,
        "minFico": 700,
        "minPayNet": null,
        "minTimeInBusinessYears": 0,
        "minRevenue": null,
        "maxEquipmentAgeYears": null,
        "excludeTrucking": true
      }
    ],
    "restrictedIndustries": [
      "Trucking"
    ],
    "restrictedStates": []
  },
  {
    "id": 2,
    "name": "Apex Commercial Capital",
    "programs": [
      {
        "id": 3,
        "lenderId": 2,
        "lender": null,
        "name": "Standard A Rate",
        "minAmount": 10000,
        "maxAmount": 500000,
        "minFico": 700,
        "minPayNet": 660,
        "minTimeInBusinessYears": 5,
        "minRevenue": null,
        "maxEquipmentAgeYears": null,
        "excludeTrucking": true
      },
      {
        "id": 4,
        "lenderId": 2,
        "lender": null,
        "name": "Standard B Rate",
        "minAmount": 10000,
        "maxAmount": 250000,
        "minFico": 670,
        "minPayNet": 650,
        "minTimeInBusinessYears": 3,
        "minRevenue": null,
        "maxEquipmentAgeYears": null,
        "excludeTrucking": true
      },
      {
        "id": 5,
        "lenderId": 2,
        "lender": null,
        "name": "Standard C Rate",
        "minAmount": 10000,
        "maxAmount": 100000,
        "minFico": 640,
        "minPayNet": 640,
        "minTimeInBusinessYears": 2,
        "minRevenue": null,
        "maxEquipmentAgeYears": null,
        "excludeTrucking": true
      },
      {
        "id": 6,
        "lenderId": 2,
        "lender": null,
        "name": "Medical A Rate",
        "minAmount": 10000,
        "maxAmount": 500000,
        "minFico": 700,
        "minPayNet": null,
        "minTimeInBusinessYears": 5,
        "minRevenue": null,
        "maxEquipmentAgeYears": null,
        "excludeTrucking": false
      },
      {
        "id": 7,
        "lenderId": 2,
        "lender": null,
        "name": "Medical B Rate",
        "minAmount": 10000,
        "maxAmount": 250000,
        "minFico": 670,
        "minPayNet": null,
        "minTimeInBusinessYears": 2,
        "minRevenue": null,
        "maxEquipmentAgeYears": null,
        "excludeTrucking": false
      },
      {
        "id": 8,
        "lenderId": 2,
        "lender": null,
        "name": "A+ Rate",
        "minAmount": 10000,
        "maxAmount": 500000,
        "minFico": 720,
        "minPayNet": 670,
        "minTimeInBusinessYears": 5,
        "minRevenue": null,
        "maxEquipmentAgeYears": 5,
        "excludeTrucking": false
      },
      {
        "id": 9,
        "lenderId": 2,
        "lender": null,
        "name": "Corp Only",
        "minAmount": 10000,
        "maxAmount": null,
        "minFico": null,
        "minPayNet": null,
        "minTimeInBusinessYears": 5,
        "minRevenue": 3000000,
        "maxEquipmentAgeYears": null,
        "excludeTrucking": false
      }
    ],
    "restrictedIndustries": [
      "Aircraft/Boats",
      "ATMs",
      "Audio/Visual",
      "Cannabis",
      "Casino/Gambling",
      "Churches/Non-profits",
      "Copiers",
      "Electric Vehicles",
      "Fad Medical",
      "Furniture",
      "Kiosks",
      "Leasehold Improvements",
      "Logging Equipment",
      "Nail Salons",
      "Petroleum Industry (Oil/Gas)",
      "Sale-Leasebacks",
      "Signage",
      "Tanning Beds",
      "Trucking (Local & Long Haul)"
    ],
    "restrictedStates": [
      "CA",
      "NV",
      "ND",
      "VT"
    ]
  },
  {
    "id": 5,
    "name": "Citizens Bank",
    "programs": [
      {
        "id": 27,
        "lenderId": 5,
        "lender": null,
        "name": "Tier 3 Full Financials",
        "minAmount": 75000,
        "maxAmount": 1000000,
        "minFico": null,
        "minPayNet": null,
        "minTimeInBusinessYears": null,
        "minRevenue": null,
        "maxEquipmentAgeYears": null,
        "excludeTrucking": false
      },
      {
        "id": 25,
        "lenderId": 5,
        "lender": null,
        "name": "Tier 1 General Program",
        "minAmount": null,
        "maxAmount": 75000,
        "minFico": 700,
        "minPayNet": null,
        "minTimeInBusinessYears": 2,
        "minRevenue": null,
        "maxEquipmentAgeYears": null,
        "excludeTrucking": false
      },
      {
        "id": 26,
        "lenderId": 5,
        "lender": null,
        "name": "Tier 2 Start-up/Non-Homeowner",
        "minAmount": null,
        "maxAmount": 50000,
        "minFico": 700,
        "minPayNet": null,
        "minTimeInBusinessYears": 0,
        "minRevenue": null,
        "maxEquipmentAgeYears": null,
        "excludeTrucking": false
      }
    ],
    "restrictedIndustries": [
      "Cannabis"
    ],
    "restrictedStates": [
      "CA"
    ]
  },
  {
    "id": 4,
    "name": "Falcon Equipment Finance",
    "programs": [
      {
        "id": 22,
        "lenderId": 4,
        "lender": null,
        "name": "D Credit",
        "minAmount": 15000,
        "maxAmount": null,
        "minFico": 680,
        "minPayNet": 660,
        "minTimeInBusinessYears": 3,
        "minRevenue": null,
        "maxEquipmentAgeYears": 15,
        "excludeTrucking": false
      },
      {
        "id": 23,
        "lenderId": 4,
        "lender": null,
        "name": "E Credit",
        "minAmount": 15000,
        "maxAmount": null,
        "minFico": 680,
        "minPayNet": 660,
        "minTimeInBusinessYears": 3,
        "minRevenue": null,
        "maxEquipmentAgeYears": 15,
        "excludeTrucking": false
      },
      {
        "id": 24,
        "lenderId": 4,
        "lender": null,
        "name": "Trucking A/B",
        "minAmount": 15000,
        "maxAmount": null,
        "minFico": 700,
        "minPayNet": 680,
        "minTimeInBusinessYears": 5,
        "minRevenue": null,
        "maxEquipmentAgeYears": 10,
        "excludeTrucking": false
      },
      {
        "id": 19,
        "lenderId": 4,
        "lender": null,
        "name": "A Credit",
        "minAmount": 15000,
        "maxAmount": null,
        "minFico": 680,
        "minPayNet": 660,
        "minTimeInBusinessYears": 3,
        "minRevenue": null,
        "maxEquipmentAgeYears": 15,
        "excludeTrucking": false
      },
      {
        "id": 20,
        "lenderId": 4,
        "lender": null,
        "name": "B Credit",
        "minAmount": 15000,
        "maxAmount": null,
        "minFico": 680,
        "minPayNet": 660,
        "minTimeInBusinessYears": 3,
        "minRevenue": null,
        "maxEquipmentAgeYears": 15,
        "excludeTrucking": false
      },
      {
        "id": 21,
        "lenderId": 4,
        "lender": null,
        "name": "C Credit",
        "minAmount": 15000,
        "maxAmount": null,
        "minFico": 680,
        "minPayNet": 660,
        "minTimeInBusinessYears": 3,
        "minRevenue": null,
        "maxEquipmentAgeYears": 15,
        "excludeTrucking": false
      }
    ],
    "restrictedIndustries": [],
    "restrictedStates": []
  },
  {
    "id": 3,
    "name": "Stearns Bank",
    "programs": [
      {
        "id": 10,
        "lenderId": 3,
        "lender": null,
        "name": "Tier 1 (With PayNet)",
        "minAmount": null,
        "maxAmount": null,
        "minFico": 725,
        "minPayNet": 685,
        "minTimeInBusinessYears": 3,
        "minRevenue": null,
        "maxEquipmentAgeYears": null,
        "excludeTrucking": false
      },
      {
        "id": 11,
        "lenderId": 3,
        "lender": null,
        "name": "Tier 2 (With PayNet)",
        "minAmount": null,
        "maxAmount": null,
        "minFico": 710,
        "minPayNet": 675,
        "minTimeInBusinessYears": 3,
        "minRevenue": null,
        "maxEquipmentAgeYears": null,
        "excludeTrucking": false
      },
      {
        "id": 13,
        "lenderId": 3,
        "lender": null,
        "name": "Corp Only Tier 1",
        "minAmount": null,
        "maxAmount": null,
        "minFico": 735,
        "minPayNet": null,
        "minTimeInBusinessYears": 5,
        "minRevenue": null,
        "maxEquipmentAgeYears": null,
        "excludeTrucking": false
      },
      {
        "id": 12,
        "lenderId": 3,
        "lender": null,
        "name": "Tier 3 (With PayNet)",
        "minAmount": null,
        "maxAmount": null,
        "minFico": 700,
        "minPayNet": 665,
        "minTimeInBusinessYears": 2,
        "minRevenue": null,
        "maxEquipmentAgeYears": null,
        "excludeTrucking": false
      },
      {
        "id": 14,
        "lenderId": 3,
        "lender": null,
        "name": "Corp Only Tier 2",
        "minAmount": null,
        "maxAmount": null,
        "minFico": 720,
        "minPayNet": null,
        "minTimeInBusinessYears": 3,
        "minRevenue": null,
        "maxEquipmentAgeYears": null,
        "excludeTrucking": false
      },
      {
        "id": 15,
        "lenderId": 3,
        "lender": null,
        "name": "Corp Only Tier 3",
        "minAmount": null,
        "maxAmount": null,
        "minFico": 710,
        "minPayNet": null,
        "minTimeInBusinessYears": 2,
        "minRevenue": null,
        "maxEquipmentAgeYears": null,
        "excludeTrucking": false
      },
      {
        "id": 16,
        "lenderId": 3,
        "lender": null,
        "name": "Corp PayNet Tier 1",
        "minAmount": null,
        "maxAmount": null,
        "minFico": null,
        "minPayNet": 700,
        "minTimeInBusinessYears": 10,
        "minRevenue": null,
        "maxEquipmentAgeYears": null,
        "excludeTrucking": false
      },
      {
        "id": 17,
        "lenderId": 3,
        "lender": null,
        "name": "Corp PayNet Tier 2",
        "minAmount": null,
        "maxAmount": null,
        "minFico": null,
        "minPayNet": 690,
        "minTimeInBusinessYears": 5,
        "minRevenue": null,
        "maxEquipmentAgeYears": null,
        "excludeTrucking": false
      },
      {
        "id": 18,
        "lenderId": 3,
        "lender": null,
        "name": "Corp PayNet Tier 3",
        "minAmount": null,
        "maxAmount": null,
        "minFico": null,
        "minPayNet": 680,
        "minTimeInBusinessYears": 5,
        "minRevenue": null,
        "maxEquipmentAgeYears": null,
        "excludeTrucking": false
      }
    ],
    "restrictedIndustries": [
      "Gaming/Gambling",
      "Hazmat",
      "Oil & Gas",
      "MSBs",
      "Adult Entertainment",
      "Non-Essential Use",
      "Weapons/Firearms",
      "Beauty/Tanning Salons",
      "Tattoo/Piercing",
      "Aesthetic",
      "Real Estate",
      "OTR",
      "Restaurants",
      "Car Wash"
    ],
    "restrictedStates": []
  }
]
```

---

#### 5. Validate Application

**POST** `/api/match/validate`

Validate an application without persisting it.

**Request Body:** Same as Submit Application

**Response:** `200 OK`
```json
{
  "isValid": true,
  "errors": [],
  "derivedFeatures": { ... },
  "validatedAt": "2026-02-01T10:30:00Z"
}
```

---

#### 6. Get Statistics

**GET** `/api/match/statistics`

Get system-wide statistics.

**Response:** `200 OK`
```json
{
  "totalApplications": 42,
  "totalLenders": 5,
  "totalPrograms": 12,
  "generatedAt": "2026-02-01T10:30:00Z"
}
```

---

## UI Features

### Navigation Tabs

| Tab | Description |
|-----|-------------|
| **Dashboard** | System statistics and quick actions |
| **New Application** | Submit a new loan application |
| **Results** | View matching results (enabled after submission) |
| **Lookup** | Find and re-evaluate past applications by ID |
| **Lender Policies** | Browse all lenders and their lending criteria |

### Dashboard

- Total applications submitted
- Total lenders and programs in the system
- Quick action to submit new application

### Application Form

Four sections with comprehensive input fields:

1. **Business Information**
   - Business Name, Industry, State
   - Years in Business, Annual Revenue

2. **Personal Guarantor**
   - Guarantor Name, FICO Score
   - Bankruptcy and Tax Lien flags

3. **Business Credit Profile**
   - PayNet Score (optional)
   - Trade Line Count
   - Comparable Debt flag

4. **Loan Request**
   - Amount, Term (months)
   - Equipment Type, Year, Mileage

**Actions:**
- **Validate First** - Check application validity without submitting
- **Find Matching Lenders** - Submit and get matches

### Results View

- **Match Summary Table** - Application ID, evaluation timestamp, eligible/ineligible counts, credit tier, best fit score
- **Application Summary** - Business, industry, loan amount, equipment details
- **Derived Features** - System-calculated values (business type, loan category, equipment age)
- **Eligible Lenders** - Expandable cards showing:
  - Fit score
  - Qualified programs (with best match highlighted)
  - Why you qualify (match reasons)
  - Criteria breakdown table
- **Ineligible Lenders** - Expandable cards showing:
  - Failure point
  - Rejection reasons
  - What's needed to qualify (suggestions)

### Application Lookup

- Enter application ID to retrieve historical submission
- Automatically re-evaluates against current lender policies
- Useful for checking if policy changes affect eligibility

### Lender Policies

- Left panel: List of all lenders with program count
- Right panel: Selected lender details
  - Restricted states (table format)
  - Restricted industries (scrollable table)
  - Programs with all criteria (Min/Max amounts, FICO, PayNet, etc.)

---

## Database Schema

### Tables

#### Applications
| Column | Type | Description |
|--------|------|-------------|
| Id | int | Primary key |
| SubmittedAt | datetime | Submission timestamp |
| BusinessName | varchar | Business name |
| Industry | varchar | Business industry |
| State | varchar(2) | US state code |
| YearsInBusiness | decimal | Years of operation |
| AnnualRevenue | decimal | Annual revenue |
| GuarantorName | varchar | Personal guarantor name |
| FicoScore | int | FICO credit score |
| HasBankruptcy | boolean | Bankruptcy flag |
| HasTaxLiens | boolean | Tax liens flag |
| BankruptcyDischargeYears | int | Years since discharge |
| PayNetScore | int? | PayNet score (nullable) |
| TradeLineCount | int | Number of trade lines |
| HasComparableDebt | boolean | Comparable debt flag |
| Amount | decimal | Requested loan amount |
| TermMonths | int | Loan term in months |
| EquipmentType | varchar | Type of equipment |
| EquipmentYear | int | Equipment model year |
| EquipmentMileage | int? | Mileage (nullable) |

#### Lenders
| Column | Type | Description |
|--------|------|-------------|
| Id | int | Primary key |
| Name | varchar | Lender name |
| RestrictedStates | text[] | Array of restricted state codes |
| RestrictedIndustries | text[] | Array of restricted industries |

#### LendingPrograms
| Column | Type | Description |
|--------|------|-------------|
| Id | int | Primary key |
| LenderId | int | Foreign key to Lenders |
| Name | varchar | Program name |
| MinAmount | decimal? | Minimum loan amount |
| MaxAmount | decimal? | Maximum loan amount |
| MinFico | int? | Minimum FICO score |
| MinPayNet | int? | Minimum PayNet score |
| MinTimeInBusinessYears | decimal? | Minimum years in business |
| MinRevenue | decimal? | Minimum annual revenue |
| MaxEquipmentAgeYears | int? | Maximum equipment age |
| ExcludeTrucking | boolean | Exclude trucking industry |

---

## Configuration

### Backend Configuration

**appsettings.json** / **appsettings.Development.json**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=lendermatch;Username=postgres;Password=password"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### Frontend Configuration

**API Base URL** (in `App.tsx`):

```typescript
const API_BASE_URL = import.meta.env.PROD 
  ? ''                       // Docker: Use relative path (nginx proxy)
  : 'http://localhost:8080'; // Local Dev: Direct to backend
```

### Docker Environment Variables

Set in `docker-compose.yml`:

```yaml
api:
  environment:
    - ASPNETCORE_ENVIRONMENT=Development
    - ConnectionStrings__DefaultConnection=Host=db;Port=5432;Database=lendermatch;Username=postgres;Password=password
```

---

## License

MIT License - See LICENSE file for details.

---

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

## Support

For issues and feature requests, please open a GitHub issue.
