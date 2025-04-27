using UnityEngine;
using UnityEngine.UI;

namespace RicKit.RFramework.UIComponents
{
    [RequireComponent(typeof(Button))]
    public abstract class BindablePropertyButton<T> : MonoBehaviour
    {
        protected BindableProperty<T> bp;
        protected T id;
        protected bool lastSelected;

        protected virtual void Awake()
        {
            var btn = GetComponentInChildren<Button>();
            btn.onClick.AddListener(() =>
            {
                if (!CanSelected())
                {
                    ClickWhileLocked();
                    return;
                }
                bp.Value = id;
            });
        }

        protected virtual void OnDestroy()
        {
            bp?.UnRegister(OnValueChange);
        }

        public void Init(T id, BindableProperty<T> bp)
        {
            this.bp?.UnRegister(OnValueChange);
            this.id = id;
            this.bp = bp;
            bp.Register(OnValueChange);
            var selected = id.Equals(bp.Value);
            InitUI(selected);
            lastSelected = selected;
        }

        private void OnValueChange(T id)
        {
            var selected = id.Equals(this.id);
            UpdateUI(selected, lastSelected);
            lastSelected = selected;
        }
        protected abstract void InitUI(bool selected);
        protected abstract void UpdateUI(bool selected, bool lastSelected);
        protected virtual bool CanSelected() => true;
        protected virtual void ClickWhileLocked()
        {
        }
    }
}