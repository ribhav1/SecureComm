using SecureCommAPI.Models;
using System.Net.Http.Json;
using System.Net.NetworkInformation;

namespace SecureComm
{
    public class ApiClient
    {
        private static HttpClient client = new()
        {
            BaseAddress = new Uri("https://localhost:7117")
        };

        // Room route functions
        public static async Task<RoomModel> GetRoomById(Guid id)
        {
            try
            {
                return await client.GetFromJsonAsync<RoomModel>($"/Room/getRoom/{id}");
            }
            catch (Exception e)
            {
                return new RoomModel();
            }
        }

        public static async Task<bool> ValidateRoomById(Guid id)
        {
            try
            {
                return await client.GetFromJsonAsync<bool>($"/Room/validateRoom/{id}");
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public static async Task<bool> ValidateRoomPassword(Guid id, string password)
        {
            try
            {
                return await client.GetFromJsonAsync<bool>($"/Room/validatePassword/{id}/{password}");
            } 
            catch (Exception e)
            {
                return false;
            }
            
        }

        public static async Task<RoomModel> CreateRoom(Guid roomGUID, string password)
        {
            HttpResponseMessage response = await client.PostAsync($"Room/createRoom/{roomGUID}/{password}", null);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<RoomModel>();
            }
            else
            {
                return new RoomModel();
            }
        }

        public static async Task<RoomModel> AddConnectedUser(Guid roomGUID, Guid newConnectedUserId, string newConnectedUserPublicKey)
        {
            string encodedPublicKey = Uri.EscapeDataString(newConnectedUserPublicKey);
            HttpResponseMessage response = await client.PostAsync($"Room/addConnectedUser/{roomGUID}/{newConnectedUserId}/{encodedPublicKey}", null);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<RoomModel>();
            }
            else
            {
                return null;
            }
        }

        public static async Task<RoomModel> RemoveConnectedUser(Guid roomGUID, Guid newConnectedUserId)
        {
            HttpResponseMessage response = await client.PostAsync($"Room/removeConnectedUser/{roomGUID}/{newConnectedUserId}", null);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<RoomModel>();
            }
            else
            {
                return null;
            }
        }

        // Message route functions
        public static async Task<List<MessageModel>> GetMessages(Guid roomGUID, DateTime lastTime, Guid? directlyToUserId)
        {
            try
            {
                string encodedTime = Uri.EscapeDataString(lastTime.ToString("yyyy-MM-dd HH:mm:ss.ffffffK"));
                List<MessageModel> newMessages = await client.GetFromJsonAsync<List<MessageModel>>($"/Message/getMessages/{roomGUID}/{encodedTime}?directRecepientUserId={directlyToUserId}");
                return newMessages;
            }
            catch (Exception e)
            {
                return new List<MessageModel>();
            }
        }

        public static async Task<MessageModel> SendMessage(Guid roomGUID, Guid userId, string username, string content, Guid? directlyToUserId, string color)
        {

            string encodedContent = Uri.EscapeDataString(content);
            HttpResponseMessage response = await client.PostAsync($"/Message/send/{roomGUID}/{userId}/{username}/{encodedContent}/{color}?directlyToUserId={directlyToUserId}", null);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<MessageModel>();
            }
            else
            {
                return null;
            }
        }

    }
}
