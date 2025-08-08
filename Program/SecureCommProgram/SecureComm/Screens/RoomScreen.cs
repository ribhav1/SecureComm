using SecureCommAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text.Json;
using System.Xml.Serialization;
using System.Text.Encodings.Web;
using System.Web;
using System.Runtime.InteropServices.Marshalling;
using System.Diagnostics.Tracing;

//using SecureComm;

namespace SecureComm.Screens
{
    class RoomScreen : IScreen
    {
        private Guid roomId = new Guid();
        private string username = "";
        private Guid userId = Guid.Empty;
        private bool isHost;

        private StringBuilder userInputBuffer = new StringBuilder();
        private int outputLineIndex = 0;
        private int inputLineIndex = Console.WindowHeight - 1;

        private bool inRoomScreen = true;

        private RSAParameters userPublicKey;
        private RSAParameters userPrivateKey;

        // init keys randomly. serves purpose of allowing user without true session keys to join room properly, just not participate until true keys are acquired
        private RSAParameters sessionPublicKey = new RSACryptoServiceProvider(2048).ExportParameters(false);
        private RSAParameters sessionPrivateKey = new RSACryptoServiceProvider(2048).ExportParameters(true);

        private Dictionary<int, string> sessionPublicKeyChunks = new Dictionary<int, string>();
        private Dictionary<int, string> sessionPrivateKeyChunks = new Dictionary<int, string>();

        bool publicKeyProcessed = false;
        bool privateKeyProcessed = false;
        bool keysComplete => publicKeyProcessed && privateKeyProcessed;

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
            // make sure DrawScreen is only called once
            if (count != 0)
            {
                return;
            }

            // add self as connected user
            string newConnectedUserPublicKeyString = EncryptionManager.RSAKeyToString(userPublicKey);
            RoomModel newUserRoom = await ApiClient.AddConnectedUser(roomId, userId, newConnectedUserPublicKeyString);
            if (newUserRoom == null)
            {
                WriteMessage("Failed to add to connected users", "SYSTEM");
            }

            await enterChatRoom(roomId, userId, username, screenManager);
            count++;
        }

        async Task enterChatRoom(Guid roomGUID, Guid userID, string username, ScreenManager screenManager)
        {
            //if client, request and retrieve session key from connected users
            if (!isHost)
            {
                await RequestSessionKeysAsClient(roomGUID, userID, username);
                await RetrieveSessionKeysAsClient(roomGUID, userID);
            }

            // start recieving direct messages in background thread
            Thread directMessageListener = new Thread(() => DirectMessageListener(roomGUID, userID));
            directMessageListener.IsBackground = true;
            directMessageListener.Start();

            // start recieving messages in background thread
            Thread messageReciever = new Thread(() => RecieveMessages(roomGUID, userID));
            messageReciever.IsBackground = true;
            messageReciever.Start();

            // start user input loop
            await HandleUserInput(roomGUID, userID, username, screenManager);
        }

        async Task RequestSessionKeysAsClient(Guid roomGUID, Guid userID, string username)
        {
            bool sessionKeyRequestSent = false;
            while (!sessionKeyRequestSent)
            {
                RoomModel targetRoom = await ApiClient.GetRoomById(roomGUID);

                Dictionary<Guid, string> targetRoomConnectedUsers = targetRoom.ConnectedUsers;
                var otherUsers = targetRoomConnectedUsers.Where(x => x.Key != userID).ToList();
                if (!otherUsers.Any())
                    continue;

                KeyValuePair<Guid, string> firstPair = otherUsers.First();
                RSAParameters firstPairPublicKey = EncryptionManager.StringToRSAKey(Uri.UnescapeDataString(firstPair.Value));

                var messageEncryptCSP = new RSACryptoServiceProvider(2048);
                messageEncryptCSP.ImportParameters(firstPairPublicKey); // encrypt direct message using the intended receipient's public key

                MessageModel directMessage = await ApiClient.SendMessage(roomGUID, userID, username, EncryptionManager.EncryptMessage($"REQUEST SESSION KEY {userID}", messageEncryptCSP), firstPair.Key, "Red");
                if (directMessage == null)
                {
                    WriteMessage("Failed to send message", "SYSTEM");
                    continue;
                }

                sessionKeyRequestSent = true;
            }
        }

