using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace TRM.Web.Helpers.Payment
{
    public static class Security
    {

        public static String Sign(IDictionary<string, string> paramsArray, string secretKey)
        {
            return Sign(BuildDataToSign(paramsArray), secretKey);
        }

        private static String Sign(String data, String secretKey)
        {
            UTF8Encoding encoding = new System.Text.UTF8Encoding();
            byte[] keyByte = encoding.GetBytes(secretKey);

            HMACSHA256 hmacsha256 = new HMACSHA256(keyByte);
            byte[] messageBytes = encoding.GetBytes(data);
            return Convert.ToBase64String(hmacsha256.ComputeHash(messageBytes));
        }

        private static String BuildDataToSign(IDictionary<string, string> paramsArray)
        {
            String[] signedFieldNames = paramsArray["signed_field_names"].Split(',');
            IList<string> dataToSign = new List<string>();

            foreach (String signedFieldName in signedFieldNames)
            {
                dataToSign.Add(signedFieldName + "=" + paramsArray[signedFieldName]);
            }

            return CommaSeparate(dataToSign);
        }

        private static String CommaSeparate(IList<string> dataToSign)
        {
            return String.Join(",", dataToSign);
        }

        public static String GetUUID()
        {
            return System.Guid.NewGuid().ToString();
        }

        public static String GetUTCDateTime()
        {
            DateTime time = DateTime.Now.ToUniversalTime();
            return time.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
        }

        public static String GetDateTimeAsReference()
        {
            return DateTime.Now.ToFileTime().ToString();
        }
    }
}