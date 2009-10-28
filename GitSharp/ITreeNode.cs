using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitSharp
{
    public interface ITreeNode
    {
        string Name { get; }
        string Path { get; }
        Tree Parent { get; }
        int Permissions { get; }

    }
}
