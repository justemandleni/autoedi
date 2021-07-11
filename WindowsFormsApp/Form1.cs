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
        private bool Is_User_Existing(string strUsername)
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
                if (Is_User_Existing(USER_USERNAME))
                {
                    MessageBox.Show("We already know you here " + USER_FIRSTNAME, "Account already exists", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Clear_All_Fields();
                }
                else
                {
                    Insert_User_tbl();
                    
                    IRestResponse guidResponseObject = Get_Guid_();
                    JsonDeserializer jsonDeserializer = new JsonDeserializer();

                    IRestResponse restResponse2;

                    if (guidResponseObject.IsSuccessful) //200 OK
                    {
                        string responseMessage = jsonDeserializer.Deserialize<Root>(guidResponseObject).message;
                        string userGuid = jsonDeserializer.Deserialize<Root>(guidResponseObject).guid; //get user Guid

                        if (responseMessage.Equals("success"))
                        {
                            restResponse2 = apiCalloutRegisterUser(userGuid, USER_ID); // call test register

                            if (restResponse2.IsSuccessful)
                                MessageBox.Show("Successfully registered " + textBoxFirstName.Text + "!", "Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            else
                            {
                                string message = jsonDeserializer.Deserialize<Root>(guidResponseObject).message;
                                MessageBox.Show("" + message, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                    else //ENCOUNTERED 404 OR SOME WIERD SERVER RESPONSE RESULT
                    {
                        MessageBox.Show("Failed to register due to error code: " + guidResponseObject.StatusCode, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        //update User db as not registered
                        Usertbl_Update(int.Parse(USER_ID));
                    }

                    Close();
                }

                SQL_CONNECTION.Close();
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

        private string Get_Guid_()
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
            if (response.IsSuccessful)
            {
                JsonDeserializer jsonDeserializer = new JsonDeserializer();
                string responseMessage = jsonDeserializer.Deserialize<Root>(response).message;
                string userGuid = jsonDeserializer.Deserialize<Root>(response).guid;

                return null;
            }
            else //ENCOUNTERED 404 OR SOME WIERD RESPONSE CODE
            {
                MessageBox.Show("Failed to register due to error code: " + response.StatusCode, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //update User db as not registered
                Usertbl_Update(int.Parse(USER_ID));
                return null;
            }
        }
        private IRestResponse apiCalloutRegisterUser(string guid, string userId)
        {
            var client = new RestClient("http://www.autoediportal.com/AutoEDI/api/v1/TestRegister.php");
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            //request.AddHeader("Authorization", "Basic VGVzdFJlZ2lzdGVyOlRlc3RQYXNzd29yZA==");
            request.AddHeader("Content-Type", "application/json");
            var body =
                @"{
                    ""guid"": """+guid+@""",
                    ""userId"": """+userId+@""",
                    ""username"":"""+USER_USERNAME+@""",
                    ""password"": """+USER_PASSWORD+@""",
                    ""accessLevelId"": """+USER_ACCESSLEVELID+@""", 
                    ""firstName"": """+USER_FIRSTNAME+@""",
                    ""lastName"": """+USER_LASTNAME+@"""
                  }";
            request.AddParameter("application/json", body, ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);
            return response;
        }

        private void Insert_User_tbl()
        {
            String INSERT_QUERY =
                "INSERT INTO dbo.[User] (Username, Password, AccessLevelId, FirstName, LastName) " +
                "OUTPUT INSERTED.Id " +
                "VALUES (@Username, @Password, @AccessLevelId, @FirstName, @LastName)";
            SqlCommand SQL_COMMAND = new SqlCommand(INSERT_QUERY, SQL_CONNECTION);
            SQL_COMMAND.Parameters.AddWithValue("@Username", textBoxUsername.Text);
            SQL_COMMAND.Parameters.AddWithValue("@Password", textBoxPassword.Text);
            SQL_COMMAND.Parameters.Add("@AccessLevelId", SqlDbType.Int);
            SQL_COMMAND.Parameters["@AccessLevelId"].Value = comboBoxAccessLevel.SelectedIndex;
            SQL_COMMAND.Parameters.AddWithValue("@FirstName", textBoxFirstName.Text);
            SQL_COMMAND.Parameters.AddWithValue("@LastName", textBoxLastName.Text);
            USER_ID = (string)SQL_COMMAND.ExecuteScalar();
        }

        private void Usertbl_Update(int strUserId)
        {

        }
    }
}
