# Backend Features & API Specification
**System:** Project Planning & Execution Tracking System
**Framework:** ASP.NET Core .NET 10 — Clean Architecture + Vertical Slice Architecture (VSA)
**API Style:** Minimal API (`app.MapGet`, `app.MapPost`, etc.) with Feature-sliced handlers
**Auth:** JWT Bearer Tokens with Role-Based Access Control

---

## Overview

The backend exposes a RESTful JSON API consumed by the React frontend. Rather than grouping code by technical layer (Controllers, Services, Repos), each **feature slice** owns its own Request, Handler, Response, and Mapper — all in one folder. Shared infrastructure (EF Core, JWT, Logging) still lives in horizontal layers.

All business logic (status computation, working days calculation, week number generation, progress calculation) is computed at query time and is **never stored** in the database.

*Note: The term "Task" has been renamed to "ActionItem" globally to prevent conflicts with C# `System.Threading.Tasks.Task`.*

---

## Authentication Module

### Endpoints

| Method | Route | Description | Auth Required |
|---|---|---|---|
| POST | /api/auth/register | Register a new user | No |
| POST | /api/auth/login | Login, receive JWT token | No |
| POST | /api/auth/refresh | Refresh JWT token | Yes |
| GET | /api/auth/me | Get current user profile | Yes |
| PUT | /api/auth/me | Update profile | Yes |
| PUT | /api/auth/change-password | Change password | Yes |

### JWT Token Payload
```json
{
  "sub": "userId",
  "email": "user@example.com",
  "name": "John Doe",
  "role": "ProjectManager",
  "iat": 1720000000,
  "exp": 1720086400
}
```

---

## Projects Module

### Endpoints

| Method | Route | Description | Min Role |
|---|---|---|---|
| GET | /api/projects | List all projects for current user | Member |
| POST | /api/projects | Create a new project | Admin |
| GET | /api/projects/{id} | Get project details | Member |
| PUT | /api/projects/{id} | Update project settings | ProjectManager |
| DELETE | /api/projects/{id} | Delete project | Admin |
| GET | /api/projects/{id}/members | List project members | Member |
| POST | /api/projects/{id}/members | Add a member to project | ProjectManager |
| PUT | /api/projects/{id}/members/{userId} | Change member role | ProjectManager |
| DELETE | /api/projects/{id}/members/{userId} | Remove member | ProjectManager |

### Key Business Logic
- `WeekStartDay` is configurable per project (0=Sunday to 6=Saturday). This affects all timeline column generation.
- `ProgressMode` determines how project-level progress is calculated: count-based or weight-based.
- `DefaultTimelineScale` sets the initial view (Daily, Weekly, Biweekly, Monthly, Quarterly). Users can override this per session.

---

## Categories Module

### Endpoints

| Method | Route | Description | Min Role |
|---|---|---|---|
| GET | /api/projects/{projectId}/categories | List categories | Member |
| POST | /api/projects/{projectId}/categories | Create category | TeamLead |
| PUT | /api/projects/{projectId}/categories/{id} | Update category | TeamLead |
| DELETE | /api/projects/{projectId}/categories/{id} | Delete category | ProjectManager |
| PUT | /api/projects/{projectId}/categories/reorder | Reorder display order | TeamLead |

---

## SubCategories Module

### Endpoints

| Method | Route | Description | Min Role |
|---|---|---|---|
| GET | /api/categories/{categoryId}/subcategories | List subcategories | Member |
| POST | /api/categories/{categoryId}/subcategories | Create subcategory | TeamLead |
| PUT | /api/categories/{categoryId}/subcategories/{id} | Update | TeamLead |
| DELETE | /api/categories/{categoryId}/subcategories/{id} | Delete | ProjectManager |

---

## Action Items Module

Core module. Every row in the timeline is one Action Item.

### Endpoints

