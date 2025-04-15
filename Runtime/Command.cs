namespace RicKit.RFramework
{
    public interface ICommand : ICanGetLocator, ICanSetLocator
    {
        void Init();
        void Execute();
    }

    public interface ICommand<out TResult> : ICanGetLocator, ICanSetLocator
    {
        void Init();
        TResult Execute();
    }

    public abstract class AbstractCommand : ICommand
    {
        private IServiceLocator locator;
        IServiceLocator ICanGetLocator.GetLocator() => locator;
        void ICanSetLocator.SetLocator(IServiceLocator locator) => this.locator = locator;

        public virtual void Init()
        {
        }

        public abstract void Execute();
    }

    public abstract class AbstractCommand<TResult> : ICommand<TResult>
    {
        private IServiceLocator locator;
        IServiceLocator ICanGetLocator.GetLocator() => locator;
        void ICanSetLocator.SetLocator(IServiceLocator locator) => this.locator = locator;

        public virtual void Init()
        {
        }

        public abstract TResult Execute();
    }

    public static class CommandExtension
    {
        public static void SendCommand(this ICanGetLocator self, ICommand command)
        {
            self.GetLocator().SendCommand(command);
        }

        public static TResult SendCommand<TResult>(this ICanGetLocator self, ICommand<TResult> command)
        {
            return self.GetLocator().SendCommand(command);
        }

        public static void SendCommand(this IServiceLocator self, ICommand command)
        {
            command.SetLocator(self);
            command.Init();
            command.Execute();
        }

        public static TResult SendCommand<TResult>(this IServiceLocator self, ICommand<TResult> command)
        {
            command.SetLocator(self);
            command.Init();
            return command.Execute();
        }
    }
}