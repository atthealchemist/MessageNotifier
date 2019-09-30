using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;

namespace MessageNotifier.Utils
{
    public class TelnetSmtpClient : IDisposable
    {
        private enum TelnetCodes
        {
            NONSTANDARD_SUCCESS_RESPONSE = 200,
            SYSTEM_STATUS = 211,
            HELP_MESSAGE = 214,
            SERVICE_READY = 220,
            SERVICE_CLOSED_CHANNEL = 221,
            LOGIN_ACCEPTED = 235,
            REQUESTED_MAIL_ACTION_OK = 250,
            USER_NOT_LOCAL_WILL_FORWARD = 251,
            CANNOT_VERIFY_USER_BUT_WILL_ATTEMPT_DELIVERY = 252,
            AUTH_CREDENTIALS_REQUEST = 334,
            MAIL_INPUT_START = 354,
            SERVICE_NOT_AVAILABLE = 421,
            MAILBOX_UNAVAILABLE = 450,
            LOCAL_ERROR_IN_PROCESSING = 451,
            INSUFFICIENT_SYSTEM_STORAGE = 452,
            SYNTAX_ERROR_COMMAND_UNRECOGNIZED = 500,
            SYNTAX_ERROR_IN_PARAMETERS = 501,
            COMMAND_NOT_IMPLEMENTED = 502,
            BAD_SEQUENCE_OF_COMMANDS = 503,
            COMMAND_PARAMETER_NOT_IMPLEMENTED = 504,
            DOMAIN_NOT_ACCEPT_MAIL = 521,
            ACCESS_DENIED = 530,
            ACTION_NOT_TAKEN_MAILBOX_UNAVAILABLE = 550,
            USER_NOT_LOCAL_TRY_FORWARD = 551,
            EXCEEDED_STORAGE_ALLOCATION = 552,
            MAILBOX_NAME_NOT_ALLOWED = 553,
            TRANSACTION_FAILED = 554
        }

        public class Response
        {

            public int Code { get; set; }
            public string Version { get; set; }
            public string Message { get; set; }
            public bool Status { get; set; }

        }

        private TcpClient client;
        private SslStream stream;

        private StreamReader reader;
        private StreamWriter writer;

        private readonly string url;
        private readonly int port;

        public bool Connected { get; set; }
        public bool Acquired { get; set; }
        public bool LoggedIn { get; set; }

        //public List<dynamic> Log { get; set; }

        public TelnetSmtpClient(string url, int port)
        {
            client = new TcpClient
            {
                ReceiveTimeout = 90000,
                SendTimeout = 90000
            };
            //Log = new List<dynamic>();

            this.url = url;
            this.port = port;

            Connect(url, port);
        }

        private void Connect(string url, int port)
        {

            Connected = false;

            if (client != null)
            {
                try
                {
                    client.Connect(url, port);

                    if (stream == null)
                    {
                        stream = new SslStream(client.GetStream());
                    }

                    stream.AuthenticateAsClient(url);

                    Connected = true;


                    var connectedAnswer = ParseOutput();
                    if (connectedAnswer.Code == (int)TelnetCodes.SERVICE_READY)
                    {
                        Acquire(url);
                    }
                    else
                    {
                        throw new Exception($"Telnet SMTP Client: Can't connect to server {url}:{port}! Code: {connectedAnswer.Code}, Message: {connectedAnswer.Message}");
                    }
                }
                catch (Exception ex)
                {
                    Connected = false;
                }
            }

        }

        public void Login(string username, string password)
        {
            LoggedIn = false;

            var user = SendCommand("auth login");

            var pass = SendCommand(Convert.ToBase64String(Encoding.ASCII.GetBytes(username)));
            var answer = SendCommand(Convert.ToBase64String(Encoding.ASCII.GetBytes(password)), TelnetCodes.LOGIN_ACCEPTED);

            LoggedIn = answer.Status;

        }

        private void Acquire(string server, bool helo = false)
        {
            string acquireCommand = helo ? "helo" : "ehlo";
            var acquire = SendCommand($"{acquireCommand} {server}", TelnetCodes.REQUESTED_MAIL_ACTION_OK);
            Acquired = acquire.Status;

        }

        public bool SendMail(string sender, string[] receipients, string subject, string message)
        {
            var mailFromAnswer = SendCommand($"mail from: <{sender}>", TelnetCodes.REQUESTED_MAIL_ACTION_OK);
            
            foreach(var recepient in receipients)
            {
                var mailToAnswer = SendCommand($"rcpt to: <{recepient}>", TelnetCodes.REQUESTED_MAIL_ACTION_OK);
            }

            var data = SendCommand("data", TelnetCodes.MAIL_INPUT_START);

            var sending = SendCommand($"Subject: {subject}{Environment.NewLine}{message}{Environment.NewLine}.", TelnetCodes.REQUESTED_MAIL_ACTION_OK);

            return sending.Status;
        }

        public bool SendMail(string sender, string recepient, string subject, string message)
        {

            var mailFromAnswer = SendCommand($"mail from: <{sender}>", TelnetCodes.REQUESTED_MAIL_ACTION_OK);
            var mailToAnswer = SendCommand($"rcpt to: <{recepient}>", TelnetCodes.REQUESTED_MAIL_ACTION_OK);

            var data = SendCommand("data", TelnetCodes.MAIL_INPUT_START);

            var sending = SendCommand($"Subject: {subject}{Environment.NewLine}{message}{Environment.NewLine}.", TelnetCodes.REQUESTED_MAIL_ACTION_OK);

            return sending.Status;
        }

        private Response ParseCommand(string line)
        {
            if (line.Length < 1)
                return new Response();

            string parsedCode = line.Substring(0, 3);
            string parsedMessage = "";
            if (parsedCode.Length > 0)
            {
                parsedMessage = line.Substring(parsedCode.Length, line.Length - parsedCode.Length);
            }

            var result = new Response
            {
                Code = int.Parse(parsedCode),
                Message = parsedMessage.Trim(' ')
            };

            if (result.Code == (int)TelnetCodes.AUTH_CREDENTIALS_REQUEST)
            {
                result.Message = Encoding.ASCII.GetString(Convert.FromBase64String(result.Message));
            }

            return result;
        }

        private dynamic SendCommand(string message, TelnetCodes expectedCode)
        {
            var response = SendCommand(message);
            if (response.Code != (int)expectedCode)
            {
                throw new Exception($"Unexpected response code: {response.Code}");
            }
            else
            {
                response.Status = true;
            }

            return response;
        }

        private Response SendCommand(string message)
        {
            WriteLine(message);
            var response = ParseOutput();

            if (response == null)
            {
                return new Response();
            }

            return response;
        }

        private Response ParseOutput()
        {
            var line = ReadLine();
            dynamic answer = "";
            if (line != null)
            {
                answer = ParseCommand(ReadLine());
                //Log.Add(answer);

            }

            return answer;
        }

        private string ReadLine()
        {
            string line = "";
            if (Connected && stream?.CanRead == true)
            {
                reader = new StreamReader(stream);

                line = reader.ReadToEnd();
            }

            return line;
        }

        private void WriteLine(string command)
        {
            if (stream != null)
            {
                writer = new StreamWriter(stream);
                writer.WriteLine(command + Environment.NewLine);
                writer.Flush();
            }
        }

        public void Dispose()
        {
            var quit = SendCommand("quit", TelnetCodes.SERVICE_CLOSED_CHANNEL);

            if (quit.Status == true && client.Connected)
            {
                stream.Close();
                client.Close();
            }
        }
    }
}
