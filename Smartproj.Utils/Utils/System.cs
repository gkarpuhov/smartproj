using Microsoft.Win32;
using System;
using System.Text;

namespace Smartproj.Utils
{
    public static class Sys
    {
        public static bool IsCloneableType(Type type, out bool _isValue)
        {
            _isValue = false;

            if (typeof(ICloneable).IsAssignableFrom(type))
            {
                return true;
            }
            else
                if (type.IsValueType)
            {
                _isValue = true;
                return true;
            }
            else
                return false;
        }
        public static string GetMacAddress()
        {
            var text = string.Empty;
            using (var managementClass = new System.Management.ManagementClass("Win32_NetworkAdapterConfiguration"))
            {
                foreach (var managementObject in managementClass.GetInstances())
                {
                    if (managementObject["MacAddress"] == null)
                    {
                        continue;
                    }
                    if (managementObject["IpEnabled"] == null || !(bool)managementObject["IpEnabled"])
                    {
                        continue;
                    }
                    var text2 = managementObject["MacAddress"].ToString().Trim().ToUpper().Replace(":", "");
                    if (string.IsNullOrEmpty(text))
                    {
                        text = text2;
                    }
                    if (text.Replace("00", "").Length <= text2.Replace("00", "").Length)
                    {
                        text = text2;
                    }
                }
            }
            return text;
        }
    }
    public static class GdPictureDeveloperKey
    {
        static RegistryKey GetGdPictureRegistryKey()
        {
            var registryKey = Registry.CurrentUser.OpenSubKey(@"Software\Orpalis\GdPicture.NET14", true) ??
                              Registry.CurrentUser.OpenSubKey(@"Software\Wow6432Node\Orpalis\GdPicture.NET14", true) ??
                              Registry.LocalMachine.OpenSubKey(@"Software\Orpalis\GdPicture.NET14", true) ??
                              Registry.LocalMachine.OpenSubKey(@"Software\Wow6432Node\Orpalis\GdPicture.NET14", true);
            return registryKey;
        }

        public static void SetGdPictureDeveloperKey(string licenseKey)
        {
            var registryKey = GetGdPictureRegistryKey();

            if (registryKey == null)
            {
                registryKey = Registry.CurrentUser.CreateSubKey(@"Software\Orpalis\GdPicture.NET14", true);
            }

            string thismac = Sys.GetMacAddress();
            var regsmac = registryKey.GetValue("Mac");

            if (regsmac == null || (string)regsmac != thismac)
            {
                string computerSpecificKey = "GdPicture.NET14" + thismac;
                byte[] base64 = Encoder.XorCrypt(computerSpecificKey, Encoding.ASCII.GetBytes(licenseKey));
                registryKey.SetValue("CoreKey", Convert.ToBase64String(base64));
                registryKey.SetValue("Edition", "GdPicture.Net Document Imaging SDK Ultimate V14");
                registryKey.SetValue("Mac", thismac);
                Console.WriteLine($"Активированна лицензия GdPicture.NET14 на mac адрес '{thismac}'");
            }
        }
    }

}
