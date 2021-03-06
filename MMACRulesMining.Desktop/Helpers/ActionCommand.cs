﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace MMACRulesMining.Helpers
{
	public class ActionCommand : ICommand
	{

		private Action<object> _execute;
		private Func<object, bool> _canExecute;

		public ActionCommand(Action<object> execute, Func<object, bool> canExecute)
		{
			_execute = execute;
			_canExecute = canExecute;
		}

		//public event EventHandler CanExecuteChanged
		//{
		//	add { CommandManager.RequerySuggested += value; }
		//	remove { CommandManager.RequerySuggested -= value; }
		//}

		public void Execute(object parameter)
		{
			if (CanExecute(parameter))
				_execute?.Invoke(parameter);
		}

		public bool CanExecute(object parameter)
		{
			return _canExecute?.Invoke(parameter) ?? true;
		}


		public event EventHandler CanExecuteChanged;

		//-- Now expose this method so your mvvm can call it and it rechecks 
		//-- it's own CanExecute reference
		public void RaiseCanExecuteChanged()
		{
			if (Application.Current.Dispatcher.CheckAccess()) // am I on the UI Thread now?
				CommandManager.InvalidateRequerySuggested();
			else
				Application.Current.Dispatcher.Invoke(() => CommandManager.InvalidateRequerySuggested());
		}
	}
}
