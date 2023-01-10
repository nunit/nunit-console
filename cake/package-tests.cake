// Representation of a single test to be run against a pre-built package.
public struct PackageTest
{
    public string Name;
    public string Description;
    public string Arguments;
    public ExpectedResult ExpectedResult;

    public PackageTest(string name, string description, string arguments, ExpectedResult expectedResult)
    {
        Name = name;
        Description = description;
        Arguments = arguments;
        ExpectedResult = expectedResult;
    }
}

