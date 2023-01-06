using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("nunit4-console.tests")]

//Allow NSubstitute to mock out internal types
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
