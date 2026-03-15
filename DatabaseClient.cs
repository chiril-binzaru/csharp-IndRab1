using Microsoft.Data.SqlClient;
using System.Data;

namespace IndRab1;

public class DatabaseClient
{
    private const string ConnectionString =
        "Server=localhost\\SQLEXPRESS;" +
        "Database=EventManagerDB;" +
        "User Id=EventAdmin;" +
        "Password=Event@dmin123;" +
        "TrustServerCertificate=True;";

    public static SqlConnection GetConnection()
    {
        return new SqlConnection(ConnectionString);
    }

    // ===== EVENTS =====

    public static DataTable GetEvents()
    {
        using var conn = GetConnection();
        var query = "SELECT EventId, Title, EventDate, Location, EventType FROM Events";
        var adapter = new SqlDataAdapter(query, conn);
        var table = new DataTable();
        adapter.Fill(table);
        return table;
    }

    public static void AddEvent(string title, DateTime date, string location, string eventType)
    {
        using var conn = GetConnection();
        conn.Open();
        var cmd = new SqlCommand(
            "INSERT INTO Events (Title, EventDate, Location, EventType) VALUES (@t, @d, @l, @et)", conn);
        cmd.Parameters.AddWithValue("@t", title);
        cmd.Parameters.AddWithValue("@d", date);
        cmd.Parameters.AddWithValue("@l", location);
        cmd.Parameters.AddWithValue("@et", eventType);
        cmd.ExecuteNonQuery();
    }

    public static void UpdateEvent(int id, string title, DateTime date, string location, string eventType)
    {
        using var conn = GetConnection();
        conn.Open();
        var cmd = new SqlCommand(
            "UPDATE Events SET Title=@t, EventDate=@d, Location=@l, EventType=@et WHERE EventId=@id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@t", title);
        cmd.Parameters.AddWithValue("@d", date);
        cmd.Parameters.AddWithValue("@l", location);
        cmd.Parameters.AddWithValue("@et", eventType);
        cmd.ExecuteNonQuery();
    }

    public static int GetEventRegistrationCount(int eventId)
    {
        using var conn = GetConnection();
        conn.Open();
        var cmd = new SqlCommand(
            "SELECT COUNT(*) FROM Registrations WHERE EventId=@id", conn);
        cmd.Parameters.AddWithValue("@id", eventId);
        return (int)cmd.ExecuteScalar();
    }

    public static void DeleteEvent(int id)
    {
        using var conn = GetConnection();
        conn.Open();
        var cmd = new SqlCommand("DELETE FROM Events WHERE EventId=@id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    // ===== PARTICIPANTS =====

    public static DataTable GetParticipants()
    {
        using var conn = GetConnection();
        var adapter = new SqlDataAdapter(
            "SELECT ParticipantId, FirstName, LastName, Email FROM Participants", conn);
        var table = new DataTable();
        adapter.Fill(table);
        return table;
    }

    public static void AddParticipant(string firstName, string lastName, string email)
    {
        using var conn = GetConnection();
        conn.Open();
        var cmd = new SqlCommand("EXEC sp_AddParticipant @fn, @ln, @em", conn);
        cmd.Parameters.AddWithValue("@fn", firstName);
        cmd.Parameters.AddWithValue("@ln", lastName);
        cmd.Parameters.AddWithValue("@em", email);
        cmd.ExecuteNonQuery();
    }

    public static void UpdateParticipant(int id, string firstName, string lastName, string email)
    {
        using var conn = GetConnection();
        conn.Open();
        var cmd = new SqlCommand(
            "UPDATE Participants SET FirstName=@fn, LastName=@ln, Email=@em WHERE ParticipantId=@id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@fn", firstName);
        cmd.Parameters.AddWithValue("@ln", lastName);
        cmd.Parameters.AddWithValue("@em", email);
        cmd.ExecuteNonQuery();
    }

    public static int GetParticipantRegistrationCount(int participantId)
    {
        using var conn = GetConnection();
        conn.Open();
        var cmd = new SqlCommand(
            "SELECT COUNT(*) FROM Registrations WHERE ParticipantId=@id", conn);
        cmd.Parameters.AddWithValue("@id", participantId);
        return (int)cmd.ExecuteScalar();
    }

    public static void DeleteParticipant(int id)
    {
        using var conn = GetConnection();
        conn.Open();
        var cmd = new SqlCommand("DELETE FROM Participants WHERE ParticipantId=@id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    // ===== REGISTRATIONS =====

    public static DataTable GetRegistrations()
    {
        using var conn = GetConnection();
        var adapter = new SqlDataAdapter("SELECT * FROM vw_RegistrationDetails", conn);
        var table = new DataTable();
        adapter.Fill(table);
        return table;
    }

    public static void AddRegistration(int eventId, int participantId, string status)
    {
        using var conn = GetConnection();
        conn.Open();
        var cmd = new SqlCommand("EXEC sp_RegisterParticipant @eid, @pid, @s", conn);
        cmd.Parameters.AddWithValue("@eid", eventId);
        cmd.Parameters.AddWithValue("@pid", participantId);
        cmd.Parameters.AddWithValue("@s", status);
        cmd.ExecuteNonQuery();
    }

    public static void UpdateRegistrationStatus(int registrationId, string status)
    {
        using var conn = GetConnection();
        conn.Open();
        var cmd = new SqlCommand(
            "UPDATE Registrations SET Status=@s WHERE RegistrationId=@id", conn);
        cmd.Parameters.AddWithValue("@id", registrationId);
        cmd.Parameters.AddWithValue("@s", status);
        cmd.ExecuteNonQuery();
    }

    public static void DeleteRegistration(int id)
    {
        using var conn = GetConnection();
        conn.Open();
        var cmd = new SqlCommand("DELETE FROM Registrations WHERE RegistrationId=@id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }
}
