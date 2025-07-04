﻿// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;

namespace NUnit.TextDisplay
{
    public class ColorConsoleWriter : ExtendedTextWrapper
    {
        public bool _colorEnabled;

        /// <summary>
        /// Construct a ColorConsoleWriter.
        /// </summary>
        public ColorConsoleWriter() : this(true)
        {
        }

        /// <summary>
        /// Construct a ColorConsoleWriter.
        /// </summary>
        /// <param name="colorEnabled">Flag indicating whether color should be enabled</param>
        public ColorConsoleWriter(bool colorEnabled)
            : base(Console.Out)
        {
            _colorEnabled = colorEnabled;
        }

        /// <summary>
        /// Writes the value with the specified style.
        /// </summary>
        /// <param name="style">The style.</param>
        /// <param name="value">The value.</param>
        public override void Write(ColorStyle style, string value)
        {
            if (_colorEnabled)
                using (new ColorConsole(style))
                {
                    Write(value);
                }
            else
                Write(value);
        }

        /// <summary>
        /// Writes the value with the specified style.
        /// </summary>
        /// <param name="style">The style.</param>
        /// <param name="value">The value.</param>
        public override void WriteLine(ColorStyle style, string value)
        {
            if (_colorEnabled)
                using (new ColorConsole(style))
                {
                    WriteLine(value);
                }
            else
                WriteLine(value);
        }

        /// <summary>
        /// Writes the label and the option that goes with it.
        /// </summary>
        /// <param name="label">The label.</param>
        /// <param name="option">The option.</param>
        public override void WriteLabel(string label, object option)
        {
            WriteLabel(label, option, ColorStyle.Value);
        }

        /// <summary>
        /// Writes the label and the option that goes with it followed by a new line.
        /// </summary>
        /// <param name="label">The label.</param>
        /// <param name="option">The option.</param>
        public override void WriteLabelLine(string label, object option)
        {
            WriteLabelLine(label, option, ColorStyle.Value);
        }

        /// <summary>
        /// Writes the label and the option that goes with it and optionally writes a new line.
        /// </summary>
        /// <param name="label">The label.</param>
        /// <param name="option">The option.</param>
        /// <param name="valueStyle">The color to display the value with</param>
        public override void WriteLabel(string label, object option, ColorStyle valueStyle)
        {
            Guard.ArgumentNotNull(option);

            Write(ColorStyle.Label, label);
            Write(valueStyle, option.ToString() ?? string.Empty);
        }

        /// <summary>
        /// Writes the label and the option that goes with it followed by a new line.
        /// </summary>
        /// <param name="label">The label.</param>
        /// <param name="option">The option.</param>
        /// <param name="valueStyle">The color to display the value with</param>
        public override void WriteLabelLine(string label, object option, ColorStyle valueStyle)
        {
            WriteLabel(label, option, valueStyle);
            WriteLine();
        }
    }
}
