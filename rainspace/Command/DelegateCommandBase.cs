// Developed by doiTTeam => devdoiTTeam@gmail.com
using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;

namespace DraggAnimatedPanelExample
{
    public abstract class DelegateCommandBase : ICommand
    {
        private readonly Func<object, bool> canExecuteMethod;
        private readonly Action<object> executeMethod;

        protected DelegateCommandBase(Action<object> executeMethod, Func<object, bool> canExecuteMethod)
        {
            if (executeMethod == null || canExecuteMethod == null)
                throw new ArgumentNullException("executeMethod");

            this.executeMethod = executeMethod;
            this.canExecuteMethod = canExecuteMethod;
        }

        #region ICommand Members

        void ICommand.Execute(object? parameter)
        {
            Execute(parameter!);
        }

        bool ICommand.CanExecute(object? parameter)
        {
            return CanExecute(parameter!);
        }

        public event EventHandler? CanExecuteChanged;

        #endregion

        protected virtual void OnCanExecuteChanged()
        {
            var handlers = CanExecuteChanged;
            if (handlers != null)
            {
                handlers(this, EventArgs.Empty);
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1030:UseEventsWhereAppropriate")]
        public void RaiseCanExecuteChanged()
        {
            OnCanExecuteChanged();
        }

        protected void Execute(object parameter)
        {
            executeMethod(parameter);
        }

        protected bool CanExecute(object parameter)
        {
            return canExecuteMethod == null || canExecuteMethod(parameter);
        }
    }
}
