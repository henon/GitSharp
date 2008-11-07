using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Gitty.Lib
{
    [Complete]
    public class AbstractIndexTreeVisitor : IndexTreeVisitor
    {

        public delegate void FinishVisitTreeDelegate(Tree tree, Tree auxTree, String curDir);
        public FinishVisitTreeDelegate FinishVisitTree { get; set; }

        public delegate void FinishVisitTreeByIndexDelegate(Tree tree, int i, String curDir);
        public FinishVisitTreeByIndexDelegate FinishVisitTreeByIndex { get; set; }
        
        public delegate void VisitEntryDelegate(TreeEntry treeEntry, GitIndex.Entry indexEntry, FileInfo file);
        public VisitEntryDelegate VisitEntry { get; set; }

        public delegate void VisitEntryAuxDelegate(TreeEntry treeEntry, TreeEntry auxEntry, GitIndex.Entry indexEntry, FileInfo file);
        public VisitEntryAuxDelegate VisitEntryAux { get; set; }

        #region IndexTreeVisitor Members

        void IndexTreeVisitor.VisitEntry(TreeEntry treeEntry, GitIndex.Entry indexEntry, FileInfo file)
        {
            VisitEntryDelegate handler = this.VisitEntry;
            if(handler!=null)
                handler(treeEntry, indexEntry, file);            
        }

        void IndexTreeVisitor.VisitEntry(TreeEntry treeEntry, TreeEntry auxEntry, GitIndex.Entry indexEntry, FileInfo file)
        {
            VisitEntryAuxDelegate handler = this.VisitEntryAux;
            if (handler != null)
                handler(treeEntry,auxEntry, indexEntry, file);    
        }

        void IndexTreeVisitor.FinishVisitTree(Tree tree, Tree auxTree, string curDir)
        {
            FinishVisitTreeDelegate handler = this.FinishVisitTree;
            if (handler != null)
                handler(tree, auxTree, curDir);
        }

        void IndexTreeVisitor.FinishVisitTree(Tree tree, int i, string curDir)
        {
            FinishVisitTreeByIndexDelegate handler = this.FinishVisitTreeByIndex;
            if (handler != null)
                handler(tree, i, curDir);
        }

        #endregion
    }
}
