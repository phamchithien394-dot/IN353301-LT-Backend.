using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString) || connectionString.Contains("YOUR_PASSWORD", StringComparison.Ordinal))
{
    throw new InvalidOperationException(
        "Chưa cấu hình chuỗi kết nối PostgreSQL thật. Hãy tạo src/appsettings.Development.json " +
        "hoặc đặt biến môi trường ConnectionStrings__DefaultConnection trước khi chạy.");
}

builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));

var app = builder.Build();

app.UseExceptionHandler();

// 1. GET: Xem danh sách đăng ký của một khoá học
app.MapGet("/api/v1/courses/{courseId:int}/registrations", async (
    int courseId,
    NpgsqlDataSource dataSource,
    HttpContext httpContext,
    CancellationToken ct) =>
{
    if (ValidatePositiveId(courseId, "courseId", httpContext) is { } invalid)
    {
        return invalid;
    }

    await using var conn = await dataSource.OpenConnectionAsync(ct);

    if (!await RecordExistsAsync(conn, "SELECT 1 FROM courses WHERE id = @id", courseId, ct))
    {
        return CreateProblem(
            StatusCodes.Status404NotFound,
            "https://eduhub.local/errors/course-not-found",
            "Course not found",
            $"Course {courseId} was not found.",
            httpContext);
    }

    const string sql = """
        SELECT r.id, r.member_id, m.full_name, m.email, r.course_id, r.status
        FROM registrations r
        JOIN members m ON m.id = r.member_id
        WHERE r.course_id = @courseId
        ORDER BY r.id ASC;
        """;

    await using var cmd = new NpgsqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("courseId", courseId);

    var items = new List<RegistrationDetailDto>();
    await using var reader = await cmd.ExecuteReaderAsync(ct);
    while (await reader.ReadAsync(ct))
    {
        items.Add(new RegistrationDetailDto(
            Id: reader.GetInt32(0),
            MemberId: reader.GetInt32(1),
            MemberName: reader.GetString(2),
            MemberEmail: reader.GetString(3),
            CourseId: reader.GetInt32(4),
            Status: reader.GetString(5)));
    }

    return Results.Ok(items);
});

// 2. POST: Tạo đăng ký mới cho một khoá học
app.MapPost("/api/v1/courses/{courseId:int}/registrations", async (
    int courseId,
    CreateRegistrationRequest request,
    NpgsqlDataSource dataSource,
    HttpContext httpContext,
    CancellationToken ct) =>
{
    if (ValidatePositiveId(courseId, "courseId", httpContext) is { } invalidCourse)
    {
        return invalidCourse;
    }

    if (ValidatePositiveId(request.MemberId, "memberId", httpContext) is { } invalidMember)
    {
        return invalidMember;
    }

    await using var conn = await dataSource.OpenConnectionAsync(ct);

    var courseCapacity = await GetCourseCapacityAsync(conn, courseId, ct);
    if (courseCapacity is null)
    {
        return CreateProblem(
            StatusCodes.Status404NotFound,
            "https://eduhub.local/errors/course-not-found",
            "Course not found",
            $"Course {courseId} was not found.",
            httpContext);
    }

    if (!await RecordExistsAsync(conn, "SELECT 1 FROM members WHERE id = @id", request.MemberId, ct))
    {
        return CreateProblem(
            StatusCodes.Status404NotFound,
            "https://eduhub.local/errors/member-not-found",
            "Member not found",
            $"Member {request.MemberId} was not found.",
            httpContext);
    }

    var activeCount = await GetActiveRegistrationCountAsync(conn, courseId, ct);
    if (activeCount >= courseCapacity.Value)
    {
        return CreateProblem(
            StatusCodes.Status409Conflict,
            "https://eduhub.local/errors/course-full",
            "Course capacity reached",
            $"Course {courseId} has reached its capacity of {courseCapacity.Value}.",
            httpContext);
    }

    const string insertSql = """
        INSERT INTO registrations (member_id, course_id)
        VALUES (@memberId, @courseId)
        RETURNING id, member_id, course_id, status;
        """;

    await using var cmd = new NpgsqlCommand(insertSql, conn);
    cmd.Parameters.AddWithValue("memberId", request.MemberId);
    cmd.Parameters.AddWithValue("courseId", courseId);

    try
    {
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        await reader.ReadAsync(ct);

        var created = new RegistrationDto(
            Id: reader.GetInt32(0),
            MemberId: reader.GetInt32(1),
            CourseId: reader.GetInt32(2),
            Status: reader.GetString(3));

        return Results.Created($"/api/v1/registrations/{created.Id}", created);
    }
    catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
    {
        return CreateProblem(
            StatusCodes.Status409Conflict,
            "https://eduhub.local/errors/duplicate-registration",
            "Duplicate registration",
            $"Member {request.MemberId} has already registered for course {courseId}.",
            httpContext);
    }
});

