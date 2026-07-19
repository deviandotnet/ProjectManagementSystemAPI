# Backend Database Schema
**System:** Project Planning & Execution Tracking System
**Database:** Microsoft SQL Server
**ORM:** Entity Framework Core (Code-First with Migrations)
**Framework:** ASP.NET Core .NET 10

---

## Overview

This schema supports a multi-project environment where each project has its own categories, subcategories, and action items. Users are registered with project-level roles. The timeline and status are computed dynamically — they are NOT stored as colors but derived from date logic at runtime.

*Note: The term "Task" has been renamed to "ActionItem" globally to prevent conflicts with the C# `System.Threading.Tasks.Task` class.*

---

## Enums

### ActionItemStatus
```
0 = Plan            → Gray   (Not started; today < PlannedStart)
1 = Ongoing         → Green Diagonal Pattern (ActualStart exists, no ActualEnd)
2 = Delayed         → Red    (Today > PlannedEnd, action item incomplete)
3 = CompletedEarly  → Blue   (ActualEnd < PlannedEnd)
4 = CompletedOnTime → Green  (ActualEnd <= PlannedEnd)
5 = CompletedLate   → Yellow (ActualEnd > PlannedEnd)
```

### ProjectStatus
```
1 = Active
2 = OnHold
3 = Completed
4 = Cancelled
```

### UserRole (per project)
```
1 = Admin
2 = ProjectManager
3 = TeamLeader
4 = Member
5 = Viewer
```

### Priority
```
0 = Low
1 = Medium
2 = High
3 = Critical
```

### TimelineScale
```
0 = Daily
1 = Weekly   (Default)
2 = Biweekly
3 = Monthly
4 = Quarterly
```

### ProgressMode
```
0 = CountBased    (Completed Action Items / Total Action Items)
1 = WeightBased   (Sum of Weight × Completion %)
```

### HolidayType
```
0 = NationalHoliday
1 = CompanyHoliday
2 = SpecialHoliday
```

---

## Core Tables

---

### 1. Users
Registered accounts in the system. Authentication uses JWT.

| Column | Type | Constraints |
|---|---|---|
| Id | UNIQUEIDENTIFIER | PK, DEFAULT NEWID() |
| FirstName | NVARCHAR(100) | NOT NULL |
| LastName | NVARCHAR(100) | NOT NULL |
| Email | NVARCHAR(256) | NOT NULL, UNIQUE |
| PasswordHash | NVARCHAR(500) | NOT NULL |
| IsActive | BIT | DEFAULT 1 |
| CreatedAt | DATETIMEOFFSET | DEFAULT SYSUTCDATETIME() |
| UpdatedAt | DATETIMEOFFSET | DEFAULT SYSUTCDATETIME() |

---

### 2. Projects
Top-level entity. Each project is independent with its own calendar and timeline settings.

| Column | Type | Constraints |
|---|---|---|
| Id | UNIQUEIDENTIFIER | PK |
| Name | NVARCHAR(200) | NOT NULL |
| Description | NVARCHAR(MAX) | NULL |
| StartDate | DATE | NOT NULL |
| EndDate | DATE | NOT NULL |
| WeekStartDay | TINYINT | NOT NULL (0=Sun to 6=Sat, Default=1 for Monday) |
| DefaultTimelineScale | TINYINT | NOT NULL (TimelineScale enum, Default=1 Weekly) |
| ProgressMode | TINYINT | NOT NULL (ProgressMode enum, Default=0 CountBased) |
| Status | TINYINT | NOT NULL (ProjectStatus enum, Default=0 Active) |
| CreatedByUserId | UNIQUEIDENTIFIER | FK → Users.Id |
| CreatedAt | DATETIMEOFFSET | DEFAULT SYSUTCDATETIME() |
| UpdatedAt | DATETIMEOFFSET | DEFAULT SYSUTCDATETIME() |

---

### 3. ProjectMembers
Assigns users to projects with per-project roles.

| Column | Type | Constraints |
|---|---|---|
| Id | UNIQUEIDENTIFIER | PK |
| ProjectId | UNIQUEIDENTIFIER | FK → Projects.Id, NOT NULL |
| UserId | UNIQUEIDENTIFIER | FK → Users.Id, NOT NULL |
| Role | TINYINT | NOT NULL (UserRole enum) |
| JoinedAt | DATETIMEOFFSET | DEFAULT SYSUTCDATETIME() |

