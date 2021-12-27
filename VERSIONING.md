# NUnit Console Runner and Engine Versioning

Both **Console** and **Engine** packages use a version number in the form MAJOR.MINOR.PATCH. We change the

- MAJOR version when we have made incompatible API changes
- MINOR version when adding functionality in a backwards-compatible manner
- PATCH version when you making backwards-compatible bug fixes

This versioning approach is, of course, what is known as "Semantic Versioning." Taken by itself, however, the above description leaves out some rather important stuff...

* What exactly are the APIs we are protecting from incompatible changes?
* What degree of change is needed in each of those APIs for it to be considered a breaking change?

The remainder of this document attempts to answer those questions.

## Console Runner API

First, let's note that the **Console Runner** is not designed to be used as a library, so there is no ABI or library-like API to worry about. However, the runner does present a command-line interface, which is used both interactively and through user-created scripts. The command-line API is defined by our documentation, including the help message the runner displays and the [published documentation](https://docs.nunit.org/articles/nunit/running-tests/Console-Command-Line.html) on our website.

### Examples of MAJOR Command-Line Changes

- Complete removal of an option.
- Changing the behavior of an option so that it no longer does what it was defined to do.
- Removing the ability to run under a particular platform.

### Examples of MINOR Command-Line Changes

- Addition of a new option
- Modifying the behavior of an option so it does the same thing better or more effectively or has added sub-options.
- Adding support for running under an new platform

### Examples of PATCH Command-Line changes

- Fixing a bug in the behavior of an option.
- Restoring an option to its original defined behavior.
- Correcting spelling errors.

## Engine APIs

The **NUnit Engine** is intended to be used by third-party runners as well as our own **Console Runner**. Usage is through the defined API, specified by interfaces included in the `nunit.engine.api` assembly. It's important to note that public types and methods included in other assemblies are not considered a part of the published API. Direct creation and use of `nunit.engine`, `nunit.engine.core` or any other assemblies included in our packages is discouraged for that reason. See below for more about this.

### Examples of MAJOR Api Changes

- Removing a published interface.
- Modifying an interface so that it breaks existing usage.

### Examples of MINOR Api Changes

- Adding a new published interface and implementation.
- Modifying an interface in a way which does __not__ break existing usage.
- Modifying the implementation of an interface so it does the same thing better or more effectively.
- Restoring an API or it's implementation to the original behavior following an erroneous MINOR release.

### Examples of PATCH Api Changes

- Fixing bugs in the implementation of an API.
- Restoring an API or it's implementation to the original behavior following an erroneous PATCH release.

## Notes

1. Access to Types and Methods through reflection, while possible, is not part of our supported API.

2. As explained above, the existing `nunit.engine.api` assembly specifies the _entire_ published API for the **NUnit Engine**. Other guaranteed APIs may be added in the future, using the same `.api` naming convention.

3. Historically, NUnit has used public visibility for many Types and Methods, which were not actually part of the API. We are in the process of changing this approach to a more modern focus on limiting visibility. That said, even where a Type or Method is publicly accessible, it is __not__ automatically a part of our API and may change from release to release.

4. Even in the case of non-breaking changes, as defined above, we will make a reasonable  effort to avoid negative impact on users.

## Unresolved (Attention Reviewers!)

* How do we consider a change to the Engine API, which requires recompilation of a runner using it. IOW, do we want source or binary compatibility for the Engine API?
