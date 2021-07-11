using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp
{
    class Response_on_Get_Guid
    {
        public string guid { get; set; }
        public string message { get; set; }
    }

    public class Errors
    {
        public string Guid { get; set; }
    }

    public class Response_on_Register
    {
        public string message { get; set; }
        public Errors errors { get; set; }
    }

}
