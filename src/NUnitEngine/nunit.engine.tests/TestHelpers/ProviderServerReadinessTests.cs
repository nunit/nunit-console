// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using NUnit.Framework;

namespace NUnit.Engine.TestHelpers
{
    /// <summary>
    /// Exercises real file-sharing failures without depending on machine speed or a child process race.
    /// </summary>
    [TestFixture]
    public sealed class ProviderServerReadinessTests
    {
        private string _directory = null!;
        private string _path = null!;

        /// <summary>Creates an isolated receipt directory.</summary>
        [SetUp]
        public void CreateDirectory()
        {
            _directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "readiness-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            _path = Path.Combine(_directory, "ready.xml");
        }

        /// <summary>Removes the receipt after all owned handles have been disposed.</summary>
        [TearDown]
        public void RemoveDirectory()
        {
            Directory.Delete(_directory, true);
        }

        /// <summary>A missing receipt has not been published yet.</summary>
        [Test]
        public void MissingReceiptIsNotReady()
        {
            Assert.That(ProviderServerReadiness.ReadIfAvailable(_path), Is.Null);
        }

        /// <summary>A readable receipt retains its actual contents.</summary>
        [Test]
        public void ReadableReceiptIsReturned()
        {
            var expected = WriteReceipt();
            Assert.That(XNode.DeepEquals(ProviderServerReadiness.ReadIfAvailable(_path), expected), Is.True);
        }

        /// <summary>
        /// An existing file may still be unreadable. Real Windows sharing and range locks
        /// must delay readiness rather than fail the fixture or bypass the lock.
        /// </summary>
        /// <param name="rangeLock">Whether a byte-range lock is used instead of exclusive sharing.</param>
        /// <param name="expectedError">The native Windows error wrapped by IOException.</param>
        [TestCase(false, unchecked((int)0x80070020))]
        [TestCase(true, unchecked((int)0x80070021))]
        [Platform("Win")]
#if NETCOREAPP
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
        public void LockedReceiptBecomesReadyAfterHandleRelease(bool rangeLock, int expectedError)
        {
            var expected = WriteReceipt();
            using (var holder = new FileStream(
                _path,
                FileMode.Open,
                rangeLock ? FileAccess.Read : FileAccess.ReadWrite,
                rangeLock ? FileShare.ReadWrite : FileShare.None))
            {
                if (rangeLock)
                    holder.Lock(0, holder.Length);
                try
                {
                    Assert.That(File.Exists(_path), Is.True);
                    Assert.That(() => XElement.Load(_path),
                        Throws.TypeOf<IOException>().With.Property(nameof(Exception.HResult)).EqualTo(expectedError));
                    Assert.That(ProviderServerReadiness.ReadIfAvailable(_path), Is.Null);
                }
                finally
                {
                    if (rangeLock)
                        holder.Unlock(0, holder.Length);
                }
            }

            Assert.That(XNode.DeepEquals(ProviderServerReadiness.ReadIfAvailable(_path), expected), Is.True);
        }

        /// <summary>A malformed published receipt is a real failure, not temporary unreadiness.</summary>
        [Test]
        public void MalformedReceiptFailsImmediately()
        {
            File.WriteAllText(_path, "<ready");
            Assert.That(() => ProviderServerReadiness.ReadIfAvailable(_path), Throws.TypeOf<XmlException>());
        }

        /// <summary>Publishes complete XML before a controlled lock is acquired.</summary>
        private XElement WriteReceipt()
        {
            var receipt = new XElement("ready", new XAttribute("pid", 123), new XAttribute("port", 4567));
            receipt.Save(_path);
            return receipt;
        }
    }
}
