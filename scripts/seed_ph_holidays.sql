-- ==============================================================================
-- Seed Script: Philippine National & Company Holidays for tbl.ms_HolidayCalendar
-- Database: PostgreSQL
-- ==============================================================================

-- Ensure table exists and insert initial national public holidays
INSERT INTO "tbl.ms_HolidayCalendar" ("Id", "HolidayDate", "Name", "Type", "IsRecurringAnnually", "Year", "CreatedAt")
VALUES
    -- Recurring Fixed National Holidays (Type: 1 = National)
    (gen_random_uuid(), '2026-01-01', 'New Year''s Day', 1, TRUE, NULL, NOW() AT TIME ZONE 'UTC'),
    (gen_random_uuid(), '2026-04-09', 'Araw ng Kagitingan (Day of Valor)', 1, TRUE, NULL, NOW() AT TIME ZONE 'UTC'),
    (gen_random_uuid(), '2026-05-01', 'Labor Day', 1, TRUE, NULL, NOW() AT TIME ZONE 'UTC'),
    (gen_random_uuid(), '2026-06-12', 'Independence Day', 1, TRUE, NULL, NOW() AT TIME ZONE 'UTC'),
    (gen_random_uuid(), '2026-11-30', 'Bonifacio Day', 1, TRUE, NULL, NOW() AT TIME ZONE 'UTC'),
    (gen_random_uuid(), '2026-12-25', 'Christmas Day', 1, TRUE, NULL, NOW() AT TIME ZONE 'UTC'),
    (gen_random_uuid(), '2026-12-30', 'Rizal Day', 1, TRUE, NULL, NOW() AT TIME ZONE 'UTC'),

    -- Recurring Fixed Special Non-Working Holidays (Type: 3 = Special)
    (gen_random_uuid(), '2026-02-25', 'EDSA People Power Revolution Anniversary', 3, TRUE, NULL, NOW() AT TIME ZONE 'UTC'),
    (gen_random_uuid(), '2026-08-21', 'Ninoy Aquino Day', 3, TRUE, NULL, NOW() AT TIME ZONE 'UTC'),
    (gen_random_uuid(), '2026-11-01', 'All Saints'' Day', 3, TRUE, NULL, NOW() AT TIME ZONE 'UTC'),
    (gen_random_uuid(), '2026-11-02', 'All Souls'' Day', 3, TRUE, NULL, NOW() AT TIME ZONE 'UTC'),
    (gen_random_uuid(), '2026-12-08', 'Feast of the Immaculate Conception of Mary', 3, TRUE, NULL, NOW() AT TIME ZONE 'UTC'),
    (gen_random_uuid(), '2026-12-31', 'Last Day of the Year (New Year''s Eve)', 3, TRUE, NULL, NOW() AT TIME ZONE 'UTC'),

    -- Moveable / Year-Specific Holidays (2026)
    (gen_random_uuid(), '2026-04-02', 'Maundy Thursday', 1, FALSE, 2026, NOW() AT TIME ZONE 'UTC'),
    (gen_random_uuid(), '2026-04-03', 'Good Friday', 1, FALSE, 2026, NOW() AT TIME ZONE 'UTC'),
    (gen_random_uuid(), '2026-04-04', 'Black Saturday', 3, FALSE, 2026, NOW() AT TIME ZONE 'UTC'),
    (gen_random_uuid(), '2026-08-31', 'National Heroes Day', 1, FALSE, 2026, NOW() AT TIME ZONE 'UTC')
ON CONFLICT ("HolidayDate", "Year") DO NOTHING;
