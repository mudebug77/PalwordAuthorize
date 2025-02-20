using System.Windows.Controls;

namespace ConsoleApp.Pages
{
    public static class TreeViewHelper
    {
        private static void ClearTreeViewItemsControlSelection(ItemCollection ic, ItemContainerGenerator icg)
        {
            if ((ic != null) && (icg != null))
                for (int i = 0; i < ic.Count; i++)
                {
                    TreeViewItem tvi = icg.ContainerFromIndex(i) as TreeViewItem;
                    if (tvi != null)
                    {
                        ClearTreeViewItemsControlSelection(tvi.Items, tvi.ItemContainerGenerator);
                        tvi.IsSelected = false;
                    }
                }
        }

        private static TreeViewItem TreeDataToTreeView(ItemCollection ic, ItemContainerGenerator icg, object data)
        {
            if ((ic != null) && (icg != null))
                for (int i = 0; i < ic.Count; i++)
                {
                    TreeViewItem tvi = icg.ContainerFromIndex(i) as TreeViewItem;
                    if (tvi != null)
                    {
                        if (tvi.DataContext == data)
                        {
                            return tvi;
                        }
                        var select = TreeDataToTreeView(tvi.Items, tvi.ItemContainerGenerator, data);
                        if (select != null) return select;
                    }
                }
            return null;
        }

        public static void ClearTreeViewItemsSelect(this TreeView self)
        {
            ClearTreeViewItemsControlSelection(self.Items, self.ItemContainerGenerator);
        }

        //獲得父節點
        public static object GetGetParentTree(this TreeView self, object data)
        {
            var currUI = TreeDataToTreeView(self.Items, self.ItemContainerGenerator, data);
            if (currUI == null) return null;
            var ParentUI = ItemsControl.ItemsControlFromItemContainer(currUI);
            if (ParentUI == null) return null;
            return ParentUI.DataContext;
        }

    }
}
