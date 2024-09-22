using Info_module.Pages.TableMenus.After_College_Selection.CSVMenu;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Globalization;

namespace Info_module.Pages.TableMenus.After_College_Selection
{
    /// <summary>
    /// Interaction logic for InstructorMenu.xaml
    /// </summary>
    public partial class InstructorMenu : Page
    {
        public int DepartmentId { get; set; }
        public int InternalEmployeeId { get; set; }

        private const string connectionString = @"Server=localhost;Database=universitydb;User ID=root;Password=;";

        public InstructorMenu(int departmentId)
        {
            InitializeComponent();
            TopBar topBar = new TopBar();
            topBar.txtPageTitle.Text = "Instructor Menu";
            topBar.Visibility = Visibility.Visible;
            topBar.BackButtonClicked += TopBar_BackButtonClicked;
            TopBarFrame.Navigate(topBar);

            DepartmentId = departmentId;
            InternalEmployeeId = 0;
            LoadDepartmentDetails();
            LoadInstructors();
            LoadSubjects();
        }

        private void LoadDepartmentDetails()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT Dept_Name, Logo_Image FROM departments WHERE Dept_Id = @deptId";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@deptId", DepartmentId);

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string departmentName = reader["Dept_Name"].ToString();
                            collegeName_txt.Text = departmentName;
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading department details: " + ex.Message);
            }
        }

        private void LoadInstructors(string statusFilter = "Active")
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    // Base query
                    string query = @"
                SELECT i.Internal_Employee_Id, 
                       d.Dept_code AS Department,
                       i.Employee_Id,
                       i.Lname AS LastName, 
                       i.Mname AS MiddleName, 
                       i.Fname AS FirstName, 
                       i.Employment_Type AS Employment, 
                       i.Employee_Sex AS Sex, 
                       i.Email,
                       i.Disability,
                       CASE 
                           WHEN i.Status = 1 THEN 'Active'
                           ELSE 'Inactive'
                       END AS Status
                FROM instructor i
                JOIN departments d ON i.Dept_Id = d.Dept_Id
                WHERE i.Dept_Id = @deptId";

                    // Add a condition for filtering by status
                    if (statusFilter == "Active")
                    {
                        query += " AND i.Status = 1";
                    }
                    else if (statusFilter == "Inactive")
                    {
                        query += " AND i.Status = 0";
                    }

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@deptId", DepartmentId);

                    MySqlDataAdapter dataAdapter = new MySqlDataAdapter(command);
                    DataTable dataTable = new DataTable();
                    dataAdapter.Fill(dataTable);
                    instructor_data.ItemsSource = dataTable.DefaultView;
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading instructor details: " + ex.Message);
            }
        }

        private void Status_cmb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Status_cmb.SelectedItem != null)
            {
                ComboBoxItem selectedItem = (ComboBoxItem)Status_cmb.SelectedItem;
                string selectedStatus = selectedItem.Content.ToString();

                // Pass the selected status to filter the data
                LoadInstructors(selectedStatus);

                // Change button content based on the selected status
                if (selectedStatus == "Active")
                {
                    instructorStatus_btn.Content = "Deactivate"; // For active departments
                    instructorStatus_btn.FontSize = 12;
                }
                else if (selectedStatus == "Inactive")
                {
                    instructorStatus_btn.Content = "Activate"; // For inactive departments
                    instructorStatus_btn.FontSize = 12;
                }
                else
                {
                    instructorStatus_btn.Content = "Switch Status"; // Default text for "All"
                    instructorStatus_btn.FontSize = 10;
                }
            }
        }



        private void NavigateBack(string sourceButton)
        {
            CollegeSelection collegeSelection = new CollegeSelection(sourceButton);
            NavigationService.Navigate(collegeSelection);
        }

        private void TopBar_BackButtonClicked(object sender, EventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            NavigateBack("Instructor");
        }

        private void btnInstrutorCSV_Click(object sender, RoutedEventArgs e)
        {
            InstructorCSV instructorCSV = new InstructorCSV(DepartmentId);
            NavigationService.Navigate(instructorCSV);
        }

        private void instructorAdd_btn_Click(object sender, RoutedEventArgs e)
        {
            // Retrieve data from input fields
            string employeeId = employeeId_txt.Text;
            string firstName = firstName_txt.Text;
            string middleName = middleName_txt.Text;
            string lastName = lastName_txt.Text;
            string employmentType = employment_txt.Text;
            string email = email_txt.Text;
            string sex = male_rbtn.IsChecked == true ? "M" : female_rbtn.IsChecked == true ? "F" : "";
            int disability = disability_ckbox.IsChecked == true ? 1 : 0;

            // Validate inputs
            if (string.IsNullOrWhiteSpace(employeeId) ||
                string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(middleName) ||
                string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(employmentType) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(sex))
            {
                MessageBox.Show("All fields must be filled out.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // Insert into instructor table, including disability
                    string query = @"
                INSERT INTO instructor (Dept_Id, Employee_Id, Fname, Mname, Lname, Employment_Type, Employee_Sex, Email, Disability) 
                VALUES (@Dept_Id, @Employee_Id, @Fname, @Mname, @Lname, @Employment_Type, @Employee_Sex, @Email, @Disability)";

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Dept_Id", DepartmentId);
                    command.Parameters.AddWithValue("@Employee_Id", employeeId);
                    command.Parameters.AddWithValue("@Fname", firstName);
                    command.Parameters.AddWithValue("@Mname", middleName);
                    command.Parameters.AddWithValue("@Lname", lastName);
                    command.Parameters.AddWithValue("@Employment_Type", employmentType);
                    command.Parameters.AddWithValue("@Employee_Sex", sex);
                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@Disability", disability); // Handle disability

                    command.ExecuteNonQuery();
                    MessageBox.Show("Instructor added successfully!");
                    LoadInstructors();

                    // Clear input fields after successful insertion
                    employeeId_txt.Clear();
                    firstName_txt.Clear();
                    middleName_txt.Clear();
                    lastName_txt.Clear();
                    employment_txt.Clear();
                    email_txt.Clear();
                    disability_ckbox.IsChecked = false;
                    male_rbtn.IsChecked = false;
                    female_rbtn.IsChecked = false;
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error adding instructor: " + ex.Message);
            }
        }


        private void instructor_data_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (instructor_data.SelectedItem is DataRowView selectedRow)
            {
                // Set other fields
                InternalEmployeeId = (int)selectedRow["Internal_Employee_Id"];
                employeeId_txt.Text = selectedRow["Employee_Id"].ToString();
                firstName_txt.Text = selectedRow["FirstName"].ToString();
                middleName_txt.Text = selectedRow["MiddleName"].ToString();
                lastName_txt.Text = selectedRow["LastName"].ToString();
                employment_txt.Text = selectedRow["Employment"].ToString();
                email_txt.Text = selectedRow["Email"].ToString();

                // Handle sex selection
                string sex = selectedRow["Sex"].ToString();
                if (sex == "M")
                {
                    male_rbtn.IsChecked = true;
                }
                else if (sex == "F")
                {
                    female_rbtn.IsChecked = true;
                }

                // Handle disability checkbox (assuming Disability can be "1" or "0", or possibly true/false)
                object disabilityValue = selectedRow["Disability"];
                if (disabilityValue != DBNull.Value)
                {
                    // Check if it's a boolean or an integer ("1" or "0")
                    if (disabilityValue is bool)
                    {
                        disability_ckbox.IsChecked = (bool)disabilityValue;
                    }
                    else if (disabilityValue is int)
                    {
                        disability_ckbox.IsChecked = ((int)disabilityValue == 1);
                    }
                    else
                    {
                        // Handle string or other types, just in case
                        disability_ckbox.IsChecked = disabilityValue.ToString() == "1";
                    }
                }
                else
                {
                    disability_ckbox.IsChecked = false; // Default to unchecked if no value
                }

                // Load related data
                LoadInstructorSubjects(InternalEmployeeId);
                LoadAvailability(InternalEmployeeId);
            }
        }



        private void instructorStatus_btn_Click(object sender, RoutedEventArgs e)
        {
            if (instructor_data.SelectedItem is DataRowView selectedRow)
            {
                try
                {
                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        connection.Open();

                        // Get the current status from the selected row
                        bool isActive = selectedRow["Status"].ToString() == "Active";

                        // If the instructor is active, set status to 0 (deactivate); if inactive, set to 1 (activate)
                        string query = isActive
                            ? "UPDATE instructor SET Status = 0 WHERE Internal_Employee_Id = @InternalEmployeeId"
                            : "UPDATE instructor SET Status = 1 WHERE Internal_Employee_Id = @InternalEmployeeId";

                        MySqlCommand command = new MySqlCommand(query, connection);
                        command.Parameters.AddWithValue("@InternalEmployeeId", (int)selectedRow["Internal_Employee_Id"]);

                        int rowsAffected = command.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            // Provide appropriate feedback based on the action performed
                            string message = isActive ? "Instructor deactivated successfully." : "Instructor activated successfully.";
                            MessageBox.Show(message);

                            // Refresh the DataGrid to reflect the updated status
                            LoadInstructors();
                        }
                        else
                        {
                            MessageBox.Show("Error updating instructor status.");
                        }
                    }
                }
                catch (MySqlException ex)
                {
                    MessageBox.Show("Error updating instructor status: " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show("Please select an instructor to change their status.");
            }
        }


        private void instructorSave_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                UPDATE instructor 
                SET Employee_Id = @Employee_Id, Lname = @LastName, Mname = @MiddleName, Fname = @FirstName, 
                    Employment_Type = @Employment, Employee_Sex = @Sex, Email = @Email, Disability = @Disability
                WHERE Internal_Employee_Id = @Internal_Employee_Id";

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Internal_Employee_Id", InternalEmployeeId);
                    command.Parameters.AddWithValue("@Employee_Id", employeeId_txt.Text);
                    command.Parameters.AddWithValue("@LastName", lastName_txt.Text);
                    command.Parameters.AddWithValue("@MiddleName", middleName_txt.Text);
                    command.Parameters.AddWithValue("@FirstName", firstName_txt.Text);
                    command.Parameters.AddWithValue("@Employment", employment_txt.Text);
                    command.Parameters.AddWithValue("@Sex", male_rbtn.IsChecked == true ? "M" : "F");
                    command.Parameters.AddWithValue("@Email", email_txt.Text);
                    command.Parameters.AddWithValue("@Disability", disability_ckbox.IsChecked == true ? 1 : 0); // Handle disability

                    int rowsAffected = command.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Instructor details updated successfully.");
                        LoadInstructors(); // Refresh the DataGrid
                    }
                    else
                    {
                        MessageBox.Show("Error updating instructor details.");
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error updating instructor details: " + ex.Message);
            }
        }


        //   Subejcts   //////////////////////////////////////////
        private void LoadSubjects()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT Subject_Id, Subject_Code FROM subjects WHERE Dept_Id = @Dept_Id";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Dept_Id", DepartmentId);

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        List<Subject> subjects = new List<Subject>();
                        while (reader.Read())
                        {
                            subjects.Add(new Subject
                            {
                                SubjectId = reader.GetInt32("Subject_Id"),
                                SubjectCode = reader.GetString("Subject_Code")
                            });
                        }
                        subjectCode_cmbx.ItemsSource = subjects;
                        subjectCode_cmbx.DisplayMemberPath = "SubjectCode";
                        subjectCode_cmbx.SelectedValuePath = "SubjectId";
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading subjects: " + ex.Message);
            }
        }


        public class Subject
        {
            public int SubjectId { get; set; }
            public string SubjectCode { get; set; }
        }

        private void subjectCode_cmbx_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (subjectCode_cmbx.SelectedItem != null)
            {
                Subject selectedSubject = (Subject)subjectCode_cmbx.SelectedItem;
                int subjectId = selectedSubject.SubjectId;

                try
                {
                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        connection.Open();
                        string query = "SELECT Subject_Title FROM subjects WHERE Subject_Id = @subjectId";
                        MySqlCommand command = new MySqlCommand(query, connection);
                        command.Parameters.AddWithValue("@subjectId", subjectId);

                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string subjectName = reader["Subject_Title"].ToString();
                                subject_txt.Text = subjectName;
                            }
                        }
                    }
                }
                catch (MySqlException ex)
                {
                    MessageBox.Show("Error loading subject details: " + ex.Message);
                }
            }
        }


        private void subjectAdd_btn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(employeeId_txt.Text))
            {
                MessageBox.Show("Please select a valid instructor first.");
                return; // Exit the method if no instructor is selected
            }

            try
            {
                // Get selected values
                int internalEmployeeId = InternalEmployeeId;
                int subjectId = Convert.ToInt32(subjectCode_cmbx.SelectedValue);

                // Check if the subject is already assigned to the instructor
                if (IsSubjectAlreadyAssigned(internalEmployeeId, subjectId))
                {
                    MessageBox.Show("This subject is already assigned to the selected instructor.");
                    return;
                }

                // Insert into database
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "INSERT INTO instructor_subject (Internal_Employee_Id, Subject_Id) VALUES (@InternalEmployeeId, @subjectId)";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@InternalEmployeeId", internalEmployeeId);
                    command.Parameters.AddWithValue("@subjectId", subjectId);
                    command.ExecuteNonQuery();
                }

                LoadInstructorSubjects(internalEmployeeId);
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error adding subject to instructor: " + ex.Message);
            }
        }

        private bool IsSubjectAlreadyAssigned(int employeeId, int subjectId)
        {
            bool isAssigned = false;
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT COUNT(*) FROM instructor_subject WHERE Internal_Employee_Id = @employeeId AND Subject_Id = @subjectId";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@employeeId", employeeId);
                    command.Parameters.AddWithValue("@subjectId", subjectId);

                    int count = Convert.ToInt32(command.ExecuteScalar());
                    if (count > 0)
                    {
                        isAssigned = true;
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error checking subject assignment: " + ex.Message);
            }

            return isAssigned;
        }


        private void subjectDelete_btn_Click(object sender, RoutedEventArgs e)
        {
            if (instrutorSubject_data.SelectedItem is DataRowView selectedRow)
            {
                try
                {
                    // Get Instructor_Subject_Id from the selected DataRowView
                    int instructorSubjectId = Convert.ToInt32(selectedRow["Instructor_Subject_Id"]);

                    // Delete from database
                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        connection.Open();
                        string query = "DELETE FROM instructor_subject WHERE Instructor_Subject_Id = @instructorSubjectId";
                        MySqlCommand command = new MySqlCommand(query, connection);
                        command.Parameters.AddWithValue("@instructorSubjectId", instructorSubjectId);
                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Subject deleted from instructor successfully.");
                            // Refresh the DataGrid
                            LoadInstructorSubjects(instructorSubjectId); // Call a method to reload data in the DataGrid
                        }
                        else
                        {
                            MessageBox.Show("Error deleting subject from instructor.");
                        }
                    }
                }
                catch (MySqlException ex)
                {
                    MessageBox.Show("Error deleting subject from instructor: " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show("Please select a subject to delete.");
            }
        }

        private void LoadInstructorSubjects(int internalEmployeeId)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                SELECT isub.Instructor_Subject_Id, s.Subject_Code, s.Subject_Title
                FROM instructor_subject isub
                JOIN subjects s ON isub.Subject_Id = s.Subject_Id
                WHERE isub.Internal_Employee_Id = @internalEmployeeId";

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@internalEmployeeId", internalEmployeeId);

                    MySqlDataAdapter dataAdapter = new MySqlDataAdapter(command);
                    DataTable dataTable = new DataTable();
                    dataAdapter.Fill(dataTable);

                    // Ensure columns are in the correct order
                    dataTable.Columns["Instructor_Subject_Id"].SetOrdinal(0);
                    dataTable.Columns["Subject_Code"].SetOrdinal(1);
                    dataTable.Columns["Subject_Title"].SetOrdinal(2);

                    instrutorSubject_data.ItemsSource = dataTable.DefaultView;
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading instructor subjects: " + ex.Message);
            }
        }




        //   Availability   /////////////////////////
        private void LoadAvailability(int entityId)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                SELECT Availability_Id, Day_Of_Week, Start_Time, End_Time
                FROM instructor_availability
                WHERE Internal_Employee_Id = @entityId";

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@entityId", entityId);

                    MySqlDataAdapter dataAdapter = new MySqlDataAdapter(command);
                    DataTable dataTable = new DataTable();
                    dataAdapter.Fill(dataTable);

                    // Arrange the DataTable
                    DataTable arrangedTable = ArrangeDataByDaysOfWeek(dataTable);

                    // Bind the arranged data to the DataGrid
                    availability_data.ItemsSource = arrangedTable.DefaultView;
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading availability: " + ex.Message);
            }
        }

        private DataTable ArrangeDataByDaysOfWeek(DataTable originalTable)
        {
            DataTable arrangedTable = originalTable.Clone(); // Clone the structure

            // Days of the week in desired order
            string[] daysOfWeekOrder = { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };

            foreach (string day in daysOfWeekOrder)
            {
                // Filter rows for the current day
                DataRow[] rows = originalTable.Select($"Day_Of_Week = '{day}'");

                foreach (DataRow row in rows)
                {
                    arrangedTable.ImportRow(row);
                }
            }

            return arrangedTable;
        }

        private void availabilityAdd_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Retrieve input values
                string dayOfWeek = ((ComboBoxItem)days_cmbx.SelectedItem)?.Content.ToString();// Selected day from ComboBox
                int startTime, endTime;
                int instructorId = InternalEmployeeId;
           

                // Convert text from TextBoxes to integers
                if (!int.TryParse(startTime_txt.Text.Trim(), out startTime) || !int.TryParse(endTime_txt.Text.Trim(), out endTime))
                {
                    MessageBox.Show("Start time and end time must be valid integers in HHmm format.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Validate inputs
                if (string.IsNullOrEmpty(dayOfWeek) || startTime < 700 || endTime > 2100)
                {
                    MessageBox.Show("Please fill out all fields correctly. Ensure start time is not lower than 0700 and end time is not higher than 2100.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!IsTimeOrderValid(startTime, endTime))
                {
                    MessageBox.Show("End time must be after start time.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Check if availability already exists for the selected day and instructor
                if (IsAvailabilityExists(instructorId, dayOfWeek))
                {
                    MessageBox.Show($"Availability already exists for {dayOfWeek}.", "Duplicate Entry", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Insert into database
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                INSERT INTO instructor_availability (Internal_Employee_Id, Day_Of_Week, Start_Time, End_Time)
                VALUES (@instructorId, @dayOfWeek, @startTime, @endTime)";

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@instructorId", instructorId);
                    command.Parameters.AddWithValue("@dayOfWeek", dayOfWeek);
                    command.Parameters.AddWithValue("@startTime", startTime);
                    command.Parameters.AddWithValue("@endTime", endTime);

                    command.ExecuteNonQuery();
                }

                // Optionally, reload the availability data in the DataGrid after adding
                LoadAvailability(instructorId); // Refresh the availability DataGrid

                MessageBox.Show("Availability added successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error adding availability: " + ex.Message);
            }
        }


        private bool IsTimeOrderValid(int startTime, int endTime)
        {
            // Check if start time is less than end time
            return startTime < endTime;
        }

        private bool IsAvailabilityExists(int instructorId, string dayOfWeek)
        {
            bool exists = false;

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                SELECT COUNT(*)
                FROM instructor_availability
                WHERE Internal_Employee_Id = @instructorId
                AND Day_Of_Week = @dayOfWeek";

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@instructorId", instructorId);
                    command.Parameters.AddWithValue("@dayOfWeek", dayOfWeek);

                    int count = Convert.ToInt32(command.ExecuteScalar());
                    if (count > 0)
                    {
                        exists = true;
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error checking availability: " + ex.Message);
            }

            return exists;
        }

        private void availability_data_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (availability_data.SelectedItem != null)
            {
                DataRowView row = (DataRowView)availability_data.SelectedItem;

                // Update TextBoxes and ComboBox
                days_cmbx.SelectedItem = GetComboBoxItem(days_cmbx, row["Day_Of_Week"].ToString());
                startTime_txt.Text = row["Start_Time"].ToString();
                endTime_txt.Text = row["End_Time"].ToString();
            }
        }

        private ComboBoxItem GetComboBoxItem(ComboBox comboBox, string content)
        {
            foreach (ComboBoxItem item in comboBox.Items)
            {
                if (item.Content.ToString() == content)
                {
                    return item;
                }
            }
            return null;
        }

        private void availabilityDelete_btn_Click(object sender, RoutedEventArgs e)
        {
            if (availability_data.SelectedItem != null)
            {
                try
                {
                    DataRowView row = (DataRowView)availability_data.SelectedItem;

                    // Retrieve the selected availability ID
                    int availabilityId = Convert.ToInt32(row["Availability_Id"]);

                    // Delete from the database
                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        connection.Open();
                        string query = "DELETE FROM instructor_availability WHERE Availability_Id = @availabilityId";
                        MySqlCommand command = new MySqlCommand(query, connection);
                        command.Parameters.AddWithValue("@availabilityId", availabilityId);
                        command.ExecuteNonQuery();
                    }

                    // Refresh the DataGrid after deletion
                    int instructorId = Convert.ToInt32(employeeId_txt.Text);
                    LoadAvailability(instructorId); // Refresh availability DataGrid

                    MessageBox.Show("Availability deleted successfully.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error deleting availability: " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show("Please select an availability record to delete.");
            }
        }

    }

}
