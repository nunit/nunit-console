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
    public class PackageSettings : IEnumerable<PackageSetting>
    {
        private readonly Dictionary<string, PackageSetting> _settings = new();

        /// <summary>
        /// Gets the number of settings in the list
        /// </summary>
        public int Count => _settings.Count;

        /// <summary>
        /// Returns true if a setting with the specified name is present
        /// </summary>
        public bool HasSetting(string name) => _settings.ContainsKey(name);

        /// <summary>
        /// Returns true if a setting with the specified definition is present
        /// </summary>
        public bool HasSetting(SettingDefinition setting) => _settings.ContainsKey(setting.Name);

        /// <summary>
        /// Return the value of a setting if present, otherwise null.
        /// </summary>
        /// <param name="name">The name of the setting</param>
        public object GetSetting(string name)
        {
            return _settings[name].Value;
        }

        /// <summary>
        /// Return the value of a setting or its defined default value.
        /// </summary>
        /// <param name="definition">The name and type of the setting</param>
        public T GetValueOrDefault<T>(SettingDefinition<T> definition)
            where T : notnull
        {
            if (_settings.TryGetValue(definition.Name, out PackageSetting? unTypedSetting))
                if (unTypedSetting is PackageSetting<T> typedSetting && typedSetting is not null)
                    return typedSetting.Value;

            return definition.DefaultValue;
        }

        /// <inheritdoc />
        public IEnumerator<PackageSetting> GetEnumerator() => _settings.Values.GetEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => _settings.Values.GetEnumerator();

        /// <summary>
        /// Adds a setting to the list directly.
        /// </summary>
        /// <param name="setting">A PackageSetting instance</param>
        public void Add(PackageSetting setting) => _settings.Add(setting.Name, setting);

        /// <summary>
        /// Creates and adds a custom string setting to the list, specifying the name and value.
        /// </summary>
        /// <param name="name">The name of the setting.</param>
        /// <param name="value">The corresponding value to set.</param>
        public void Add(string name, string value)
        {
            Add(new PackageSetting<string>(name, value));
        }

        /// <summary>
        /// Creates and adds a custom boolean setting to the list, specifying the name and value.
        /// </summary>
        /// <param name="name">The name of the setting.</param>
        /// <param name="value">The corresponding value to set.</param>
        public void Add(string name, bool value)
        {
            Add(new PackageSetting<bool>(name, value));
        }

        /// <summary>
        /// Creates and adds a custom int setting to the list, specifying the name and value.
        /// </summary>
        /// <param name="name">The name of the setting.</param>
        /// <param name="value">The corresponding value to set.</param>
        public void Add(string name, int value)
        {
            Add(new PackageSetting<int>(name, value));
        }

        /// <summary>
        /// Adds or replaces a setting to the list.
        /// </summary>
        public void Set(PackageSetting setting) => _settings[setting.Name] = setting;

        /// <summary>
        /// Adds or replaces a setting to the list.
        /// </summary>
        /// <param name="name">The name of the setting.</param>
        /// <param name="value">The corresponding value to set.</param>
        public void Set<T>(string name, T value)
            where T : notnull
        {
            Set(new PackageSetting<T>(name, value));
        }
    }
}
