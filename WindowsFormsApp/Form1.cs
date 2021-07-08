using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp
{
    public partial class Form1 : Form
    {
        SqlConnection CONNECTION;
        string CONNECTION_STRING = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=C:\Users\Admin\Documents\TEAM_12_ULTRON_3_FORAGE_DATABASE.mdf;Integrated Security=True;Connect Timeout=30";


        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            populateComboBox();
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

            comboBoxAccessLevel.DataSource = dt;
            comboBoxAccessLevel.DisplayMember = "Name";
            comboBoxAccessLevel.ValueMember = "Id";

        }

        private bool isMatching(string a, string b)
        {
            if (a.Equals(b))
                return true;
            else return false;
        }

        private bool isExisting(string str1, SqlConnection connection)
        {
            string SQL_SELECT_STMNT = "SELECT * FROM Student WHERE studentUsername = '" + str1 + "'";

            SqlDataAdapter DATA_ADAPTER = new SqlDataAdapter(SQL_SELECT_STMNT, connection);
            DataTable DATA_TABLE = new DataTable();
            DATA_ADAPTER.Fill(DATA_TABLE);

            if (DATA_TABLE.Rows.Count > 0)
                return true;
            else return false;
        }


        private void buttonAddUser_Click(object sender, EventArgs e)
        {
            if (!isMatching(textBoxPassword.Text, textBoxConfirmPassword.Text))
            {
                MessageBox.Show("Passwords do not match", "", MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning);
                textBoxConfirmPassword.Clear();
                return;
            }
            if (!isExisting(textBoxUsername.Text, CONNECTION))
            {
                SqlCommand command = new SqlCommand(@"INSERT INTO User (Username, Password, AccessLevelId, FirstName, LastName) 
                  VALUES ('" + textBoxUsername.Text + "', " +
                         "'" + textBoxPassword.Text + "', " +
                         "'" + comboBoxAccessLevel.Text + "', " +
                         "'" + textBoxFirstName.Text + "', " +
                         "'" + textBoxLastName.Text + 
                         "')", CONNECTION);
                command.ExecuteNonQuery();
                CONNECTION.Close();
                MessageBox.Show("Successfully registered " + textBoxFirstName.Text + "!", "Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Close();
            }
            else
            {
                MessageBox.Show("We already know you here " + textBoxFirstName.Text + "! Rather log in.", "Account already exists", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
