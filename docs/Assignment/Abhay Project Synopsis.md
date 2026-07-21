## PROJECT SYNOPSIS

---

**Submitted by:** Abhay Kumar Mandal

**Course:** Master of Computer Science

**Multi-Tenant Client Management and Billing System Using Blazor and ASP.NET Core**

**Keywords:** Multi-tenancy; Blazor Server; Invoice Management; Clean Architecture; Nepali Fiscal Year; Entity Framework Core; PostgreSQL

---

### Introduction

Small and medium enterprises (SMEs) and accounting firms in Nepal and similar developing markets manage clients, projects, invoices, and payment receipts through a combination of spreadsheet applications, paper-based ledgers, and generic office productivity tools. This fragmented approach engenders duplicated data entry, loss of transactional records, delayed payment cycles, and an absence of real-time visibility into overall business performance. As client portfolios expand, manual tracking becomes increasingly error-prone and time-consuming, directly impacting cash flow predictability and customer relationship management.

#### Background

Contemporary cloud-based accounting platforms such as Tally, Zoho Books, and QuickBooks offer robust billing functionality; however, they are typically priced for larger enterprise deployments, necessitate persistent internet connectivity, and do not accommodate the specific regulatory and cultural requirements of the Nepali business ecosystem. Among the critical gaps are the absence of native support for the Bikram Sambat (B.S.) calendar system, the lack of PAN-based billing with statutory 13% VAT computation, and the inability to render financial amounts in the Nepali numbering system. Consequently, there exists a pronounced gap in the market for a solution that unifies multi-tenant architecture, granular role-based access control, and localized billing features within a single, self-hostable software platform.

The proposed system — SunyaSuite — addresses this gap by providing a comprehensive, multi-tenant billing ecosystem. Organizations can manage their client portfolios, project engagements, invoice lifecycles, and payment receipts through a modern, interactive web interface. The system automatically monitors overdue invoices, generates professional-grade PDF invoices and receipts, provides a color-coded traffic-light visualization of client account health, and exports data to spreadsheet format for external analysis. Built on the .NET 10 framework with Blazor for rich client interactivity and PostgreSQL for transactional data integrity, SunyaSuite delivers a production-ready platform tailored for Nepali businesses and accounting professionals seeking a localized, self-hosted billing infrastructure.

---

### Problem Statement

The billing and client management solutions presently available to Nepali SMEs suffer from several critical limitations. Spreadsheet-based approaches lack centralized data storage, rendering it difficult to maintain a coherent history of invoice transactions, client payment behaviour, or project progress across multiple concurrent users. Generic cloud accounting platforms, while feature-rich in their target markets, do not support the Bikram Sambat calendar for fiscal year definition, lack native computation of 13% VAT for PAN-based invoices, and cannot render currency amounts in the Nepali word format required for statutory documents. Furthermore, most commercial solutions operate on per-seat subscription pricing that becomes economically prohibitive for organizations managing multiple client entities under a single organizational umbrella.

Organizations must also dedicate substantial manual effort to tracking invoice due dates and following up with clients regarding overdue payments — a process that is both labour-intensive and susceptible to oversight. There exists no automated mechanism to escalate delinquent accounts or to provide a real-time, at-a-glance summary of client payment health across the entire portfolio.

*There is a need for an automated, centralized, multi-tenant billing system that supports Nepali fiscal years, VAT-compliant invoicing, and automated overdue tracking while remaining affordable and self-hostable for small and medium enterprises.*

---

### General Objective

To develop a comprehensive, multi-tenant client management and billing system that streamlines invoicing, payment tracking, and client health monitoring for Nepali businesses.

### Specific Objectives

1. To design and implement a multi-tenant database architecture that isolates each organization's data securely while maintaining a shared configuration store.
2. To develop a complete invoicing module supporting both VAT (13%) and non-VAT bill types with Nepali fiscal year numbering and auto-generated invoice sequences.
3. To implement a payment receipt system that allocates received amounts across multiple invoices and automatically updates invoice payment status.
4. To create a traffic-light client status monitoring system that flags overdue accounts (red), near-due accounts (yellow), and healthy accounts (green) in real time using invoice payment data.
5. To generate professional PDF invoices and receipts incorporating seller and buyer information, line-item detail tables, VAT breakdowns, and Nepali currency word conversion.
6. To implement role-based access control with distinct system-level (administrator, staff) and organization-level (owner, administrator, member) authorization policies.

