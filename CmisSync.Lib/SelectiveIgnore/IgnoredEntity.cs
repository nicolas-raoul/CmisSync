//-----------------------------------------------------------------------
// <copyright file="IgnoredEntity.cs" company="GRAU DATA AG">
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

namespace CmisSync.Lib.SelectiveIgnore {
    using System;

    using CmisSync.Lib.PathMatcher;

    using DotCMIS.Client;

    /// <summary>
    /// Ignored entity holds the ignored remote object id and the local path.
    /// </summary>
    public class IgnoredEntity : AbstractIgnoredEntity {
        /// <summary>
        /// Initializes a new instance of the <see cref="CmisSync.Lib.SelectiveIgnore.IgnoredEntity"/> class.
        /// </summary>
        /// <param name="folder">Ignored folder.</param>
        /// <param name="matcher">Path matcher to create local path.</param>
        public IgnoredEntity(IFolder folder, IPathMatcher matcher) {
            if (folder == null) {
                throw new ArgumentNullException("folder");
            }

            if (matcher == null) {
                throw new ArgumentNullException("matcher");
            }

            if (!matcher.CanCreateLocalPath(folder)) {
                throw new ArgumentException("Cannot create a local path for the given remote folder");
            }

            this.ObjectId = folder.Id;
            this.LocalPath = matcher.CreateLocalPath(folder);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CmisSync.Lib.SelectiveIgnore.IgnoredEntity"/> class.
        /// </summary>
        /// <param name="doc">Ignored document.</param>
        /// <param name="matcher">Path matcher to create local path.</param>
        public IgnoredEntity(IDocument doc, IPathMatcher matcher) {
            if (doc == null) {
                throw new ArgumentNullException("doc");
            }

            if (matcher == null) {
                throw new ArgumentNullException("matcher");
            }

            if (!matcher.CanCreateLocalPath(doc)) {
                throw new ArgumentException("Cannot create a local path for the given remote folder");
            }

            this.ObjectId = doc.Id;
            this.LocalPath = matcher.CreateLocalPath(doc);
        }
    }
}