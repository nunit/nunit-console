// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;

namespace NUnit.Engine
{
	/// <summary>
	/// The IRecentFiles interface is used to isolate the app
	/// from various implementations of recent files.
	/// </summary>
	public interface IRecentFiles
	{ 
		/// <summary>
		/// The max number of files saved
		/// </summary>
		int MaxFiles { get; set; }

		/// <summary>
		/// Get a list of all the file entries
		/// </summary>
		/// <returns>The most recent file list</returns>
		IList<string> Entries { get; }

		/// <summary>
		/// Set the most recent file name, reordering
		/// the saved names as needed and removing the oldest
		/// if the max number of files would be exceeded.
		/// The current CLR version is used to create the entry.
		/// </summary>
		void SetMostRecent( string fileName );

		/// <summary>
		/// Remove a file from the list
		/// </summary>
		/// <param name="fileName">The name of the file to remove</param>
		void Remove( string fileName );
	}
}
