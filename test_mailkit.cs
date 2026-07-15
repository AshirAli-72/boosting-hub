using System;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

public class MailKitTest {
    public static void Run() {
        try {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Boosting Hub", "ashir72ali@gmail.com"));
            message.To.Add(new MailboxAddress("Test", "ashir72ali@gmail.com"));
            message.Subject = "Test";
            message.Body = new TextPart("plain") { Text = "Test body" };

            using var client = new SmtpClient();
            client.Timeout = 30000;
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;
            
            Console.WriteLine("Connecting...");
            client.Connect("smtp.gmail.com", 587, SecureSocketOptions.Auto);
            
            Console.WriteLine("Authenticating...");
            client.Authenticate("ashir72ali@gmail.com", "ulvgmyclkgfserrs");
            
            Console.WriteLine("Sending...");
            client.Send(message);
            
            Console.WriteLine("Disconnecting...");
            client.Disconnect(true);
            
            Console.WriteLine("Success with MailKit");
        } catch (Exception ex) {
            Console.WriteLine("MailKit Error: " + ex.Message);
            if (ex.InnerException != null) Console.WriteLine("Inner: " + ex.InnerException.Message);
        }
    }
}
