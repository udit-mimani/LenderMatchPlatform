# Design Decisions

This document outlines the key architectural and implementation decisions made during the development of LenderMatch AI.

---

## 1. Lender Requirements Prioritized

### Core Matching Criteria (Implemented)

| Requirement | Priority | Rationale |
|-------------|----------|-----------|
| **FICO Score** | Critical | Primary credit indicator; every lender uses this as a baseline filter |
| **Loan Amount Range** | Critical | Fundamental to program eligibility; determines tier placement |
| **Time in Business** | High | Key risk indicator; separates startups from established businesses |
| **State Restrictions** | High | Legal/licensing requirements; non-negotiable hard stops |
| **Industry Restrictions** | High | Risk-based exclusions; critical for compliance |
| **PayNet Score** | Medium | Business credit indicator; important but not universally required |
| **Annual Revenue** | Medium | Capacity to repay; used by most commercial lenders |
| **Equipment Age** | Medium | Collateral valuation factor; affects program eligibility |
| **Trucking Exclusion** | Low | Specialized flag for lenders avoiding transportation sector |

### Evaluation Order

The matching engine evaluates in a deliberate order to fail fast on hard stops:

```
1. Lender-Level Checks (fail entire lender)
   ├── State Restriction Check
   └── Industry Restriction Check

2. Program-Level Checks (fail individual program)
   ├── Loan Amount Range
   ├── FICO Minimum
   ├── PayNet Minimum (if required)
   ├── Time in Business
   ├── Annual Revenue
   ├── Equipment Age
   └── Trucking Exclusion
```

This order ensures:
- Geographic/industry restrictions are checked first (fastest disqualification)
- Credit checks happen before financial capacity checks
- Equipment-specific checks happen last

---

## 2. Simplifications Made

### 2.1 Single-Guarantor Model

**Decision:** Support only one personal guarantor per application.

**Why:** 
- Most small business equipment loans have a single primary guarantor
- Multi-guarantor logic adds complexity in aggregating scores and handling partial guarantees
- Simplifies the UI form and validation

**Trade-off:** Cannot model co-signer scenarios or partnerships with multiple owners.

---

### 2.2 Simple Industry Matching

**Decision:** Use case-insensitive substring matching for industry restrictions.

**Implementation:**
```csharp
restrictedIndustries.Any(ri => 
    industry.Contains(ri, StringComparison.OrdinalIgnoreCase))
```

**Why:**
- Avoids need for NAICS/SIC code lookups
- Handles variations like "Trucking" matching "Long-haul Trucking"
- No external data dependencies

**Trade-off:** May produce false positives (e.g., "Gaming" matching "Gaming Software Development").

---

### 2.3 Linear Fit Score Calculation

**Decision:** Use a simple weighted scoring model.

**Current Algorithm:**
```
Base Score: 50
+ 20 if FICO ≥ 700
+ 15 if PayNet ≥ 680
+ 10 if Years in Business ≥ 5
+ 5 if Has Comparable Debt
```

**Why:**
- Transparent and explainable
- Easy to tune weights
- No ML model training required

**Trade-off:** Doesn't capture complex interactions between factors.

---

### 2.4 In-Memory Evaluation

**Decision:** Load all lenders and programs into memory for each evaluation.

**Why:**
- Lender count is small (typically < 50)
- Avoids complex query optimization
- Simplifies matching logic

**Trade-off:** Won't scale to thousands of lenders without caching layer.

---

### 2.5 No Authentication/Authorization

**Decision:** API is open without authentication.

**Why:**
- Focus on core matching functionality
- Reduces setup complexity for demo/evaluation
- Auth is typically handled by API gateway in production

**Trade-off:** Not production-ready without adding auth layer.

---

### 2.6 Synchronous Processing

**Decision:** All matching happens synchronously in the request lifecycle.

**Why:**
- Matching is fast (< 100ms for typical workloads)
- Simplifies error handling
- Immediate feedback to users

**Trade-off:** Long-running evaluations could timeout with many lenders.

---

### 2.7 Static Seed Data

**Decision:** Lenders and programs are seeded on startup from code.

**Why:**
- Ensures consistent demo data
- No admin UI needed for initial setup
- Easy to version control lender policies

**Trade-off:** Requires code deployment to change lender policies.

---

### 2.8 PostgreSQL Arrays for Restrictions

**Decision:** Store `RestrictedStates` and `RestrictedIndustries` as PostgreSQL `text[]` arrays.

**Why:**
- Native PostgreSQL feature with good performance
- Cleaner than junction tables for simple string lists
- Simplifies queries with `ANY()` operator

**Trade-off:** Less normalized; harder to query "which lenders restrict state X?"

---

### 2.9 Single-File React Component

**Decision:** Keep all UI components in a single `App.tsx` file.

