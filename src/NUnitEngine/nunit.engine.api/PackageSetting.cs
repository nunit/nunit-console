// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

namespace NUnit.Engine
{
    /// <summary>
    /// Base Class for all package Settings
    /// </summary>
    public abstract class PackageSetting
    {
        /// <summary>
        /// Construct a PackageSetting
        /// </summary>
        /// <param name="name">The name of this setting.</param>
        /// <param name="value">The value of the setting</param>
        protected PackageSetting(string name, object value)
        {
            Name = name;
            Value = value;
        }

        /// <summary>
        /// Gets the name of this setting.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the value of this setting.
        /// </summary>
        public object Value { get; }
    }

    /// <summary>
    /// The PackageSetting class represents one setting value contained in a
    /// TestPackage. Instances of PackageSetting are immutable.
    /// </summary>
    // TODO: Move this class out of the api assembly when possible. This will
    // require modifying or moving the TestPackage serialization code.
    public sealed class PackageSetting<T> : PackageSetting
        where T : notnull
    {
        /// <summary>
        /// Construct a PackageSetting with a string value
        /// </summary>
        /// <param name="name">The setting name.</param>
        /// <param name="value">The value of this setting instance.</param>
        public PackageSetting(string name, T value) : base(name, value)
        {
            Value = value;
        }

        /// <summary>
        /// Get the setting value as correct Type.
        /// </summary>
        public new T Value { get; }

        // TODO: Remove this experiment when no longer needed
        //public T GetSettingOrDefault<T>(T defaultValue)
        //    where T : notnull
        //{
        //    if (Value is T)
        //        return (T)Value;

        //    string? s = Value as string;
        //    if (s is not null)
        //        if (typeof(T) == typeof(int))
        //            return int.Parse(s);
        //        else
        //        if (typeof(T) == typeof(bool))
        //            return bool.Parse(s);
        //}
    }
}
