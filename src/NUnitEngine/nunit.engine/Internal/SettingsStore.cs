// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Xml;

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
            }
            catch (Exception)
            {
                // So we won't try this again
                _writeable = false;
                throw;
            }
        }
    }
}