---

### Scope of the Project

SunyaSuite is a web-based, multi-tenant billing and client management platform. The system encompasses the complete lifecycle of client engagement: client registration with traffic-light health tracking, project assignment with progress monitoring, invoice generation with VAT and non-VAT billing options, payment receipting with multi-invoice allocation, automated overdue detection and email notification, and audit logging of all data mutations. The system supports both the Gregorian (AD) and Nepali Bikram Sambat (BS) calendar systems for fiscal year management.

**Included:**

- Multi-tenant organization management with per-tenant database isolation.
- Company and branch entity management within each organization.
- Full client CRUD with cascading soft-delete across related projects and invoices.
- Invoice creation, editing, status lifecycle management (Draft, Sent, Paid, Overdue), and soft-delete.
- Money receipt creation with invoice allocation and automatic payment tracking.
- PDF document generation for invoices and receipts using the QuestPDF library.
- Dashboard with revenue trend charts, status distribution breakdowns, and key performance indicators.
- Background service for scheduled overnight overdue invoice processing.
- Excel data export for clients, projects, invoices, and summary reports.
- Audit logging for all entity mutations with filterable retrieval.
- Role-based access control (SystemAdmin, SystemStaff, OrgOwner, OrgAdmin, OrgMember).

**Excluded:**

- Mobile application (iOS/Android) — the system is designed for desktop browser access.
- Real-time payment gateway integration — payments are recorded manually as receipts.
- Inventory or stock management functionality.
- Payroll or employee management.
- Direct bank reconciliation or general ledger integration.

**Intended Users:**

- System administrators responsible for managing organizations and user accounts.
- Organization owners and administrators who configure companies, branches, and fiscal years.
- Organization members (staff) who perform day-to-day client, project, invoice, and receipt management.

---

### Significance of the Project

**For End-Users (Business Staff):** The system eliminates duplicated manual data entry by centralizing all client, project, invoice, and receipt data within a single authoritative platform. Users can generate invoices and receipts with minimal input, track payment status in real time, and assess client account health through the intuitive traffic-light dashboard. Automated overdue processing reduces the cognitive burden of tracking payment deadlines across a large client base.

**For Organizations:** Organizations benefit from reduced administrative overhead, accelerated invoice-to-payment cycles through automated overdue reminders, and enhanced cash flow visibility via real-time revenue dashboards. The multi-tenant architecture permits a single deployment instance to serve multiple organizations cost-effectively, reducing per-organization infrastructure and maintenance expenditure. The soft-delete mechanism provides a safety net against accidental data loss, allowing restoration of inadvertently removed records.

**For the Developer:** The project demonstrates technical proficiency in Clean Architecture principles, multi-tenant database design, Blazor Server interactive rendering with the MudBlazor component library, JWT-based authentication, background job processing, and PDF document generation — all within a modern .NET 10 technology stack. This project serves as a portfolio-grade demonstration of full-stack enterprise application development adhering to industry best practices.

**For Society:** By lowering the barrier to professional billing infrastructure for Nepali SMEs, the system promotes digital adoption in the small-business sector, reduces paper waste through electronic invoicing, and contributes to greater financial transparency and record-keeping discipline across the broader Nepali economy.

---

### Literature Review

The design of SunyaSuite draws on six overlapping bodies of literature: multi-tenant data isolation, layered/Clean Architecture for enterprise maintainability, hosting-model selection in modern web frameworks, role-based authorization, digitalization of VAT invoicing and tax compliance, and automated accounts-receivable (dunning) management. Each is reviewed below and mapped to a corresponding design decision in the proposed system.

