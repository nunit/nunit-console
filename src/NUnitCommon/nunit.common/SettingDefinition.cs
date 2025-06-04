// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Reflection;

namespace NUnit.Engine
{
    /// <summary>
    ///
    /// </summary>
    public abstract class SettingDefinition
    {
        public SettingDefinition(string name, Type valueType)
        {
            Name = name;
            ValueType = valueType;
        }

        /// <summary>
        /// Gets the name of this setting.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Overridden in each derived definition to return the Type
        /// required for the value of the setting.
        /// </summary>
        public Type ValueType { get; }

        /// <summary>
        /// Create a PackageSetting based on this definition.
        /// </summary>
        /// <param name="value">The value to assign the setting.</param>
        /// <returns>A PackageSetting.</returns>
        public PackageSetting WithValue<T>(T value)
            where T : notnull
        {
            return new PackageSetting<T>(Name, value);
        }
    }

    /// <summary>
    /// SettingDefinition class is used as a sort of template for creating settings.
    /// It specifies the name of the setting and the Type required for its value but not
    /// the value itself. Its WithValue method is used to generate an actual PackageSetting.
    /// </summary>
    public sealed class SettingDefinition<T> : SettingDefinition
        where T : notnull
    {
        /// <summary>
        /// Construct a SettingDefinition
        /// </summary>
        /// <param name="name">Name of this setting</param>
        public SettingDefinition(string name)
            : base(name, typeof(T))
        {
        }

        //public override Type ValueType => typeof(T);

        ///// <summary>
        ///// Create a PackageSetting based on this definition.
        ///// </summary>
        ///// <param name="value">The value to assign the setting.</param>
        ///// <returns>A PackageSetting.</returns>
        //public PackageSetting WithValue(T value)
        //{
        //    return new PackageSetting<T>(Name, value);
        //}
    }
}
