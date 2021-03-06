//-----------------------------------------------------------------------
// <copyright file="IDirectoryInfoDecoratorTest.cs" company="GRAU DATA AG">
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
namespace TestLibrary.StorageTests.FileSystemTests.ReadOnlyIgnoringDecorator {
    using System;
    using System.IO;

    using CmisSync.Lib.Storage.FileSystem;

    using Moq;

    using NUnit.Framework;

    [TestFixture]
    public class IDirectoryInfoDecoratorTest {
        [Test, Category("Fast")]
        public void CreateDirectory(
            [Values(true, false)]bool parentIsReadOnly,
            [Values(true, false)]bool directoryExistsAlready)
        {
            var readOnlyParent = new Mock<IDirectoryInfo>(MockBehavior.Strict);
            readOnlyParent.SetupProperty(d => d.ReadOnly, parentIsReadOnly);
            var hiddenDirInfo = new Mock<IDirectoryInfo>(MockBehavior.Strict);
            hiddenDirInfo.Setup(d => d.Parent).Returns(readOnlyParent.Object);
            if (directoryExistsAlready) {
                hiddenDirInfo.Setup(d => d.Create()).Throws<IOException>();
            } else {
                hiddenDirInfo.Setup(d => d.Create());
            }
            var underTest = new ReadOnlyIgnoringDirectoryInfoDecorator(hiddenDirInfo.Object);

            if (directoryExistsAlready) {
                Assert.Throws<IOException>(() => underTest.Create());
            } else {
                underTest.Create();
            }


            readOnlyParent.VerifyGet(d => d.ReadOnly, Times.Once());
            readOnlyParent.VerifySet(d => d.ReadOnly = false, parentIsReadOnly ? Times.Once() : Times.Never());
            readOnlyParent.VerifySet(d => d.ReadOnly = true, parentIsReadOnly ? Times.Once() : Times.Never());
            hiddenDirInfo.Verify(d => d.Create(), Times.Once());
            Assert.That(readOnlyParent.Object.ReadOnly, Is.EqualTo(parentIsReadOnly));
        }

        [Test, Category("Fast")]
        public void RemoveDirectory(
            [Values(true, false)]bool parentIsReadOnly,
            [Values(true, false)]bool doesNotExists,
            [Values(true, false)]bool folderItselfIsReadOnly,
            [Values(true, false)]bool recursive)
        {
            var readOnlyParent = new Mock<IDirectoryInfo>(MockBehavior.Strict);
            readOnlyParent.SetupProperty(d => d.ReadOnly, parentIsReadOnly);
            var hiddenDirInfo = new Mock<IDirectoryInfo>(MockBehavior.Strict);
            hiddenDirInfo.Setup(d => d.Parent).Returns(readOnlyParent.Object);
            hiddenDirInfo.SetupProperty(d => d.ReadOnly, folderItselfIsReadOnly);
            if (doesNotExists) {
                hiddenDirInfo.Setup(d => d.TryToSetReadWritePermissionRecursively());
                hiddenDirInfo.Setup(d => d.Delete(recursive)).Throws<DirectoryNotFoundException>();
            } else {
                if (recursive) {
                    hiddenDirInfo.Setup(d => d.TryToSetReadWritePermissionRecursively());
                }

                hiddenDirInfo.Setup(d => d.Delete(recursive)).Callback<bool>(r => {
                    hiddenDirInfo.Verify(dir => dir.TryToSetReadWritePermissionRecursively(), r ? Times.Once() : Times.Never());
                });
            }

            var underTest = new ReadOnlyIgnoringDirectoryInfoDecorator(hiddenDirInfo.Object);

            if (doesNotExists) {
                Assert.Throws<DirectoryNotFoundException>(() => underTest.Delete(recursive));
            } else {
                underTest.Delete(recursive);
            }

            hiddenDirInfo.VerifyGet(d => d.ReadOnly, Times.Once());
            hiddenDirInfo.VerifySet(d => d.ReadOnly = false, folderItselfIsReadOnly ? Times.Once() : Times.Never());
            readOnlyParent.VerifyGet(d => d.ReadOnly, Times.Once());
            readOnlyParent.VerifySet(d => d.ReadOnly = false, parentIsReadOnly ? Times.Once() : Times.Never());
            readOnlyParent.VerifySet(d => d.ReadOnly = true, parentIsReadOnly ? Times.Once() : Times.Never());
            hiddenDirInfo.Verify(d => d.Delete(recursive), Times.Once());
            Assert.That(readOnlyParent.Object.ReadOnly, Is.EqualTo(parentIsReadOnly));
        }

        [Test, Category("Medium")]
        public void MoveDirectory(
            [Values(true, false)]bool sourceParentIsReadOnly,
            [Values(true, false)]bool targetParentIsReadOnly,
            [Values(true, false)]bool isItselfReadOnly)
        {
            string oldName = "oldDir";
            string newName = "newDir";
            var sourceParent = new DirectoryInfoWrapper(new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())));
            var targetParent = new DirectoryInfoWrapper(new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())));
            var decoratedDirInfo = new DirectoryInfoWrapper(new DirectoryInfo(Path.Combine(sourceParent.FullName, oldName)));

            try {
                sourceParent.Create();
                targetParent.Create();
                sourceParent.ReadOnly = sourceParentIsReadOnly;
                targetParent.ReadOnly = targetParentIsReadOnly;

                var underTest = new ReadOnlyIgnoringDirectoryInfoDecorator(decoratedDirInfo);
                underTest.Create();
                underTest.ReadOnly = isItselfReadOnly;
                underTest.MoveTo(Path.Combine(targetParent.FullName, newName));

                sourceParent.Refresh();
                targetParent.Refresh();
                Assert.That(underTest.ReadOnly, Is.EqualTo(isItselfReadOnly));
                Assert.That(sourceParent.ReadOnly, Is.EqualTo(sourceParentIsReadOnly));
                Assert.That(targetParent.ReadOnly, Is.EqualTo(targetParentIsReadOnly));
            } finally {
                decoratedDirInfo.Refresh();
                sourceParent.Refresh();
                targetParent.Refresh();
                if (decoratedDirInfo.Exists) {
                    decoratedDirInfo.ReadOnly = false;
                }

                if (sourceParent.Exists) {
                    sourceParent.ReadOnly = false;
                    sourceParent.Delete(true);
                }

                if (targetParent.Exists) {
                    targetParent.ReadOnly = false;
                    targetParent.Delete(true);
                }
            }
        }

        [Test, Category("Fast")]
        public void ToStringReturnsWrappedToString() {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var hiddenDirInfo = new DirectoryInfoWrapper(new DirectoryInfo(path));
            var underTest = new ReadOnlyIgnoringDirectoryInfoDecorator(hiddenDirInfo);
            Assert.That(underTest.ToString(), Is.EqualTo(path));
        }
    }
}