**Multi-Tenant Data Isolation.** Architecting SaaS platforms that serve several client organizations from a shared codebase requires an explicit choice among competing tenancy models. Industry and academic analyses converge on three canonical patterns — a fully siloed database per tenant, a shared database with per-tenant schemas, and a shared database with row-level discrimination by tenant identifier — each trading off isolation strength against operational cost and scalability (Chandra & Kumar, 2025). The siloed model offers the strongest guarantee that one tenant can never access another tenant's records, even under application-layer failure, which is the principal reason regulated or compliance-sensitive domains such as financial and healthcare software gravitate toward it despite its higher per-tenant infrastructure overhead. This trade-off directly motivates SunyaSuite's default database-per-organization strategy: because the system stores clients' financial and tax records, isolation guarantees are prioritized over the marginal efficiency gains of a shared-schema design, while a configurable shared-database mode is retained for deployments with a large number of low-activity tenants.

**Layered and Clean Architecture.** Martin (2017) formalized the principle that source-code dependencies should point inward toward stable business rules, insulating the domain and application layers from volatile details such as user interfaces, databases, and third-party frameworks. This dependency-inversion discipline is widely credited with improving testability, framework independence, and long-term maintainability of enterprise systems, since business logic can be exercised in isolation from infrastructure concerns. SunyaSuite's five-project solution structure (Domain, Application, Infrastructure, Web, Web.Client) is a direct implementation of this model: entities and use cases in the inner layers have no compile-time dependency on Entity Framework Core, Blazor, or PostgreSQL, which allows the persistence or presentation technology to be replaced with comparatively localized changes.

**Hosting-Model Selection for Interactive Web Applications.** Within the .NET ecosystem, Blazor offers two principal hosting models with divergent performance characteristics: Blazor Server, which executes application logic on the server and streams UI updates over a persistent SignalR connection, and Blazor WebAssembly, which downloads and executes the .NET runtime in the browser. Comparative analyses consistently report that the server-hosted model yields a smaller initial payload and lower time-to-interactive, at the cost of continuous network dependency and reduced horizontal scalability under high concurrent load, whereas the WebAssembly model trades a larger initial download for offline capability and reduced server-side computation. Given that SunyaSuite targets internal, browser-based use by staff within a small number of organizations (rather than a public, high-concurrency audience), and given that sensitive financial data is preferably kept server-side rather than shipped to the client sandbox, the Blazor Server model was selected as the better fit for this deployment profile.

**Role-Based Access Control.** Sandhu et al. (1996) introduced a family of reference models (RBAC0–RBAC3) that formalized the assignment of permissions to roles rather than directly to individual users, later consolidated by Ferraiolo et al. (2001) into the NIST/ANSI standard that underlies most contemporary authorization frameworks. A recurring theme in this literature is the need for hierarchical or scoped roles when a system must express both platform-wide administrative authority and context-dependent authority within a bounded unit (such as a department, tenant, or organization) — a requirement common to multi-tenant SaaS platforms. SunyaSuite's dual-layer authorization model, comprising system-level roles (SystemAdmin, SystemStaff) and organization-scoped roles (Owner, OrgAdmin, Member) resolved through custom ASP.NET Core authorization handlers, reflects this constrained/scoped-RBAC pattern rather than a single flat role list.

**Digitalization of VAT Invoicing and Tax Compliance.** A substantial empirical literature examines the effect of mandatory electronic invoicing on firm reporting behaviour and government revenue. Using firm-level administrative data from Peru, Bellon et al. (2022) found that VAT liabilities rose materially among small firms and historically low-compliance sectors in the first year after e-invoicing adoption, an effect attributed to improved monitoring and perceived audit risk rather than to the invoicing technology itself. Complementary evidence from Rwanda's tax administration similarly links e-invoicing to higher audit effectiveness, though the authors note that digitalization alone is insufficient without accompanying enforcement capacity. A systematic review of e-invoicing and prefilled-return research by Hesami et al. (2024) further concludes that these technologies chiefly reduce compliance and administrative costs for smaller firms, which are typically least equipped to absorb manual bookkeeping overhead. These findings substantiate the premise of SunyaSuite's problem statement: structured, software-enforced VAT computation is not merely a convenience feature but a documented lever for compliance among SME-scale taxpayers. Locally, Nepal's Inland Revenue Department has progressively expanded mandatory electronic billing under the Electronic Invoice Procedure (2074 B.S., with subsequent amendments) and the Central Billing Monitoring System (CBMS), extending real-time invoice reporting obligations from large taxpayers toward smaller VAT-registered businesses; industry commentary notes that the cost and technical heterogeneity of commercially available, IRD-approved billing software remains a barrier for smaller organizations. This regulatory trajectory is the direct justification for SunyaSuite's built-in 13% VAT computation, PAN-based invoice fields, and Bikram Sambat fiscal-year numbering, since these are the concrete technical requirements a locally deployable billing platform must satisfy to remain audit-ready as the mandate expands.

