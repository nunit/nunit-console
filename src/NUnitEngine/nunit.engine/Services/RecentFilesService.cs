// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;

namespace NUnit.Engine.Services
{
    /// <summary>
    /// Summary description for RecentFilesService.
    /// </summary>
    public class RecentFilesService : Service, IRecentFiles
    {
        private IList<string> _fileEntries = new List<string>();
        private ISettings _userSettings;

        private const int MAX_FILES = 24;

        public int MaxFiles
        {
            get { return MAX_FILES; }
            set { /* Noop in this implementation */ }
            // NOTE: we retain the setter to avoid changing the interface
        }

        public IList<string> Entries { get { return _fileEntries; } }
        
        public void Remove( string fileName )
        {
            _fileEntries.Remove(fileName);
        }

        public void SetMostRecent( string filePath )
        {
            _fileEntries.Remove(filePath);

            _fileEntries.Insert( 0, filePath );
            if( _fileEntries.Count > MAX_FILES )
                _fileEntries.RemoveAt( MAX_FILES );
        }

        private void LoadEntriesFromSettings()
        {
            _fileEntries.Clear();

            // TODO: Prefix should be provided by caller
            AddEntriesForPrefix("Gui.RecentProjects");
        }

        private void AddEntriesForPrefix(string prefix)
        {
            for (int index = 1; index < MAX_FILES; index++)
            {
                if (_fileEntries.Count >= MAX_FILES) break;

                string fileSpec = _userSettings.GetSetting(GetRecentFileKey(prefix, index)) as string;
                if (fileSpec != null) _fileEntries.Add(fileSpec);
            }
        }

        private void SaveEntriesToSettings()
        {
            string prefix = "Gui.RecentProjects";

            while( _fileEntries.Count > MAX_FILES )
                _fileEntries.RemoveAt( _fileEntries.Count - 1 );

            for( int index = 0; index < MAX_FILES; index++ ) 
            {
                string keyName = GetRecentFileKey( prefix, index + 1 );
                if ( index < _fileEntries.Count )
                    _userSettings.SaveSetting( keyName, _fileEntries[index] );
                else
                    _userSettings.RemoveSetting( keyName );
            }

            // Remove legacy entries here
            _userSettings.RemoveGroup("RecentProjects");
        }

        private string GetRecentFileKey( string prefix, int index )
        {
            return string.Format( "{0}.File{1}", prefix, index );
        }

        public override void StopService()
        {
            try
            {
                SaveEntriesToSettings();
            }
            finally
            {
                Status = ServiceStatus.Stopped;
            }
        }

        public override void StartService()
        {
            try
            {
                // RecentFilesService requires SettingsService
                _userSettings = ServiceContext.GetService<ISettings>();

                // Anything returned from ServiceContext is an IService
                if (_userSettings != null && ((IService)_userSettings).Status == ServiceStatus.Started)
                {
                    LoadEntriesFromSettings();
                    Status = ServiceStatus.Started;
                }
                else
                {
                    Status = ServiceStatus.Error;
                }
            }
            catch
            {
                Status = ServiceStatus.Error;
                throw;
            }
        }
    }
}
