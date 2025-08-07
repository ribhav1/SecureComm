using SecureCommAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace SecureComm.Screens
{
    class RoomScreen : IScreen
    {
        private Guid roomId = new Guid();
        private string username = "";
        private Guid userId = Guid.Empty;

        private StringBuilder userInputBuffer = new StringBuilder();
        private int outputLineIndex = 0;
        private int inputLineIndex = Console.WindowHeight - 1;

        private bool inRoomScreen = true;

        private bool isHost;

        private RSAParameters userPublicKey;
        private RSAParameters userPrivateKey;

        private RSAParameters sessionPublicKey;
        private RSAParameters sessionPrivateKey;

        public RoomScreen(Guid _roomId, string _username, Guid _userId, bool _isHost)
        {
            roomId = _roomId;
            username = _username;
            userId = _userId;
            isHost = _isHost;

            var userCSP = new RSACryptoServiceProvider(2048);
            userPublicKey = userCSP.ExportParameters(false); // set user public key
            userPrivateKey = userCSP.ExportParameters(true); // set user private key

            if (isHost)
            {
                var sessionCSP = new RSACryptoServiceProvider(2048);
                sessionPublicKey = sessionCSP.ExportParameters(false); // if host, set session public key
                sessionPrivateKey = sessionCSP.ExportParameters(true); // if host, set session private key
            }

        }

        int count = 0;
        public async Task DrawScreen(ScreenManager screenManager)
        {
            // make sure this function is only called once
            if (count != 0)
            {
                return;
            }

            // add host as first connected user so clients can query it for session key
            if (isHost)
            {

            }

            await enterChatRoom(roomId, userId, username, screenManager);
            count++;
        }

        async Task enterChatRoom(Guid roomGUID, Guid userID, string username, ScreenManager screenManager)
        {

            //if client, retrieve session key from connected users
            if (!isHost)
            {
                while (sessionPublicKey.Modulus == null && sessionPrivateKey.Modulus == null)
                {
                    // -> query the connected users column in the rooms table
                    
                    // -> get the first, or any, user
                    //   -> this means it is important that a user can only become a "connected user"
                    //      if they have retrieved the session key in the first place. This is also why
                    //      is is important that the host becomes the first connected user. Now, even if
                    //      the host leaves then there will be other "connected users" that can provide
                    //      the session key
                    
                    // -> send a direct message to that user requesting a session key
                    
                    // -> the user will send a direct message back with the session key as response
                    
                    // -> set the session key variables
                    
                    // -> continue
                }
            }

            // start recieving messages in background thread
            Thread messageReciever = new Thread(() => RecieveMessages(roomGUID));
            messageReciever.IsBackground = true;
            messageReciever.Start();

            // start user input loop
            await HandleUserInput(roomGUID, userID, username, screenManager);
        }

        async Task RecieveMessages(Guid roomGUID)
        {
            DateTime lastTime = DateTime.UtcNow;

            while (inRoomScreen)
            {
                List<MessageModel> newMessages = await ApiClient.GetMessages(roomGUID, lastTime.ToUniversalTime());

                foreach (MessageModel message in newMessages)
                {
                    WriteMessage(message.Content, message.Username);
                }

                // Update lastTime to max timestamp received, or keep as is
                if (newMessages.Any())
                {
                    lastTime = newMessages.Max(message => message.CreatedAt);
                }
            }
        }

        void WriteMessage(string message, string username)
        {
            // Save current input and cursor position
            int cursorTop = outputLineIndex; // Cursor Row Position
            string currentInput = userInputBuffer.ToString();

            // Output is configured so that current line will always have empty line above it

            //Move to output line and print message
            Console.SetCursorPosition(0, cursorTop);
            Console.Write("\r" + new string(' ', Console.WindowWidth) + "\r");
            Console.SetCursorPosition(12 - (username.Length + 4), cursorTop);
            Console.Write($"[{username}]: ");
            Console.SetCursorPosition(12, cursorTop); // Stay at current row but move right to line up output
            Console.WriteLine(message);

            // Set line for next output as 2 lines below last output line - due to the above 2 Console.WriteLines
            outputLineIndex += 2;

            // Move back to input line and restore user input
            Console.SetCursorPosition(0, inputLineIndex);
            Console.Write("> " + currentInput);
            Console.SetCursorPosition(2 + currentInput.Length, Console.WindowHeight - 1);

        }

        async Task HandleUserInput(Guid roomGUID, Guid userID, string username, ScreenManager screenManager)
        {

            Console.SetCursorPosition(0, inputLineIndex);
            Console.Write("> ");
            while (inRoomScreen)
            {
                EnsureBufferSize();

                var key = Console.ReadKey(intercept: true);


                if (key.Key == ConsoleKey.Enter)
                {
                    string userInput = userInputBuffer.ToString();
                    if (userInput.Trim().ToLower() == "exit")
                    {
                        screenManager.Exit();
                        inRoomScreen = false;
                        break;
                    }

                    var messageEncryptCSP = new RSACryptoServiceProvider();
                    messageEncryptCSP.ImportParameters(sessionPublicKey);

                    var userInputBytes = System.Text.Encoding.Unicode.GetBytes(userInput);
                    var userInputEncryptedBytes = messageEncryptCSP.Encrypt(userInputBytes, false);
                    var userInputEncryptedString = Convert.ToBase64String(userInputEncryptedBytes);

                    MessageModel userMessage = await ApiClient.SendMessage(roomGUID, userID, username, userInputEncryptedString, null, "Red");

                    if (userMessage == null)
                    {
                        WriteMessage("Failed to send message", "SYSTEM");
                        continue;
                    }

                    // Clear input line
                    Console.Write("\r" + new string(' ', Console.WindowWidth) + "\r");
                    userInputBuffer.Clear();

                    // Move back to input line
                    Console.SetCursorPosition(0, inputLineIndex);
                    Console.Write("> ");
                    Console.SetCursorPosition(2, inputLineIndex);
                }
                else if (key.Key == ConsoleKey.Backspace)
                {

                    if (userInputBuffer.Length > 0)
                    {
                        userInputBuffer.Length--;
                        Console.Write("\b \b");
                    }
                }
                else
                {
                    if (userInputBuffer.Length >= 100)
                    {
                        continue;
                    }

                    userInputBuffer.Append(key.KeyChar);
                    Console.Write(key.KeyChar);
                }
            }

        }

        void EnsureBufferSize()
        {
            if (outputLineIndex + 10 >= Console.BufferHeight)
            {
                Console.SetBufferSize(Console.BufferWidth, Console.BufferHeight + 10);
            }
        }

    }
}
