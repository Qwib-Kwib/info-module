using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using MySql.Data.MySqlClient;
using Microsoft.Win32;
using System.Data;
using CsvHelper;
using System.Globalization;
using static Info_module.Pages.TableMenus.After_College_Selection.CurriculumMenu;
using Info_module.Pages.TableMenus.After_College_Selection.CSVMenu;

namespace Info_module.Pages.TableMenus.After_College_Selection
{
    /// <summary>
    /// Interaction logic for CurriculumMenu.xaml
    /// </summary>
    public partial class CurriculumMenu : Page
    {
        public int DepartmentId { get; set; }

        private const string connectionString = @"Server=localhost;Database=universitydb;User ID=root;Password=;";

        public CurriculumMenu(int departmentId)
        {
            InitializeComponent();
            TopBar topBar = new TopBar();
            topBar.txtPageTitle.Text = "Curriculum Menu";
            topBar.Visibility = Visibility.Visible;
            topBar.BackButtonClicked += TopBar_BackButtonClicked;
            TopBarFrame.Navigate(topBar);

            DepartmentId = departmentId;
            LoadDepartmentDetails();
            LoadCurriculum();
        }

        private void TopBar_BackButtonClicked(object sender, EventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            NavigateBack("Curriculum");
        }
        private void NavigateBack(string sourceButton)
        {
            CollegeSelection collegeSelection = new CollegeSelection(sourceButton);
            NavigationService.Navigate(collegeSelection);
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
                    command.Parameters.AddWithValue("@deptId", DepartmentId); // Assuming departmentId is defined elsewhere

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string departmentName = reader["Dept_Name"].ToString();
                            DeptName_txt.Text = departmentName;

                            // Load logo image
                            if (!(reader["Logo_Image"] is DBNull))
                            {
                                byte[] imgData = (byte[])reader["Logo_Image"];
                                BitmapImage bitmap = new BitmapImage();
                                bitmap.BeginInit();
                                bitmap.StreamSource = new MemoryStream(imgData);
                                bitmap.EndInit();
                                Logo_img.Source = bitmap;
                            }
                            else
                            {
                                // Handle case where no image is found
                                Logo_img.Source = null;
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading department details: " + ex.Message);
            }
        }

        private void LoadCurriculum()
        {
            try 
            { using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                        SELECT 
                            Curriculum_Id, 
                            Curriculum_Revision, 
                            Curriculum_Description, 
                            CONCAT(Year_Effective_In, '-', Year_Effective_Out) AS Year_Effective
                            FROM curriculum
                            Where Status = 1";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        DataTable dataTable = new DataTable();
                        using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                        {
                            adapter.Fill(dataTable);
                        }

                        curriculumDataGrid.ItemsSource = dataTable.DefaultView;
                    }

                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading curriculum details: " + ex.Message);
            }
        }

        private void ClearTextboxes()
        {
            curriculumId_txt.Clear();
            curriculumRevision_txt.Clear();
            curriculumDescription_txt.Clear();
            yearEffectiveIn_txt.Clear();
            yearEffectiveOut_txt.Clear();
        }

