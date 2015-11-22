using System;
using Twilio;

namespace ConsoleTest
{
    class Program
    {
        static void Main()
        {
            const string accountSid = "";
            const string authToken = "";
            // Find your Account Sid and Auth Token at twilio.com/user/account
            var twilio = new TwilioRestClient(accountSid, authToken);
            var message = twilio.SendMessage("+12027598248", "+14077163399", "YEAH", "");

            Console.WriteLine(message.Sid);
        }
    }
}
