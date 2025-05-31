// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Collections;
using System.Collections.Generic;

namespace NUnit.Engine
{
    /// <summary>
    /// PackageSettingList contains all the PackageSettings for a TestPackage.
    /// It supports enumeration of the settings and various operations on
    /// individual settings in the list.
    /// </summary>
    public class PackageSettingsList : IEnumerable<PackageSetting>
    {
        private Dictionary<string, PackageSetting> _settings = new Dictionary<string, PackageSetting>();

        /// <summary>
        /// Gets the number of settings in the list
        /// </summary>
        public int Count => _settings.Count;

        /// <summary></summary>
        public PackageSetting this[string key]
        {
            get { return _settings[key]; }
            set { _settings[key] = value; }
        }

        /// <summary>
        /// Returns true if a setting with the specified name is present
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool HasSetting(string name) => _settings.ContainsKey(name);

        /// <summary>
        ///
        /// </summary>
        /// <param name="name"></param>
        /// <param name="setting"></param>
        /// <returns></returns>
        public bool TryGetSetting(string name, out PackageSetting? setting)
        {
            return _settings.TryGetValue(name, out setting);
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public IEnumerator<PackageSetting> GetEnumerator() => _settings.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _settings.Values.GetEnumerator();

        /// <summary>
        /// Add a setting to the list.
        /// </summary>
        /// <param name="setting"></param>
        public void Add(PackageSetting setting) => _settings.Add(setting.Name, setting);
    }
}
