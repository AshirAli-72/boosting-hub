using System;
using System.Net;
using System.Net.Mail;

public class SmtpTest {
    public static void Run() {
        try {
            var client = new SmtpClient("smtp.gmail.com", 587) {
                Credentials = new NetworkCredential("ashir72ali@gmail.com", "ulvgmyclkgfserrs"),
                EnableSsl = true
            };
            client.Send("ashir72ali@gmail.com", "ashir72ali@gmail.com", "Test", "Test body");
            Console.WriteLine("Success with System.Net.Mail");
        } catch (Exception ex) {
            Console.WriteLine("System.Net.Mail Error: " + ex.Message);
            if (ex.InnerException != null) Console.WriteLine("Inner: " + ex.InnerException.Message);
        }
    }
}
