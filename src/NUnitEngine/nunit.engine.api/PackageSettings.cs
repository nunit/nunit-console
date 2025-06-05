// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

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
        /// Return the value of a setting or a default.
        /// </summary>
        /// <param name="name">The name of the setting</param>
        public object GetSetting(string name)
        {
            return _settings[name].Value;
        }

        /// <summary>
        /// Tries to get a setting by definition.
        /// </summary>
        private bool TryGetSetting<T>(SettingDefinition<T> definition, [NotNullWhen(true)] out PackageSetting<T>? setting)
            where T : notnull
        {
            if (_settings.TryGetValue(definition.Name, out PackageSetting? unTypedSetting))
            {
                if (unTypedSetting is PackageSetting<T> typedSetting)
                {
                    setting = typedSetting;
                    return true;
                }
                else
                {
                    setting = null;
                    return false; // Type mismatch. Should we throw an exception instead?
                }
            }

            setting = null;
            return false; // Setting not found
        }

        /// <summary>
        /// Return the value of a setting or a default.
        /// </summary>
        /// <param name="definition">The name and type of the setting</param>
        /// <param name="value">The value stored for the setting</param>
        public bool TryGetSetting<T>(SettingDefinition<T> definition, [NotNullWhen(true)] out T? value)
            where T : notnull
        {
            if (TryGetSetting(definition, out PackageSetting<T>? setting))
            {
                value = setting.Value;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Return the value of a setting or a default.
        /// </summary>
        /// <param name="definition">The name and type of the setting</param>
        /// <param name="defaultSetting">The default value</param>
        public T GetSetting<T>(SettingDefinition<T> definition, T defaultSetting)
            where T : notnull
        {
            return TryGetSetting(definition, out PackageSetting<T>? setting)
                ? setting.Value
                : defaultSetting;
        }

        /// <inheritdoc />
        public IEnumerator<PackageSetting> GetEnumerator() => _settings.Values.GetEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => _settings.Values.GetEnumerator();

        /// <summary>
        /// Adds a setting to the list.
        /// </summary>
        public void Add(PackageSetting setting) => _settings.Add(setting.Name, setting);

        /// <summary>
        /// Adds a setting to the list.
        /// </summary>
        /// <param name="name">The name of the setting.</param>
        /// <param name="value">The corresponding value to set.</param>
        public void Add<T>(string name, T value)
            where T : notnull
        {
            Add(new PackageSetting<T>(name, value));
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