**Accounts Receivable Automation and Overdue Monitoring.** Practitioner and applied-finance literature on the "dunning" process — the structured, escalating follow-up on unpaid invoices — identifies early detection of at-risk accounts and consistent, automated reminders as the primary levers for reducing days-sales-outstanding (DSO) and bad-debt write-offs, while cautioning that poorly calibrated escalation can damage client relationships or, in aggressive cases, raise legal exposure. This motivates two related design choices in SunyaSuite: the traffic-light client-health indicator, which surfaces overdue exposure at a glance rather than requiring staff to inspect individual invoice ledgers, and the scheduled background service that transitions overdue invoices and triggers notification without requiring manual daily review — an automated, lower-friction analogue of the reminder-escalation workflows described in this literature, scaled to the needs of a single SME rather than an enterprise collections department.

---

### Proposed Methodology and System Approach

The project follows an iterative development methodology grounded in Agile principles. The system was constructed incrementally across distinct feature phases: (1) foundation, authentication, and data model; (2) client CRUD and traffic-light status; (3) project tracking; (4) invoicing with line items; (5) PDF generation, dashboard, and background services; (6) reports, audit logging, and email; and (7) testing and hardening. Each phase delivered a working, testable increment before proceeding to the subsequent phase. This approach facilitated continuous feedback integration and mitigated the integration risk commonly associated with waterfall-style development.

**Programming Language and Frameworks:**

- **Language:** C# 13 (.NET 10 SDK 10.0.301) with nullable reference types and implicit usings enabled.
- **Frontend Framework:** Blazor Server with MudBlazor 9.6 component library for the user interface.
- **Backend Framework:** ASP.NET Core 10 with controller-based REST endpoints and JWT Bearer token authentication.
- **Object-Relational Mapping:** Entity Framework Core 10 with the Npgsql provider for PostgreSQL.
- **PDF Generation:** QuestPDF (Community license) for invoice and receipt documents.
- **Excel Export:** ClosedXML for spreadsheet generation.
- **Email Delivery:** MailKit for SMTP-based email communication.
- **Structured Logging:** Serilog with console and rolling file sinks.
- **Calendar Conversion:** NepDate for Bikram Sambat date handling; NumericWordsConversion for currency-to-words.
- **Input Validation:** FluentValidation with automatic validation pipeline integration.
- **Containerization:** Docker with multi-stage builds and docker-compose orchestration.
- **Version Control:** Git with GitHub for source code management and GitHub Actions for CI/CD.

**Database Technology:** PostgreSQL 16 with Entity Framework Core migrations. The multi-tenant architecture employs a database-per-organization model by default, with per-tenant connection strings stored in a shared configuration database. An alternative shared-database mode is supported via configuration.

**Development Environment:** Visual Studio 2022, JetBrains Rider, or VS Code with the C# Dev Kit extension. Unit testing uses the xUnit framework with bUnit for Blazor component testing, Moq for mock object creation, and FluentAssertions for expressive test assertions.

---

### System Requirements

#### Hardware Requirements

| Component | Minimum Specification |
| --- | --- |
| Processor | Intel Core i3 (10th generation or higher) / AMD equivalent |
| RAM | 4 GB (8 GB recommended for concurrent multi-user access) |
| Storage | 128 GB free disk space (SSD strongly recommended) |
| Network | Broadband internet connection for multi-user deployment |

