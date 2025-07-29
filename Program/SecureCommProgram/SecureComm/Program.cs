using SecureComm;
using SecureCommAPI.Models;
using System.Text;

StringBuilder userInputBuffer = new StringBuilder();
bool isRunning = true;
int outputLineIndex = 0;
int inputLineIndex = Console.WindowHeight - 1;

Console.WriteLine("Welcome to SecureComm. Please enter the ID of the room you would like to connect to:");
string inputId = Console.ReadLine();
Guid room_id = CheckForValidGUID(inputId);

bool isValidRoom = await ApiClient.ValidateRoomById(room_id);
while (!isValidRoom)
{
    Console.WriteLine("No room with given GUID exists. Try again:");
    room_id = CheckForValidGUID(Console.ReadLine());
    isValidRoom = await ApiClient.ValidateRoomById(room_id);
}

Console.WriteLine("Please enter the password:");
bool isValidPassword = await ApiClient.ValidateRoomPassword(room_id, Console.ReadLine());
while (!isValidPassword)
{
    Console.WriteLine("Wrong password. Try again:");
    isValidPassword = await ApiClient.ValidateRoomPassword(room_id, Console.ReadLine());
}

Console.WriteLine("Please enter a display name.");
Console.WriteLine("It must be at least 3 characters but no more than 8");
string inputUserId = "";
while (inputUserId.Length < 3 || inputUserId.Length > 8)
{
    inputUserId = Console.ReadLine();
}

Console.Clear();

await enterChatRoom(room_id, inputUserId);

Guid CheckForValidGUID(string userInput)
{
    Guid roomId;
    string _input = userInput;

    while (!Guid.TryParse(_input, out roomId))
    {
        Console.WriteLine("Invalid GUID. Please enter a valid GUID:");
        _input = Console.ReadLine();
    }

    return roomId;
}

async Task enterChatRoom(Guid roomGUID, string userID)
{

    // Start recieving messages in background thread
    Thread messageReciever = new Thread(() => RecieveMessages(roomGUID));
    messageReciever.IsBackground = true;
    messageReciever.Start();

    // Start user input loop
    await HandleUserInput(roomGUID, userID);
}

async Task RecieveMessages(Guid roomGUID)
{
    DateTime lastTime = DateTime.UtcNow;

    while (isRunning)
    {
        List<MessageModel> newMessages = await ApiClient.GetMessages(roomGUID, lastTime.ToUniversalTime());
       
        foreach (MessageModel message in newMessages)
        {
            //Console.WriteLine(message.Content);
            WriteMessage(message.Content, message.UserId);
        }

        // Update lastTime to max timestamp received, or keep as is
        if (newMessages.Any())
        {
            lastTime = newMessages.Max(message => message.CreatedAt);
        }
    }
}

void WriteMessage(string message, string userID)
{
    // Save current input and cursor position
    int cursorTop = outputLineIndex; // Cursor Row Position
    string currentInput = userInputBuffer.ToString();

    // Output is configured so that current line will always have empty line above it

    //Move to output line and print message
    Console.SetCursorPosition(0, cursorTop);
    Console.Write("\r" + new string(' ', Console.WindowWidth) + "\r");
    Console.SetCursorPosition(12 - (userID.Length + 4), cursorTop);
    Console.Write($"[{userID}]: ");
    Console.SetCursorPosition(12, cursorTop); // Stay at current row but move right to line up output
    Console.WriteLine(message);

    // Set line for next output as 2 lines below last output line - due to the above 2 Console.WriteLines
    outputLineIndex += 2;

    // Move back to input line and restore user input
    Console.SetCursorPosition(0, inputLineIndex);
    Console.Write("> " + currentInput);
    Console.SetCursorPosition(2 + currentInput.Length, Console.WindowHeight-1);

}

async Task HandleUserInput(Guid roomGUID, string userID)
{

    Console.SetCursorPosition(0, inputLineIndex);
    Console.Write("> ");
    while (isRunning)
    {
        EnsureBufferSize();

        var key = Console.ReadKey(intercept: true);


        if (key.Key == ConsoleKey.Enter)
        {
            string userInput = userInputBuffer.ToString();
            if (userInput.Trim().ToLower() == "exit")
            {
                isRunning = false;
                break;
            }


            MessageModel userMessage = await ApiClient.SendMessage(roomGUID, userID, userInput, "Red");

            if (userMessage == null)
            {
                Console.WriteLine("Failed to send message");
                continue;
            }

            // Clear input line
            Console.Write("\r" + new string(' ', Console.WindowWidth) + "\r");

            //// Move to output line and print message and blank line under it
            //Console.SetCursorPosition(2, outputLineIndex);
            //Console.Write("[USER]: ");
            //Console.SetCursorPosition(10, Console.CursorTop);
            //Console.WriteLine($"{userInput}");
            //Console.WriteLine();

            //outputLineIndex += 2;

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