        async Task DirectMessageListener(Guid roomGUID, Guid userId)
        {
            DateTime lastTime = DateTime.UtcNow;

            // direct messages to the user are encrypted using their public key, so their private key is needed to decrypt direct messages
            var directMessageDecryptCSP = new RSACryptoServiceProvider(2048);
            directMessageDecryptCSP.ImportParameters(userPrivateKey);

            while (inRoomScreen)
            {
                List<MessageModel> newDirectMessages = await ApiClient.GetMessages(roomGUID, lastTime.ToUniversalTime(), userId);

                foreach (MessageModel message in newDirectMessages)
                {
                    try
                    {
                        string decryptedDirectMessage = EncryptionManager.DecryptMessage(message.Content, directMessageDecryptCSP);

                        if (decryptedDirectMessage.StartsWith("REQUEST SESSION KEY")) // if recieving a request, send a response
                        {
                            // get the sender's guid
                            string senderGuidString = decryptedDirectMessage.Remove(0, new string("REQUEST SESSION KEY").Length + 1);
                            Guid senderGuid = Guid.Parse(senderGuidString);

                            // get the sender's public key using their guid
                            string senderPublicKeyString = Uri.UnescapeDataString((await ApiClient.GetRoomById(roomGUID)).ConnectedUsers[senderGuid]);
                            RSAParameters senderPublicKey = EncryptionManager.StringToRSAKey(senderPublicKeyString);

                            // create a new instance of RSACryptoServiceProvider that can encrypt using the sender's public key
                            var encryptCSP = new RSACryptoServiceProvider(2048);
                            encryptCSP.ImportParameters(senderPublicKey);

                            // convert the session public keys to strings
                            string sessionPublicKeyString = EncryptionManager.RSAKeyToString(sessionPublicKey);
                            string sessionPrivateKeyString = EncryptionManager.RSAKeyToString(sessionPrivateKey);

                            // divide the keys into chunks to be encrypted as there is an upper limit on the length of data that can be encrypted at a time
                            int chunkSize = 80; // key size = 2048 bits = 256 bytes - 11 bytes for padding and header data = 245 bytes / 2 = 122.5 chars = 122 chars - chunk message prefix length = 101 chars ~ 100
                            int totalChunks = (int)Math.Ceiling((double)sessionPublicKeyString.Length / chunkSize);

                            // send public key
                            for (int i = 0; i < totalChunks; i++)
                            {
                                int startIndex = i * chunkSize;
                                int length = Math.Min(chunkSize, sessionPublicKeyString.Length - startIndex);

                                string chunk = sessionPublicKeyString.Substring(startIndex, length);
                                string chunkMessage = $"SESSION_PUB_PART {i + 1}/{totalChunks} {chunk}";

                                await ApiClient.SendMessage(roomGUID, userId, username, EncryptionManager.EncryptMessage(chunkMessage, encryptCSP), senderGuid, "Red");
                            }

                            // send private key
                            totalChunks = (int)Math.Ceiling((double)sessionPrivateKeyString.Length / chunkSize);
                            for (int i = 0; i < totalChunks; i++)
                            {
                                int startIndex = i * chunkSize;
                                int length = Math.Min(chunkSize, sessionPrivateKeyString.Length - startIndex);

                                string chunk = sessionPrivateKeyString.Substring(startIndex, length);
                                string chunkMessage = $"SESSION_PRV_PART {i + 1}/{totalChunks} {chunk}";

                                await ApiClient.SendMessage(roomGUID, userId, username, EncryptionManager.EncryptMessage(chunkMessage, encryptCSP), senderGuid, "Red");
                            }

                        }
                    }
                    catch (Exception e)
                    {
                        WriteMessage("DECRYPT ERROR: " + e.Message, message.Username);
                    }

                }

                if (newDirectMessages.Any())
                {
                    lastTime = newDirectMessages.Max(m => m.CreatedAt);
                }
            }
        }

