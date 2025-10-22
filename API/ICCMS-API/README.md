## ICCMS API - Endpoint Guide

This document lists the available endpoints in the ASP.NET Core ICCMS API, how to call them, and what data they accept/return.

### Base URL

- Local (Dev): `https://localhost:5031` (check your `launchSettings.json`/Swagger UI)
- All endpoints below are relative to the base URL.

### Authentication

- Authentication Scheme: `Bearer` (Firebase token)
- Header: `Authorization: Bearer <firebase_id_token>`
- Most endpoints require role-based authorization: Admin, Project Manager, Client, Contractor. Some are Tester-only for development.

### Content Types

- JSON requests: `Content-Type: application/json`
- File upload requests: `Content-Type: multipart/form-data`

### Common Models (simplified)

- LoginRequest: `{ "email": string, "password": string }`
- ProcessBlueprintRequest: `{ "blueprintUrl": string, "projectId": string, "contractorId": string }`
- ExtractLineItemsRequest: `{ "blueprintUrl": string }`
- ConvertToQuotationRequest: `{ "clientId": string }`
- CreateMessageRequest: `{ senderId, receiverId, projectId, subject, content, threadId?, parentMessageId?, threadParticipants[], messageType }`
- BroadcastMessageRequest: `{ senderId, projectId, subject, content }`
- QuoteApprovalRequest: `{ quoteId, action, userId }`
- InvoicePaymentRequest: `{ invoiceId, action, userId }`
- ProjectUpdateRequest: `{ projectId, updateType, userId }`

Note: Additional entity models exist for `Project`, `Phase`, `ProjectTask`, `Document`, `Estimate`, `Quotation`, `Invoice`, `Payment`, etc., and are stored in Firestore. Fields are self-descriptive in code.

---

## AuthController (`api/auth`)

- POST `api/auth/login` (anonymous) → login with email/password
  - Body: `LoginRequest`
- POST `api/auth/verify-token` (anonymous) → verify Firebase token
  - Body: `{ token: string }`
- GET `api/auth/profile` (auth) → current user profile

Example:

```bash
curl -X POST "{BASE}/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"secret"}'
```

## AdminController (`api/admin`) [Roles: Admin, Tester]

- GET `dashboard` → metrics/overview
- GET `users` → list users
- GET `deactivated` → list deactivated users
- GET `user/{id}` → get user
- GET `messages` → list messages
- GET `message/{id}` → get message
- GET `projects/{id}/status` → status
- GET `documents` → list docs
- POST `create/user` → create user
  - Body: `CreateUserRequest`
- POST `create/document` → create document metadata
  - Body: `Document`
- POST `users/{id}/activate` → activate user
- POST `users/{id}/deactivate` → deactivate user
- PUT `update/user/{id}` → update user
  - Body: `User`
- PUT `users/{id}/role` → update role
  - Body: `{ role: string }`
- PUT `notifications/{id}` → update notification
  - Body: `Notification`
- PUT `update/document/{id}` → update document metadata
  - Body: `Document`
- DELETE `delete/user/{id}`
- DELETE `delete/documents/{id}`

## ProjectManagerController (`api/projectmanager`) [Roles: Project Manager, Tester]

- GET `projects` → all projects
- GET `projects/paginated?page=&pageSize=` → paginated projects
- GET `projects/simple?page=&pageSize=` → simple list with pagination
- GET `projects/all` → all projects for current PM
- GET `projects/draft?page=&pageSize=` → draft projects

- POST `save-draft` → create draft `Project`
  - Body: `Project`
- PUT `update-draft/{id}` → update draft `Project`
  - Body: `Project`
- PUT `projects/{id}/autosave` → partial autosave
  - Body: partial `Project`
- POST `projects/{id}/phases-bulk` → create phases
  - Body: `Phase[]`
- POST `projects/{id}/tasks-bulk` → create tasks
  - Body: `ProjectTask[]`
- POST `projects/{id}/finalize` → finalize draft to Planning

- GET `project/{id}` → get project
- GET `project/{id}/phases` → get phases
- GET `project/{id}/tasks` → get tasks
- GET `project/{id}/documents` → get documents

- POST `create/project` → create project
  - Body: `Project`
- POST `create/project/{projectId}/phase` → create phase
  - Body: `Phase`
- POST `create/project/{projectId}/task` → create task
  - Body: `ProjectTask`
- POST `create/project/{projectId}/document` → create document metadata
  - Body: `Document`
- PUT `update/project/{id}` → update project
  - Body: `Project`
- PUT `update/phase/{id}` → update phase
  - Body: `Phase`
- PUT `update/task/{id}` → update task
  - Body: `ProjectTask`
