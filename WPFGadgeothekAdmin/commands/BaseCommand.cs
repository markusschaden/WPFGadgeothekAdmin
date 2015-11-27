using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WPFGadgeothekAdmin.commands
{
    public abstract class BaseCommand : ICommand
    {
        public abstract bool CanExecute(object parameter);
        public abstract void Execute(object parameter);
        public abstract event EventHandler CanExecuteChanged;
    }
}
