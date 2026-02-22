using Npgsql;
using System.Globalization;

static string GetConnectionString()
{
    // Keep in sync with diplom.Data/AppDbContext.cs default.
    return Environment.GetEnvironmentVariable("TIMEFLOW_CONNECTION_STRING")
           ?? "Host=localhost;Port=5432;Database=TimeFlow;Username=postgres;Password=123";
}

static async Task<int> ExecuteScalarIntAsync(NpgsqlCommand cmd)
{
    var result = await cmd.ExecuteScalarAsync();
    return Convert.ToInt32(result, CultureInfo.InvariantCulture);
}

var connString = GetConnectionString();
await using var conn = new NpgsqlConnection(connString);
await conn.OpenAsync();

await using var tx = await conn.BeginTransactionAsync();

const string demoUsername = "demo.employee";
const string demoFullName = "Demo Employee";
const string demoJobTitle = "QA Engineer";
const int demoRoleEmployee = 0; // UserRole.Employee
const bool demoIsActive = true;
const string demoPasswordHash = "seed-only-not-for-login";

// 1) Ensure demo user exists.
int demoUserId;
await using (var cmd = new NpgsqlCommand("""
SELECT "Id" FROM "Users" WHERE "Username" = @username LIMIT 1;
""", conn, tx))
{
    cmd.Parameters.AddWithValue("username", demoUsername);
    var found = await cmd.ExecuteScalarAsync();
    if (found is null)
    {
        await using var insert = new NpgsqlCommand("""
INSERT INTO "Users" ("Username","PasswordHash","FullName","JobTitle","Role","IsActive","LastSeenUtc","CurrentSessionId")
VALUES (@username,@hash,@fullName,@jobTitle,@role,@active,NULL,NULL)
RETURNING "Id";
""", conn, tx);
        insert.Parameters.AddWithValue("username", demoUsername);
        insert.Parameters.AddWithValue("hash", demoPasswordHash);
        insert.Parameters.AddWithValue("fullName", demoFullName);
        insert.Parameters.AddWithValue("jobTitle", demoJobTitle);
        insert.Parameters.AddWithValue("role", demoRoleEmployee);
        insert.Parameters.AddWithValue("active", demoIsActive);
        demoUserId = await ExecuteScalarIntAsync(insert);
    }
    else
    {
        demoUserId = Convert.ToInt32(found, CultureInfo.InvariantCulture);
    }
}

// 2) Ensure demo project exists.
int demoProjectId;
await using (var cmd = new NpgsqlCommand("""
SELECT "Id" FROM "Projects" WHERE "Title" = 'Demo Project' LIMIT 1;
""", conn, tx))
{
    var found = await cmd.ExecuteScalarAsync();
    if (found is null)
    {
        await using var insert = new NpgsqlCommand("""
INSERT INTO "Projects" ("Title","Description","CreatedAt","IsArchived")
VALUES ('Demo Project','Seeded for analytics demo', (now() at time zone 'utc'), false)
RETURNING "Id";
""", conn, tx);
        demoProjectId = await ExecuteScalarIntAsync(insert);
    }
    else
    {
        demoProjectId = Convert.ToInt32(found, CultureInfo.InvariantCulture);
    }
}

// 3) Clear previous demo tasks + logs for this user/project to avoid duplicates.
await using (var delLogs = new NpgsqlCommand("""
DELETE FROM "TimeLogs"
WHERE "UserId" = @uid
  AND "TaskId" IN (SELECT "Id" FROM "Tasks" WHERE "ProjectId" = @pid AND "AssigneeId" = @uid);
""", conn, tx))
{
    delLogs.Parameters.AddWithValue("uid", demoUserId);
    delLogs.Parameters.AddWithValue("pid", demoProjectId);
    await delLogs.ExecuteNonQueryAsync();
}

await using (var delTasks = new NpgsqlCommand("""
DELETE FROM "Tasks" WHERE "ProjectId" = @pid AND "AssigneeId" = @uid;
""", conn, tx))
{
    delTasks.Parameters.AddWithValue("uid", demoUserId);
    delTasks.Parameters.AddWithValue("pid", demoProjectId);
    await delTasks.ExecuteNonQueryAsync();
}

// 4) Insert tasks + ended time logs across 14 days in UTC.
var baseDay = DateTime.UtcNow.Date.AddDays(-13);

record SeedTask(
    string Title,
    int Status,
    int Priority,
    int CreatedOffsetDays,
    int AssignedOffsetDays,
    int? CompletedOffsetDays,
    TimeSpan? DeadlineOffset,
    double EstimatedHours);

