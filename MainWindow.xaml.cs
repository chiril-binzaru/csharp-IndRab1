using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using MessageBox = System.Windows.MessageBox;
using Microsoft.Data.SqlClient;

namespace IndRab1;

public partial class MainWindow : Window
{
    private int? _selectedEventId;
    private int? _selectedParticipantId;

    public MainWindow()
    {
        InitializeComponent();
        LoadEvents();
        LoadParticipants();
        LoadRegistrations();
        PopulateRegistrationComboBoxes();
        PopulateReportEventComboBox();
    }

    // ===== EVENTS =====

    private DataView? _eventsView;

    private void LoadEvents()
    {
        try
        {
            _eventsView = DatabaseClient.GetEvents().DefaultView;
            dgEvents.ItemsSource = _eventsView;
            PopulateEventTypeFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading events: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void PopulateEventTypeFilter()
    {
        if (_eventsView == null) return;
        var selected = (cbFilterType.SelectedItem as ComboBoxItem)?.Content?.ToString();

        cbFilterType.SelectionChanged -= TxtFilterEvents_Changed;
        cbFilterType.Items.Clear();
        cbFilterType.Items.Add(new ComboBoxItem { Content = "All" });
        foreach (DataRowView row in _eventsView)
        {
            var type = row["EventType"].ToString()!;
            if (cbFilterType.Items.Cast<ComboBoxItem>().All(i => i.Content.ToString() != type))
                cbFilterType.Items.Add(new ComboBoxItem { Content = type });
        }
        cbFilterType.SelectedItem = cbFilterType.Items.Cast<ComboBoxItem>()
            .FirstOrDefault(i => i.Content.ToString() == selected)
            ?? cbFilterType.Items[0];
        cbFilterType.SelectionChanged += TxtFilterEvents_Changed;
    }

    private void TxtFilterEvents_Changed(object sender, EventArgs e)
    {
        if (_eventsView == null) return;

        var location = txtFilterLocation.Text.Trim().Replace("'", "''");
        var type = (cbFilterType.SelectedItem as ComboBoxItem)?.Content?.ToString();

        var filters = new List<string>();
        if (!string.IsNullOrEmpty(location))
            filters.Add($"Location LIKE '%{location}%'");
        if (!string.IsNullOrEmpty(type) && type != "All")
            filters.Add($"EventType = '{type}'");

        _eventsView.RowFilter = string.Join(" AND ", filters);
    }

    private void BtnClearEventFilters_Click(object sender, RoutedEventArgs e)
    {
        txtFilterLocation.Text = "";
        cbFilterType.SelectedIndex = 0;
    }

    private void DgEvents_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (dgEvents.SelectedItem is DataRowView row)
        {
            _selectedEventId = (int)row["EventId"];
            txtEventTitle.Text = row["Title"].ToString();
            dpEventDate.SelectedDate = (DateTime)row["EventDate"];
            txtEventLocation.Text = row["Location"].ToString();
            txtEventType.Text = row["EventType"].ToString();
        }
    }

    private void BtnAddEvent_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateEventFields()) return;

