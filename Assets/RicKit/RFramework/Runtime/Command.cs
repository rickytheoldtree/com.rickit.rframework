namespace RicKit.RFramework
{
    public interface ICommand : ICanGetLocator, ICanSetLocator
    {
        void Init();
        void Execute();
    }

    public interface ICommand<out TResult> : ICommand
    {
        new TResult Execute();
    }
    public interface ICommandOnlyArgs<in TArgs> : ICommand
    {
        void Execute(TArgs args);
    }
    public interface ICommand<in TArgs, out TResult> : ICommand
    {
        TResult Execute(TArgs args);
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

        void ICommand.Execute() => Execute();
        
        public abstract TResult Execute();
    }
    
    public abstract class AbstractCommand<TArgs, TResult> : ICommand<TArgs, TResult>
    {
        private IServiceLocator locator;
        IServiceLocator ICanGetLocator.GetLocator() => locator;
        void ICanSetLocator.SetLocator(IServiceLocator locator) => this.locator = locator;

        public virtual void Init()
        {
        }

        void ICommand.Execute() => Execute(default);
        
        public abstract TResult Execute(TArgs args);
    }
    
    public abstract class AbstractCommandOnlyArgs<TArgs> : ICommandOnlyArgs<TArgs>
    {
        private IServiceLocator locator;
        IServiceLocator ICanGetLocator.GetLocator() => locator;
        void ICanSetLocator.SetLocator(IServiceLocator locator) => this.locator = locator;

        public virtual void Init()
        {
        }

        void ICommand.Execute() => Execute(default);
        
        public abstract void Execute(TArgs args);
    }

    public static class CommandExtension
    {
        public static void SendCommand<TCommand>(this ICanGetLocator self) where TCommand : class, ICommand, new()
        {
            self.GetLocator().SendCommand<TCommand>();
        }

        public static TResult SendCommand<TCommand, TResult>(this ICanGetLocator self) where TCommand : class, ICommand<TResult>, new()
        {
            return self.GetLocator().SendCommand<TCommand, TResult>();
        }
        
        public static void SendCommand<TCommand, TArgs, TResult>(this ICanGetLocator self, TArgs args) where TCommand : class, ICommand<TArgs, TResult>, new()
        {
            self.GetLocator().SendCommand<TCommand, TArgs, TResult>(args);
        }
        
        public static void SendCommandOnlyArgs<TCommand, TArgs>(this ICanGetLocator self, TArgs args) where TCommand : class, ICommandOnlyArgs<TArgs>, new()
        {
            self.GetLocator().SendCommandOnlyArgs<TCommand, TArgs>(args);
        }
        

        public static void SendCommand<TCommand>(this IServiceLocator self) where TCommand : class, ICommand, new()
        {
            if (self.Commands.TryGetValue(typeof(TCommand), out var command))
            {
                command.Execute();
                return;
            }
            command = new TCommand();
            self.Commands.Add(typeof(TCommand), command);
            command.SetLocator(self);
            command.Init();
            command.Execute();
        }

        public static TResult SendCommand<TCommand, TResult>(this IServiceLocator self) where TCommand : class, ICommand<TResult>, new()
        {
            if (self.Commands.TryGetValue(typeof(TCommand), out var command))
            {
                return ((ICommand<TResult>)command).Execute();
            }
            command = new TCommand();
            self.Commands.Add(typeof(TCommand), command);
            command.SetLocator(self);
            command.Init();
            return ((ICommand<TResult>)command).Execute();
        }
        
        public static TResult SendCommand<TCommand, TArgs, TResult>(this IServiceLocator self, TArgs args) where TCommand : class, ICommand<TArgs, TResult>, new()
        {
            if (self.Commands.TryGetValue(typeof(TCommand), out var command))
            {
                return ((ICommand<TArgs, TResult>)command).Execute(args);
            }
            command = new TCommand();
            self.Commands.Add(typeof(TCommand), command);
            command.SetLocator(self);
            command.Init();
            return ((ICommand<TArgs, TResult>)command).Execute(args);
        }
        
        public static void SendCommandOnlyArgs<TCommand, TArgs>(this IServiceLocator self, TArgs args) where TCommand : class, ICommandOnlyArgs<TArgs>, new()
        {
            if (self.Commands.TryGetValue(typeof(TCommand), out var command))
            {
                ((ICommandOnlyArgs<TArgs>)command).Execute(args);
                return;
            }
            command = new TCommand();
            self.Commands.Add(typeof(TCommand), command);
            command.SetLocator(self);
            command.Init();
            ((ICommandOnlyArgs<TArgs>)command).Execute(args);
        }
    }
}