# NUnit Console Runner and Engine Versioning

Both **Console** and **Engine** packages use a version number in the form MAJOR.MINOR.PATCH. We change the

- MAJOR version when we have made incompatible changes to a published interface
- MINOR version when adding functionality in a backwards-compatible manner
- PATCH version when making backwards-compatible bug fixes

This versioning approach is, of course, what is known as "Semantic Versioning." Taken by itself, however, the above description leaves out some rather important stuff...

* What do we mean by a "published interface"?
* What are the specific interfaces we will protect from incompatible (breaking) changes?
* What degree of change is needed in each of those interface for it to be considered a breaking change?

The remainder of this document attempts to answer those questions. We start out with a set of definitions and then lay out specific considerations for the each of the **Console Runner** and **Engine** packages. In each case, examples of MAJOR, MINOR and PATCH level changes are given.

## Definitions

In this document, we use the term _interface_ to refer to any of the various ways a user or program may interact with software. It includes programming language `interfaces` such as those defined in C#, but goes beyond that to deal with things like the structure of a command-line and the specific behavior (semantics) a user may expect when using an interface. In this document, we'll use the phrase `C# interface` when that's what is meant.

An _API_ is an application programming interface. In our case, it's a .NET interface implemented in C# but generally available to other languages as well. It may consist of actual `C# interfaces` defined in code as well as other programming constructs.

An _internal interface_ is an interface, which we have defined for our own use, generally in order to facilitate communication between different software modules. Internal interfaces may be recognized by use of an `Internal` namespace, by lack of public visibility or by absence of documentation.

A _published interface_ is an interface, which we have defined and published for general use. There is an implied guarantee that _published interfaces_ will not be broken, except on release of a new MAJOR version of the software.

Publishing an interface always involves documentation. The engine team is responsible for creating and maintaining documentation of all our supported interfaces. Users of the software are responsible for ensuring that they only rely on specifically supported interfaces. When in doubt about the status of an interface, it's best to ask before relying on its stability.

## Console Runner Interfaces

First, let's note that the **Console Runner** is not designed to be used as a library, so there is no ABI or library-like API to worry about. However, the runner does present a command-line interface, which is used both interactively and through user-created scripts.

The command-line is a _published interface_ as defined above. It is specified in our documentation, including the help message the runner itself displays and the [published documentation](https://docs.nunit.org/articles/nunit/running-tests/Console-Command-Line.html) on our website.

### Examples of MAJOR Command-Line Changes

- Complete removal of an option.
- Changing the behavior of an option so that it no longer does what it was previously defined to do.
- Removing the ability to run under a particular platform.

### Examples of MINOR Command-Line Changes

- Adding a new option
- Modifying the behavior of an option so it does the same thing better or more effectively or has added sub-options.
- Adding support for running under an new platform

### Examples of PATCH Command-Line Changes

- Fixing a bug in the behavior of an option, by restoring it to its original defined behavior.
- Correcting spelling errors.

## Engine APIs

The **NUnit Engine** is intended to be used by third-party runners as well as our own **Console Runner**. Usage is through a defined API, which forms its _public interface_. That interface is entirely contained in the `nunit.engine.api` assembly. Types and methods included in other assemblies are not considered a part of the _published interface_. Direct creation and use of `nunit.engine`, `nunit.engine.core` or any other assemblies included in our packages is discouraged for that reason. See below for more about this.

### Examples of MAJOR Api Changes

- Removing a published interface.
- Modifying an interface so that it breaks existing usage.

### Examples of MINOR Api Changes

- Adding a new published interface and implementation.
- Modifying an interface in a way which does __not__ break existing usage.
- Modifying the implementation of an interface so it does the same thing better or more effectively.
- Restoring an API or its implementation to the original behavior following an erroneous MINOR release.

### Examples of PATCH Api Changes

- Fixing bugs in the implementation of an API.
- Restoring an API or its implementation to the original behavior following an erroneous PATCH release.

## Notes

1. Access to Types and Methods through reflection, while possible, is not part of our supported API.

2. As explained above, the existing `nunit.engine.api` assembly specifies the _entire_ published API for the **NUnit Engine**. Other guaranteed APIs may be added in the future, using the same `.api` naming convention.

3. Historically, NUnit has used public visibility for many Types and Methods, which were not actually part of the API. We are in the process of changing this approach to a more modern focus on limiting visibility.

4. Even in the case of non-breaking changes, as defined above, we will make a reasonable  effort to avoid negative impact on users.
