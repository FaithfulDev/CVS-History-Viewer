/*
    JSON Parser
    Version: 1.0.0.0 (modified)

    MIT License

    Copyright (c) 2019 Marcel Rütjerodt

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/

using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace JSONParser
{
    /// <summary>
    /// Test
    /// </summary>
    public class JSONParser
    {

        private string sJSONRaw;
        private Dictionary<string, object> cJSONData = new Dictionary<string, object>();

        /// <summary>
        /// Load JSON data from a file. You don't have to use this method if you want to work with a empty JSON data set
        /// (e.g. you only want to add things).
        /// </summary>
        /// <param name="sFile">JSON data File with full path.</param>
        public void Load(string sFile)
        {
            if (File.Exists(sFile))
            {
                StreamReader oStreamReader = new StreamReader(new FileStream(sFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite),
                                                          System.Text.Encoding.UTF8);
                this.sJSONRaw = oStreamReader.ReadToEnd();
                oStreamReader.Close();

                cJSONData = Parse(this.sJSONRaw);
            }
            else
            {
                throw new System.ArgumentException("File does not exist.", "sFile");
            }
        }

        /// <summary>
        /// Save JSON data to a file. Set JSON data by using <c>Load</c> or <c>SetValue</c> method.
        /// </summary>
        /// <param name="sFile">File with full path. If the file or path does not yet exist it will be created.</param>
        public void Save(string sFile)
        {
            string sJSON = BuildJSON(cJSONData);

            Directory.CreateDirectory(Path.GetDirectoryName(sFile));
            File.Create(sFile).Close();
            StreamWriter oStreamWriter = new StreamWriter(new FileStream(sFile, FileMode.Truncate, FileAccess.Write, FileShare.None),
                                                         System.Text.Encoding.UTF8);
            oStreamWriter.Write(sJSON);
            oStreamWriter.Close();
        }

        /// <summary>
        /// Internal method that is used to create a JSON formated string based on the dictionary provided.
        /// </summary>
        /// <param name="cData"></param>
        /// <param name="iLevel"></param>
        /// <returns>JSON formated string</returns>
        private string BuildJSON(Dictionary<string, object> cData, int iLevel = 1)
        {

            string sJSON = "{";

            foreach (KeyValuePair<string, object> oItem in cData)
            {
                sJSON += $"\r\n{string.Empty.PadLeft(iLevel, '\t')}\"{oItem.Key}\":";

                if (oItem.Value is string)
                {
                    sJSON += $"\"{oItem.Value}\"";
                }
                else if (oItem.Value is int)
                {
                    sJSON += oItem.Value;
                }
                else if (oItem.Value is bool)
                {
                    string sBoolValue = "";
                    sBoolValue += oItem.Value;
                    sJSON += sBoolValue.ToLower();
                }
                else
                {
                    sJSON += BuildJSON((Dictionary<string, object>)oItem.Value, iLevel + 1);
                }

                sJSON += ",";
            }

            sJSON = sJSON.TrimEnd(',');
            sJSON += "\r\n" + string.Empty.PadLeft(iLevel - 1, '\t') + "}";

            return sJSON;
        }

        /// <summary>
        /// Internal method used for parsing the JSON data read from the file provided via <c>Load</c> method.
        /// </summary>
        /// <param name="sJSON">JSON formated string.</param>
        /// <returns>Dictionary with JSON data</returns>
        private Dictionary<string, object> Parse(string sJSON)
        {
            Dictionary<string, object> cData = new Dictionary<string, object>();

            foreach (Match sKeyValueMatch in
                        new Regex("\"[\\w]+\":[ ]?(\"[^\"]*\"|[0-9]+|{[^}]*}|true|false)", RegexOptions.Singleline).Matches(sJSON))
            {
                string sKeyValue = sKeyValueMatch.ToString();
                string sItemKey;
                object oItemValue = null;

                sItemKey = new Regex("\"[\\w]+\":[ ]?").Match(sKeyValue).Value.Replace("\"", "").Replace(":", "").Replace(" ", "");
                sKeyValue = new Regex("\"[\\w]+\":[ ]?").Replace(sKeyValue, "", 1);

                //At this point sKeyValue only has the value remaining ("book", 10, {...}).
                if (new Regex("{[^}]*", RegexOptions.Singleline).IsMatch(sKeyValue))
                {
                    oItemValue = Parse(sKeyValue.Trim(new char[] { '\r', '\n', '\t' }));
                }
                else if (new Regex("\"[^\"]*\"", RegexOptions.Singleline).IsMatch(sKeyValue))
                {
                    oItemValue = sKeyValue.Replace("\"", "");
                }
                else if (new Regex("[0-9]+").IsMatch(sKeyValue))
                {
                    oItemValue = int.Parse(sKeyValue);
                }
                else if (new Regex("true|false", RegexOptions.IgnoreCase).IsMatch(sKeyValue))
                {
                    oItemValue = bool.Parse(sKeyValue);
                }
                else
                {
                    throw new System.ArgumentException(string.Format("\"{0}\" is not a valid JSON value.", sKeyValue), "sJSON");
                }

                cData.Add(sItemKey, oItemValue);
            }

            return cData;
        }

        /// <summary>
        /// Get JSON data via key. Returns <c>null</c> if key is not found.
        /// </summary>
        /// <param name="sKey">JSON key.</param>
        /// <param name="oDefault">Default value in case the key can not be found.</param>
        /// <returns>Value of specified key. Returns default if key does not exist.</returns>
        public object GetValue(string sKey, object oDefault)
        {
            return GetValue(new string[] { sKey }, oDefault);
        }

        /// <summary>
        /// Get JSON data via series of keys. This allows you to access nested data. 
        /// Returns <c>null</c> if key is not found.
        /// </summary>
        /// <param name="aKeys">Series of keys provided as string array.</param>
        /// <param name="oDefault">Default value in case the key can not be found.</param>
        /// <returns>Value of specified key. Returns default if key does not exist.</returns>
        public object GetValue(string[] aKeys, object oDefault)
        {

            Dictionary<string, object> cData = cJSONData;

            for (int i = 0; i < aKeys.Length; i++)
            {

                if (!cData.ContainsKey(aKeys[i]))
                {
                    return oDefault;
                }

                //Check if we are at the end of the key chain
                if (i == aKeys.Length - 1)
                {
                    return cData[aKeys[i]];
                }

                if (cData[aKeys[i]] is Dictionary<string, object>)
                {
                    cData = (Dictionary<string, object>)cData[aKeys[i]];
                }

            }

            return oDefault;
        }

        /// <summary>
        /// Set JSON data. This will create a new key, if it does not already exist. 
        /// Use <c>Save</c> to save internal JSON data to file.
        /// </summary>
        /// <param name="sKey">JSON key</param>
        /// <param name="oValue">JSON value</param>
        /// <returns>True if key was added/modified successfully. False if something went wrong.</returns>
        public bool SetValue(string sKey, object oValue)
        {
            return SetValue(new string[] { sKey }, oValue);
        }

        /// <summary>
        /// Set JSON data based on series of keys. This will create a new key, if it does not already exist. 
        /// Use <c>Save</c> to save internal JSON data to file.
        /// </summary>
        /// <param name="sKey">JSON key</param>
        /// <param name="oValue">JSON value</param>
        /// <returns>True if key was added/modified successfully. False if something went wrong.</returns>
        public bool SetValue(string[] aKeys, object oValue)
        {

            Dictionary<string, object> cData = cJSONData;

            for (int i = 0; i < aKeys.Length; i++)
            {

                //Check if we are at the end of the key chain
                if (i == aKeys.Length - 1)
                {
                    if (!cData.ContainsKey(aKeys[i]))
                    {
                        cData.Add(aKeys[i], "");
                    }

                    cData[aKeys[i]] = oValue;
                    return true;
                }

                if (!cData.ContainsKey(aKeys[i]))
                {
                    cData.Add(aKeys[i], new Dictionary<string, object>());
                }

                if (cData[aKeys[i]] is Dictionary<string, object>)
                {
                    cData = (Dictionary<string, object>)cData[aKeys[i]];
                }

            }

            return false;
        }

    }

}
