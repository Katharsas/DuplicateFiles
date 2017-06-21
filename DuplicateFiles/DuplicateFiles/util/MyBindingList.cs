using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DuplicateFiles
{
    /// <summary>
    /// Has an event for inspection of a removed item before it is lost.
    /// </summary>
    public class MyBindingList<T> : System.ComponentModel.BindingList<T>
    {
        public MyBindingList() : base() {}

        public MyBindingList(IList<T> list) : base(list) {}

        protected override void RemoveItem(int itemIndex)
        {
            //itemIndex = index of item which is going to be removed
            //get item from binding list at itemIndex position
            T deletedItem = this.Items[itemIndex];

            if (BeforeRemove != null)
            {
                //raise event containing item which is going to be removed
                BeforeRemove(deletedItem);
            }

            //remove item from list
            base.RemoveItem(itemIndex);
        }

        public delegate void OnRemove(T deletedItem);
        public event OnRemove BeforeRemove;
    }
}