**Why:**
- Faster iteration during development
- Easy to understand complete application flow
- No prop-drilling complexity across files

**Trade-off:** File is ~1100 lines; should be split for maintainability.

---

### 2.10 Client-Side State Only

**Decision:** No global state management (Redux, Zustand, etc.).

**Why:**
- Application state is simple and localized
- React's useState is sufficient
- Reduces bundle size and complexity

**Trade-off:** State resets on page refresh; no persistence.

---

## 3. What I Would Add With More Time

### 3.1 High Priority

| Feature | Description | Effort |
|---------|-------------|--------|
| **Admin Panel** | CRUD interface for lenders and programs | 2-3 days |
| **User Authentication** | JWT-based auth with role-based access | 1-2 days |
| **Application History** | List all past applications with filtering/sorting | 1 day |
| **Export to PDF** | Generate professional match reports | 1 day |
| **Email Notifications** | Send match results to applicant/broker | 1 day |

---

### 3.2 Matching Engine Enhancements

| Feature | Description | Benefit |
|---------|-------------|---------|
| **Weighted Scoring Config** | Store fit score weights in database | Tune without code changes |
| **Custom Program Rules** | Support complex AND/OR logic for criteria | Handle edge cases |
| **Soft vs Hard Requirements** | Distinguish "must have" vs "nice to have" | Better match explanations |
| **Historical Rate Data** | Include estimated rates based on profile | More actionable results |
| **Lender Capacity/Appetite** | Factor in current lending volume | More realistic matches |

---

### 3.3 Technical Improvements

| Improvement | Description |
|-------------|-------------|
| **Split React Components** | Separate files for each view component |
| **Add Unit Tests** | Jest/Vitest for frontend, xUnit for backend |
| **Add Integration Tests** | Test API endpoints with test database |
| **Caching Layer** | Redis cache for lender data |
| **Rate Limiting** | Prevent API abuse |
| **Request Validation** | FluentValidation for complex rules |
| **Logging/Monitoring** | Structured logging with Serilog, APM integration |
| **CI/CD Pipeline** | GitHub Actions for automated testing and deployment |

---

### 3.4 UX Enhancements

| Enhancement | Description |
|-------------|-------------|
| **Form Persistence** | Save draft applications to localStorage |
| **Comparison View** | Side-by-side comparison of eligible lenders |
| **What-If Analysis** | Adjust inputs to see how eligibility changes |
| **Mobile Responsive** | Optimize layouts for tablet/mobile |
| **Dark/Light Theme Toggle** | User preference for theme |
| **Keyboard Navigation** | Accessibility improvements |

---

### 3.5 Data & Analytics

| Feature | Description |
|---------|-------------|
| **Match Analytics** | Dashboard showing approval rates by lender |
| **Trend Analysis** | Track how match rates change over time |
| **A/B Testing** | Test different scoring algorithms |
| **Lender Performance** | Which lenders convert most often |

---

## 4. Known Limitations

| Limitation | Impact | Workaround |
|------------|--------|------------|
| No multi-guarantor support | Can't model co-signers | Use primary guarantor only |
| Simple industry matching | Possible false positives | Manual review of matches |
| No document upload | Can't attach financials | External document management |
| No real-time lender updates | Stale policies possible | Redeploy with new seed data |
| Single database | No read replicas | Sufficient for current scale |

---

## 5. Architecture Decisions Record (ADR)

### ADR-001: PostgreSQL over SQL Server

**Context:** Need a relational database for structured loan data.

**Decision:** Use PostgreSQL 15.

**Rationale:**
- Open source, no licensing costs
- Native array types for restrictions
- Excellent Docker support
- Strong .NET support via Npgsql

---

### ADR-002: Monolithic API over Microservices

**Context:** How to structure the backend services.

**Decision:** Single ASP.NET Core Web API.

**Rationale:**
- Matching logic is tightly coupled
- No need for independent scaling of components
- Simpler deployment and debugging
- Can extract services later if needed

---

### ADR-003: Vite over Create React App

**Context:** Need a build tool for React frontend.

**Decision:** Use Vite 7.

**Rationale:**
- Much faster HMR (Hot Module Replacement)
- Better TypeScript support
- Modern ESM-based architecture
- Smaller bundle sizes

---

### ADR-004: Tailwind over Component Libraries

**Context:** How to style the UI.

**Decision:** Use Tailwind CSS 4.

**Rationale:**
- Full design control
- No component library lock-in
- Smaller bundle (only used classes)
- Rapid prototyping with utility classes

---

## 6. Conclusion

The current implementation prioritizes:

1. **Core functionality** over edge cases
2. **Simplicity** over flexibility
3. **Speed of development** over comprehensive features
4. **Explainability** over optimization

These trade-offs are appropriate for an MVP/demo phase. Production deployment would require addressing the limitations outlined in Section 3.
