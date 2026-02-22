-- Demo seed for user analytics charts (PostgreSQL).
-- Creates a demo employee + tasks + time logs spread across multiple days.
--
-- IMPORTANT:
-- Uses a dummy PasswordHash (seed-only-not-for-login) because this account
-- exists only to demo analytics charts for Admin/Manager views.
-- Run after applying migrations (AssignedAtUtc/CompletedAtUtc columns exist).
--
-- Usage (psql):
--   Run the whole file in pgAdmin Query Tool (or any SQL client).

DO $$
DECLARE
  demo_user_id integer;
  demo_project_id integer;
  base_day timestamp := (date_trunc('day', now() at time zone 'utc') - interval '13 days');
BEGIN
  -- Project (minimal)
  INSERT INTO "Projects" ("Title","Description","CreatedAt","IsArchived")
  VALUES ('Demo Project','Seeded for analytics demo', now() at time zone 'utc', false)
  RETURNING "Id" INTO demo_project_id;

  -- User
  INSERT INTO "Users" ("Username","PasswordHash","FullName","JobTitle","Role","IsActive","LastSeenUtc","CurrentSessionId")
  VALUES ('demo.employee', 'seed-only-not-for-login', 'Demo Employee', 'QA Engineer', 0, true, null, null)
  RETURNING "Id" INTO demo_user_id;

  -- Tasks: mix of done/on-progress, with deadlines and estimates
  -- Status values: ToDo=0, InProgress=1, OnHold=2, Done=3
  INSERT INTO "Tasks" ("Title","Description","Status","Priority","CreatedAt","AssignedAtUtc","CompletedAtUtc","Deadline","EstimatedHours","ProjectId","AssigneeId")
  VALUES
    ('Prepare test plan','',3,2, base_day + interval '1 day', base_day + interval '1 day', base_day + interval '2 days', base_day + interval '2 days' + interval '2 hours', 4, demo_project_id, demo_user_id),
    ('Regression run','',3,3, base_day + interval '3 days', base_day + interval '3 days', base_day + interval '5 days', base_day + interval '4 days' + interval '6 hours', 6, demo_project_id, demo_user_id),
    ('Bug triage','',1,2, base_day + interval '6 days', base_day + interval '6 days', null, base_day + interval '8 days', 2, demo_project_id, demo_user_id),
    ('Write report','',3,2, base_day + interval '8 days', base_day + interval '8 days', base_day + interval '9 days', base_day + interval '9 days', 3, demo_project_id, demo_user_id),
    ('Hotfix verification','',3,4, base_day + interval '10 days', base_day + interval '10 days', base_day + interval '12 days', base_day + interval '11 days', 5, demo_project_id, demo_user_id);

  -- Time logs (ended entries only)
  -- Link logs to tasks by selecting task ids by title.
  INSERT INTO "TimeLogs" ("StartTime","EndTime","Comment","IsManual","TaskId","UserId")
  SELECT base_day + interval '1 day' + interval '09:00', base_day + interval '1 day' + interval '12:30', 'Planning', true, t."Id", demo_user_id
  FROM "Tasks" t WHERE t."Title"='Prepare test plan';

  INSERT INTO "TimeLogs" ("StartTime","EndTime","Comment","IsManual","TaskId","UserId")
  SELECT base_day + interval '3 days' + interval '10:00', base_day + interval '3 days' + interval '13:00', 'Run suite', true, t."Id", demo_user_id
  FROM "Tasks" t WHERE t."Title"='Regression run';

  INSERT INTO "TimeLogs" ("StartTime","EndTime","Comment","IsManual","TaskId","UserId")
  SELECT base_day + interval '5 days' + interval '14:00', base_day + interval '5 days' + interval '17:00', 'Fix rechecks', true, t."Id", demo_user_id
  FROM "Tasks" t WHERE t."Title"='Regression run';

  INSERT INTO "TimeLogs" ("StartTime","EndTime","Comment","IsManual","TaskId","UserId")
  SELECT base_day + interval '8 days' + interval '09:30', base_day + interval '8 days' + interval '11:00', 'Draft', true, t."Id", demo_user_id
  FROM "Tasks" t WHERE t."Title"='Write report';

  INSERT INTO "TimeLogs" ("StartTime","EndTime","Comment","IsManual","TaskId","UserId")
  SELECT base_day + interval '10 days' + interval '11:00', base_day + interval '10 days' + interval '16:00', 'Verify', true, t."Id", demo_user_id
  FROM "Tasks" t WHERE t."Title"='Hotfix verification';
END $$;