- PUT `approve/document/{id}` → approve document
- PUT `update/document/{id}` → update document metadata
  - Body: `Document`
- DELETE `delete/project/{id}`
- DELETE `delete/phase/{id}`
- DELETE `delete/task/{id}`
- DELETE `delete/document/{id}`

## ClientsController (`api/clients`) [Roles: Client, Tester]

- GET `projects`
- GET `project/{id}`
- GET `quotations`
- GET `quotation/{id}`
- GET `invoices`
- GET `invoice/{id}`
- GET `maintenanceRequests`
- GET `maintenanceRequest/{id}`
- POST `create/maintenanceRequest`
  - Body: `MaintenanceRequest`
- POST `pay/invoice/{id}`
  - Body: `Payment`
- PUT `update/maintenanceRequest/{id}`
  - Body: `MaintenanceRequest`
- PUT `approve/quotation/{id}`
- PUT `reject/quotation/{id}`
- DELETE `delete/maintenanceRequest/{id}`

## ContractorsController (`api/contractors`) [Roles: Contractor, Tester]

- GET `Project/Tasks` → contractor tasks
- GET `project/phases`
- GET `project/documents`
- POST `upload/project/{projectId}/document` (multipart)
  - Form: `file`, `description`
- PUT `update/project/task/{id}`
  - Body: `ProjectTask`
- PUT `update/document/{id}`
  - Body: `Document`
- DELETE `delete/document/{id}`

## DocumentsController (`api/documents`) [Roles: Admin, Project Manager, Client, Contractor, Tester]

- GET `` → list files (Supabase bucket)
- GET `{fileName}` → download bytes
- GET `project/{projectId}` → list project documents (metadata)
- POST `upload` (multipart) → upload + create metadata
  - Form: `file`, `projectId`, `description?`
- PUT `update/{fileName}` (multipart) → replace file
  - Form: `file`, `description?`
- DELETE `{fileName}` → delete file from bucket

## MessagesController (`api/messages`) [Roles: Admin, Project Manager, Client, Contractor, Tester]

- GET `` → list messages
- GET `{id}` → get message
- GET `user/{userId}` → by user
- GET `project/{projectId}` → by project
- POST `` → create message
  - Body: `CreateMessageRequest`
- POST `broadcast` → broadcast message
  - Body: `BroadcastMessageRequest`
- POST `thread` → create thread
  - Body: `CreateThreadRequest`
- POST `reply` → reply to message
  - Body: `ReplyToMessageRequest`
- GET `threads` → list threads (supports `projectId`, `workflowType` query in code)
- GET `thread/{threadId}` → thread messages
- POST `attachment` (multipart) → upload attachment to message
  - Form: `file`, `messageId`, `description?`, `category?`
- GET `attachment/{attachmentId}` → download attachment (hidden in API explorer)
- GET `message/{messageId}/attachments` → list attachments for a message
- DELETE `attachment/{attachmentId}` → delete attachment

- GET `workflow` → list workflow messages (`projectId?`, `workflowType?`)
- POST `workflow/quote-approval` → send quote approval notification
  - Body: `QuoteApprovalRequest`
- POST `workflow/invoice-payment` → send invoice payment notification
  - Body: `InvoicePaymentRequest`
- POST `workflow/project-update` → send project update notification
  - Body: `ProjectUpdateRequest`
- POST `workflow/system-alert` → send system alert

  - Body: `SystemAlertRequest`

- PUT `{id}` → update message
  - Body: `Message`
- DELETE `{id}` → delete message

## NotificationsController (`api/notifications`) [Roles: Admin, Project Manager, Client, Contractor, Tester]

- GET `` → list notifications
- GET `{id}` → get notification
- GET `user/{userId}` → by user
- GET `user/{userId}/unread` → unread by user
- POST `` → create notification
  - Body: `Notification`
- PUT `{id}` → update notification
  - Body: `Notification`
- PUT `{id}/mark-read` → mark as read
- DELETE `{id}`

## UsersController (`api/users`)

- GET `profile` → current user profile
- PUT `device-token` → update device token
  - Body: `{ deviceToken: string }`
- GET `clients` → list users with role Client (admin/pm context assumed)
- GET `contractors` → list users with role Contractor (admin/pm context assumed)

## EstimatesController (`api/estimates`) [Roles: Admin, Project Manager, Client, Contractor, Tester]

- GET `` → list estimates
- GET `{id}` → get estimate
- GET `project/{projectId}` → estimates for project
- POST `` → create estimate
  - Body: `Estimate`
- PUT `{id}` → update estimate
  - Body: `Estimate`
- DELETE `{id}` → delete estimate