// 3. PATCH: Xác nhận (hoặc cập nhật trạng thái) phiếu đăng ký
app.MapPatch("/api/v1/registrations/{registrationId:int}", async (
    int registrationId,
    UpdateRegistrationStatusRequest request,
    NpgsqlDataSource dataSource,
    HttpContext httpContext,
    CancellationToken ct) =>
{
    if (ValidatePositiveId(registrationId, "registrationId", httpContext) is { } invalidId)
    {
        return invalidId;
    }

    var normalizedStatus = request.Status?.Trim().ToLowerInvariant();
    if (normalizedStatus is not ("confirmed" or "cancelled"))
    {
        return CreateProblem(
            StatusCodes.Status400BadRequest,
            "https://eduhub.local/errors/invalid-status",
            "Invalid registration status",
            "Field 'status' must be 'confirmed' or 'cancelled'.",
            httpContext);
    }

    await using var conn = await dataSource.OpenConnectionAsync(ct);

    var currentStatus = await GetRegistrationStatusAsync(conn, registrationId, ct);
    if (currentStatus is null)
    {
        return CreateProblem(
            StatusCodes.Status404NotFound,
            "https://eduhub.local/errors/registration-not-found",
            "Registration not found",
            $"Registration {registrationId} was not found.",
            httpContext);
    }

    if (currentStatus == "cancelled" && normalizedStatus == "confirmed")
    {
        return CreateProblem(
            StatusCodes.Status409Conflict,
            "https://eduhub.local/errors/invalid-status-transition",
            "Invalid status transition",
            $"Registration {registrationId} is already cancelled and cannot be confirmed.",
            httpContext);
    }

    const string updateSql = """
        UPDATE registrations
        SET status = @status
        WHERE id = @id
        RETURNING id, member_id, course_id, status;
        """;

    await using var cmd = new NpgsqlCommand(updateSql, conn);
    cmd.Parameters.AddWithValue("status", normalizedStatus);
    cmd.Parameters.AddWithValue("id", registrationId);

    await using var reader = await cmd.ExecuteReaderAsync(ct);
    await reader.ReadAsync(ct);

    var updated = new RegistrationDto(
        Id: reader.GetInt32(0),
        MemberId: reader.GetInt32(1),
        CourseId: reader.GetInt32(2),
        Status: reader.GetString(3));

    return Results.Ok(updated);
});

// 4. DELETE: Huỷ đăng ký (chuyển trạng thái sang cancelled)
app.MapDelete("/api/v1/registrations/{registrationId:int}", async (
    int registrationId,
    NpgsqlDataSource dataSource,
    HttpContext httpContext,
    CancellationToken ct) =>
{
    if (ValidatePositiveId(registrationId, "registrationId", httpContext) is { } invalidId)
    {
        return invalidId;
    }

    await using var conn = await dataSource.OpenConnectionAsync(ct);

    var currentStatus = await GetRegistrationStatusAsync(conn, registrationId, ct);
    if (currentStatus is null)
    {
        return CreateProblem(
            StatusCodes.Status404NotFound,
            "https://eduhub.local/errors/registration-not-found",
            "Registration not found",
            $"Registration {registrationId} was not found.",
            httpContext);
    }

    if (currentStatus == "cancelled")
    {
        return CreateProblem(
            StatusCodes.Status409Conflict,
            "https://eduhub.local/errors/already-cancelled",
            "Registration already cancelled",
            $"Registration {registrationId} has already been cancelled.",
            httpContext);
    }

    const string cancelSql = """
        UPDATE registrations
        SET status = 'cancelled'
        WHERE id = @id;
        """;

    await using var cmd = new NpgsqlCommand(cancelSql, conn);
    cmd.Parameters.AddWithValue("id", registrationId);
    await cmd.ExecuteNonQueryAsync(ct);

    return Results.NoContent();
});

app.Run();

static IResult? ValidatePositiveId(int value, string fieldName, HttpContext httpContext) =>
    value > 0
        ? null
        : CreateProblem(
            StatusCodes.Status400BadRequest,
            "https://eduhub.local/errors/invalid-identifier",
            "Invalid identifier",
            $"'{fieldName}' must be a positive integer.",
            httpContext);

static IResult CreateProblem(
    int statusCode,
    string type,
    string title,
    string detail,
    HttpContext httpContext) =>
    Results.Problem(
        detail: detail,
        instance: httpContext.Request.Path,
        statusCode: statusCode,
        title: title,
        type: type);

static async Task<bool> RecordExistsAsync(
    NpgsqlConnection conn,
    string sql,
    int id,
    CancellationToken ct)
{
    await using var cmd = new NpgsqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("id", id);
    return await cmd.ExecuteScalarAsync(ct) is not null;
}

static async Task<int?> GetCourseCapacityAsync(
    NpgsqlConnection conn,
    int courseId,
    CancellationToken ct)
{
    await using var cmd = new NpgsqlCommand("SELECT capacity FROM courses WHERE id = @id;", conn);
    cmd.Parameters.AddWithValue("id", courseId);
    var result = await cmd.ExecuteScalarAsync(ct);
    return result is int capacity ? capacity : null;
}

static async Task<long> GetActiveRegistrationCountAsync(
    NpgsqlConnection conn,
    int courseId,
    CancellationToken ct)
{
    const string sql = """
        SELECT COUNT(*)
        FROM registrations
        WHERE course_id = @courseId AND status IN ('pending', 'confirmed');
        """;

    await using var cmd = new NpgsqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("courseId", courseId);
    var result = await cmd.ExecuteScalarAsync(ct);
    return result is long count ? count : 0;
}

static async Task<string?> GetRegistrationStatusAsync(
    NpgsqlConnection conn,
    int registrationId,
    CancellationToken ct)
{
    await using var cmd = new NpgsqlCommand("SELECT status FROM registrations WHERE id = @id;", conn);
    cmd.Parameters.AddWithValue("id", registrationId);
    var result = await cmd.ExecuteScalarAsync(ct);
    return result as string;
}

internal sealed record CreateRegistrationRequest(int MemberId);

internal sealed record UpdateRegistrationStatusRequest(string? Status);

internal sealed record RegistrationDto(int Id, int MemberId, int CourseId, string Status);

internal sealed record RegistrationDetailDto(
    int Id,
    int MemberId,
    string MemberName,
    string MemberEmail,
    int CourseId,
    string Status);
