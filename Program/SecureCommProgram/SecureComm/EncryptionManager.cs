using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SecureComm
{
    public static class EncryptionManager
    {
        public static string EncryptMessage(string message, RSACryptoServiceProvider messageEncryptCSP)
        {
            var messageBytes = System.Text.Encoding.Unicode.GetBytes(message);
            var messageEncryptedBytes = messageEncryptCSP.Encrypt(messageBytes, false);
            var messageEncryptedString = Convert.ToBase64String(messageEncryptedBytes);

            return messageEncryptedString;
        }

        public static string DecryptMessage(string message, RSACryptoServiceProvider messageDecryptCSP)
        {
            var messageBytes = Convert.FromBase64String(Uri.UnescapeDataString(message));
            var messageDecryptedBytes = messageDecryptCSP.Decrypt(messageBytes, false);
            var messageDecryptedString = Encoding.Unicode.GetString(messageDecryptedBytes);

            return messageDecryptedString;
        }

        public static string RSAKeyToString(RSAParameters publicKey)
        {
            using (var sw = new StringWriter())
            {
                var serializer = new XmlSerializer(typeof(RSAParameters));
                serializer.Serialize(sw, publicKey);
                return sw.ToString();
            }
        }

        public static RSAParameters StringToRSAKey(string xml)
        {
            using (var sr = new StringReader(xml))
            {
                var serializer = new XmlSerializer(typeof(RSAParameters));
                return (RSAParameters)serializer.Deserialize(sr);
            }
        }
    }
}
