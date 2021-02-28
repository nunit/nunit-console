// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;

namespace NUnit.Engine.Extensibility
{
    /// <summary>
    /// TypeExtensionPointAttribute is used to bind an extension point
    /// to a class or interface.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple=true, Inherited=false)]
    public class TypeExtensionPointAttribute : Attribute
    {
        /// <summary>
        /// Construct a TypeExtensionPointAttribute, specifying the path.
        /// </summary>
        /// <param name="path">A unique string identifying the extension point.</param>
        public TypeExtensionPointAttribute(string path)
        {
            Path = path;
        }

        /// <summary>
        /// Construct an TypeExtensionPointAttribute, without specifying the path.
        /// The extension point will use a path constructed based on the interface
        /// or class to which the attribute is applied.
        /// </summary>
        public TypeExtensionPointAttribute()
        {

        }

        /// <summary>
        /// The unique string identifying this ExtensionPoint. This identifier
        /// is typically formatted as a path using '/' and the set of extension 
        /// points is sometimes viewed as forming a tree.
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// An optional description of the purpose of the ExtensionPoint
        /// </summary>
        public string Description { get; set; }
    }
}