| Method | Route | Description | Min Role |
|---|---|---|---|
| GET | /api/projects/{projectId}/action-items | List all action items (with status computed) | Member |
| POST | /api/projects/{projectId}/action-items | Create action item | Member |
| GET | /api/projects/{projectId}/action-items/{id} | Get single action item | Member |
| PUT | /api/projects/{projectId}/action-items/{id} | Update action item | Member |
| DELETE | /api/projects/{projectId}/action-items/{id} | Delete action item | TeamLead |
| PUT | /api/projects/{projectId}/action-items/reorder | Reorder action items | TeamLead |
| GET | /api/projects/{projectId}/action-items/{id}/history | Get audit history for action item | Member |

### Query Parameters (GET /action-items)
```
?categoryId=...
?subCategoryId=...
?status=0,1,2           (comma-separated ActionItemStatus values)
?priority=2             (Priority enum value)
?ownerName=John
?search=requirements
?weekStart=WW01
?weekEnd=WW20
?startDate=2026-01-01
?endDate=2026-06-30
```

### Action Item Response DTO (computed fields included)
```json
{
  "id": "guid",
  "actionItemName": "Requirements checking for AI visualization",
  "categoryId": "guid",
  "categoryName": "Project Planning",
  "subCategoryId": "guid",
  "subCategoryName": "Project Planning Details",
  "priority": 2,
  "ownerName": "John",
  "sequence": 1,
  "plannedSchedule": {
    "plannedStartDate": "2026-01-03",
    "plannedEndDate": "2026-01-24",
    "plannedStartWeek": "WW01",
    "plannedEndWeek": "WW04",
    "durationCalendarDays": 21,
    "durationWorkingDays": 15
  },
  "actualExecution": {
    "actualStartDate": "2026-01-03",
    "actualEndDate": null,
    "actualHours": 12.5,
    "delayReason": null
  },
  "computedStatus": 1,
  "computedStatusLabel": "Ongoing",
  "weight": null,
  "remarks": null
}
```

---

## Timeline Engine (Server-side computation)

The timeline is generated server-side and returned as structured data. The frontend renders this into the CSS Grid.

### Endpoint

| Method | Route | Description |
|---|---|---|
| GET | /api/projects/{projectId}/timeline | Get full timeline data |

### Query Parameters
```
?scale=Weekly           (TimelineScale enum)
?startDate=2026-01-01   (optional override)
?endDate=2026-12-31     (optional override)
```

### Timeline Response DTO
```json
{
  "projectId": "guid",
  "scale": "Weekly",
  "weekStartDay": "Monday",
  "columns": [
    { "label": "WW01", "startDate": "2026-01-05", "endDate": "2026-01-11" },
    { "label": "WW02", "startDate": "2026-01-12", "endDate": "2026-01-18" }
  ],
  "rows": [
    {
      "rowType": "Category",
      "id": "guid",
      "label": "Project Planning",
      "color": "#3A86FF"
    },
    {
      "rowType": "ActionItem",
      "id": "guid",
      "label": "Requirements checking for AI visualization",
      "plannedStartWeekIndex": 0,
      "plannedEndWeekIndex": 3,
      "actualStartWeekIndex": 0,
      "actualEndWeekIndex": null,
      "status": 1,
      "statusLabel": "Ongoing"
    }
  ]
}
```

---

## Calendar Engine

### Endpoints

| Method | Route | Description |
|---|---|---|
| GET | /api/projects/{projectId}/calendar/working-days | Calculate working days between two dates |
| GET | /api/holidays | List all holidays |
| POST | /api/holidays | Add a custom company holiday (Admin) |
| PUT | /api/holidays/{id} | Update holiday |
| DELETE | /api/holidays/{id} | Delete holiday |

### Query Parameters (working-days)
```
?startDate=2026-01-03
?endDate=2026-01-24
```

### Philippine Public Holidays (Pre-loaded)
The system seeds the following recurring PH national holidays on startup:
- New Year's Day (Jan 1)
- EDSA People Power Revolution (Feb 25)
- Araw ng Kagitingan (Apr 9)
- Maundy Thursday (moveable)
- Good Friday (moveable)
- Labor Day (May 1)
- Independence Day (Jun 12)
- Ninoy Aquino Day (Aug 21)
- National Heroes Day (Aug 25)
- All Saints' Day (Nov 1)
- Bonifacio Day (Nov 30)
- Christmas Day (Dec 25)
- Rizal Day (Dec 30)

---

