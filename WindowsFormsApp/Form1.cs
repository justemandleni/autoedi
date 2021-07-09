using System;
using System.Data;
using System.Data.SqlClient;
using RestSharp;
using System.Windows.Forms;
using RestSharp.Serialization.Json;

namespace WindowsFormsApp
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            populateComboBox();
        }

        private void clearAll()
        {
            textBoxUsername.Clear();
            textBoxPassword.Clear();
            textBoxConfirmPassword.Clear();
            textBoxFirstName.Clear();
            textBoxLastName.Clear();
            comboBoxAccessLevel.SelectedIndex = 0;
        }
        private bool isEmptyEntry(String value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return true;
            return false;
        }
        private bool isCompletedForm()
        {
            if (isEmptyEntry(textBoxUsername.Text) || isEmptyEntry(textBoxPassword.Text) ||
                isEmptyEntry(textBoxConfirmPassword.Text) || isEmptyEntry(textBoxFirstName.Text) ||
                isEmptyEntry(textBoxLastName.Text) || isEmptyEntry(comboBoxAccessLevel.Text))
                return false;
            else return true;
        }
        private void populateComboBox()
        {
            SqlConnection conn = new SqlConnection(@"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=AutoEDITest;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False");

            string query = "SELECT Id, Name FROM AccessLevel;";
            SqlDataAdapter da = new SqlDataAdapter(query, conn);
            conn.Open();
            DataTable dt = new DataTable();
            da.Fill(dt);
            DataRow row = dt.NewRow();
            row[0] = 0;
            dt.Rows.InsertAt(row, 0);

            comboBoxAccessLevel.ValueMember = "Id";
            comboBoxAccessLevel.DisplayMember = "Name";
            comboBoxAccessLevel.DataSource = dt;
        }
        private bool isConfirmedPassword()
        {
            if (!textBoxPassword.Text.Equals(textBoxConfirmPassword.Text))
            {
                MessageBox.Show("Passwords do not match", "", MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning);
                textBoxConfirmPassword.Clear();
                return false;
            }
            return true;
        }
        private bool isExistingUser(string str, SqlConnection connection)
        {
            string SQL_SELECT_STMNT = "SELECT * FROM dbo.[User] WHERE Username = '" + str + "'";

            SqlDataAdapter DATA_ADAPTER = new SqlDataAdapter(SQL_SELECT_STMNT, connection);
            DataTable DATA_TABLE = new DataTable();
            DATA_ADAPTER.Fill(DATA_TABLE);

            if (DATA_TABLE.Rows.Count > 0)
                return true;
            else return false;
        }
        private void buttonAddUser_Click(object sender, EventArgs e)
        {
            if (!isConfirmedPassword())
                return;

            SqlConnection connection = new SqlConnection(@"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=AutoEDITest;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False");

            if (isCompletedForm())
            {
                if (!isExistingUser(textBoxUsername.Text, connection))
                {
                    connection.Open();
                    String query = "INSERT INTO dbo.[User] (Username, Password, AccessLevelId, FirstName, LastName) OUTPUT INSERTED.Id VALUES (@Username, @Password, @AccessLevelId, @FirstName, @LastName)";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Username", textBoxUsername.Text);
                    command.Parameters.AddWithValue("@Password", textBoxPassword.Text);
                    command.Parameters.Add("@AccessLevelId", SqlDbType.Int);
                    command.Parameters["@AccessLevelId"].Value = comboBoxAccessLevel.SelectedIndex;
                    command.Parameters.AddWithValue("@FirstName", textBoxFirstName.Text);
                    command.Parameters.AddWithValue("@LastName", textBoxLastName.Text);
                    int userId = (int)command.ExecuteScalar();

                    IRestResponse restResponse = getApiCalloutResponse();
                    IRestResponse restResponse2;
                    if (restResponse.IsSuccessful)
                    {
                        JsonDeserializer jsonDeserializer = new JsonDeserializer();
                        string currentUserGuid = jsonDeserializer.Deserialize<Root>(restResponse).guid;

                        restResponse2 = apiCalloutRegisterUser(currentUserGuid, userId.ToString() ,textBoxUsername.Text,textBoxPassword.Text,comboBoxAccessLevel.SelectedIndex.ToString(),textBoxFirstName.Text,textBoxLastName.Text);

                        if (restResponse2.IsSuccessful)
                            MessageBox.Show("Successfully registered " + textBoxFirstName.Text + "!", "Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        else
                        {
                            MessageBox.Show("" + restResponse2.ErrorMessage, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show("" + restResponse.ErrorMessage, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    Close();
                }
                else
                {
                    MessageBox.Show("We already know you here " + textBoxFirstName.Text + "! Rather log in.", "Account already exists", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    clearAll();
                }
                connection.Close();
            }
            else
            {
                MessageBox.Show("Please fill in all blanks", "", MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning);
            }
        }

        private IRestResponse getApiCalloutResponse()
        {
            var client = new RestClient("http://www.autoediportal.com/AutoEDI/Api/v1/TestLogin.php");
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Authorization", "Basic VGVzdFJlZ2lzdGVyOlRlc3RQYXNzd29yZA==");
            request.AddHeader("Content-Type", "application/json");
            var body = @"{
                            " + "\n" +
                           @"  ""username"":""TestRegister"",
                            " + "\n" +
                           @"  ""password"":""TestPassword""
                            " + "\n" +
                       @"}";
            request.AddParameter("application/json", body, ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);
            return response;
        }

        private IRestResponse apiCalloutRegisterUser(string guid, string userId, string userName, string passWord, string accessLevelId, string firstName, string lastName)
        {
            var client = new RestClient("http://www.autoediportal.com/AutoEDI/api/v1/TestRegister.php");
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Authorization", "Basic VGVzdFJlZ2lzdGVyOlRlc3RQYXNzd29yZA==");
            request.AddHeader("Content-Type", "application/json");
            var body =
                @"{
                    ""guid"": """+guid+@""",
                    ""userId"": """+userId+@""",
                    ""username"":"""+userName+@""",
                    ""password"": """+passWord+@""",
                    ""accessLevelId"": """+accessLevelId+@""", 
                    ""firstName"": """+firstName+@""",
                    ""lastName"": """+lastName+@"""
                  }";
            request.AddParameter("application/json", body, ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);
            return response;
        }
    }
}
