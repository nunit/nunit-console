// ***********************************************************************
// Copyright (c) 2017 Charlie Poole
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;

namespace NUnit.Engine.Services.Tests
{
    public class AgentDataBaseTests
    {
        private static int[] Counts = new int[] { 1, 3, 10 };

        const string COUNTS = nameof(Counts);

        AgentDataBase _data;
        List<Guid> _generatedGuids;

        [SetUp]
        public void SetUp()
        {
            _data = new AgentDataBase();
            _generatedGuids = new List<Guid>();
        }

        [TestCaseSource(COUNTS)]
        public void AddData(int count)
        {
            AddRecords(count);

            Assert.That(_generatedGuids.Count, Is.EqualTo(count));
            Assert.That(_data.GetGuids(), Is.EqualTo(_generatedGuids));
        }

        [TestCaseSource(COUNTS)]
        public void AddData_Parallel(int count)
        {
            GenerateGuids(count);
            RunInParallel((g) => AddRecord((Guid)g));

            Assert.That(_generatedGuids.Count, Is.EqualTo(count));
            Assert.That(_data.GetGuids(), Is.EqualTo(_generatedGuids));
        }

        [TestCaseSource(COUNTS)]
        public void ReadData(int count)
        {
            AddRecords(count);

            foreach (var guid in _generatedGuids)
            {
                var r = _data[guid];
                Assert.NotNull(r);
                Assert.That(r.Id, Is.EqualTo(guid));
            }
        }

        [TestCaseSource(COUNTS)]
        public void ReadData_Parallel(int count)
        {
            AddRecords(count);

            RunInParallel((g) =>
            {
                var guid = (Guid)g;
                var r = _data[guid];
                Assert.NotNull(r);
                Assert.That(r.Id, Is.EqualTo(guid));
            });
        }

        private void GenerateGuids(int count)
        {
            while (count-- > 0)
                _generatedGuids.Add(Guid.NewGuid());
        }

        private void AddRecord(Guid guid)
        {
            _data.Add(new AgentRecord(guid, null, null, AgentStatus.Ready));
        }
        
        private void AddRecords(int count)
        {
            GenerateGuids(count);

            foreach (var guid in _generatedGuids)
                AddRecord(guid);
        }

        // Run in parallel for each generated guid
        private void RunInParallel(ParameterizedThreadStart start)
        {
            var threads = new List<Thread>();

            for (int i = 0; i < _generatedGuids.Count; i++)
                threads.Add(new Thread(start));

            for (int i = 0; i < _generatedGuids.Count; i++)
                threads[i].Start(_generatedGuids[i]);

            foreach (var thread in threads)
                thread.Join();
        }
    }
}
