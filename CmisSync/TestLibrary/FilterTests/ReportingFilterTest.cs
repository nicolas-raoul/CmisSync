//-----------------------------------------------------------------------
// <copyright file="ReportingFilterTest.cs" company="GRAU DATA AG">
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General private License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//   GNU General private License for more details.
//
//   You should have received a copy of the GNU General private License
//   along with this program. If not, see http://www.gnu.org/licenses/.
//
// </copyright>
//-----------------------------------------------------------------------

namespace TestLibrary.FilterTests {
    using System;

    using CmisSync.Lib.Events;
    using CmisSync.Lib.Queueing;
    using CmisSync.Lib.Filter;
    using CmisSync.Lib.Storage.FileSystem;

    using Moq;

    using NUnit.Framework;

    [TestFixture, Category("Fast"), Category("EventFilter")]
    public class ReportingFilterTest {
        private IgnoredFoldersFilter ignoreFoldersFilter;
        private IgnoredFileNamesFilter ignoreFileNamesFilter;
        private IgnoredFolderNameFilter ignoreFolderNamesFilter;
        private InvalidFolderNameFilter invalidFolderNameFilter;
        private SymlinkFilter symlinkFilter;

        private Mock<ISyncEventQueue> queue;

        [SetUp]
        public void SetUpFilter() {
            this.ignoreFoldersFilter = Mock.Of<IgnoredFoldersFilter>();
            this.ignoreFileNamesFilter = Mock.Of<IgnoredFileNamesFilter>();
            this.ignoreFolderNamesFilter = new Mock<IgnoredFolderNameFilter>(Mock.Of<IDirectoryInfo>()).Object;
            this.invalidFolderNameFilter = Mock.Of<InvalidFolderNameFilter>();
            this.symlinkFilter = Mock.Of<SymlinkFilter>();
            this.queue = new Mock<ISyncEventQueue>();
        }

        [Test]
        public void ConstructorTakesAllFilter() {
            new ReportingFilter(
                this.queue.Object,
                this.ignoreFoldersFilter,
                this.ignoreFileNamesFilter,
                this.ignoreFolderNamesFilter,
                this.invalidFolderNameFilter,
                this.symlinkFilter);
        }

        [Test]
        public void ConstructorThrowsExceptionIfQueueIsNull() {
            Assert.Throws<ArgumentNullException>(() => new ReportingFilter(
                null,
                this.ignoreFoldersFilter,
                this.ignoreFileNamesFilter,
                this.ignoreFolderNamesFilter,
                this.invalidFolderNameFilter,
                this.symlinkFilter));
        }

        [Test]
        public void ConstructorThrowsExceptionIfIgnoreFoldersFilterIsNull() {
            Assert.Throws<ArgumentNullException>(() => new ReportingFilter(
                this.queue.Object,
                null,
                this.ignoreFileNamesFilter,
                this.ignoreFolderNamesFilter,
                this.invalidFolderNameFilter,
                this.symlinkFilter));
        }

        [Test]
        public void ConstructorThrowsExceptionIfIgnoreFileNamesFilterIsNull() {
            Assert.Throws<ArgumentNullException>(() => new ReportingFilter(
                this.queue.Object,
                this.ignoreFoldersFilter,
                null,
                this.ignoreFolderNamesFilter,
                this.invalidFolderNameFilter,
                this.symlinkFilter));
        }

        [Test]
        public void ConstructorThrowsExceptionIfIgnoreFolderNameFilterIsNull() {
            Assert.Throws<ArgumentNullException>(() => new ReportingFilter(
                this.queue.Object,
                this.ignoreFoldersFilter,
                this.ignoreFileNamesFilter,
                null,
                this.invalidFolderNameFilter,
                this.symlinkFilter));
        }

        [Test]
        public void ConstructorThrowsExceptionIfInvalidFolderNameFilterIsNull() {
            Assert.Throws<ArgumentNullException>(() => new ReportingFilter(
                this.queue.Object,
                this.ignoreFoldersFilter,
                this.ignoreFileNamesFilter,
                this.ignoreFolderNamesFilter,
                null,
                this.symlinkFilter));
        }

        [Test]
        public void ConstructorThrowsExceptionIfSymlinkFilterIsNull() {
            Assert.Throws<ArgumentNullException>(() => new ReportingFilter(
                this.queue.Object,
                this.ignoreFoldersFilter,
                this.ignoreFileNamesFilter,
                this.ignoreFolderNamesFilter,
                this.invalidFolderNameFilter,
                null));
        }

        [Test]
        public void HandleFilterableFileNameEvent() {
            var filter = this.CreateFilter();
            var filterableNameEvent = Mock.Of<IFilterableNameEvent>(e => e.Name == "name");
            Mock<IgnoredFileNamesFilter> fileNameFilter = Mock.Get<IgnoredFileNamesFilter>(this.ignoreFileNamesFilter);
            Assert.That(filter.Handle(filterableNameEvent), Is.False);
            this.queue.Verify(q => q.AddEvent(It.IsAny<ISyncEvent>()), Times.Never);
            string reason = null;
            fileNameFilter.Verify(f => f.CheckFile("name", out reason), Times.Once());
            Assert.That(string.IsNullOrEmpty(reason));
        }

        private ReportingFilter CreateFilter() {
            return new ReportingFilter(
                this.queue.Object,
                this.ignoreFoldersFilter,
                this.ignoreFileNamesFilter,
                this.ignoreFolderNamesFilter,
                this.invalidFolderNameFilter,
                this.symlinkFilter);
        }
    }
}