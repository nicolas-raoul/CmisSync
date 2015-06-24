//-----------------------------------------------------------------------
// <copyright file="AbstractTarjanNode.cs" company="GRAU DATA AG">
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

namespace CmisSync.Lib.Algorithms {
    using System;
    using System.Collections.Generic;

    public abstract class AbstractTarjanNode {
        public AbstractTarjanNode(params AbstractTarjanNode[] neighbors) {
            this.Neighbors = new List<AbstractTarjanNode>(neighbors);
        }

        public long LowLink { get; set; }
        public long dfs { get; set; }
        public bool OnStack { get; set; }
        public List<AbstractTarjanNode> Neighbors { get; private set; }
    }
}