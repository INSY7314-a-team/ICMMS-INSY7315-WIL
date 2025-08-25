# ICMMS Requirements Specifications

## Contents

- Functional Requirements
  - FR 1.0: User Management
  - FR 2.0: Project Management
  - FR 3.0: Maintenance Management
  - FR 4.0: Document Management
  - FR 5.0: Communication
  - FR 6.0: Notifications
  - FR 7.0: Quotation and Invoice Management
  - FR 8.0: Reporting and Analytics
  - FR 9.0: Security and Compliance

## Functional Requirements

This document defines the core functional requirements of the Integrated Construction
and Maintenance Management System. It is designed as the specification and
development guideline for system implementation.

### FR 1.0: User Management

- **FR 1.1** User Onboarding and Role Management – Admins shall create, update,
  deactivate, and assign roles (Admin, Project Manager, Contractor, Client)
  o **FR 1.1.1** Admins shall be able to view all created users.
  o **FR 1.1.2** Admin shall be able to reassign roles.
- **FR 1.2** User Authentication and Access Control – The system shall authenticate
  users via Firebase Authentication and enforce role-based access rules.
  o **FR 1.2.1** Authentication shall require email and password.
  o **FR 1.2.2** Access rules shall restrict actions to permitted roles.

### FR 2.0: Project Management

- **FR 2.1** Project Creation – Only Project Managers shall create projects with
  timelines, budgets, and phases
- **FR 2.2** Project Managers shall be able to allocate resources per phase.
- **FR 2. 3** Project Tracking – Project Managers shall track actual progress vs planned
  milestones.
  o **FR 2. 3 .1** Progress tracking shall include percentage completion.
  o **FR 2. 3 .2** Milestone status updates shall be logged.

### FR 3.0: Maintenance Management

- **FR 3.1** Maintenance Request Submission – Clients shall submit maintenance
  requests with descriptions, photos, and videos.
- **FR 3.2** Maintenance Request Tracking – Clients shall be able to track
  maintenance updates.
- **FR 3. 3** Contractor Assignment – Project Managers shall assign contractors to
  maintenance requests or project tasks.
- **FR 3. 4** Task Management – Contractors shall view assigned tasks, update
  progress, and upload completion evidence.
- **FR 3. 5** Task Corrections – Contractors shall delete or correct their own
  submissions before final approval.

### FR 4.0: Document Management

- **FR 4.1** Document Upload & Access – All users shall upload, access, and
  download project documents according to permissions.
- **FR 4.2** Document Upload Type – All users shall be able to upload PDF, DOCX,
  PNG or DWG files.
- **FR 4. 3** Document Version Control – The system shall maintain version history for
  documents.
- **FR 4. 4** Document Verification – Project Managers shall review and verify
  uploaded documents for accuracy and completeness.
- **FR 4. 5** Document Approval – Admins shall approve verified documents to ensure
  all project stakeholders are working from approved and accurate files.
- **FR 4.6** Document Access Logging – The system shall log all document views, and
  modifications.
  o **FR 4.6.** 1 Admins shall have full visibility of document access logs.
  o **FR 4.6.2** Project Managers shall have visibility of document access logs
  for projects they manage.

### FR 5.0: Communication

- **FR 5.1** Messaging – Users shall send messages within the system.
- **FR 5.2** Broadcast Messaging – Admins may broadcast or monitor
  communications.
- **FR 5. 3** Message Access – Users shall be able to search through messages
  through a search feature.

### FR 6.0: Notifications

- **FR 6.1** Real-Time Notifications – The system shall send real-time alerts for new
  assignments, updates, and overdue tasks.
  o **FR 6.1.1** Notifications shall be delivered via email and in-app alerts.
  o **FR 6.1.2** Notifications shall be sent out to project shareholders regarding
  the completion of project milestones.

### FR 7.0: Quotation and Invoice Management

- **FR 7.1** Quotation Generation – The system shall generate cost estimates from
  blueprints using AI.
  o **FR 7.1.1** The AI will parse uploaded building plans using computer vision
  and natural language processing.
  o **FR 7.1.2** The AI shall identify rooms, dimensions, and construction
  elements.
  o **FR 7.1.3** The AI shall produce a quotation with estimated costs on
  identified elements and labour costs.
  o **FR 7.1.4** Each quotation shall have a status of Draft, Sent, Approved,
  Accepted, Rejected.
- **FR 7.2** Quotation Approval – Admins approve quotations for workflow; Clients
  provide final acceptance.
- **FR 7.3** Invoice Generation – Invoices are auto-generated upon client acceptance
  of quotations.
- **FR 7.4** Payment Tracking – The system shall log and track payments via EFT,
  PayFast, and PayPal.
  o **FR 7.4.1** The application shall use a mock non-functional payment
  system for specified payment options.
  o **FR 7.4.2** The application shall update the status of payment upon
  payment confirmation, with statuses “Paid” or “Not Paid”.

### FR 8.0: Reporting and Analytics

- **FR 8.1** Reporting & Dashboards – The system shall provide project, task, and
  financial performance reports.
  o **FR 8.1.1** The dashboard shall display project status.
  o **FR 8.1.2** The dashboard shall display budget vs actual expenditures.
  o **FR 8.1.3** The dashboard shall display contractor ratings.
  o **FR 8.1.4** The dashboard shall make use of Gantt Chart, Pie Chart, Bar
  Graph and Line graph to display data.
  o **FR 8.1. 5** Admins and Project Managers shall be able to view contractor
  ratings.
- **FR 8.2** AI Risk Analysis – AI shall identify potential project delays and risks.
- **FR 8.3** AI Maintenance Forecasting – AI shall predict future maintenance needs
  based on historical data.

### FR 9.0: Security and Compliance

- **FR 9.1** Audit Logging – All critical actions shall be logged with timestamps.
- **FR 9.2** Role-Based Data Visibility – Data visibility shall be restricted according to
  user roles.
- **FR 9.3** Escalation Handling – Admins shall intervene in stalled workflows or
  disputes.
- **FR 9.4** System Configuration – Admins shall configure global settings,
  notification templates, and retention policies.
