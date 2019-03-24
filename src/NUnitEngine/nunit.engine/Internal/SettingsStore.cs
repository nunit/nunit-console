// ***********************************************************************
// Copyright (c) 2013 Charlie Poole, Rob Prouse
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Xml;
#if NETSTANDARD1_6
using System.Xml.Linq;
#endif

namespace NUnit.Engine.Internal
{
    /// <summary>
    /// SettingsStore extends SettingsGroup to provide for
    /// loading and saving the settings from an XML file.
    /// </summary>
    public class SettingsStore : SettingsGroup
    {
        private string _settingsFile;
        private bool _writeable;

        #region Constructors

        /// <summary>
        /// Construct a SettingsStore without a backing file - used for testing.
        /// </summary>
        public SettingsStore() { }

        /// <summary>
        /// Construct a SettingsStore with a file name and indicate whether it is writeable
        /// </summary>
        /// <param name="settingsFile"></param>
        /// <param name="writeable"></param>
        public SettingsStore(string settingsFile, bool writeable)
        {
            _settingsFile = Path.GetFullPath(settingsFile);
            _writeable = writeable;
        }

        #endregion

        #region Public Methods

        public void LoadSettings()
        {
            FileInfo info = new FileInfo(_settingsFile);
            if (!info.Exists || info.Length == 0)
                return;

            try
            {
                XmlDocument doc = new XmlDocument();
                using (var stream = new FileStream(_settingsFile, FileMode.Open, FileAccess.Read))
                {
                    doc.Load(stream);
                }

                foreach (XmlElement element in doc.DocumentElement["Settings"].ChildNodes)
                {
                    if (element.Name != "Setting")
                        throw new Exception("Unknown element in settings file: " + element.Name);

                    if (!element.HasAttribute("name"))
                        throw new Exception("Setting must have 'name' attribute");

                    if (!element.HasAttribute("value"))
                        throw new Exception("Setting must have 'value' attribute");

                    SaveSetting(element.GetAttribute("name"), element.GetAttribute("value"));
                }
            }
            catch (Exception ex)
            {
                string msg = string.Format("Error loading settings {0}. {1}", _settingsFile, ex.Message);
                throw new NUnitEngineException(msg, ex);
            }
        }

        public void SaveSettings()
        {
            if (!_writeable || _settings.Keys.Count <= 0)
                return;

            try
            {
                string dirPath = Path.GetDirectoryName(_settingsFile);
                if (!Directory.Exists(dirPath))
                    Directory.CreateDirectory(dirPath);

#if NETSTANDARD1_6
                var settings = new XElement("Settings");

                List<string> keys = new List<string>(_settings.Keys);
                keys.Sort();

                foreach (string name in keys)
                {
                    object val = GetSetting(name);
                    if (val != null)
                    {
                        settings.Add(new XElement("Setting",
                                                    new XAttribute("name", name),
                                                    new XAttribute("value", TypeDescriptor.GetConverter(val.GetType()).ConvertToInvariantString(val))
                                                    ));
                    }
                }
                var doc = new XDocument(new XElement("NUnitSettings", settings));
                using (var file = new FileStream(_settingsFile, FileMode.Create, FileAccess.Write))
                {
                    doc.Save(file);
                }
#else
                var stream = new MemoryStream();
                using (var writer = new XmlTextWriter(stream, Encoding.UTF8))
                {
                    writer.Formatting = Formatting.Indented;

                    writer.WriteProcessingInstruction("xml", "version=\"1.0\"");
                    writer.WriteStartElement("NUnitSettings");
                    writer.WriteStartElement("Settings");

                    List<string> keys = new List<string>(_settings.Keys);
                    keys.Sort();

                    foreach (string name in keys)
                    {
                        object val = GetSetting(name);
                        if (val != null)
                        {
                            writer.WriteStartElement("Setting");
                            writer.WriteAttributeString("name", name);
                            writer.WriteAttributeString("value",
                                TypeDescriptor.GetConverter(val).ConvertToInvariantString(val));
                            writer.WriteEndElement();
                        }
                    }

                    writer.WriteEndElement();
                    writer.WriteEndElement();
                    writer.Flush();

                    var reader = new StreamReader(stream, Encoding.UTF8, true);
                    stream.Seek(0, SeekOrigin.Begin);
                    var contents = reader.ReadToEnd();
                    File.WriteAllText(_settingsFile, contents, Encoding.UTF8);
                }
#endif
            }
            catch (Exception)
            {
                // So we won't try this again
                _writeable = false;
                throw;
            }
        }

        #endregion
    }
}
