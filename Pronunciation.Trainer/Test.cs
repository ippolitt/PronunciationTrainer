using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Diagnostics;

namespace Pronunciation.Trainer
{
    class Test
    {
        RelayCommand _saveCommand; 
        
        public ICommand SaveCommand 
        { 
            get 
            { 
                if (_saveCommand == null) 
                { 
                    //_saveCommand = new RelayCommand(param => this.Save(), param => this.CanSave); 
                } 
                return _saveCommand; 
            } 
        }

        // In App.xaml.cs 
        //protected override void OnStartup(StartupEventArgs e) 
        //{ 
        //    base.OnStartup(e); 

        //    MainWindow window = new MainWindow(); 
            
        //    // Create the ViewModel to which 
        //    // the main window binds. 
        //    string path = "Data/customers.xml"; 
        //    var viewModel = new MainWindowViewModel(path); 

        //    // When the ViewModel asks to be closed, 
        //    // close the window. 
        //    viewModel.RequestClose += delegate 
        //        { window.Close(); }; 
            
        //    // Allow all controls in the window to 
        //    // bind to the ViewModel by setting the 
        //    // DataContext, which propagates down 
        //    // the element tree. 
        //    window.DataContext = viewModel; 
        //    window.Show(); 
        //}
    }

    public class RelayCommand : ICommand 
    { 
        #region Fields readonly 
        Action<object> _execute; 
        readonly Predicate<object> _canExecute; 
        #endregion 

        // Fields 
        #region Constructors 
        public RelayCommand(Action<object> execute) : this(execute, null) { } 
        
        public RelayCommand(Action<object> execute, Predicate<object> canExecute) 
        { 
            if (execute == null)
            throw new ArgumentNullException("execute"); 
            
            _execute = execute; 
            _canExecute = canExecute; 
        } 
        #endregion 
        // Constructors 
        
        #region ICommand Members 
        [DebuggerStepThrough] 
        public bool CanExecute(object parameter) 
        { 
            return _canExecute == null ? true : _canExecute(parameter); 
        } 
        
        public event EventHandler CanExecuteChanged 
        { 
            add { CommandManager.RequerySuggested += value; } 
            remove { CommandManager.RequerySuggested -= value; } 
        } 
        
        public void Execute(object parameter) 
        { 
            _execute(parameter); 
        } 
        #endregion 
        // ICommand Members 
    }



    //<!-- In MainWindowResources.xaml --> 
   // <!-- This template explains how to render the list of commands on the left side in the main window (the 'Control Panel' area). --> 
    //<DataTemplate x:Key="CommandsTemplate"> 
    //    <ItemsControl ItemsSource="{Binding Path=Commands}"> 
    //        <ItemsControl.ItemTemplate> 
    //            <DataTemplate> 
    //                <TextBlock Margin="2,6"> 
    //                    <Hyperlink Command="{Binding Path=Command}"> 
    //                        <TextBlock Text="{Binding Path=DisplayName}" /> 
    //                    </Hyperlink> 
    //                </TextBlock> 
    //            </DataTemplate> 
    //        </ItemsControl.ItemTemplate> 
    //    </ItemsControl> 
    //</DataTemplate>

    //<DataTemplate x:Key="ClosableTabItemTemplate"> 
    //    <DockPanel Width="120"> 
    //        <Button Command="{Binding Path=CloseCommand}" Content="X" DockPanel.Dock="Right" Width="16" Height="16" /> 
    //        <ContentPresenter Content="{Binding Path=DisplayName}" /> 
    //    </DockPanel> 
    //</DataTemplate>

    //<!-- This resource dictionary is used by the MainWindow. --> 
    //<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" 
    //    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" 
    //    xmlns:vm="clr-namespace:DemoApp.ViewModel" 
    //    xmlns:vw="clr-namespace:DemoApp.View" > 
    //    <!-- This template applies an AllCustomersView to an instance of the AllCustomersViewModel class shown in the main window. --> 
    //    <DataTemplate DataType="{x:Type vm:AllCustomersViewModel}"> 
    //        <vw:AllCustomersView /> 
    //    </DataTemplate> 
    //    <!-- This template applies a CustomerView to an instance of the CustomerViewModel class shown in the main window. --> 
    //    <DataTemplate DataType="{x:Type vm:CustomerViewModel}"> 
    //        <vw:CustomerView /> 
    //    </DataTemplate> 
    //    <!-- Other resources omitted for clarity... --> 
    // </ResourceDictionary>

    //<!-- In MainWindow.xaml --> 
    //<Menu> 
    //    <MenuItem Header="_File"> 
    //        <MenuItem Header="_Exit" Command="{Binding Path=CloseCommand}" /> 
    //    </MenuItem> 
    //    <MenuItem Header="_Edit" /> 
    //    <MenuItem Header="_Options" /> 
    //    <MenuItem Header="_Help" /> 
    //</Menu>
}
