//-----------------------------------------------------------------------
// <copyright file="GraphOutputConvenienceExtension.cs" company="GRAU DATA AG">
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

namespace CmisSync.Lib.Storage.Database {
    using System;
    using System.Collections.Generic;
    using System.IO;

    using CmisSync.Lib.Storage.FileSystem;

    public static class GraphOutputConvenienceExtension {
        public static void ToDotFile<T>(this IObjectTree<T> tree, string path) {
            tree.ToDotFile(new FileInfoWrapper(new FileInfo(path)));
        }

        public static void ToDotFile<T>(this IObjectTree<T> tree, IFileInfo file) {
            using (var stream = file.Open(FileMode.CreateNew)) {
                tree.ToDotStream(stream);
            }
        }

        public static void ToDotStream<T>(this IObjectTree<T> tree, Stream outputsteam) {
            using (var writer = new StreamWriter(outputsteam)) {
                writer.WriteLine("digraph tree {");
                tree.ToDotString(writer);
                writer.WriteLine("}");
            }
        }

        private static void ToDotString<T>(this IObjectTree<T> tree, StreamWriter writer) {
            DotTreeWriterFactory.CreateWriter<T>().ToDotString(tree, writer);
        }

        private class DotTreeWriterFactory {
            public static IDotTreeWriter<T> CreateWriter<T>() {
                if (typeof(T) == typeof(IFileSystemInfo)) {
                    return new LocalTreeDotWriter<T>();
                }

                return new StringTreeDotWriter<T>();
            }
        }

        private class StringTreeDotWriter<T> : IDotTreeWriter<T> {
            public void ToDotString(IObjectTree<T> tree, StreamWriter writer) {
                foreach (var child in tree.Children ?? new List<IObjectTree<T>>()) {
                    writer.Write("\t");
                    writer.Write(tree.Item.ToString());
                    writer.Write(" -> ");
                    writer.Write(child.Item.ToString());
                    writer.WriteLine(";");
                    child.ToDotString(writer);
                }
            }
        }

        private class LocalTreeDotWriter<T> : IDotTreeWriter<T> {
            public void ToDotString(IObjectTree<T> tree, StreamWriter writer) {
                IObjectTree<IFileSystemInfo> t = tree as IObjectTree<IFileSystemInfo>;
                foreach (var child in t.Children ?? new List<IObjectTree<IFileSystemInfo>>()) {
                    writer.Write("\t");
                    writer.Write(t.Item.Name);
                    writer.Write(" -> ");
                    writer.Write(child.Item.Name);
                    writer.WriteLine(";");
                    child.ToDotString(writer);
                }
            }
        }
    }
}