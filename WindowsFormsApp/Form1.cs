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
        private string USER_ID, USER_USERNAME, USER_PASSWORD, USER_ACCESSLEVELID, USER_FIRSTNAME, USER_LASTNAME;

        public Form1()
        {
            InitializeComponent();
            SQL_CONNECTION = new SqlConnection(@"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=AutoEDITest;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False");
            USER_USERNAME = textBoxUsername.Text;
            USER_PASSWORD = textBoxPassword.Text;
            USER_ACCESSLEVELID = comboBoxAccessLevel.SelectedIndex.ToString();
            USER_FIRSTNAME = textBoxFirstName.Text;
            USER_LASTNAME = textBoxLastName.Text;
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            AccessLeveltbl_Load();
        }
        private void AccessLeveltbl_Load()
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
        private void Clear_All_Fields()
        {
            textBoxUsername.Clear();
            textBoxPassword.Clear();
            textBoxConfirmPassword.Clear();
            textBoxFirstName.Clear();
            textBoxLastName.Clear();
            comboBoxAccessLevel.SelectedIndex = 0;
        }
        private bool Is_Entry_Empty(string strValue)
        {
            if (String.IsNullOrWhiteSpace(strValue))
                return true;
            return false;
        }
        private bool Is_Form_Complete()
        {
            if (Is_Entry_Empty(textBoxUsername.Text) || Is_Entry_Empty(textBoxPassword.Text) ||
                Is_Entry_Empty(textBoxConfirmPassword.Text) || Is_Entry_Empty(textBoxFirstName.Text) ||
                Is_Entry_Empty(textBoxLastName.Text) || Is_Entry_Empty(comboBoxAccessLevel.Text))
            {
                MessageBox.Show("Please fill in all entries", "", MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }
        private bool Is_Password_Confirmed()
        {
            if (textBoxPassword.Text.Equals(textBoxConfirmPassword.Text))
                return true;

            MessageBox.Show("Passwords do not match", "", MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning);
            textBoxConfirmPassword.Clear();
            textBoxPassword.Clear();
            return false;
        }
        private bool Is_Existing_User(string strUsername)
        {
            string SQL_SELECT_STMNT = "SELECT * FROM dbo.[User] WHERE Username = '" + strUsername + "'";
            SqlDataAdapter DATA_ADAPTER = new SqlDataAdapter(SQL_SELECT_STMNT, SQL_CONNECTION);
            DataTable DATA_TABLE = new DataTable();
            DATA_ADAPTER.Fill(DATA_TABLE);
            if (DATA_TABLE.Rows.Count > 0)
                return true;
            return false;
        }
        private void buttonAddUser_Click(object sender, EventArgs e)
        {
            if (isValidated())
            {
                if (Is_Existing_User(USER_USERNAME))
                {
                    MessageBox.Show("We already know you here " + USER_FIRSTNAME, "Account already exists", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Clear_All_Fields();
                }
                else
                {
                    Insert_User_tbl();
                    string userGuid = Get_Guid_("TestRegister", "TestPassword");
                    if (userGuid.Equals(""))
                        MessageBox.Show("Failed to Get Guid.", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Register_User(userGuid);
                }

                //TO:DO
                //refresh and show comm table

            }
        }

        private bool isValidated()
        {
            if (Is_Password_Confirmed())
            {
                if (Is_Form_Complete())
                    return true;
            }
            return false;
        }

        private string Get_Guid_(string authUsername, string authPassword)
        {
            var client = new RestClient("http://www.autoediportal.com/AutoEDI/Api/v1/TestLogin.php");
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Authorization", "Basic VGVzdFJlZ2lzdGVyOlRlc3RQYXNzd29yZA==");
            request.AddHeader("Content-Type", "application/json");
            var body = @"{
                            " + "\n" +
                           @"  ""username"":"""+authUsername+@""",
                            " + "\n" +
                           @"  ""password"":"""+authPassword+@"""
                            " + "\n" +
                       @"}";
            request.AddParameter("application/json", body, ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);
            if (response.IsSuccessful) //200 OK
            {
                JsonDeserializer jsonDeserializer = new JsonDeserializer();
                string message = jsonDeserializer.Deserialize<Response_on_Get_Guid>(response).message;
                if (message.Equals("success"))
                    return jsonDeserializer.Deserialize<Response_on_Get_Guid>(response).guid;
                else return "";
            }
            else //ENCOUNTERED 404 OR SOME WIERD RESPONSE CODE
                return "";
        }

        private void Register_User(string GUID)
        {
            var client = new RestClient("http://www.autoediportal.com/AutoEDI/api/v1/TestRegister.php");
            client.Timeout = -1;
            var request = new RestRequest(Method.GET);
            request.AddHeader("guid", GUID);
            request.AddHeader("userId", USER_ID);
            request.AddHeader("username", USER_USERNAME);
            request.AddHeader("password", USER_PASSWORD);
            request.AddHeader("accessLevelId", USER_ACCESSLEVELID);
            request.AddHeader("firstName", USER_FIRSTNAME);
            request.AddHeader("lastName", USER_LASTNAME);
            var body = @"";
            request.AddParameter("text/plain", body, ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);
            if (response.IsSuccessful) // 200 OK
            {
                //deserialise 
                JsonDeserializer jsonDeserializer = new JsonDeserializer();
                string message = jsonDeserializer.Deserialize<Response_on_Register>(response).message;
                if (message.Equals("success"))
                {
                    Usertbl_Update();
                    MessageBox.Show("Successfully registered " + USER_FIRSTNAME + ".", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    //TO:DO
                    //insert into comm table show success
                }
                else
                {
                    Errors objInresponseObj = jsonDeserializer.Deserialize<Response_on_Register>(response).errors;
                    string Guiderror = objInresponseObj.Guid;
                    MessageBox.Show("Failed to register user on server. " + Guiderror, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    //TO:DO
                    //insert into comm table show failure
                }
            }
            else //ENCOUNTERED 404 OR SOME WIERD RESPONSE CODE
            {
                MessageBox.Show("Failed to hit server. Error: " + response.ErrorMessage, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //TO:DO
                //insert into comm table show failure
            }
        }

        private void Insert_User_tbl()
        {
            string INSERT_QUERY =
                "INSERT INTO dbo.[User] (Username, Password, AccessLevelId, FirstName, LastName, Registered) " +
                "OUTPUT INSERTED.Id " +
                "VALUES (@Username, @Password, @AccessLevelId, @FirstName, @LastName, @Registered)";
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
        }

        private void Usertbl_Update()
        {
            string UPDATE_QUERY =
                "UPDATE dbo.[User]" +
                "SET Registered = 'True'" +
                "WHERE Username = " + USER_ID;
            SQL_CONNECTION.Open();
            SqlCommand SQL_COMMAND = new SqlCommand(UPDATE_QUERY, SQL_CONNECTION);
            SQL_COMMAND.ExecuteNonQuery();
            SQL_CONNECTION.Close();
        }
    }
}
