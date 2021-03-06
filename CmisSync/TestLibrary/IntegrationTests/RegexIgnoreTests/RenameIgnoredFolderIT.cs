//-----------------------------------------------------------------------
// <copyright file="RenameIgnoredFolderIT.cs" company="GRAU DATA AG">
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
﻿
namespace TestLibrary.IntegrationTests.RegexIgnoreTests {
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading;

    using CmisSync.Lib.Cmis.ConvenienceExtenders;

    using DotCMIS.Client;

    using NUnit.Framework;

    using TestLibrary.TestUtils;

    [TestFixture, TestName("RenameIgnoredFolder"), Category("RegexIgnore"), Category("Slow"), Timeout(180000)]
    public class RenameIgnoredFolderIT : BaseRegexIgnoreTest {
        private readonly string ignoredName = ".Ignored";
        private readonly string fileName = "file.bin";
        private readonly string normalName = "NotIgnoredFolder";
        [Test]
        public void RenameLocalIgnoredFolderToNotIgnoredFolder(
            [Values(true, false)]bool contentChanges,
            [Values(true, false)]bool withFileInside)
        {
            this.ContentChangesActive = contentChanges;
            var ignoredLocalFolder = this.localRootDir.CreateSubdirectory(ignoredName);
            if (withFileInside) {
                var file = new FileInfo(Path.Combine(ignoredLocalFolder.FullName, fileName));
                using (file.Create());
            }

            this.InitializeAndRunRepo(swallowExceptions: true);
            Assert.That(this.remoteRootDir.GetChildren().TotalNumItems, Is.EqualTo(0));

            ignoredLocalFolder.MoveTo(Path.Combine(this.localRootDir.FullName, normalName));

            WaitUntilQueueIsNotEmpty();
            AddStartNextSyncEvent();
            repo.Run();
            WaitForRemoteChanges(sleepDuration: 15000);
            AddStartNextSyncEvent();
            repo.Run();

            AssertThatFolderStructureIsEqual();
            AssertThatEventCounterIsZero();
        }

        [Test]
        public void RenameRemoteIgnoredFolderToNotIgnoredFolder(
            [Values(true, false)]bool contentChanges,
            [Values(true, false)]bool withFileInside)
        {
            this.ContentChangesActive = contentChanges;
            var ignoredRemoteFolder = this.remoteRootDir.CreateFolder(ignoredName);
            if (withFileInside) {
                ignoredRemoteFolder.CreateDocument(fileName, string.Empty);
            }

            this.InitializeAndRunRepo();
            Assert.That(this.localRootDir.GetFileSystemInfos(), Is.Empty);

            ignoredRemoteFolder.Rename(normalName);

            this.WaitForRemoteChanges();
            this.AddStartNextSyncEvent();
            this.repo.Run();
            WaitForRemoteChanges(sleepDuration: 15000);
            AddStartNextSyncEvent();
            repo.Run();

            AssertThatFolderStructureIsEqual();
            AssertThatEventCounterIsZero();
        }
    }
}