- POST `process-blueprint` [Roles: Project Manager, Contractor, Tester]
  - Body: `ProcessBlueprintRequest`
- POST `{id}/convert-to-quotation` [Roles: Project Manager, Tester]
  - Body: `ConvertToQuotationRequest`
- GET `materials` [Roles: Project Manager, Contractor, Tester]
- GET `materials/category/{category}` [Roles: Project Manager, Contractor, Tester]
- GET `materials/categories` [Roles: Project Manager, Contractor, Tester]

## QuotationsController (`api/quotations`) [Auth required]

- GET `` [PM, Admin, Tester]
- GET `{id}` [PM, Admin, Tester]
- GET `project/{projectId}` [PM, Admin, Tester]
- GET `maintenance/{maintenanceRequestId}` [PM, Admin, Tester]
- GET `client/{clientId}` [PM, Admin, Tester]
- GET `me` [Client, Tester]
- POST `` [Project Manager, Tester]
  - Body: `Quotation`
- POST `from-estimate/{estimateId}` [Project Manager, Tester]
  - Body: `{ /* CreateQuotationFromEstimateRequest */ }`
- PUT `{id}` [Project Manager, Tester]
  - Body: `Quotation`
- DELETE `{id}` [Project Manager, Tester]
- POST `{id}/submit-for-approval` [Project Manager, Tester]
- POST `{id}/pm-approve` [Project Manager, Tester]
- POST `{id}/pm-reject` [Project Manager, Tester]
  - Body: `{ reason: string }`
- POST `{id}/send-to-client` [Project Manager, Tester]
- POST `{id}/client-decision` [Client, Tester]
  - Body: `{ decision: "approved"|"rejected", note? }`
- POST `{id}/convert-to-invoice` [Project Manager, Tester]
- Deprecated (return 410): `submit-for-admin`, `admin-approve`
- GET `debug-claims` [AllowAnonymous]

## InvoicesController (`api/invoices`) [Auth required]

- GET `` [PM, Admin, Tester]
- GET `{id}` [PM, Admin, Tester]
- GET `project/{projectId}` [PM, Admin, Tester]
- GET `client/{clientId}` [PM, Admin, Tester]
- GET `me` [Client, Tester]
- POST `` [Project Manager, Tester]
  - Body: `Invoice`
- PUT `{id}` [Project Manager, Tester]
  - Body: `Invoice`
- DELETE `{id}` [Project Manager, Tester]
- POST `{id}/issue` [Project Manager, Tester]
- POST `{id}/mark-paid` [Project Manager, Tester]
  - Body: `{ paidDate?: string, paidBy?: string }`
- POST `{id}/cancel` [Project Manager, Tester]

## PaymentsController (`api/payments`) [Roles: Admin, Project Manager, Client, Tester]

- GET `` → list
- GET `{id}`
- GET `invoice/{invoiceId}`
- GET `project/{projectId}`
- GET `client/{clientId}`
- POST `` → create payment
  - Body: `Payment`
- PUT `{id}` → update
  - Body: `Payment`
- DELETE `{id}`

## AuditLogsController (`api/auditlogs`) [Roles: Admin, Project Manager, Tester]

- POST `` → append-only create
  - Body: `CreateAuditLogInput`
- GET `` → recent logs (`?limit=200` default)
- GET `{id}` → by id
- GET `search?userId=&logType=&entityId=&fromUtc=&toUtc=&limit=` → search
- GET `by-entity/{entityId}` → logs for entity
- GET `types` → supported log types

## BlueprintController (`api/blueprint`) [Roles: Admin, Tester]

- POST `process-supabase-blueprint`
  - Body: `ProcessBlueprintRequest`
- POST `extract-line-items`
  - Body: `ExtractLineItemsRequest`

## Users (`api/users`)

- See UsersController above for profile and lookups.

---

## Calling the API

### With curl

```bash
curl -H "Authorization: Bearer $TOKEN" \
     -H "Content-Type: application/json" \
     "{BASE}/api/projectmanager/projects?page=1&pageSize=8"
```

### File Upload (multipart/form-data)

```bash
curl -X POST "{BASE}/api/documents/upload" \
  -H "Authorization: Bearer $TOKEN" \
  -F file=@"/path/to/file.pdf" \
  -F projectId="<PROJECT_ID>" \
  -F description="Optional notes"
```

### Error Handling

- 401 Unauthorized: missing/invalid bearer token
- 403 Forbidden: role does not have access
- 404 Not Found: resource not found
- 400 Bad Request: validation failed
- 500 Internal Server Error: unexpected failure

### Notes

- Swagger UI is enabled in Development; run the API and navigate to `/swagger`.
- CORS allows localhost origins configured in `Program.cs`.
