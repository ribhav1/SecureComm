using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureComm.Screens
{
    class EnterPasswordScreen : IScreen
    {
        private Guid roomId = new Guid();

        public EnterPasswordScreen(Guid _roomId)
        {
            roomId = _roomId;
        }

        public async Task DrawScreen(ScreenManager screenManager)
        {
            Console.WriteLine($"Please enter the password for room {roomId} or type in 'back' to go back: ");
            //bool isValidPassword = await ApiClient.ValidateRoomPassword(roomId, Console.ReadLine());
            bool isValidPassword = false;

            while (!isValidPassword)
            {
                

                string input = Console.ReadLine();

                if (input.Trim().ToLower() == "back")
                {
                    screenManager.GoBack();
                    return;
                }

                Console.WriteLine("Wrong password. Try again:");
                isValidPassword = await ApiClient.ValidateRoomPassword(roomId, input);
            }
        
            screenManager.Navigate(new EnterUserIdScreen(roomId), saveCurrentInStack: false);
            return;
        }

    }
}
