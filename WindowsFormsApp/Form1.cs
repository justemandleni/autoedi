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
        private SqlConnection SQL_CONNECTION;
        private string USER_ID;

        public Form1()
        {
            InitializeComponent();
            SQL_CONNECTION = new SqlConnection(@"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=AutoEDITest;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False");
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            populateComboBox();
            populateDataGridView();
        }
        private void buttonAddUser_Click(object sender, EventArgs e)
        {
            if (isFormValidated())
            {
                bool insertedDb = Insert_User_tbl();
                if (insertedDb)
                {
                    string userGuid = TestLogin("TestRegister", "TestPassword");
                    if (userGuid.Equals(""))
                        MessageBox.Show("Failed to Get Guid.", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    TestRegister(userGuid, USER_ID, textBoxUsername.Text, textBoxPassword.Text, comboBoxAccessLevel.SelectedIndex.ToString(), textBoxFirstName.Text, textBoxLastName.Text);
                }

                populateDataGridView();
                clearAllFields();
            }
        }
        private string TestLogin(string authUsername, string authPassword)
        {
            var client = new RestClient("http://www.autoediportal.com/AutoEDI/Api/v1/TestLogin.php");
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Authorization", "Basic VGVzdFJlZ2lzdGVyOlRlc3RQYXNzd29yZA==");
            request.AddHeader("Content-Type", "application/json");
            var body = @"{
                            " + "\n" +
                           @"  ""username"":""" + authUsername + @""",
                            " + "\n" +
                           @"  ""password"":""" + authPassword + @"""
                            " + "\n" +
                       @"}";
            request.AddParameter("application/json", body, ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);
            if (response.IsSuccessful) //200 OK
            {
                JsonDeserializer jsonDeserializer = new JsonDeserializer();
                string message = jsonDeserializer.Deserialize<Response_on_Get_Guid>(response).message;
                if (message.Equals("success"))
                {
                    Insert_Comm_tbl("TestLogin", true);
                    return jsonDeserializer.Deserialize<Response_on_Get_Guid>(response).guid;
                }
                else
                {
                    Insert_Comm_tbl("TestLogin", false);
                    return "";
                }
            }
            else //ENCOUNTERED 404 OR SOME WIERD RESPONSE CODE
            {
                Insert_Comm_tbl("TestLogin", false);
                return "";
            }
        }
        private void TestRegister(string strGuid, string strUserId, string strUserName, string strUserPassword, string strUserAccLvlId, string strUserFirstName, string strUserLastName)
        {
            var client = new RestClient("http://www.autoediportal.com/AutoEDI/Api/v1/TestRegister.php");
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            var body = @"{
                             " + "\n" +
                            @"    ""guid"": """+strGuid+@""",
                             " + "\n" +
                            @"    ""userId"": """+strUserId+@""",
                             " + "\n" +
                            @"    ""username"": """+strUserName+@""",
                             " + "\n" +
                            @"    ""password"": """+strUserPassword+@""",
                             " + "\n" +
                            @"    ""accessLevelId"": """+strUserAccLvlId+@""",
                             " + "\n" +
                            @"    ""firstName"": """+strUserFirstName+@""",
                             " + "\n" +
                            @"    ""lastName"": """+strUserLastName+@"""
                            " + "\n" +
                        @"}";
            request.AddParameter("text/plain", body, ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);
            if (response.IsSuccessful) // 200 OK
            {
                //deserialise 
                //JsonDeserializer jsonDeserializer = new JsonDeserializer();
                //string message = jsonDeserializer.Deserialize<Response_on_Register>(response).message;
                if (true) //message.Equals("success")
                {
                    Update_User_tbl();
                    MessageBox.Show("Successfully registered", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    //TO:DO
                    //insert into comm table show success
                    Insert_Comm_tbl("TestRegister", true);
                    populateDataGridView();
                }
                else
                {
                    //string[] errors = jsonDeserializer.Deserialize<Response_on_Register>(response).errors;
                    //string Guiderror = errors[0];
                    //MessageBox.Show("Failed to register user on server. " + Guiderror, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    MessageBox.Show("Failed to register user on server. ", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    //TO:DO
                    //insert into comm table show failure
                    Insert_Comm_tbl("TestRegister", false);
                    populateDataGridView();
                }
            }
            else //ENCOUNTERED 404 OR SOME WIERD RESPONSE CODE
            {
                MessageBox.Show("Failed to hit server. Error: " + response.ErrorMessage, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //TO:DO
                //insert into comm table show failure
                Insert_Comm_tbl("TestRegister", false);
                populateDataGridView();
            }
        }
        private void Insert_Comm_tbl(string action, bool isSuccess)
        {
            string INSERT_QUERY =
                            "INSERT INTO dbo.[Communication] (CreatedAt, Action, Result) " +
                            "VALUES (@CreatedAt, @Action, @Result)";
            if (SQL_CONNECTION.State == ConnectionState.Closed)
                SQL_CONNECTION.Open();
            SqlCommand SQL_COMMAND = new SqlCommand(INSERT_QUERY, SQL_CONNECTION);
            SQL_COMMAND.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
            if (action.Equals("TestRegister"))
                SQL_COMMAND.Parameters.AddWithValue("@Action", "TestRegister");
            else SQL_COMMAND.Parameters.AddWithValue("@Action", "TestLogin");
            if (!isSuccess)
                SQL_COMMAND.Parameters.AddWithValue("@Result", "Failed");
            else SQL_COMMAND.Parameters.AddWithValue("@Result", "Success");
            SQL_COMMAND.ExecuteScalar();
            SQL_CONNECTION.Close();
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
        private bool isEmptyEntry(string strValue)
        {
            if (string.IsNullOrWhiteSpace(strValue))
                return true;
            return false;
        }
        private void populateComboBox()
        {
            string QUERY = "SELECT Id, Name FROM AccessLevel;";
            SqlDataAdapter DATA_ADAPTER = new SqlDataAdapter(QUERY, SQL_CONNECTION);
            SQL_CONNECTION.Open();
            DataTable DATA_TABLE = new DataTable();
            DATA_ADAPTER.Fill(DATA_TABLE);
            DataRow ROW = DATA_TABLE.NewRow();
            ROW[0] = 0;
            DATA_TABLE.Rows.InsertAt(ROW, 0);
            comboBoxAccessLevel.ValueMember = "Id";
            comboBoxAccessLevel.DisplayMember = "Name";
            comboBoxAccessLevel.DataSource = DATA_TABLE;
            SQL_CONNECTION.Close();
        }
        private void populateDataGridView()
        {
            if (SQL_CONNECTION.State == ConnectionState.Closed)
                SQL_CONNECTION.Open();

            string selectQuery = "SELECT * FROM Communication";
            SqlDataAdapter dataAdapter = new SqlDataAdapter(selectQuery, SQL_CONNECTION);
            dataAdapter.SelectCommand.CommandType = CommandType.Text;
            DataTable dataTable = new DataTable();
            dataAdapter.Fill(dataTable);

            dataGridView1.DataSource = dataTable;
            dataGridView1.Columns[0].Visible = false;

            SQL_CONNECTION.Close();
        }
        private void clearAllFields()
        {
            textBoxUsername.Clear();
            textBoxPassword.Clear();
            textBoxConfirmPassword.Clear();
            textBoxFirstName.Clear();
            textBoxLastName.Clear();
            comboBoxAccessLevel.SelectedIndex = 0;
        }
        private bool isFormCompleted()
        {
            if (isEmptyEntry(textBoxUsername.Text) || isEmptyEntry(textBoxPassword.Text) ||
                isEmptyEntry(textBoxConfirmPassword.Text) || isEmptyEntry(textBoxFirstName.Text) ||
                isEmptyEntry(textBoxLastName.Text) || isEmptyEntry(comboBoxAccessLevel.Text))
            {
                MessageBox.Show("Please fill in all entries", "", MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }
        private bool isPasswordConfirmed()
        {
            if (textBoxPassword.Text.Equals(textBoxConfirmPassword.Text))
                return true;

            MessageBox.Show("Passwords do not match", "", MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning);
            textBoxConfirmPassword.Clear();
            textBoxPassword.Clear();
            return false;
        }
        private bool isFormValidated()
        {
            if (isPasswordConfirmed())
            {
                if (isFormCompleted())
                    return true;
            }
            return false;
        }
        private bool Insert_User_tbl()
        {
            if (isExistingUser(textBoxUsername.Text, SQL_CONNECTION))
            {
                MessageBox.Show("User already exists", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
                clearAllFields();
                return false;
            }
            else
            {
                string INSERT_QUERY =
                    "INSERT INTO dbo.[User] (Username, Password, AccessLevelId, FirstName, LastName, Registered) " +
                    "OUTPUT INSERTED.Id " +
                    "VALUES (@Username, @Password, @AccessLevelId, @FirstName, @LastName, @Registered)";
                if (SQL_CONNECTION.State == ConnectionState.Closed)
                    SQL_CONNECTION.Open();
                SqlCommand SQL_COMMAND = new SqlCommand(INSERT_QUERY, SQL_CONNECTION);
                SQL_COMMAND.Parameters.AddWithValue("@Username", textBoxUsername.Text);
                SQL_COMMAND.Parameters.AddWithValue("@Password", textBoxPassword.Text);
                SQL_COMMAND.Parameters.Add("@AccessLevelId", SqlDbType.Int);
                SQL_COMMAND.Parameters["@AccessLevelId"].Value = comboBoxAccessLevel.SelectedIndex;
                SQL_COMMAND.Parameters.AddWithValue("@FirstName", textBoxFirstName.Text);
                SQL_COMMAND.Parameters.AddWithValue("@LastName", textBoxLastName.Text);
                SQL_COMMAND.Parameters.AddWithValue("@Registered", "False");    //default value
                USER_ID = SQL_COMMAND.ExecuteScalar().ToString();
                SQL_CONNECTION.Close();
                return true;
            }
        }
        private void Update_User_tbl()
        {
            string UPDATE_QUERY =
                "UPDATE dbo.[User]" +
                "SET Registered = 'True'" +
                "WHERE Id = " + USER_ID;
            SQL_CONNECTION.Open();
            SqlCommand SQL_COMMAND = new SqlCommand(UPDATE_QUERY, SQL_CONNECTION);
            SQL_COMMAND.ExecuteNonQuery();
            SQL_CONNECTION.Close();
        }
    }
}