        async Task RetrieveSessionKeysAsClient(Guid roomGUID, Guid userId)
        {
            Console.WriteLine("Waiting for session keys...", "SYSTEM");

            DateTime lastTime = DateTime.UtcNow;

            // direct messages to the user are encrypted using their public key, so their private key is needed to decrypt direct messages
            var directMessageDecryptCSP = new RSACryptoServiceProvider(2048);
            directMessageDecryptCSP.ImportParameters(userPrivateKey);

            while (inRoomScreen && !keysComplete)
            {
                List<MessageModel> newDirectMessages = await ApiClient.GetMessages(roomGUID, lastTime.ToUniversalTime(), userId);

                foreach (MessageModel message in newDirectMessages)
                {
                    try
                    {
                        string decryptedDirectMessage = EncryptionManager.DecryptMessage(message.Content, directMessageDecryptCSP);

                        if (decryptedDirectMessage.StartsWith("SESSION_PUB_PART")) // if recieving the public session key, parse string and set public session key
                        {
                            var data = ParseChunk(decryptedDirectMessage);
                            string chunkData = data.Item1;
                            int chunkNumber = data.Item2;
                            int totalChunks = data.Item3;

                            sessionPublicKeyChunks[chunkNumber] = chunkData;
                            if (!publicKeyProcessed && sessionPublicKeyChunks.Count == totalChunks)
                            {
                                var orderedData = string.Concat(sessionPublicKeyChunks.OrderBy(c => c.Key).Select(c => c.Value));
                                sessionPublicKey = EncryptionManager.StringToRSAKey(orderedData);

                                publicKeyProcessed = true;
                                WriteMessage("Session Public Key Recieved", "SYSTEM");
                            }
                        }
                        else if (decryptedDirectMessage.StartsWith("SESSION_PRV_PART")) // if recieving the private session key, parse string and set public session key
                        {
                            var data = ParseChunk(decryptedDirectMessage);
                            string chunkData = data.Item1;
                            int chunkNumber = data.Item2;
                            int totalChunks = data.Item3;

                            sessionPrivateKeyChunks[chunkNumber] = chunkData;
                            if (!privateKeyProcessed && sessionPrivateKeyChunks.Count == totalChunks)
                            {
                                var orderedData = string.Concat(sessionPrivateKeyChunks.OrderBy(c => c.Key).Select(c => c.Value));
                                sessionPrivateKey = EncryptionManager.StringToRSAKey(orderedData);
                                //messageDecryptCSP.ImportParameters(sessionPrivateKey);

                                privateKeyProcessed = true;
                                WriteMessage("Session Private Key Recieved", "SYSTEM");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        WriteMessage("DECRYPT ERROR: " + e.Message, message.Username);
                    }

                }

                if (newDirectMessages.Any())
                {
                    lastTime = newDirectMessages.Max(m => m.CreatedAt);
                }
            }

            WriteMessage("Finished Retrieving Keys", "SYSTEM");
        }

        async Task RecieveMessages(Guid roomGUID, Guid userId)
        {
            DateTime lastTime = DateTime.UtcNow;

            var messageDecryptCSP = new RSACryptoServiceProvider(2048);
            messageDecryptCSP.ImportParameters(sessionPrivateKey);

            while (inRoomScreen)
            {
                EnsureBufferSize();

                // recieve room messages
                List<MessageModel> newMessages = await ApiClient.GetMessages(roomGUID, lastTime.ToUniversalTime(), null);
                
                foreach (MessageModel message in newMessages)
                {
                    try
                    {
                        string decryptedMessage = EncryptionManager.DecryptMessage(message.Content, messageDecryptCSP);
                        WriteMessage(decryptedMessage, message.Username);
                    }
                    catch (Exception e)
                    {
                        WriteMessage("DECRYPT ERROR: " + e.Message, message.Username);
                    }

                }

                // Update lastTime to max timestamp received, or keep as is
                var allMessages = newMessages;
                if (allMessages.Any())
                {
                    lastTime = allMessages.Max(m => m.CreatedAt);
                }

            }
        }

        private (string, int, int) ParseChunk(string message)
        {
            var parts = message.Split(' ', 3);
            
            var chunkInfo = parts[1].Split('/');
            int chunkNumber = int.Parse(chunkInfo[0]);
            int totalChunks = int.Parse(chunkInfo[1]);

            string chunkData = parts[2];

            return (chunkData, chunkNumber, totalChunks);
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

                    var messageEncryptCSP = new RSACryptoServiceProvider(2048);
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
