using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using Pronunciation.Trainer.Utility;

namespace Pronunciation.Trainer.Controls
{
    public class UserControlExt : UserControl
    {
        private bool _isFirstBuild = true;

        public UserControlExt()
        {
            this.Loaded += UserControlExt_Loaded;
        }

        private void UserControlExt_Loaded(object sender, RoutedEventArgs e)
        {
            if (ControlsHelper.IsInVisualTree(this))
            {
                OnVisualTreeBuilt(_isFirstBuild);
                _isFirstBuild = false;
            }
        }

        // The control is loaded and visual controls tree has been built (we can navigate on it)
        protected virtual void OnVisualTreeBuilt(bool isFirstBuild)
        {
        }
    }
}
