using UnityEngine;
using System;

namespace RicKit.RFramework.UIComponents.EnhancedScroller
{
    public class EnhancedScrollerCellView : MonoBehaviour
    {
        public string cellIdentifier;
        
        [NonSerialized]
        public int cellIndex;
        
        [NonSerialized]
        public int dataIndex;
        
        [NonSerialized]
        public bool active;

        public virtual void RefreshCellView() { }
    }
}