// Status: ToDo=0, InProgress=1, OnHold=2, Done=3
var tasksToSeed = new[]
{
    new SeedTask("Prepare test plan", 3, 2, 1, 1, 2, TimeSpan.FromDays(2) + TimeSpan.FromHours(2), 4),
    new SeedTask("Regression run", 3, 3, 3, 3, 5, TimeSpan.FromDays(4) + TimeSpan.FromHours(6), 6), // done overdue
    new SeedTask("Bug triage", 1, 2, 6, 6, null, TimeSpan.FromDays(8), 2),
    new SeedTask("Write report", 3, 2, 8, 8, 9, TimeSpan.FromDays(9), 3),
    new SeedTask("Hotfix verification", 3, 4, 10, 10, 12, TimeSpan.FromDays(11), 5) // done overdue
};

var taskIdByTitle = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

foreach (var t in tasksToSeed)
{
    var createdAt = baseDay.AddDays(t.CreatedOffsetDays);
    var assignedAt = baseDay.AddDays(t.AssignedOffsetDays);
    DateTime? completedAt = t.CompletedOffsetDays.HasValue ? baseDay.AddDays(t.CompletedOffsetDays.Value) : null;
    DateTime? deadline = t.DeadlineOffset.HasValue ? baseDay.Add(t.DeadlineOffset.Value) : null;

    await using var insert = new NpgsqlCommand("""
INSERT INTO "Tasks" ("Title","Description","Status","Priority","CreatedAt","AssignedAtUtc","CompletedAtUtc","Deadline","EstimatedHours","ProjectId","AssigneeId")
VALUES (@title,'',@status,@priority,@createdAt,@assignedAt,@completedAt,@deadline,@est,@pid,@uid)
RETURNING "Id";
""", conn, tx);
    insert.Parameters.AddWithValue("title", t.Title);
    insert.Parameters.AddWithValue("status", t.Status);
    insert.Parameters.AddWithValue("priority", t.Priority);
    insert.Parameters.AddWithValue("createdAt", createdAt);
    insert.Parameters.AddWithValue("assignedAt", assignedAt);
    insert.Parameters.AddWithValue("completedAt", (object?)completedAt ?? DBNull.Value);
    insert.Parameters.AddWithValue("deadline", (object?)deadline ?? DBNull.Value);
    insert.Parameters.AddWithValue("est", t.EstimatedHours);
    insert.Parameters.AddWithValue("pid", demoProjectId);
    insert.Parameters.AddWithValue("uid", demoUserId);
    var taskId = await ExecuteScalarIntAsync(insert);
    taskIdByTitle[t.Title] = taskId;
}

static async Task InsertLogAsync(NpgsqlConnection conn, NpgsqlTransaction tx, int userId, int taskId, DateTime startUtc, DateTime endUtc, string comment)
{
    await using var cmd = new NpgsqlCommand("""
INSERT INTO "TimeLogs" ("StartTime","EndTime","Comment","IsManual","TaskId","UserId")
VALUES (@start,@end,@comment,true,@taskId,@userId);
""", conn, tx);
    cmd.Parameters.AddWithValue("start", startUtc);
    cmd.Parameters.AddWithValue("end", endUtc);
    cmd.Parameters.AddWithValue("comment", comment);
    cmd.Parameters.AddWithValue("taskId", taskId);
    cmd.Parameters.AddWithValue("userId", userId);
    await cmd.ExecuteNonQueryAsync();
}

await InsertLogAsync(conn, tx, demoUserId, taskIdByTitle["Prepare test plan"],
    baseDay.AddDays(1).AddHours(9), baseDay.AddDays(1).AddHours(12.5), "Planning");

await InsertLogAsync(conn, tx, demoUserId, taskIdByTitle["Regression run"],
    baseDay.AddDays(3).AddHours(10), baseDay.AddDays(3).AddHours(13), "Run suite");
await InsertLogAsync(conn, tx, demoUserId, taskIdByTitle["Regression run"],
    baseDay.AddDays(5).AddHours(14), baseDay.AddDays(5).AddHours(17), "Rechecks");

await InsertLogAsync(conn, tx, demoUserId, taskIdByTitle["Write report"],
    baseDay.AddDays(8).AddHours(9.5), baseDay.AddDays(8).AddHours(11), "Draft");

await InsertLogAsync(conn, tx, demoUserId, taskIdByTitle["Hotfix verification"],
    baseDay.AddDays(10).AddHours(11), baseDay.AddDays(10).AddHours(16), "Verify");

await tx.CommitAsync();

Console.WriteLine($"Seeded demo analytics data for userId={demoUserId}, projectId={demoProjectId}.");

