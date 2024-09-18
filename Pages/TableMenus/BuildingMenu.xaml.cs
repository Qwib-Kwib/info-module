using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

namespace Info_module.Pages.TableMenus
{
    public partial class BuildingMenu : Page
    {
        public BuildingMenu()
        {
            InitializeComponent();
            TopBar topBar = new TopBar();
            topBar.txtPageTitle.Text = "Building Menu";
            topBar.Visibility = Visibility.Visible;
            topBar.BackButtonClicked += TopBar_BackButtonClicked;
            TopBarFrame.Navigate(topBar);
        }

        private void TopBar_BackButtonClicked(object sender, EventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.MainFrame.Navigate(new MainMenu());
        }

        private const string connectionString = @"Server=localhost;Database=universitydb;User ID=root;Password=;";

        private void LoadData()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT Building_Id, Building_Code, Building_Name, CONCAT(FORMAT(Pos_X, 0), ',', FORMAT(Pos_Y, 0)) AS Position FROM buildings WHERE status = 1\r\n";
                    MySqlDataAdapter adapter = new MySqlDataAdapter(query, connection);
                    DataTable dataTable = new DataTable();
                    adapter.Fill(dataTable);
                    account_data.ItemsSource = dataTable.DefaultView;
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading data: " + ex.Message);
            }
        }

        private void btnCSVRooms_Click(object sender, RoutedEventArgs e)
        {
            int buildingId;
            if (!int.TryParse(ID_txt.Text, out buildingId))
            {
                MessageBox.Show("Pick a valid Building Id.");
                return;
            }

            CSVRooms rooms = new CSVRooms(buildingId);
            NavigationService.Navigate(rooms);
        }

        private void building_data_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void add_btn_Click(object sender, RoutedEventArgs e)
        {
            string buildingCode = buildingCode_txt.Text;
            string buildingName = buildingName_txt.Text;
            string posX = posX_txt.Text;
            string posY = PosY_txt.Text;

            if (string.IsNullOrWhiteSpace(buildingCode) || string.IsNullOrWhiteSpace(buildingName) ||
                string.IsNullOrWhiteSpace(posX) || string.IsNullOrWhiteSpace(posY))
            {
                MessageBox.Show("Please fill in all fields.");
                return;
            }

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "INSERT INTO buildings (Building_Code, Building_Name, Pos_X, Pos_Y, status) VALUES (@buildingCode, @buildingName, @posX, @posY, 1)";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@buildingCode", buildingCode);
                        command.Parameters.AddWithValue("@buildingName", buildingName);
                        command.Parameters.AddWithValue("@posX", posX);
                        command.Parameters.AddWithValue("@posY", posY);
                        command.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Building added successfully.");
                LoadData();
                ClearTextFields();
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error adding building: " + ex.Message);
            }
        }

        private void edit_btn_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(ID_txt.Text, out int buildingId))
            {
                MessageBox.Show("Invalid ID.");
                return;
            }

            string buildingCode = buildingCode_txt.Text;
            string buildingName = buildingName_txt.Text;
            string posX = posX_txt.Text;
            string posY = PosY_txt.Text;

            if (string.IsNullOrWhiteSpace(buildingCode) || string.IsNullOrWhiteSpace(buildingName) ||
                string.IsNullOrWhiteSpace(posX) || string.IsNullOrWhiteSpace(posY))
            {
                MessageBox.Show("Please fill in all fields.");
                return;
            }

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "UPDATE buildings SET Building_Code = @buildingCode, Building_Name = @buildingName, Pos_X = @posX, Pos_Y = @posY WHERE Building_Id = @buildingId";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@buildingId", buildingId);
                        command.Parameters.AddWithValue("@buildingCode", buildingCode);
                        command.Parameters.AddWithValue("@buildingName", buildingName);
                        command.Parameters.AddWithValue("@posX", posX);
                        command.Parameters.AddWithValue("@posY", posY);
                        command.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Building details updated successfully.");
                LoadData();
                ClearTextFields();
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error updating building details: " + ex.Message);
            }
        }

        private void remove_btn_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(ID_txt.Text, out int buildingId))
            {
                MessageBox.Show("Invalid ID.");
                return;
            }

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "UPDATE buildings SET status = 0 WHERE Building_Id = @buildingId";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@buildingId", buildingId);
                        command.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Building disabled successfully.");
                LoadData();
                ClearTextFields();
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error disabling building: " + ex.Message);
            }
        }

        private void account_data_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (account_data.SelectedItem is DataRowView selectedRow)
            {
                ID_txt.Text = selectedRow["Building_Id"].ToString();
                buildingCode_txt.Text = selectedRow["Building_Code"].ToString();
                buildingName_txt.Text = selectedRow["Building_Name"].ToString();
                string[] positions = selectedRow["Position"].ToString().Split(',');
                if (positions.Length == 2)
                {
                    posX_txt.Text = positions[0];
                    PosY_txt.Text = positions[1];
                }
            }
        }

        private void clear_btn_Click(object sender, RoutedEventArgs e)
        {
            ClearTextFields();
        }

        private void ClearTextFields()
        {
            ID_txt.Text = string.Empty;
            buildingCode_txt.Text = string.Empty;
            buildingName_txt.Text = string.Empty;
            posX_txt.Text = string.Empty;
            PosY_txt.Text = string.Empty;
        }
        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Accept only digits (0-9) and one decimal point
            if (!char.IsDigit(e.Text, e.Text.Length - 1) && e.Text != ".")
            {
                e.Handled = true;
            }
            // Ensure only one decimal point is allowed
            TextBox textBox = sender as TextBox;
            if (e.Text == "." && textBox.Text.Contains("."))
            {
                e.Handled = true;
            }
        }
    }
}
