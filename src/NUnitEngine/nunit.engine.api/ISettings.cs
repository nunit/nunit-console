// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;

namespace NUnit.Engine
{
    /// <summary>
    /// Event handler for settings changes
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="args">The <see cref="SettingsEventArgs"/> instance containing the event data.</param>
    public delegate void SettingsEventHandler(object sender, SettingsEventArgs args);

    /// <summary>
    /// Event argument for settings changes
    /// </summary>
    public class SettingsEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsEventArgs"/> class.
        /// </summary>
        /// <param name="settingName">Name of the setting that has changed.</param>
        public SettingsEventArgs(string settingName)
        {
            SettingName = settingName;
        }

        /// <summary>
        /// Gets the name of the setting that has changed
        /// </summary>
        public string SettingName { get; private set; }
    }

    /// <summary>
    /// The ISettings interface is used to access all user
    /// settings and options.
    /// </summary>
    public interface ISettings
    {
        /// <summary>
        /// Occurs when the settings are changed.
        /// </summary>
        event SettingsEventHandler Changed;

        /// <summary>
        /// Load a setting from the storage.
        /// </summary>
        /// <param name="settingName">Name of the setting to load</param>
        /// <returns>Value of the setting or null</returns>
        object GetSetting(string settingName);

        /// <summary>
        /// Load a setting from the storage or return a default value
        /// </summary>
        /// <param name="settingName">Name of the setting to load</param>
        /// <param name="defaultValue">Value to return if the setting is missing</param>
        /// <returns>Value of the setting or the default value</returns>
        T GetSetting<T>(string settingName, T defaultValue);

        /// <summary>
        /// Remove a setting from the storage
        /// </summary>
        /// <param name="settingName">Name of the setting to remove</param>
        void RemoveSetting(string settingName);

        /// <summary>
        /// Remove an entire group of settings from the storage
        /// </summary>
        /// <param name="groupName">Name of the group to remove</param>
        void RemoveGroup(string groupName);

        /// <summary>
        /// Save a setting in the storage
        /// </summary>
        /// <param name="settingName">Name of the setting to save</param>
        /// <param name="settingValue">Value to be saved</param>
        void SaveSetting(string settingName, object settingValue);
    }
}