        try
        {
            DatabaseClient.AddEvent(
                txtEventTitle.Text.Trim(),
                dpEventDate.SelectedDate!.Value,
                txtEventLocation.Text.Trim(),
                txtEventType.Text.Trim());

            LoadEvents();
            ClearEventFields();
            MessageBox.Show("Event added.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            MessageBox.Show("An event with this title already exists.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error adding event: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnEditEvent_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedEventId == null)
        {
            MessageBox.Show("Select an event to edit.", "Warning",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!ValidateEventFields()) return;

        try
        {
            DatabaseClient.UpdateEvent(
                _selectedEventId.Value,
                txtEventTitle.Text.Trim(),
                dpEventDate.SelectedDate!.Value,
                txtEventLocation.Text.Trim(),
                txtEventType.Text.Trim());

            LoadEvents();
            LoadRegistrations();
            PopulateRegistrationComboBoxes();
            ClearEventFields();
            MessageBox.Show("Event updated.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            MessageBox.Show("An event with this title already exists.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error updating event: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnDeleteEvent_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedEventId == null)
        {
            MessageBox.Show("Select an event to delete.", "Warning",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        int regCount = DatabaseClient.GetEventRegistrationCount(_selectedEventId.Value);
        string message = regCount > 0
            ? $"This event has {regCount} registration(s) that will also be deleted. Are you sure?"
            : "Are you sure you want to delete this event?";

        var result = MessageBox.Show(message, "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        try
        {
            DatabaseClient.DeleteEvent(_selectedEventId.Value);
            LoadEvents();
            LoadRegistrations();
            PopulateRegistrationComboBoxes();
            ClearEventFields();
            MessageBox.Show("Event deleted.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error deleting event: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnClearEvent_Click(object sender, RoutedEventArgs e)
    {
        ClearEventFields();
    }

    private void ClearEventFields()
    {
        _selectedEventId = null;
        txtEventTitle.Text = "";
        dpEventDate.SelectedDate = null;
        txtEventLocation.Text = "";
        txtEventType.Text = "";
        dgEvents.SelectedItem = null;
    }

    private bool ValidateEventFields()
    {
        if (string.IsNullOrWhiteSpace(txtEventTitle.Text) ||
            dpEventDate.SelectedDate == null ||
            string.IsNullOrWhiteSpace(txtEventLocation.Text) ||
            string.IsNullOrWhiteSpace(txtEventType.Text))
        {
            MessageBox.Show("Please fill in all fields.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        return true;
    }

    // ===== PARTICIPANTS =====

    private DataView? _participantsView;

    private void LoadParticipants()
    {
        try
        {
            _participantsView = DatabaseClient.GetParticipants().DefaultView;
            dgParticipants.ItemsSource = _participantsView;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading participants: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void TxtParticipantSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_participantsView == null) return;

        var term = txtParticipantSearch.Text.Trim().Replace("'", "''");
        _participantsView.RowFilter = string.IsNullOrEmpty(term)
            ? ""
            : $"FirstName LIKE '%{term}%' OR LastName LIKE '%{term}%' OR Email LIKE '%{term}%'";
    }

    private void BtnClearParticipantSearch_Click(object sender, RoutedEventArgs e)
    {
        txtParticipantSearch.Text = "";
    }

    private void DgParticipants_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (dgParticipants.SelectedItem is DataRowView row)
        {
            _selectedParticipantId = (int)row["ParticipantId"];
            txtFirstName.Text = row["FirstName"].ToString();
            txtLastName.Text  = row["LastName"].ToString();
            txtEmail.Text     = row["Email"].ToString();
        }
    }

    private void BtnAddParticipant_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateParticipantFields()) return;

        try
        {
            DatabaseClient.AddParticipant(
                txtFirstName.Text.Trim(),
                txtLastName.Text.Trim(),
                txtEmail.Text.Trim());

            LoadParticipants();
            ClearParticipantFields();
            MessageBox.Show("Participant added.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error adding participant: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnEditParticipant_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedParticipantId == null)
        {
            MessageBox.Show("Select a participant to edit.", "Warning",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!ValidateParticipantFields()) return;

        try
        {
            DatabaseClient.UpdateParticipant(
                _selectedParticipantId.Value,
                txtFirstName.Text.Trim(),
                txtLastName.Text.Trim(),
                txtEmail.Text.Trim());

            LoadParticipants();
            LoadRegistrations();
            PopulateRegistrationComboBoxes();
            ClearParticipantFields();
            MessageBox.Show("Participant updated.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error updating participant: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnDeleteParticipant_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedParticipantId == null)
        {
            MessageBox.Show("Select a participant to delete.", "Warning",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        int regCount = DatabaseClient.GetParticipantRegistrationCount(_selectedParticipantId.Value);
        string message = regCount > 0
            ? $"This participant has {regCount} registration(s) that will also be deleted. Are you sure?"
            : "Are you sure you want to delete this participant?";

        var result = MessageBox.Show(message, "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        try
        {
            DatabaseClient.DeleteParticipant(_selectedParticipantId.Value);
            LoadParticipants();
            LoadRegistrations();
            PopulateRegistrationComboBoxes();
            ClearParticipantFields();
            MessageBox.Show("Participant deleted.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error deleting participant: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnClearParticipant_Click(object sender, RoutedEventArgs e)
    {
        ClearParticipantFields();
    }

    private void ClearParticipantFields()
    {
        _selectedParticipantId = null;
        txtFirstName.Text = "";
        txtLastName.Text  = "";
        txtEmail.Text     = "";
        dgParticipants.SelectedItem = null;
    }

    private bool ValidateParticipantFields()
    {
        if (string.IsNullOrWhiteSpace(txtFirstName.Text) ||
            string.IsNullOrWhiteSpace(txtLastName.Text)  ||
            string.IsNullOrWhiteSpace(txtEmail.Text))
        {
            MessageBox.Show("Please fill in all fields.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (!Regex.IsMatch(txtEmail.Text.Trim(), @"^[^@\s]+@(gmail\.com|yahoo\.com|outlook\.com|hotmail\.com|mail\.ru|icloud\.com|ukr\.net)$", RegexOptions.IgnoreCase))
        {
            MessageBox.Show("Please enter a valid email address.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        return true;
    }

    // ===== REGISTRATIONS =====

    private int? _selectedRegistrationId;

    private void PopulateRegistrationComboBoxes()
    {
        try
        {
            cbRegEvent.ItemsSource = DatabaseClient.GetEvents().DefaultView;

            var participants = DatabaseClient.GetParticipants();
            participants.Columns.Add("FullName", typeof(string), "FirstName + ' ' + LastName");
            cbRegParticipant.ItemsSource = participants.DefaultView;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading combo data: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private DataView? _registrationsView;

    private void LoadRegistrations()
    {
        try
        {
            _registrationsView = DatabaseClient.GetRegistrations().DefaultView;
            dgRegistrations.ItemsSource = _registrationsView;
            ApplyRegStatusFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading registrations: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CbFilterRegStatus_Changed(object sender, SelectionChangedEventArgs e)
    {
        ApplyRegStatusFilter();
    }

    private void ApplyRegStatusFilter()
    {
        if (_registrationsView == null) return;
        var status = (cbFilterRegStatus.SelectedItem as ComboBoxItem)?.Content?.ToString();
        _registrationsView.RowFilter = string.IsNullOrEmpty(status) || status == "All"
            ? ""
            : $"Status = '{status}'";
    }

    private void BtnClearRegFilter_Click(object sender, RoutedEventArgs e)
    {
        cbFilterRegStatus.SelectedIndex = 0;
    }

    private void DgRegistrations_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (dgRegistrations.SelectedItem is DataRowView row)
        {
            _selectedRegistrationId = (int)row["RegistrationId"];

            var statusItem = cbRegStatus.Items.Cast<ComboBoxItem>()
                .FirstOrDefault(i => i.Content.ToString() == row["Status"].ToString());
            cbRegStatus.SelectedItem = statusItem;
        }
    }

    private void BtnAddReg_Click(object sender, RoutedEventArgs e)
    {
        if (cbRegEvent.SelectedValue == null || cbRegParticipant.SelectedValue == null || cbRegStatus.SelectedItem == null)
        {
            MessageBox.Show("Please select an event, participant, and status.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            DatabaseClient.AddRegistration(
                (int)cbRegEvent.SelectedValue,
                (int)cbRegParticipant.SelectedValue,
                ((ComboBoxItem)cbRegStatus.SelectedItem).Content.ToString()!);

            LoadRegistrations();
            ClearRegFields();
            MessageBox.Show("Registration added.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error adding registration: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnUpdateReg_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedRegistrationId == null)
        {
            MessageBox.Show("Select a registration to update.", "Warning",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (cbRegStatus.SelectedItem == null)
        {
            MessageBox.Show("Please select a status.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            DatabaseClient.UpdateRegistrationStatus(
                _selectedRegistrationId.Value,
                ((ComboBoxItem)cbRegStatus.SelectedItem).Content.ToString()!);

            LoadRegistrations();
            ClearRegFields();
            MessageBox.Show("Status updated.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error updating status: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnDeleteReg_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedRegistrationId == null)
        {
            MessageBox.Show("Select a registration to delete.", "Warning",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = MessageBox.Show("Are you sure you want to delete this registration?",
            "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;

        try
        {
            DatabaseClient.DeleteRegistration(_selectedRegistrationId.Value);
            LoadRegistrations();
            ClearRegFields();
            MessageBox.Show("Registration deleted.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error deleting registration: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnClearReg_Click(object sender, RoutedEventArgs e)
    {
        ClearRegFields();
    }

    private void ClearRegFields()
    {
        _selectedRegistrationId = null;
        cbRegEvent.SelectedItem       = null;
        cbRegParticipant.SelectedItem = null;
        cbRegStatus.SelectedItem      = null;
        dgRegistrations.SelectedItem  = null;
    }

    // ===== REPORTS =====

    private void PopulateReportEventComboBox()
    {
        try
        {
            cbReportEvent.ItemsSource = DatabaseClient.GetEvents().DefaultView;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading events for report: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnReportParticipants_Click(object sender, RoutedEventArgs e)
    {
        if (cbReportEvent.SelectedValue == null)
        {
            MessageBox.Show("Please select an event.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            int eventId = (int)cbReportEvent.SelectedValue;
            string eventTitle = ((DataRowView)cbReportEvent.SelectedItem)["Title"].ToString()!;
            var data = DatabaseClient.GetParticipantsByEvent(eventId);
            ReportHelper.ShowParticipantsByEventReport(eventTitle, data);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error generating report: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnReportPeriod_Click(object sender, RoutedEventArgs e)
    {
        if (dpReportFrom.SelectedDate == null || dpReportTo.SelectedDate == null)
        {
            MessageBox.Show("Please select both dates.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (dpReportFrom.SelectedDate > dpReportTo.SelectedDate)
        {
            MessageBox.Show("'From' date must be before 'To' date.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var data = DatabaseClient.GetEventsByPeriod(
                dpReportFrom.SelectedDate.Value,
                dpReportTo.SelectedDate.Value);
            ReportHelper.ShowEventsByPeriodReport(
                dpReportFrom.SelectedDate.Value,
                dpReportTo.SelectedDate.Value,
                data);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error generating report: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
