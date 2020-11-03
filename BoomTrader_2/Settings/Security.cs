using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;

namespace BoomTrader_2.Settings
{
    public static class Security
    {
        public static string Encode(string ishText, string pass,
               string sol = "boompool", string cryptographicAlgorithm = "SHA1",
               int passIter = 2, string initVec = "a8doSuDitOz1hZe#",
               int keySize = 256)
        {
            if (string.IsNullOrEmpty(ishText))
                return "";
            byte[] initVecB = Encoding.ASCII.GetBytes(initVec);
            byte[] solB = Encoding.ASCII.GetBytes(sol);
            byte[] ishTextB = Encoding.UTF8.GetBytes(ishText);
            PasswordDeriveBytes derivPass = new PasswordDeriveBytes(pass, solB, cryptographicAlgorithm, passIter);
            byte[] keyBytes = derivPass.GetBytes(keySize / 8);
            RijndaelManaged symmK = new RijndaelManaged();
            symmK.Mode = CipherMode.CBC;
            byte[] cipherTextBytes = null;
            using (ICryptoTransform encryptor = symmK.CreateEncryptor(keyBytes, initVecB))
            {
                using (MemoryStream memStream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memStream, encryptor, CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(ishTextB, 0, ishTextB.Length);
                        cryptoStream.FlushFinalBlock();
                        cipherTextBytes = memStream.ToArray();
                        memStream.Close();
                        cryptoStream.Close();
                    }
                }
            }
            symmK.Clear();
            return Convert.ToBase64String(cipherTextBytes);
        }
        //метод дешифрования строки
        public static string Decode(string ciphText, string pass,
               string sol = "boompool", string cryptographicAlgorithm = "SHA1",
               int passIter = 2, string initVec = "a8doSuDitOz1hZe#",
               int keySize = 256)
        {
            if (string.IsNullOrEmpty(ciphText))
                return "";
            byte[] initVecB = Encoding.ASCII.GetBytes(initVec);
            byte[] solB = Encoding.ASCII.GetBytes(sol);
            byte[] cipherTextBytes = Convert.FromBase64String(ciphText);
            PasswordDeriveBytes derivPass = new PasswordDeriveBytes(pass, solB, cryptographicAlgorithm, passIter);
            byte[] keyBytes = derivPass.GetBytes(keySize / 8);
            RijndaelManaged symmK = new RijndaelManaged();
            symmK.Mode = CipherMode.CBC;
            byte[] plainTextBytes = new byte[cipherTextBytes.Length];
            int byteCount = 0;
            using (ICryptoTransform decryptor = symmK.CreateDecryptor(keyBytes, initVecB))
            {
                using (MemoryStream mSt = new MemoryStream(cipherTextBytes))
                {
                    using (CryptoStream cryptoStream = new CryptoStream(mSt, decryptor, CryptoStreamMode.Read))
                    {
                        byteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                        mSt.Close();
                        cryptoStream.Close();
                    }
                }
            }
            symmK.Clear();
            return Encoding.UTF8.GetString(plainTextBytes, 0, byteCount);
        }

        #region Authentification Licence
        private static bool serverAuth(string server, string ip)
        {
            var serverIP = "";
            IPAddress[] ipaddress = Dns.GetHostAddresses(server.Replace("https://", "").Replace("/auth", ""));
            foreach (IPAddress ip4 in ipaddress.Where(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork))
            {
                serverIP = ip4.ToString();
            }
            if (serverIP == ip)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static Tuple<bool, string, string> checkLicence(string key, string wallet)
        {
            var ip = "194.67.92.118";
            var licenceServer = "https://license.boomtrader.info/auth";
            if (serverAuth(licenceServer, ip))
            {
                int unixTime = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;

                JObject Answer;
                try
                {
                    Answer = JObject.Parse(Request.GET(licenceServer, "key=" + key + "&wallet=" + wallet + "&version=" + Resources.Version + "&hwid=" + GetMAC()));
                }
                catch
                {
                    Answer = JObject.Parse("{'type':'error', 'message':'Error request license or error on license server'}");
                }

                // +"&signature="+ signature
                var type = Answer["type"].ToString();
                if (type != "error")
                {
                    //TraderBot.Instance.Licence = key;

                    return new Tuple<bool, string, string>(true, Answer["message"].ToString(), type);


                }
                else { return new Tuple<bool, string, string>(false, Answer["message"].ToString(), type); }

            }
            else
            {
                return new Tuple<bool, string, string>(false, "Fail Auth license server", "error");
            }
        }
        #endregion
        public static string GetMAC()
        {
            var macAddr =
            (
            from nic in NetworkInterface.GetAllNetworkInterfaces()
            where nic.OperationalStatus == OperationalStatus.Up
            select nic.GetPhysicalAddress().ToString()
            ).FirstOrDefault();

            return macAddr;
        }


    }
}
