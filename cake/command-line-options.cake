// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric GUI contributors.
// Licensed under the MIT License. See LICENSE.txt in root directory.
// ***********************************************************************

CommandLineOptions.Initialize(Context);

public static class CommandLineOptions
{
	static private ICakeContext _context;

	static public ValueOption<string> Target;
	static public ValueOption<string> Configuration;
	static public ValueOption<string> PackageVersion;
    static public ValueOption<string> PackageSelector; 
	static public ValueOption<int> TestLevel;
	static public ValueOption<string> TraceLevel;
	static public SimpleOption NoBuild;
	static public SimpleOption NoPush;
	static public SimpleOption Usage;

	public static void Initialize(ICakeContext context)
	{
		_context = context;

		// The name of the TARGET task to be run, e.g. Test.
		Target = new ValueOption<string>("target", "Default", 1);

		Configuration = new ValueOption<String>("configuration", DEFAULT_CONFIGURATION, 1);
		
		PackageVersion = new ValueOption<string>("packageVersion", null, 4);

        PackageSelector = new ValueOption<string>("where", null, 1);

		TestLevel = new ValueOption<int>("level", 0, 3);

		TraceLevel = new ValueOption<string>("trace", "Off", 2);

		NoBuild = new SimpleOption("nobuild", 3);

		NoPush = new SimpleOption("nopush",  3);

		Usage = new SimpleOption("usage", 2);
	}

	// Nested classes to represent individual options

	// AbstractOption has a name and can tell us if it exists.
	public abstract class AbstractOption
	{
		public string Name { get; }
		
		public int MinimumAbbreviation { get; internal set; }
		
		public bool Exists 
		{
			get
			{
				for (int len = Name.Length; len >= MinimumAbbreviation; len--)
					if (_context.HasArgument(Name.Substring(0,len)))
						return true;
				return false;
			}
		}

		public string Description { get; }

		public AbstractOption(string name, int minimumAbbreviation = 0, string description = null)
		{
			Name = name;
			MinimumAbbreviation = minimumAbbreviation > 0 && minimumAbbreviation <= name.Length
				? minimumAbbreviation
				: name.Length;
			Description = description;
		}
	}

	// Simple Option adds an implicit boolean conversion operator.
	// It throws an exception if you gave it a value on the command-line.
	public class SimpleOption : AbstractOption
	{
		static public implicit operator bool(SimpleOption o) => o.Exists;

		public SimpleOption(string name, int minimumAbbreviation = 0, string description = null)
			: base(name, minimumAbbreviation, description)
		{
			if (_context.Argument(name, (string)null) != null)
				throw new Exception($"Option --{name} does not take a value.");
		}
	}

	// Generic ValueOption adds Value as well as a default value
	public class ValueOption<T> : AbstractOption
	{
		public T DefaultValue { get; }

		public ValueOption(string name, T defaultValue, int minimumAbbreviation = 0, string description = null)
			: base(name, minimumAbbreviation, description)
		{
			DefaultValue = defaultValue;
		}

		public T Value
		{
			get
			{
				for (int len = Name.Length; len >= MinimumAbbreviation; len--)
				{
					string abbrev = Name.Substring(0,len);
					if (_context.HasArgument(abbrev))
						return _context.Argument<T>(abbrev);
				}
			
				return DefaultValue;
			}
		}
	}
}