For production server deployment, a cloud-based virtual machine with 2 virtual CPUs, 4 GB RAM, and 40 GB SSD storage is recommended for workloads serving up to ten organizations concurrently.

#### Software Requirements

| Component | Specification |
| --- | --- |
| Operating System | Windows 10/11, Windows Server 2019 or later, Ubuntu 22.04 LTS or later, or macOS 13 Ventura or later |
| Runtime Environment | .NET 10 Runtime (SDK 10.0.301 for development) |
| Database Management System | PostgreSQL 16 or later |
| Integrated Development Environment | Visual Studio 2022 or later, JetBrains Rider, or VS Code with C# Dev Kit |
| Container Runtime (optional) | Docker Engine 24 or later with Docker Compose plugin |
| Web Server | Kestrel (built-in) deployed behind a reverse proxy (nginx, Caddy, or IIS) |
| Client Browser | Microsoft Edge, Google Chrome, Mozilla Firefox, or Safari (latest two major versions) |

---

### System Architecture and System Design

**Figure 1 — Clean Architecture Layer Diagram**

*\[Placeholder: Insert Figure 1 — Clean Architecture Layer Diagram here\]*

**Figure 2 — Three-Tier Deployment Architecture** 

*\[Placeholder: Insert Figure 2 — Three-Tier Deployment Architecture here\]*

**Figure 3 — Invoice Creation Data Flow**

*\[Placeholder: Insert Figure 3 — Invoice Creation Data Flow Diagram here\]*

**Figure 4 — Entity-Relationship Diagram**

*\[Placeholder: Insert Figure 4 — Entity-Relationship Diagram here\]*

**Figure 5 — Use Case Diagram**

*\[Placeholder: Insert Figure 5 — Use Case Diagram here\]*

**Figure 6 — Core Class Diagram**

*\[Placeholder: Insert Figure 6 — Core Class Diagram here\]*

---

### Modules and Functional Description

**1. Authentication and Authorization Module:** Manages user registration through invite-code-based signup, credential-based login, JWT token issuance and refresh, password reset, and session lifecycle. Implements a dual-layer role hierarchy: system-level roles (SystemAdmin, SystemStaff) control access to global administration interfaces, while organization-level roles (Owner, OrgAdmin, Member) govern permissions within each tenant organization. Custom AuthorizationHandler implementations resolve organization-level permissions by querying the user's role within the currently selected tenant organization.

**2. Client Management Module:** Provides complete CRUD operations for client entities with soft-delete support. Each client is assigned an automated traffic-light status computed in real time: clients with overdue invoices are flagged red, clients with invoices due within seven days are flagged yellow, and clients with no outstanding issues are flagged green. The module supports server-side filtering, pagination, and data export to Excel format.

**3. Project Management Module:** Tracks client-specific projects with a structured data model comprising name, description, deadline date, and progress percentage (0–100). Project status values include NotStarted, InProgress, Completed, and OnHold. Projects may be linked to invoices for project-scoped billing. Soft-delete behaviour cascades from the parent client entity.

**4. Invoice Management Module:** The core billing module supporting two bill types: VAT bills (13% tax computation, PAN number required) and non-VAT bills. Invoice numbers are auto-generated using PostgreSQL sequences scoped to the fiscal year (format: "FY-sequential-number"). The module enforces a strict status machine with permitted transitions: Draft to Sent, Sent to Paid or Overdue, and Overdue to Paid. Line items support Harmonized System (HS) codes, unit of measurement, quantity, unit price, and computed line amount. Optimistic concurrency control through Entity Framework Core row versioning prevents conflicting concurrent edits.

**5. Payment Receipt Module:** Records incoming payments from clients and allocates them across one or more outstanding invoices. Upon receipt creation, the system automatically updates each invoice's AmountPaid field and transitions invoices to Paid status when the total amount is fully satisfied. Soft-delete of a receipt correctly reverses all allocations and reverts invoice statuses. Receipt numbers follow the "MR-FY-sequence" pattern.

