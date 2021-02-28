// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;

namespace NUnit.Engine.Extensibility
{
    /// <summary>
    /// ExtensionPointAttribute is used at the assembly level to identify and
    /// document any ExtensionPoints supported by the assembly.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple=true, Inherited=false)]
    public class ExtensionPointAttribute : Attribute
    {
        /// <summary>
        /// Construct an ExtensionPointAttribute
        /// </summary>
        /// <param name="path">A unique string identifying the extension point.</param>
        /// <param name="type">The required Type of any extension that is installed at this extension point.</param>
        public ExtensionPointAttribute(string path, Type type)
        {
            Path = path;
            Type = type;
        }

        /// <summary>
        /// The unique string identifying this ExtensionPoint. This identifier
        /// is typically formatted as a path using '/' and the set of extension 
        /// points is sometimes viewed as forming a tree.
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// The required Type (usually an interface) of any extension that is 
        /// installed at this ExtensionPoint.
        /// </summary>
        public Type Type { get; private set; }

        /// <summary>
        /// An optional description of the purpose of the ExtensionPoint
        /// </summary>
        public string Description { get; set; }
    }
}
