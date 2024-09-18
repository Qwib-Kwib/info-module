using System;
using System.Collections.Generic;
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
using Microsoft.Win32;
using MySql.Data.MySqlClient;

namespace Info_module.Pages.TableMenus
{
    /// newwwwwwwwwwwwwwwwwwwwwww
    /// <summary>
    /// Interaction logic for DepartmentMenu.xaml
    /// </summary>
    public partial class DepartmentMenu : Page
    {
        public string PageTitle { get; set; }
        public int departmentId;
        public DepartmentMenu()
        {
            InitializeComponent();
            TopBar topBar = new TopBar();
            topBar.txtPageTitle.Text = "Department Menu";
            topBar.Visibility = Visibility.Visible;
            topBar.BackButtonClicked += TopBar_BackButtonClicked;
            TopBarFrame.Navigate(topBar);
            PopulateBuildingCodes();
            LoadDepartmentsData();
        }

        private const string connectionString = @"Server=localhost;Database=universitydb;User ID=root;Password=;";

        private void TopBar_BackButtonClicked(object sender, EventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.MainFrame.Navigate(new MainMenu());
        }

        private void LoadDepartmentsData()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                SELECT 
                    d.Dept_Id AS 'Department_ID',
                    d.Building_Id,
                    b.Building_Code AS 'Building_Code',
                    d.Dept_Code AS 'Department_Code',
                    d.Dept_Name AS 'Department_Name',
                    d.Status AS 'Status'
                FROM departments d
                INNER JOIN buildings b ON d.Building_Id = b.Building_Id
                WHERE d.Status = 1";

                    MySqlCommand command = new MySqlCommand(query, connection);

                    DataTable dataTable = new DataTable();
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                    {
                        adapter.Fill(dataTable);
                    }

                    department_data.ItemsSource = dataTable.DefaultView;
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error retrieving data: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void PopulateBuildingCodes()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT Building_Id, Building_Code FROM buildings";
                    MySqlCommand command = new MySqlCommand(query, connection);

                    DataTable dataTable = new DataTable();
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                    {
                        adapter.Fill(dataTable);
                    }

