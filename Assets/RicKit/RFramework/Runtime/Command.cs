namespace RicKit.RFramework
{
    public interface ICommand : ICanGetLocator, ICanSetLocator
    {
        void Init();
        void Execute(params object[] args);
    }

    public interface ICommand<out TResult> : ICommand
    {
        new TResult Execute(params object[] args);
    }

    public abstract class AbstractCommand : ICommand
    {
        private IServiceLocator locator;
        IServiceLocator ICanGetLocator.GetLocator() => locator;
        void ICanSetLocator.SetLocator(IServiceLocator locator) => this.locator = locator;

        public abstract void Init();

        public abstract void Execute(params object[] args);
    }

    public abstract class AbstractCommand<TResult> : ICommand<TResult>
    {
        private IServiceLocator locator;
        IServiceLocator ICanGetLocator.GetLocator() => locator;
        void ICanSetLocator.SetLocator(IServiceLocator locator) => this.locator = locator;

        public abstract void Init();

        void ICommand.Execute(params object[] args) => Execute(args);
        
        public abstract TResult Execute(params object[] args);
    }

    public static class CommandExtension
    {
        public static void SendCommand<TCommand>(this ICanGetLocator self, params object[] args) where TCommand : class, ICommand, new()
        {
            self.GetLocator().SendCommand<TCommand>(args);
        }

        public static TResult SendCommand<TCommand, TResult>(this ICanGetLocator self, params object[] args) where TCommand : class, ICommand<TResult>, new()
        {
            return self.GetLocator().SendCommand<TCommand, TResult>(args);
        }

        public static void SendCommand<TCommand>(this IServiceLocator self, params object[] args) where TCommand : class, ICommand, new()
        {
            if (self.Commands.TryGetValue(typeof(TCommand), out var command))
            {
                command.Execute(args);
                return;
            }
            command = new TCommand();
            self.Commands.Add(typeof(TCommand), command);
            command.SetLocator(self);
            command.Init();
            command.Execute(args);
        }

        public static TResult SendCommand<TCommand, TResult>(this IServiceLocator self, params object[] args) where TCommand : class, ICommand<TResult>, new()
        {
            if (self.Commands.TryGetValue(typeof(TCommand), out var command))
            {
                return ((ICommand<TResult>)command).Execute(args);
            }
            command = new TCommand();
            self.Commands.Add(typeof(TCommand), command);
            command.SetLocator(self);
            command.Init();
            return ((ICommand<TResult>)command).Execute(args);
        }
    }
}