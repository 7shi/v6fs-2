using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Silverlight
{
    public class Command : ICommand
    {
        private Action exec;
        private bool canExec;

        public Command(Action exec)
        {
            this.exec = exec;
        }

        public bool CanExecute(object parameter)
        {
            if (parameter is bool)
            {
                var b = (bool)parameter;
                if (canExec != b)
                {
                    canExec = b;
                    CanExecuteChanged(this, EventArgs.Empty);
                }
            }
            return canExec;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            exec();
        }
    }
}
