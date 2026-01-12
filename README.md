ADDPerformance - Data Management System

A robust ASP.NET Core MVC and Web API solution designed for financial data tracking, automated CSV processing, and performance analytics.
 Key Features
 Multi-Module Performance Tracking

    Revenue USD: Tracks Current Year vs. Last Year performance with automated "Against Target" (AT) calculations.

    Online Sales: Management of digital sales metrics.

    ByTourCodes: Granular tracking of sales performance by specific tour identifiers and corporate types.

    Date Master: A centralized "Source of Truth" for time-series data, ensuring all modules align on a monthly reporting grain.

Technical Improvements & Best Practices

    Data Normalization: Implemented an automated date-normalization engine that forces all transactional data to the 1st of the month. This ensures seamless SQL joins and prevents reporting fragmentation.

    Smart CSV Processing (Upsert Logic): The upload engine is designed to be "Idempotent." It checks for existing records based on unique keys (Date/Code) and automatically decides whether to Update an existing entry or Insert a new one, preventing data duplication.

    Audit Trail & Security: Integrated with ASP.NET Core Identity. Every record is automatically stamped with CreatedAt, CreatedBy, UpdatedAt, and UpdatedBy to maintain a strict audit log.

    Soft Deletion: Implemented a Status-based deletion system (Active/Inactive), ensuring historical data integrity while keeping the UI clean.

    Calculation Engine: Centralized logic for calculating percentages and growth metrics to ensure consistency between manual UI entries and bulk API uploads.

Tech Stack

    Framework: ASP.NET Core 7.0/8.0 (MVC & Web API)

    Database: SQL Server via Entity Framework Core

    Identity: Microsoft Identity for Authentication & Authorization

       Getting Started
Prerequisites

    .NET SDK (latest version)

    SQL Server (LocalDB or Express)

Installation

    Clone the repo:git clone https://github.com/YourUsername/ADDPerformance.git

Update Connection String: Modify appsettings.json with your local database credentials.

Run Migrations:
    dotnet ef database update

Run the application:
    dotnet run

Running Tests
    dotnet test

Project Structure
Plaintext

Controllers/      # MVC and API Controllers with logic separation
Models/           # Domain entities and Data Transfer Objects (DTOs)
Data/             # DBContext and Seed data
FileStore/        # Storage for CSV upload templates
Views/            # Responsive Razor templates
 
 API Documentation
 
 The system provides a centralized API for data management across various financial modules.
 
 ### Centralized File Processing
 - **Home Controller** (`/api/Home`)
   - `POST /api/Home/uploadFile`: Route uploads to specific services based on `FileTypeChoice`.
     - `1`: ADD_CK
     - (Other types implemented via direct controller endpoints)
 
 ### Module-Specific Endpoints
 
 #### 1. Revenue USD (`/api/RevUsd`)
 - **Purpose**: Tracks revenue performance (CY vs LY).
 - `GET /api/RevUsd`: List all active records.
 - `POST /api/RevUsd/upload`: Bulk import via CSV with automated normalization and calculation.
 - `POST/PUT/DELETE`: Standard CRUD operations for granular control.
 
 #### 2. Online Sales (`/api/OnlineSales`)
 - **Purpose**: Management of digital sales metrics.
 - `GET /api/OnlineSales`: Retrieve performance data.
 - `POST /api/OnlineSales/upload`: Bulk CSV processing with upsert logic.
 
 #### 3. Corporate Sales (`/api/CorporateSales`)
 - **Purpose**: Tracking sales performance across different corporate segments.
 - `GET /api/CorporateSales`: List active corporate sales data.
 - `POST /api/CorporateSales/upload`: CSV upload engine for bulk updates.
 
 #### 4. ByTourCodes (`/api/ByTourCodes`)
 - **Purpose**: Granular tracking by tour identifier and corporate type.
 - `GET /api/ByTourCodes`: List performance by code.
 - `POST /api/ByTourCodes`: Create new tracking entry.
 - `PUT/DELETE`: Management of specific code records.
 
 #### 5. Date Master (`/api/DateMasters`)
 - **Purpose**: The system's "Source of Truth" for reporting periods.
 - `GET /api/DateMasters`: Retrieve all reporting months/years.
 - `POST /api/DateMasters`: Define a new reporting period.
 
 #### 6. ADD_CK (`/api/AddCk`)
 - **Purpose**: Comprehensive financial check-off tracking.
 - `GET /api/AddCk`: List active check-off records.
 - `POST /api/AddCk/upload`: Automated CSV importer.
 
 ### CSV Format Guidelines
 Most upload endpoints expect a CSV with the following columns:
 `Date (e.g., JAN-2024), CYValue, LYValue, Target`
 
 The system automatically:
 - Normalizes dates to the 1st of the month.
 - Calculates "Against Target" (AT%) and "Against Last Year" (ALY) metrics.
 - Stamps records with the current user's identification for auditing.

Security & Identity Architecture

The system implements a robust security layer using ASP.NET Core Identity, ensuring that financial data is only accessible to authorized personnel.
 Authentication & Authorization

    Identity Framework: Uses Microsoft Identity with Entity Framework Core to manage users, passwords, and security claims.

    Role-Based Access Control (RBAC): Access to critical actions (like CSV uploads or record deletion) is restricted based on user roles and identity claims.

    Anti-Forgery Protection: All state-changing operations (POST/PUT/DELETE) are protected by ValidateAntiForgeryToken attributes to prevent Cross-Site Request Forgery (CSRF) attacks.

Comprehensive Auditing

Every data point in the system is tracked through a built-in audit engine. This is crucial for financial compliance and accountability:

    Creator Tracking: Automatically captures the UserName or Email of the user who initially imported or created a record.

    Modification Logs: Updates the UpdatedBy and UpdatedAt timestamps whenever a record is modified via the UI or the API.

    System Fallbacks: API-driven imports that occur without a direct user context are intelligently tagged as API_System to maintain audit trail continuity.

Secure Data Handling

    Password Hashing: Passwords are never stored in plain text; they use industry-standard PBKDF2 hashing.

    Data Detachment: The system utilizes .AsNoTracking() and manual entity detaching during updates to prevent database concurrency issues and unintended data leakage between requests.