**6. Fiscal Year Management Module:** Defines financial year periods using Nepali Bikram Sambat dates with automatic Gregorian calendar conversion. Each fiscal year carries an open/closed status that controls whether new transactions may be posted against it. Only one fiscal year may be designated as current at any time. Invoice and receipt numbering sequences are scoped per fiscal year.

**7. PDF Generation Module:** Generates professional A4-format PDF invoices and receipts using the QuestPDF library. Generated documents include seller and buyer information panels, an optional business logo, line-item tables with adaptive column visibility, VAT computation breakdowns, monetary amounts rendered in Nepali words, a signature block, and copy-type watermarks (Original, Duplicate, Triplicate) with colour differentiation.

**8. Dashboard and Reporting Module:** Provides an aggregated overview of key business metrics including total client count, active project count, invoice status distribution (paid, overdue, outstanding), and monthly revenue trend data. Recent invoice transactions are displayed for rapid reference. All dashboard data is exportable to Excel format using the ClosedXML library.

**9. Background Services Module:** Executes scheduled tasks through ASP.NET Core hosted service abstractions. The OverdueBackgroundService executes daily at a configurable time, identifies invoices in Sent status that have passed their due date, transitions them to Overdue status, recalculates affected client traffic-light statuses, and dispatches email notifications to clients with a concurrency limit of five simultaneous sends.

**10. Audit Logging Module:** Records all entity mutations — including creation, modification, soft-delete, restoration, and status transitions — with captured metadata including user identifier, action type, entity name, entity identifier, timestamp, and a JSON-encoded detail payload. Audit log entries are retrievable through filtered queries supporting action type, entity name, date range, and full-text search predicates.

---

### Expected Output and Deliverables

- A fully functional, multi-tenant Blazor Server web application deployable via Docker Compose.
- Complete C# source code organized in a Clean Architecture solution comprising five projects (Domain, Application, Infrastructure, Web, Web.Client).
- PostgreSQL database schema with Entity Framework Core migration scripts for both the configuration database and the per-organization tenant databases.
- Project documentation encompassing system architecture descriptions, module specifications, and deployment guides.
- Sample PDF output files: professionally formatted invoice and receipt documents generated by the system.
- Excel export samples: client, project, invoice, and summary report exports in .xlsx format.
- Screenshots of key system interfaces: authentication, dashboard, client listing, invoice creation and editing, PDF output preview, and administration panel.
- Automated unit tests and component tests covering core business logic and Blazor component behaviour.

---

### Project Timeline

| Phase | Week 1–2 | Week 3–4 | Week 5–6 | Week 7–8 | Week 9–10 | Week 11–12 |
| --- | --- | --- | --- | --- | --- | --- |
| Requirement Analysis | ████████ |  |  |  |  |  |
| System Design |  | ████████ |  |  |  |  |
| Implementation and Coding |  |  | ████████ | ████████ |  |  |
| Testing and Debugging |  |  |  |  | ████████ |  |
| Documentation and Report Writing |  |  |  |  |  | ████████ |
| Final Presentation and Submission |  |  |  |  |  | ████████ |

*Figure 7: Twelve-week project timeline indicating the duration and sequencing of major project phases.*

---

### Limitations

1. The system does not include a native mobile application; the Blazor Server user interface is optimized for desktop-class browser viewports and may not render optimally on mobile devices without additional responsive design adjustments.
2. Payment processing follows a manual entry model — the system records receipts as data entries but does not integrate with payment gateways for real-time online collection.
3. The QuestPDF library, used under its Community license, applies a watermark to generated PDF documents in the free tier; acquisition of a commercial license is necessary for watermark-free document output in production environments.
4. The database-per-organization multi-tenancy model, while providing strong data isolation guarantees, increases aggregate database server resource consumption as the number of tenant organizations grows, compared to a shared-database architecture.
5. Email notification delivery depends on the availability and correct configuration of an SMTP server; the system gracefully degrades to a no-operation mode when SMTP is not configured but cannot provide delivery guarantees.

