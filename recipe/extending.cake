// This file provides information on how to change the pre-defined tasks in
// the NUnit Cake Recipe for a particular project, without changing the 
// recipe files themselves. Those files should not be changed unless you
// are trying to make changes for all projects, which use the recipe.
//
// In addition, this file defines a few static methods intended to make it
// easier to override task definitions for a project.
//
// Code given in the following examples should be added to your file after
// the recipe itself has been loaded.
//
// SIMPLE EXAMPLES:
//
// Adding a new dependency after those already present
//    BuildTasks.SomeTask.IsDependentOn("SomeOtherTask");
//
// Adding a new action, which will be performed after the existing action.
//    BuildTasks.SomeTask.Does(() => {
//        // Code here to do something
//    });
//
// MORE COMPLEX EXAMPLES:
//
// Replacing the existing dependencies, criteria or actions is a bit harder.
// You must first clear the relevant items and then re-add them in the desired
// order along with any new items. The following static methods allow you to do
// so a bit more simply than would otherwise be possible.

public static void ClearCriteria(CakeTaskBuilder builder) => ((CakeTask)builder.Task).Criterias.Clear();
public static void ClearDependencies(CakeTaskBuilder builder) => ((CakeTask)builder.Task).Dependencies.Clear();
public static void ClearActions(CakeTaskBuilder builder) => ((CakeTask)builder.Task).Actions.Clear();

// Redefining the action of the build task. Note that dependencies will still run, since we haven't changed them.
//    ClearActions(BuildTasks.Build);
//    BuildTasks.Build.Does(() => Information("I'm not building today!"));
//
// In some cases, you may wish to completely redefine what a  task does. The following static method
// supports a fluent syntax for doing just that

public static CakeTaskBuilder Redefine(CakeTaskBuilder builder)
{
    ClearCriteria(builder);
    ClearDependencies(builder);
    ClearActions(builder);

    return builder;
}

// Redefine the build task completely. Note that in this case, dependencies are not run.
//    Redefine(BuildTasks.BuildTask)
//        .Does(() => {
//            Information("Not Building today!");
//        });
//
// Modify the Default task for a project to run "Test" rather than "Build".
//    Redefine(BuildTasks.DefaultTask)
//        .IsDependentOn("Test");