        private void Add_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    curriculumId_txt.Text = "";
                    connection.Open();
                    string query = @"INSERT INTO curriculum (Course_Id, Curriculum_Revision, Curriculum_Description, Year_Effective_In, Year_Effective_Out)
                             VALUES (@Course_Id, @Curriculum_Revision, @Curriculum_Description, @Year_Effective_In, @Year_Effective_Out)";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Course_Id", DepartmentId);
                        command.Parameters.AddWithValue("@Curriculum_Revision", curriculumRevision_txt.Text);
                        command.Parameters.AddWithValue("@Curriculum_Description", curriculumDescription_txt.Text);
                        command.Parameters.AddWithValue("@Year_Effective_In", yearEffectiveIn_txt.Text);
                        command.Parameters.AddWithValue("@Year_Effective_Out", yearEffectiveOut_txt.Text);
                        command.ExecuteNonQuery();
                    }
                }
                LoadCurriculum();
                ClearTextboxes();
                MessageBox.Show("Curriculum added successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error adding curriculum: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void Edit_btn_Click(object sender, RoutedEventArgs e)
        {
            int curriculumId;
            if (!int.TryParse(curriculumId_txt.Text, out curriculumId))
            {
                MessageBox.Show("Please enter a valid Curriculum ID.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(curriculumRevision_txt.Text) ||
                    string.IsNullOrWhiteSpace(curriculumDescription_txt.Text) ||
                    !int.TryParse(yearEffectiveIn_txt.Text, out int yearEffectiveIn) ||
                    !int.TryParse(yearEffectiveOut_txt.Text, out int yearEffectiveOut))
                {
                    MessageBox.Show("Please fill in all fields with valid data.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"UPDATE curriculum 
                             SET Course_Id = @Course_Id, 
                                 Curriculum_Revision = @Curriculum_Revision, 
                                 Curriculum_Description = @Curriculum_Description, 
                                 Year_Effective_In = @Year_Effective_In, 
                                 Year_Effective_Out = @Year_Effective_Out 
                             WHERE Curriculum_Id = @Curriculum_Id";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Course_Id", DepartmentId);
                        command.Parameters.AddWithValue("@Curriculum_Revision", curriculumRevision_txt.Text);
                        command.Parameters.AddWithValue("@Curriculum_Description", curriculumDescription_txt.Text);
                        command.Parameters.AddWithValue("@Year_Effective_In", yearEffectiveIn);
                        command.Parameters.AddWithValue("@Year_Effective_Out", yearEffectiveOut);
                        command.Parameters.AddWithValue("@Curriculum_Id", curriculumId);

                        int rowsAffected = command.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            // Refresh the DataGrid
                            LoadCurriculum();
                            ClearTextboxes();
                            MessageBox.Show("Curriculum updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("No curriculum found with the specified ID.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error updating curriculum: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void Remove_btn_Click(object sender, RoutedEventArgs e)
        {
            // Check if the Curriculum_Id textbox is empty
            if (string.IsNullOrWhiteSpace(curriculumId_txt.Text))
            {
                MessageBox.Show("Please enter a Curriculum ID.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(curriculumId_txt.Text, out int curriculumId))
            {
                MessageBox.Show("Invalid Curriculum ID.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // Update query to set Status to 0 (Inactive)
                    string query = "UPDATE curriculum SET Status = 0 WHERE Curriculum_Id = @Curriculum_Id";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Curriculum_Id", curriculumId);
                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            // Optionally update the DataGrid or refresh data
                            LoadCurriculum();
                            ClearTextboxes();
                            MessageBox.Show("Curriculum status updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("Curriculum ID not found.", "Update Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error updating curriculum status: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void curriculumDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (curriculumDataGrid.SelectedItem is DataRowView selectedRow)
            {
                curriculumId_txt.Text = selectedRow["Curriculum_Id"].ToString();
                curriculumRevision_txt.Text = selectedRow["Curriculum_Revision"].ToString();
                curriculumDescription_txt.Text = selectedRow["Curriculum_Description"].ToString();

                // Split the "Year_Effective" column if it contains the combined year range
                string yearEffective = selectedRow["Year_Effective"].ToString();
                if (yearEffective.Contains('-'))
                {
                    var years = yearEffective.Split('-');
                    yearEffectiveIn_txt.Text = years[0];
                    yearEffectiveOut_txt.Text = years[1];
                }
                else
                {
                    yearEffectiveIn_txt.Text = yearEffective;
                    yearEffectiveOut_txt.Text = string.Empty;
                }
            }
        }

        private void subject_btn_Click(object sender, RoutedEventArgs e)
        {
            // Check if the Curriculum_Id textbox is empty
            if (string.IsNullOrWhiteSpace(curriculumId_txt.Text))
            {
                MessageBox.Show("Please enter a Curriculum ID.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            int.TryParse(curriculumId_txt.Text, out int curriculumId);
            NavigationService.Navigate(new CurriculumCSV(DepartmentId, curriculumId));
        }
    }
}
