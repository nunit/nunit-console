using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
#if NETSTANDARD1_6
[assembly: AssemblyTitle("NUnit .NET Standard Engine")]
[assembly: AssemblyDescription("Provides a common interface for loading, exploring and running NUnit tests in .NET Core and .NET Standard")]
#else
[assembly: AssemblyTitle("NUnit Engine")]
[assembly: AssemblyDescription("Provides a common interface for loading, exploring and running NUnit tests")]
#endif
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("5796938b-03c9-4b75-8b43-89a8adc4acd0")]

[assembly: InternalsVisibleTo("nunit.engine.tests, PublicKey="+
    "002400000480000094000000060200000024000052534131000400000100010031eea370b1984b" +
    "fa6d1ea760e1ca6065cee41a1a279ca234933fe977a096222c0e14f9e5a17d5689305c6d7f1206"+
    "a85a53c48ca010080799d6eeef61c98abd18767827dc05daea6b6fbd2e868410d9bee5e972a004"+
    "ddd692dec8fa404ba4591e847a8cf35de21c2d3723bc8d775a66b594adeb967537729fe2a446b5"+
    "48cd57a6")]

//Allow NSubstitute to mock out internal types
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2,PublicKey=002400000480000094" +
    "0000000602000000240000525341310004000001000100c547cac37abd99c8db225ef2f6c8a3602" +
    "f3b3606cc9891605d02baa56104f4cfc0734aa39b93bf7852f7d9266654753cc297e7d2edfe0bac" +
    "1cdcf9f717241550e0a7b191195b7667bb4f64bcb8e2121380fd1d9d46ad2d92d2d15605093924c" +
    "ceaf74c4861eff62abf69b9291ed0a340e113be11e6a7d3113e92484cf7045cc7")]
