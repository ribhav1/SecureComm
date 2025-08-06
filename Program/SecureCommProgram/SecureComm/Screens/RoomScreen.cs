using SecureCommAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public RoomScreen(Guid _roomId, string _username, Guid _userId)
        {
            roomId = _roomId;
            username = _username;
            userId = _userId;
        }

        public async Task DrawScreen(ScreenManager screenManager)
        {
            await enterChatRoom(roomId, userId, username, screenManager);
        }

        async Task enterChatRoom(Guid roomGUID, Guid userID, string username, ScreenManager screenManager)
        {

            // Start recieving messages in background thread
            Thread messageReciever = new Thread(() => RecieveMessages(roomGUID));
            messageReciever.IsBackground = true;
            messageReciever.Start();

            // Start user input loop
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

                    MessageModel userMessage = await ApiClient.SendMessage(roomGUID, userID, username, userInput, "Red");

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