                    // Assuming buildingCode_cbx is your ComboBox name
                    buildingCode_cbx.ItemsSource = dataTable.DefaultView;
                    buildingCode_cbx.DisplayMemberPath = "Building_Code";
                    buildingCode_cbx.SelectedValuePath = "Building_Id";
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading building codes: " + ex.Message);
            }
        }

        private byte[] uploadedImageBytes;

        private void imageUpload_btn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string filename = openFileDialog.FileName;
                    using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
                    {
                        uploadedImageBytes = new byte[fs.Length];
                        fs.Read(uploadedImageBytes, 0, uploadedImageBytes.Length);
                    }
                    // Display image in preview
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = new MemoryStream(uploadedImageBytes);
                    bitmap.EndInit();
                    logoPreview_img.Source = bitmap;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error uploading image: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void addDepartment_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"INSERT INTO departments (Building_Id,Dept_Code, Dept_Name, Logo_Image) 
                             VALUES (@Building_Id,@Dept_Code, @Dept_Name, @Logo_Image)";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Building_Id", buildingCode_cbx.SelectedValue);
                        command.Parameters.AddWithValue("@Dept_Code", deparmentCode_txt.Text);
                        command.Parameters.AddWithValue("@Dept_Name", departmentName_txt.Text);
                        command.Parameters.AddWithValue("@Logo_Image", uploadedImageBytes);
                        command.ExecuteNonQuery();
                    }
                }
                LoadDepartmentsData();
                MessageBox.Show("Department added successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error adding department: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void department_data_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (department_data.SelectedItem != null)
            {
                DataRowView selectedRow = department_data.SelectedItem as DataRowView;

                if (selectedRow != null)
                {
                    // Populate other text boxes
                    departmentId_txt.Text = selectedRow["Department_ID"].ToString();
                    buildingCode_cbx.SelectedValue = Convert.ToInt32(selectedRow["Building_Id"]); // Use Building_Id
                    deparmentCode_txt.Text = selectedRow["Department_Code"].ToString();
                    departmentName_txt.Text = selectedRow["Department_Name"].ToString();

                    departmentId = Convert.ToInt32(selectedRow["Department_ID"]);
                    LoadAndDisplayImage(departmentId);
                    LoadCoursesData(departmentId);
                }
            }
        }

        private byte[] currentImageBytes;

        private void LoadAndDisplayImage(int departmentId)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT Logo_Image FROM departments WHERE Dept_Id = @Dept_Id";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Dept_Id", departmentId);

                    byte[] imageBytes = (byte[])command.ExecuteScalar();
                    if (imageBytes != null && imageBytes.Length > 0)
                    {
                        currentImageBytes = imageBytes;
                        BitmapImage bitmap = new BitmapImage();
                        using (MemoryStream stream = new MemoryStream(imageBytes))
                        {
                            stream.Position = 0;
                            bitmap.BeginInit();
                            bitmap.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.UriSource = null;
                            bitmap.StreamSource = stream;
                            bitmap.EndInit();
                        }
                        bitmap.Freeze(); // Freeze to make it cross-thread accessible
                        logoPreview_img.Source = bitmap;
                    }
                    else
                    {
                        logoPreview_img.Source = null;
                        currentImageBytes = null;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading image: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void editDepartment_btn_Click(object sender, RoutedEventArgs e)
        {
            if (department_data.SelectedItem != null)
            {
                DataRowView selectedRow = department_data.SelectedItem as DataRowView;

                if (selectedRow != null)
                {
                    try
                    {
                        using (MySqlConnection connection = new MySqlConnection(connectionString))
                        {
                            connection.Open();
                            string query = @"
                    UPDATE departments 
                    SET Building_Id = @Building_Id, 
                        Dept_Code = @Dept_Code, 
                        Dept_Name = @Dept_Name" +
                                (uploadedImageBytes != null ? ", Logo_Image = @Logo_Image" : "") +
                            " WHERE Dept_Id = @Dept_Id";

                            using (MySqlCommand command = new MySqlCommand(query, connection))
                            {
                                command.Parameters.AddWithValue("@Dept_Id", Convert.ToInt32(selectedRow["Department_ID"]));
                                command.Parameters.AddWithValue("@Building_Id", buildingCode_cbx.SelectedValue);
                                command.Parameters.AddWithValue("@Dept_Code", deparmentCode_txt.Text);
                                command.Parameters.AddWithValue("@Dept_Name", departmentName_txt.Text);

                                if (uploadedImageBytes != null)
                                {
                                    command.Parameters.AddWithValue("@Logo_Image", uploadedImageBytes);
                                }

                                command.ExecuteNonQuery();
                            }
                        }
                        LoadDepartmentsData();
                        MessageBox.Show("Department updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (MySqlException ex)
                    {
                        MessageBox.Show("Error updating department: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a department to edit.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private void removeDepartment_btn_Click(object sender, RoutedEventArgs e)
        {
            if (department_data.SelectedItem != null)
            {
                DataRowView selectedRow = department_data.SelectedItem as DataRowView;

                if (selectedRow != null)
                {
                    int departmentId = Convert.ToInt32(selectedRow["Department_ID"]);

                    try
                    {
                        using (MySqlConnection connection = new MySqlConnection(connectionString))
                        {
                            connection.Open();
                            string query = "UPDATE departments SET Status = 0 WHERE Dept_Id = @Dept_Id";
                            using (MySqlCommand command = new MySqlCommand(query, connection))
                            {
                                command.Parameters.AddWithValue("@Dept_Id", departmentId);
                                command.ExecuteNonQuery();
                            }
                        }

                        // Refresh departments data after update
                        LoadDepartmentsData(); // Assuming this method reloads department_data DataGrid
                        MessageBox.Show("Department removed successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (MySqlException ex)
                    {
                        MessageBox.Show("Error removing department: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a department to remove.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }

        }

        private void clearDepartment_btn_Click(object sender, RoutedEventArgs e)
        {
            // Clear TextBoxes
            departmentId_txt.Text = string.Empty;
            deparmentCode_txt.Text = string.Empty;
            departmentName_txt.Text = string.Empty;

            // Reset ComboBox (if applicable)
            buildingCode_cbx.SelectedIndex = -1; // Reset ComboBox selection to none

            // Clear Image
            logoPreview_img.Source = null; // Assuming logoPreview_img is your Image control

            // Clear any other fields or reset UI state as needed
        }

        ///////////////////////////////////////////////////////// Courses /////////////////////////////////////////////////////////////////

        private void LoadCoursesData(int deptId)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                SELECT 
                    c.Course_ID AS 'Course_ID',
                    d.Dept_Code AS 'Dept_Code',
                    c.Course_Code AS 'Course_Code',
                    c.Course_Name AS 'Course_Name'
                FROM course c
                INNER JOIN departments d ON c.Dept_Id = d.Dept_Id
                WHERE c.Status = 1 AND c.Dept_Id = @DeptId";

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@DeptId", deptId);

                    DataTable dataTable = new DataTable();
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                    {
                        adapter.Fill(dataTable);
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error retrieving course data: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
