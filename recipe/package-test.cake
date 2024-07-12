// Representation of a single test to be run against a pre-built package.
// Each test has a Level, with the following values defined...
//  0 Do not run - used for temporarily disabling a test
//  1 Run for all CI tests - that is every time we test packages
//  2 Run only on PRs, dev builds and when publishing
//  3 Run only when publishing
public struct PackageTest
{
	public int Level;
	public string Name;
	public string Description;
	public string Arguments;
	public ExpectedResult ExpectedResult;
    public IPackageTestRunner[] TestRunners;
	public ExtensionSpecifier[] ExtensionsNeeded;

    public PackageTest(int level, string name, string description, string arguments, ExpectedResult expectedResult )
    {
        if (name == null)
            throw new ArgumentNullException(nameof(name));
		if (description == null)
			throw new ArgumentNullException(nameof(description));
		if (arguments == null)
			throw new ArgumentNullException(nameof(arguments));
		if (expectedResult == null)
			throw new ArgumentNullException(nameof(expectedResult));

		Level = level;
        Name = name;
        Description = description;
        Arguments = arguments;
        ExpectedResult = expectedResult;
        ExtensionsNeeded = new ExtensionSpecifier[0];
        TestRunners = new IPackageTestRunner[0];
    }

    public PackageTest(int level, string name, string description, string arguments, ExpectedResult expectedResult, params ExtensionSpecifier[] extensionsNeeded )
    {
        if (name == null)
            throw new ArgumentNullException(nameof(name));
		if (description == null)
			throw new ArgumentNullException(nameof(description));
		if (arguments == null)
			throw new ArgumentNullException(nameof(arguments));
		if (expectedResult == null)
			throw new ArgumentNullException(nameof(expectedResult));

		Level = level;
        Name = name;
        Description = description;
        Arguments = arguments;
        ExpectedResult = expectedResult;
		ExtensionsNeeded = extensionsNeeded;
        TestRunners = new IPackageTestRunner[0];
    }

	public PackageTest(int level, string name, string description, string arguments, ExpectedResult expectedResult, params IPackageTestRunner[] testRunners )
    {
        if (name == null)
            throw new ArgumentNullException(nameof(name));
		if (description == null)
			throw new ArgumentNullException(nameof(description));
		if (arguments == null)
			throw new ArgumentNullException(nameof(arguments));
		if (expectedResult == null)
			throw new ArgumentNullException(nameof(expectedResult));

		Level = level;
        Name = name;
        Description = description;
        Arguments = arguments;
        ExpectedResult = expectedResult;
		TestRunners = testRunners;
        ExtensionsNeeded = new ExtensionSpecifier[0];
    }
}