> **Note:** One user can be a member of multiple projects with different roles per project.

---

### 4. Categories
High-level groupings of action items within a project.

| Column | Type | Constraints |
|---|---|---|
| Id | UNIQUEIDENTIFIER | PK |
| ProjectId | UNIQUEIDENTIFIER | FK → Projects.Id, NOT NULL |
| Name | NVARCHAR(150) | NOT NULL |
| DisplayOrder | INT | NOT NULL, DEFAULT 0 |
| Color | NVARCHAR(7) | NULL (Hex color e.g. #3A86FF) |
| CreatedAt | DATETIMEOFFSET | DEFAULT SYSUTCDATETIME() |
| UpdatedAt | DATETIMEOFFSET | DEFAULT SYSUTCDATETIME() |

---

### 5. SubCategories
Optional second-level grouping under a Category.

| Column | Type | Constraints |
|---|---|---|
| Id | UNIQUEIDENTIFIER | PK |
| CategoryId | UNIQUEIDENTIFIER | FK → Categories.Id, NOT NULL |
| Name | NVARCHAR(150) | NOT NULL |
| DisplayOrder | INT | NOT NULL, DEFAULT 0 |
| CreatedAt | DATETIMEOFFSET | DEFAULT SYSUTCDATETIME() |
| UpdatedAt | DATETIMEOFFSET | DEFAULT SYSUTCDATETIME() |

---

### 6. ActionItems
The core entity. Each row in the timeline grid is one Action Item.

| Column | Type | Constraints |
|---|---|---|
| Id | UNIQUEIDENTIFIER | PK |
| ProjectId | UNIQUEIDENTIFIER | FK → Projects.Id, NOT NULL |
| CategoryId | UNIQUEIDENTIFIER | FK → Categories.Id, NOT NULL |
| SubCategoryId | UNIQUEIDENTIFIER | FK → SubCategories.Id, NULL |
| ActionItemName | NVARCHAR(500) | NOT NULL |
| Description | NVARCHAR(MAX) | NULL |
| Priority | TINYINT | NOT NULL (Priority enum, Default=1 Medium) |
| OwnerName | NVARCHAR(200) | NULL (free text or project member name) |
| OwnerId | UNIQUEIDENTIFIER | FK → Users.Id, NULL (optional link to registered user) |
| Weight | DECIMAL(5,2) | NULL (used when ProgressMode = WeightBased, 0-100%) |
| Sequence | INT | NOT NULL, DEFAULT 0 (display order within category/subcategory) |
| Remarks | NVARCHAR(MAX) | NULL |
| CreatedByUserId | UNIQUEIDENTIFIER | FK → Users.Id, NULL |
| CreatedAt | DATETIMEOFFSET | DEFAULT SYSUTCDATETIME() |
| UpdatedAt | DATETIMEOFFSET | DEFAULT SYSUTCDATETIME() |

---

### 7. PlannedSchedules
Planning data for each action item (PLAN columns in Excel).

| Column | Type | Constraints |
|---|---|---|
| Id | UNIQUEIDENTIFIER | PK |
| ActionItemId | UNIQUEIDENTIFIER | FK → ActionItems.Id, UNIQUE (one planned schedule per action item) |
| PlannedStartDate | DATE | NOT NULL |
| PlannedEndDate | DATE | NOT NULL |
| PlannedStartWeek | NVARCHAR(5) | COMPUTED (e.g., WW03) |
| PlannedEndWeek | NVARCHAR(5) | COMPUTED (e.g., WW07) |
| DurationCalendarDays | INT | COMPUTED (PlannedEndDate - PlannedStartDate) |
| DurationWorkingDays | INT | NOT NULL (calculated excluding weekends + holidays) |
| CreatedAt | DATETIMEOFFSET | DEFAULT SYSUTCDATETIME() |
| UpdatedAt | DATETIMEOFFSET | DEFAULT SYSUTCDATETIME() |

---

### 8. ActualExecutions
Tracks the actual execution data (ACTUAL columns in Excel).

| Column | Type | Constraints |
|---|---|---|
| Id | UNIQUEIDENTIFIER | PK |
| ActionItemId | UNIQUEIDENTIFIER | FK → ActionItems.Id, UNIQUE |
| ActualStartDate | DATE | NULL |
| ActualEndDate | DATE | NULL |
| ActualHours | DECIMAL(8,2) | NULL |
| CompletedByName | NVARCHAR(200) | NULL |
| CompletedById | UNIQUEIDENTIFIER | FK → Users.Id, NULL |
| DelayReason | NVARCHAR(MAX) | NULL |
| ActualRemarks | NVARCHAR(MAX) | NULL |
| CreatedAt | DATETIMEOFFSET | DEFAULT SYSUTCDATETIME() |
| UpdatedAt | DATETIMEOFFSET | DEFAULT SYSUTCDATETIME() |

---

### 9. HolidayCalendar
Stores national (PH) and company holidays. Excludes those dates from Working Days calculation.

| Column | Type | Constraints |
|---|---|---|
| Id | UNIQUEIDENTIFIER | PK |
| HolidayDate | DATE | NOT NULL |
| Name | NVARCHAR(200) | NOT NULL |
| Type | TINYINT | NOT NULL (HolidayType enum) |
| IsRecurringAnnually | BIT | DEFAULT 0 |
| Year | INT | NULL (NULL if recurring) |
| CreatedAt | DATETIMEOFFSET | DEFAULT SYSUTCDATETIME() |

---

### 10. AuditLogs
Captures every change made to any entity in the system (structured log).

| Column | Type | Constraints |
|---|---|---|
| Id | BIGINT | PK, IDENTITY(1,1) |
| EntityName | NVARCHAR(100) | NOT NULL (e.g., "ActionItem", "PlannedSchedule") |
| EntityId | NVARCHAR(100) | NOT NULL (the GUID of the changed record) |
| Action | NVARCHAR(50) | NOT NULL ("Create", "Update", "Delete") |
| FieldName | NVARCHAR(100) | NULL (e.g., "PlannedStartDate") |
| OldValue | NVARCHAR(MAX) | NULL |
| NewValue | NVARCHAR(MAX) | NULL |
| ChangedByUserId | UNIQUEIDENTIFIER | FK → Users.Id, NULL |
| ChangedByName | NVARCHAR(200) | NULL |
| ChangedAt | DATETIMEOFFSET | DEFAULT SYSUTCDATETIME() |
| IpAddress | NVARCHAR(50) | NULL |

> **Note:** Also exposed as a human-readable Activity Feed in the UI (e.g., *"John changed PlannedStartDate from Jan 3 to Jan 10 on Jan 14, 2026"*).

---

## Entity Relationship Summary

```
Users
  └── (created) Projects
          ├── ProjectMembers ←→ Users (per-project roles)
          ├── Categories
          │     └── SubCategories
          │
          └── ActionItems
                ├── (belongs to) Category
                ├── (belongs to) SubCategory (optional)
                ├── PlannedSchedule (1:1)
                ├── ActualExecution (1:1)
                └── (linked to) Owner User (optional)

HolidayCalendar (global, used by all projects)
AuditLogs (global, keyed by EntityName + EntityId)
```

---

## Status Computation Rules (Runtime, NOT stored)

These rules are applied by the **Status Engine** at query time.

| Condition | Status |
|---|---|
| ActualEndDate IS NOT NULL AND ActualEndDate < PlannedEndDate | CompletedEarly |
| ActualEndDate IS NOT NULL AND ActualEndDate = PlannedEndDate | CompletedOnTime |
| ActualEndDate IS NOT NULL AND ActualEndDate > PlannedEndDate | CompletedLate |
| ActualStartDate IS NOT NULL AND ActualEndDate IS NULL | Ongoing |
| ActualStartDate IS NULL AND GETDATE() > PlannedEndDate | Delayed |
| ActualStartDate IS NULL AND GETDATE() <= PlannedEndDate | Plan |

---

## Working Days Calculation Logic

```
WorkingDays = 0
CurrentDate = PlannedStartDate

WHILE CurrentDate <= PlannedEndDate:
  IF CurrentDate is NOT a weekend (based on Project.WeekStartDay config)
  AND CurrentDate is NOT in HolidayCalendar:
    WorkingDays++
  CurrentDate++
```