---

### Future Enhancements

1. Deploy to cloud infrastructure (Amazon Web Services, Microsoft Azure, or on-premises) with automated database backup scheduling and disaster recovery procedures.
2. Develop a progressive web application or mobile application using Blazor Hybrid or .NET MAUI to enable offline-capable mobile access.
3. Integrate domestic payment gateways (eSewa, Khalti, ConnectIPS) for real-time online payment collection with automatic receipt generation and invoice status updates.
4. Incorporate machine learning models for invoice payment date prediction, client churn risk assessment, and intelligent cash flow forecasting.
5. Implement full multilingual user interface support (Nepali, English, and additional languages) extending beyond the current limited use of Nepali in PDF documents.
6. Add advanced analytics dashboards with interactive drill-down capability, comparative period-over-period analysis, and exportable chart visualizations.
7. Introduce automated recurring invoice generation for subscription-based and retainer-based billing models.
8. Implement webhook-based event notifications to facilitate integration with external enterprise resource planning, customer relationship management, and accounting systems.

---

### Conclusion

The proposed system, SunyaSuite, is a multi-tenant client management and billing platform constructed with .NET 10 and Blazor Server that streamlines the complete invoicing and payment tracking workflow for Nepali businesses. The system addresses the demonstrated gap in localized billing software by natively supporting Nepali Bikram Sambat fiscal years, statutory VAT-compliant invoicing, automated overdue detection and notification, and real-time client account health monitoring through a traffic-light status indicator system. The multi-tenant architecture employing database-per-organization isolation delivers both data security and operational cost-effectiveness, while the dual-layer role-based authorization framework ensures appropriate access control at both system administration and organization membership levels. Built on a Clean Architecture foundation with comprehensive module coverage — encompassing client management, project tracking, invoicing, payment receipting, PDF generation, background processing, and audit logging — and packaged with Docker-ready deployment infrastructure, SunyaSuite delivers a production-ready solution that reduces administrative overhead, improves cash flow visibility, and establishes a robust technical foundation for future extension and enhancement.

---

### References

Bellon, M., Dabla-Norris, E., Khalid, S., & Lima, F. (2022). Digitalization to improve tax compliance: Evidence from VAT e-invoicing in Peru. *Journal of Public Economics, 210*, Article 104661. https://doi.org/10.1016/j.jpubeco.2022.104661

Chandra, R., & Kumar, S. (2025). Multi-tenant SaaS architectures: Design principles and security considerations. *Journal of Software Engineering Studies*. https://www.researchgate.net/publication/391673039

Ferraiolo, D. F., Sandhu, R., Gavrila, S., Kuhn, D. R., & Chandramouli, R. (2001). Proposed NIST standard for role-based access control. *ACM Transactions on Information and System Security, 4*(3), 224–274. https://doi.org/10.1145/501978.501980

Hesami, S., Jenkins, H., & Jenkins, G. P. (2024). Digital transformation of tax administration and compliance: A systematic literature review on e-invoicing and prefilled returns. *Digital Government: Research and Practice, 5*(3), Article 18. https://doi.org/10.1145/3643687

Inland Revenue Department, Government of Nepal. (2017). *Electronic Invoice Procedure, 2074*. Ministry of Finance.

Martin, R. C. (2017). *Clean architecture: A craftsman's guide to software structure and design*. Prentice Hall.

Sandhu, R. S., Coyne, E. J., Feinstein, H. L., & Youman, C. E. (1996). Role-based access control models. *Computer, 29*(2), 38–47. https://doi.org/10.1109/2.485845

Note: The Nepal-specific regulatory background (Electronic Invoice Procedure and Central Billing Monitoring System rollout) and the Blazor Server/WebAssembly hosting comparison in this section are additionally informed by current Inland Revenue Department notices and Microsoft/.NET community technical documentation rather than peer-reviewed sources; these should be supplemented with the official IRD circulars (available at ird.gov.np) and the Microsoft Learn Blazor hosting-models documentation before final submission, per your institution's citation requirements.