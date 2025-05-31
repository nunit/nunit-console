// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

namespace NUnit.Engine
{
    /// <summary>
    /// SettingDefinition class is used as a sort of template for creating settings.
    /// It specifies the name of the setting and the Type required for its value but not
    /// the value itself. Its WithValue method is used to generate an actual PackageSetting.
    /// </summary>
    public sealed class SettingDefinition<T>
        where T : notnull
    {
        /// <summary>
        /// Construct a SettingDefinition
        /// </summary>
        /// <param name="name">Name of this setting</param>
        public SettingDefinition(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Gets the name of this setting.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Create a PackageSetting based on this definition.
        /// </summary>
        /// <param name="value">The value to assign the setting.</param>
        /// <returns>A PackageSetting.</returns>
        public PackageSetting<T> WithValue(T value)
        {
            return new PackageSetting<T>(Name, value);
        }
    }
}
