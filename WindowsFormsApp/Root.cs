namespace WindowsFormsApp
{
    class Response_on_Get_Guid
    {
        public string guid { get; set; }
        public string message { get; set; }
    }


    public class Response_on_Register
    {
        public string message { get; set; }
        public string[] errors { get; set; }
    }


    //public class Errors
    //{
    //    public string Guid { get; set; }
    //}

    //public class Response_on_Register
    //{
    //    public string message { get; set; }
    //    public Errors errors { get; set; }
    //}

}
