using UnityEngine;
using UnityEngine.UI;

namespace RicKit.RFramework.UIComponents
{
    [RequireComponent(typeof(Button))]
    public abstract class BindablePropertyButton<T> : MonoBehaviour
    {
        protected BindableProperty<T> bp;
        protected T id; 
        protected virtual void Awake()
        {
            var btn = GetComponentInChildren<Button>();
            btn.onClick.AddListener(() =>
            {
                if (!CanSelected()) return;
                bp.Value = id;
            });
        }

        protected virtual void OnDestroy()
        {
            bp?.UnRegister(OnValueChange);
        }

        public virtual void Init(T id, BindableProperty<T> bp)
        {
            this.bp?.UnRegister(OnValueChange);
            this.id = id;
            this.bp = bp;
            bp.RegisterAndInvoke(OnValueChange);
        }

        private void OnValueChange(T id)
        {
            UpdateUI(id.Equals(this.id));
        }
        
        protected abstract void UpdateUI(bool selected);
        protected virtual bool CanSelected() => true;
    }
}