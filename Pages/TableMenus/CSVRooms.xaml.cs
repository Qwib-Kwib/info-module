using System;
using System.Collections.Generic;
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
using System.IO;
using System.Data;
using System.Diagnostics;

namespace Info_module.Pages.TableMenus
{
    /// <summary>
    /// Interaction logic for CSVRooms.xaml
    /// </summary>
    public partial class CSVRooms : Page
    {
        private int buildingId;
        private string buildingCode;
        private string buildingName;

        private int selectedRoomId;
        private string selectedBuildingCode;
        private string selectedRoomCode;
        private int selectedFloorLevel;
        private string selectedRoomType;
        private int selectedMaxSeat;
        private int selectedAvailability;

        public CSVRooms(int buildingId)
        {
            InitializeComponent();
            TopBar topBar = new TopBar();
            topBar.txtPageTitle.Text = "Configure Room";
            topBar.Visibility = Visibility.Visible;
            topBar.BackButtonClicked += TopBar_BackButtonClicked;
            TopBarFrame.Navigate(topBar);

            this.buildingId = buildingId;
            LoadBuildingDetails();
            LoadBuilding();


        }

        private const string connectionString = @"Server=localhost;Database=universitydb;User ID=root;Password=;";

        private void LoadBuildingDetails() //change text block
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT Building_Code, Building_Name FROM buildings WHERE Building_Id = @buildingId";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@buildingId", buildingId);
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            buildingCode = reader["Building_Code"].ToString();
                            buildingName = reader["Building_Name"].ToString();
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error loading building details: " + ex.Message);
            }

            buildingCode_txtblck.Text = buildingCode;
            buildingName_txtblck.Text = buildingName;
        }

        private void LoadBuilding()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"SELECT 
                                Room_Id AS Room_Id, 
                                @BuildingCode AS Building_Code, 
                                Room_Code, 
                                Room_Floor AS Floor_Level, 
                                Room_Type, 
                                Max_Seat, 
                                CASE
                                            When Status = 1 then 'Active'
                                            Else 'Inactive'
                                        End as 'Status' 
                             FROM rooms 
                             WHERE Building_Id = @BuildingId";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@BuildingId", buildingId);
                    command.Parameters.AddWithValue("@BuildingCode", buildingCode);

                    DataTable dataTable = new DataTable();
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                    {
                        adapter.Fill(dataTable);
                    }

                    room_data.ItemsSource = dataTable.DefaultView;
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error retrieving data: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Upload_btn_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Exclude ID and Building Code from CSV.");

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                try
                {
                    DataTable dataTable = ReadCsvFile(filePath);
                    if (dataTable != null)
                    {
                        room_data.ItemsSource = dataTable.DefaultView;
                        Cancel_btn.Visibility = Visibility.Visible;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error reading CSV file: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        private DataTable ReadCsvFile(string filePath)
        {
            DataTable csvData = new DataTable();
            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    // Define the columns based on the expected CSV format
                    csvData.Columns.Add("Room_Code", typeof(string));
                    csvData.Columns.Add("Floor_Level", typeof(int));
                    csvData.Columns.Add("Room_Type", typeof(string));
                    csvData.Columns.Add("Max_Seat", typeof(int));
                    csvData.Columns.Add("Availability", typeof(int));

                    // Read the header line first to skip it
                    sr.ReadLine();

                    // Read the data lines
                    while (!sr.EndOfStream)
                    {
                        string[] rows = sr.ReadLine().Split(',');

                        // Ensure that the CSV row has the expected number of columns
                        if (rows.Length != 6)
                        {
                            MessageBox.Show("Error: CSV file format is incorrect.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return null;
                        }

                        try
                        {
                            DataRow dr = csvData.NewRow();
                            dr["Room_Code"] = rows[0].Trim();
                            dr["Floor_Level"] = int.Parse(rows[1].Trim());
                            dr["Room_Type"] = rows[2].Trim();
                            dr["Max_Seat"] = int.Parse(rows[4].Trim());
                            dr["Availability"] = int.Parse(rows[5].Trim());

                            csvData.Rows.Add(dr);
                        }
                        catch (FormatException ex)
                        {
                            MessageBox.Show($"Error parsing row: {string.Join(",", rows)}\n\n{ex.Message}", "Format Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error reading CSV file: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            return csvData;
        }







        private void TopBar_BackButtonClicked(object sender, EventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.MainFrame.Navigate(new BuildingMenu());
        }

        private void back_btn_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void Add_btn_Click(object sender, RoutedEventArgs e)
        {
            if (room_data.Items.Count == 0)
            {
                MessageBox.Show("No data to add.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            DataView dataView = (DataView)room_data.ItemsSource;
            DataTable dataTable = dataView.Table;
            InsertDataIntoDatabase(dataTable);
        }

        private void InsertDataIntoDatabase(DataTable dataTable)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    foreach (DataRow row in dataTable.Rows)
                    {
                        // Only insert rows that have not already been inserted
                        if (row.RowState == DataRowState.Added)
                        {
                            string query = "INSERT INTO rooms (Building_Id, Room_Code, Room_Floor, Room_Type, Max_Seat, status) " +
                                           "VALUES (@Building_Id, @Room_Code, @Room_Floor, @Room_Type, @Max_Seat, @status)";
                            using (MySqlCommand command = new MySqlCommand(query, connection))
                            {
                                command.Parameters.AddWithValue("@Building_Id", buildingId);
                                command.Parameters.AddWithValue("@Room_Code", row["Room_Code"]);
                                command.Parameters.AddWithValue("@Room_Floor", row["Floor_Level"]);
                                command.Parameters.AddWithValue("@Room_Type", row["Room_Type"]);
                                command.Parameters.AddWithValue("@Max_Seat", row["Max_Seat"]);
                                command.Parameters.AddWithValue("@status", row["Availability"]);

                                command.ExecuteNonQuery();
                            }
                        }
                    }
                }
                MessageBox.Show("Data inserted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error inserting data into database: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void Remove_btn_Click(object sender, RoutedEventArgs e)
        {
            if (room_data.SelectedItems.Count > 0)
            {
                try
                {
                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        connection.Open();
                        List<DataRowView> rowsToRemove = new List<DataRowView>();

                        foreach (DataRowView rowView in room_data.SelectedItems)
                        {
                            DataRow row = rowView.Row;
                            int roomId = Convert.ToInt32(row["ID"]);

                            if (roomId != -1) // Assuming -1 means it's not in the database
                            {
                                string query = "DELETE FROM rooms WHERE Room_Id = @RoomId";
                                using (MySqlCommand command = new MySqlCommand(query, connection))
                                {
                                    command.Parameters.AddWithValue("@RoomId", roomId);
                                    command.ExecuteNonQuery();
                                }
                            }

                            // Add row to list of rows to remove
                            rowsToRemove.Add(rowView);
                        }

                        // Remove rows from DataGrid
                        foreach (DataRowView rowView in rowsToRemove)
                        {
                            (room_data.ItemsSource as DataView).Table.Rows.Remove(rowView.Row);
                        }
                    }
                    MessageBox.Show("Selected entries deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (MySqlException ex)
                {
                    MessageBox.Show("Error deleting selected entries: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select at least one entry to delete.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Cancel_btn_Click(object sender, RoutedEventArgs e)
        {
            LoadBuilding();
            Cancel_btn.Visibility = Visibility.Hidden;

        }



        private void Status_btn_Click(object sender, RoutedEventArgs e)
        {
            if (room_data.SelectedItems.Count > 0)
            {
                try
                {
                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        connection.Open();
                        foreach (DataRowView rowView in room_data.SelectedItems)
                        {
                            DataRow row = rowView.Row;
                            int roomId = selectedRoomId;
                            int currentStatus = Convert.ToInt32(row["Availability"]);

                            int newStatus = (currentStatus == 1) ? 0 : 1;

                            string query = "UPDATE rooms SET status = @Status WHERE Room_Id = @RoomId";
                            using (MySqlCommand command = new MySqlCommand(query, connection))
                            {
                                command.Parameters.AddWithValue("@Status", newStatus);
                                command.Parameters.AddWithValue("@RoomId", roomId);
                                command.ExecuteNonQuery();
                            }
                        }
                    }
                    MessageBox.Show("Status updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    Cancel_btn_Click(sender, e); // Refresh data after updating status
                }
                catch (MySqlException ex)
                {
                    MessageBox.Show("Error updating status: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select at least one entry to update status.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }

        }

        private void room_data_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (room_data.SelectedItem is DataRowView selectedRow)
            {
                selectedRoomId = Convert.ToInt32(selectedRow["Room_Id"]);
                selectedBuildingCode = selectedRow["Building_Code"].ToString();
                selectedRoomCode = selectedRow["Room_Code"].ToString();
                selectedFloorLevel = Convert.ToInt32(selectedRow["Floor_Level"]);
                selectedRoomType = selectedRow["Room_Type"].ToString();
                selectedMaxSeat = Convert.ToInt32(selectedRow["Max_Seat"]);
                string statusString = selectedRow["Status"].ToString();
                selectedAvailability = statusString == "Active" ? 1 : 0;


                // roomCodeTextBox.Text = selectedRoomCode;
                // floorLevelTextBox.Text = selectedFloorLevel.ToString();
                // roomTypeTextBox.Text = selectedRoomType;
                // airConditionedTextBox.Text = selectedAirConditioned;
                // maxSeatTextBox.Text = selectedMaxSeat.ToString();
                // availabilityTextBox.Text = selectedAvailability.ToString();
            }
        }
    }

}
