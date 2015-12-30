using System;
using Twilio;

namespace ConsoleTest
{
    class Program
    {
        static void Main()
        {
            const string accountSid = "AC783d5c8576eb1aa7aa03abca29e7a488";
            const string authToken = "ec8c9bdecaa381736288af8c3452e171";
            // Find your Account Sid and Auth Token at twilio.com/user/account
            var twilio = new TwilioRestClient(accountSid, authToken);
            var message = twilio.SendMessage("+12027598248", "+14077163399", "YEAH", "");

            Console.WriteLine(message.Sid);
        }
    }
}