## Status Engine

Status is **never stored in the database**. It is computed in real-time by the `ActionItemStatusService` in the Domain/Application layer.

```csharp
public static ActionItemStatus ComputeStatus(
    DateTime today,
    DateTime plannedEnd,
    DateTime? actualStart,
    DateTime? actualEnd)
{
    if (actualEnd.HasValue)
    {
        if (actualEnd < plannedEnd) return ActionItemStatus.CompletedEarly;
        if (actualEnd == plannedEnd) return ActionItemStatus.CompletedOnTime;
        return ActionItemStatus.CompletedLate;
    }
    if (actualStart.HasValue) return ActionItemStatus.Ongoing;
    if (today > plannedEnd) return ActionItemStatus.Delayed;
    return ActionItemStatus.Plan;
}
```

---

## Progress Computation

### Count-Based (Default)
```
ProjectProgress = (CompletedActionItems / TotalActionItems) × 100
CompletedActionItems = ActionItems where ActualEndDate IS NOT NULL
```

### Weight-Based (Optional, set per project)
```
ProjectProgress = SUM(ActionItem.Weight × (ActionItem.ActualEndDate != null ? 100 : 0))
```

### Endpoint

| Method | Route | Description |
|---|---|---|
| GET | /api/projects/{projectId}/progress | Get project progress summary |

---

## Dashboard Module

Returns a card-based summary for each project the current user belongs to. Each card includes the project's KPIs.

### Endpoint

| Method | Route | Description |
|---|---|---|
| GET | /api/dashboard | Get all projects with KPI summary |

### Dashboard Response DTO
```json
{
  "projects": [
    {
      "projectId": "guid",
      "projectName": "AI Visualization NG Prediction",
      "status": "Active",
      "progressPercent": 42.5,
      "totalActionItems": 80,
      "completedActionItems": 34,
      "ongoingActionItems": 20,
      "delayedActionItems": 6,
      "plannedActionItems": 20,
      "startDate": "2026-01-03",
      "endDate": "2026-11-06",
      "myRole": "ProjectManager"
    }
  ]
}
```

---

## Filters & Search Module

Applied as query parameters on `/api/projects/{projectId}/action-items`.

### Filter Options
```
?categoryId=        Filter by Category
?subCategoryId=     Filter by SubCategory
?status=            Filter by Status (comma-separated)
?priority=          Filter by Priority
?ownerName=         Filter by Owner Name
?weekStart=WW01     Filter from this week
?weekEnd=WW52       Filter up to this week
?startDate=         Filter by planned start date >=
?endDate=           Filter by planned end date <=
?search=            Full-text search across ActionItemName, Description, OwnerName
```

---

## Export Module

### Endpoints

| Method | Route | Description | Min Role |
|---|---|---|---|
| GET | /api/projects/{projectId}/export/excel | Download Excel (.xlsx) | Member |

### Excel Export Contents
The exported Excel file mirrors the original spreadsheet format:
- **Sheet 1:** Action item list with Category, SubCategory, Action Item Name, Plan Start, Plan End, Actual Start, Actual End, Duration, Status
- **Sheet 2:** Timeline view with colored cells matching the Status Engine color rules
- **Library:** ClosedXML

---

## Audit Trail Module

### Endpoints

| Method | Route | Description | Min Role |
|---|---|---|---|
| GET | /api/projects/{projectId}/audit | Get audit logs for project | TeamLead |
| GET | /api/projects/{projectId}/action-items/{actionItemId}/audit | Get audit logs for action item | Member |

### Activity Feed Format
Human-readable format returned alongside raw audit log:
> *"John Dela Cruz changed PlannedStartDate of 'Requirements Checking' from Jan 3 to Jan 10 on Jul 14, 2026 at 9:45 AM"*

---

## SignalR (Real-Time Collaboration) — Future Phase

> SignalR hub (`/hubs/project`) will broadcast action item updates in real time when multiple users are viewing the same project. This is marked for a **future phase**.

---

## Hangfire (Background Jobs) — Future Phase

> Scheduled jobs (deadline reminders, delay detection notifications, weekly summary emails) will be handled by Hangfire. Marked for a **future phase**.
