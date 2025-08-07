using SecureCommAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureComm.Screens
{
    class EnterPasswordScreen : IScreen
    {
        private Guid roomId;
        private bool isHost;

        public EnterPasswordScreen(Guid _roomId, bool _isHost)
        {
            roomId = _roomId;
            isHost = _isHost;
        }

        public async Task DrawScreen(ScreenManager screenManager)
        {
            if (isHost)
            {
                Console.WriteLine($"Please create a password for room {roomId}:");
                RoomModel newRoom = await ApiClient.CreateRoom(roomId, Console.ReadLine());
                Console.WriteLine($"{roomId}");

                screenManager.Navigate(new EnterUserIdScreen(roomId, isHost), saveCurrentInStack: false);
                return;
            }

            Console.WriteLine($"Please enter the password for room {roomId} or type in 'back' to go back: ");
            bool isValidPassword = await ApiClient.ValidateRoomPassword(roomId, Console.ReadLine());

            while (!isValidPassword)
            {
                Console.WriteLine("Wrong password. Try again:");
                string input = Console.ReadLine();

                if (input.Trim().ToLower() == "back")
                {
                    screenManager.GoBack();
                    return;
                }

                //Console.WriteLine("Wrong password. Try again:");
                isValidPassword = await ApiClient.ValidateRoomPassword(roomId, input);
            }
        
            screenManager.Navigate(new EnterUserIdScreen(roomId, false), saveCurrentInStack: false);
            return;
        }

    }
}
