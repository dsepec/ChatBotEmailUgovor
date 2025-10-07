namespace ChatBotSQL.Models
{
    public class EmailSettings
    {
        public string SenderAddress { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public string SmtpServer { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool EnableSsl { get; set; }
    }
}
