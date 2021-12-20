// Copyright (c) The NUnit Project and distributed under the MIT License

// This file contains tasks intended to be run locally by developers
// in any NUnit project. To use these tasks in a project, copy the file
// to an accessible directory and #load it in your build.cake file.

// Since the tasks are intended for use by the NUnit Project, it follows
// certain conventions and will need to be modified for use elsewhere.

using System.Linq;

//////////////////////////////////////////////////////////////////////
// DELETE ALL OBJ DIRECTORIES
//////////////////////////////////////////////////////////////////////

Task("DeleteObjectDirectories")
    .WithCriteria(BuildSystem.IsLocalBuild)
    .Does(() =>
    {
        Information("Deleting object directories");

        foreach (var dir in GetDirectories("src/**/obj/"))
            DeleteDirectory(dir, new DeleteDirectorySettings() { Recursive = true });
    });

// NOTE: Any project to which this file is added is required to have a 'Clean' target
Task("CleanAll")
    .Description("Perform standard 'Clean' followed by deleting object directories")
    .IsDependentOn("Clean")
    .IsDependentOn("DeleteObjectDirectories");
